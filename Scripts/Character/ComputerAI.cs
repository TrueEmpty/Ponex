using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Shared computer-player brain: continuous tracking, persistent per-character learning,
/// difficulty bands (Training / Easy–Impossible), and multiple style variants per level.
/// Learning always updates the active variant Profile; difficulty shapes runtime use.
/// </summary>
public static class ComputerAI
{
    const string SaveFile = "computer_ai_profiles.json";
    public const float MinSkill = 0.15f;
    public const float MaxSkill = 0.95f;
    public const int VariantsPerLevel = 3;
    const float MaxAggression = 0.85f;

    /// <summary>
    /// Match difficulty. Training uses the learned profile directly (best for practice).
    /// Easy–Impossible clamp/scale that learned skill into a band.
    /// </summary>
    public enum CpuDifficulty
    {
        Training = 0,
        Easy = 1,
        Normal = 2,
        Hard = 3,
        Expert = 4,
        Impossible = 5,
    }

    [Serializable]
    public class Profile
    {
        public string key = "global";
        public string characterName = "";
        public int difficulty = 0;
        public int variant = 0;
        public float skill = 0.35f;
        public float aggression = 0.25f;
        public float interceptLead = 0.55f;
        public float deadZone = 0.45f;
        public float centerPull = 0.2f;
        /// <summary>Per-variant style offsets applied at runtime (seeded once).</summary>
        public float aggressionBias = 0f;
        public float leadBias = 0f;
        public float noiseBias = 0f;
        public float centerBias = 0f;
        public int matchesPlayed;
        public int wins;
        public int losses;
        public int paddleHits;
        public int goalsAgainst;
    }

    /// <summary>Runtime knobs after difficulty is applied. Characters should read this, not raw Profile.skill.</summary>
    public struct RuntimeBrain
    {
        public float skill;
        public float aggression;
        public float interceptLead;
        public float deadZone;
        public float centerPull;
        public float noiseMult;
        public CpuDifficulty difficulty;
        public int variant;
    }

    struct DifficultyTuning
    {
        public float skillFloor;
        public float skillCap;
        public float noiseMult;
        public float aggressionMult;
        public float leadMult;
    }

    [Serializable]
    class SaveData
    {
        public List<Profile> profiles = new List<Profile>();
    }

    public struct Decision
    {
        public int moveDir;
        public bool wantBump;
        public bool wantSuper;
        public bool wantDash; // direction matches moveDir (-1 left / +1 right)
    }

    /// <summary>Free-roam characters (e.g. Nari) — cardinal world move + dash.</summary>
    public struct FreeRoamDecision
    {
        public Vector3 moveDir;
        public bool wantDash;
        public bool hasTarget;
        public Vector3 target;
        public float distToTarget;
    }

    static readonly Dictionary<string, Profile> profiles = new Dictionary<string, Profile>();
    static readonly Dictionary<int, float> bumpCooldown = new Dictionary<int, float>();
    static readonly Dictionary<int, float> superCooldown = new Dictionary<int, float>();
    static readonly Dictionary<int, float> dashCooldown = new Dictionary<int, float>();
    static readonly Dictionary<int, Decision> lastDecision = new Dictionary<int, Decision>();
    static readonly Dictionary<int, FreeRoamDecision> lastFreeRoam = new Dictionary<int, FreeRoamDecision>();
    static readonly Dictionary<int, FreeRoamPersonality> freeRoamPersonality = new Dictionary<int, FreeRoamPersonality>();
    static readonly Dictionary<int, System.Random> playerRng = new Dictionary<int, System.Random>();
    static bool loaded;

    /// <summary>Per-slot free-roam quirks so multiple Naris don't clone the same path.</summary>
    class FreeRoamPersonality
    {
        public float axisBias;      // prefer horizontal vs vertical
        public float wanderBias;    // how often to explore
        public float leadBias;      // intercept timing personality
        public float commitUntil;   // hold a choice until this Time.time
        public Vector3 committedDir;
        public int committedBallId;
        public float styleSeed;
    }

    static string SavePath => Path.Combine(Application.persistentDataPath, SaveFile);

    public static void EnsureLoaded()
    {
        if (loaded)
            return;
        Load();
        loaded = true;
    }

    public static Profile GetProfile(Player player)
    {
        EnsureLoaded();
        string key = ProfileKey(player);
        if (!profiles.TryGetValue(key, out Profile profile))
        {
            string charName = player != null && !string.IsNullOrEmpty(player.name) ? player.name : "global";
            CpuDifficulty diff = GetDifficulty(player);
            int variant = player != null ? Mathf.Clamp(player.cpuVariant, 0, VariantsPerLevel - 1) : 0;
            profile = CreateSeededProfile(charName, diff, variant, null);
            profiles[key] = profile;
        }
        return profile;
    }

    public static string ProfileKey(string characterName, CpuDifficulty difficulty, int variant)
    {
        if (string.IsNullOrEmpty(characterName))
            characterName = "global";
        variant = Mathf.Clamp(variant, 0, VariantsPerLevel - 1);
        return characterName + "|" + (int)difficulty + "|" + variant;
    }

    static string ProfileKey(Player player)
    {
        if (player == null || string.IsNullOrEmpty(player.name))
            return ProfileKey("global", CpuDifficulty.Training, 0);
        return ProfileKey(player.name, GetDifficulty(player), player.cpuVariant);
    }

    public static CpuDifficulty GetDifficulty(Player player)
    {
        if (player == null)
            return CpuDifficulty.Training;
        return player.cpuDifficulty;
    }

    public static void SetDifficulty(Player player, CpuDifficulty difficulty)
    {
        if (player == null)
            return;
        player.cpuDifficulty = difficulty;
    }

    /// <summary>
    /// Pick a style variant for this match (prefers least-played among VariantsPerLevel).
    /// Call once per computer player at match start.
    /// </summary>
    public static void AssignMatchBrain(Player player)
    {
        if (player == null || !player.computer)
            return;

        EnsureLoaded();
        string charName = string.IsNullOrEmpty(player.name) ? "global" : player.name;
        CpuDifficulty diff = GetDifficulty(player);

        int bestVariant = 0;
        int bestPlayed = int.MaxValue;
        List<int> ties = new List<int>();

        for (int v = 0; v < VariantsPerLevel; v++)
        {
            Profile p = GetOrCreateProfile(charName, diff, v);
            if (p.matchesPlayed < bestPlayed)
            {
                bestPlayed = p.matchesPlayed;
                ties.Clear();
                ties.Add(v);
            }
            else if (p.matchesPlayed == bestPlayed)
            {
                ties.Add(v);
            }
        }

        bestVariant = ties.Count > 0
            ? ties[UnityEngine.Random.Range(0, ties.Count)]
            : UnityEngine.Random.Range(0, VariantsPerLevel);

        player.cpuVariant = bestVariant;
        GetOrCreateProfile(charName, diff, bestVariant);
    }

    /// <summary>Force a specific variant (for UI / tests).</summary>
    public static void AssignMatchBrain(Player player, int variant)
    {
        if (player == null)
            return;
        player.cpuVariant = Mathf.Clamp(variant, 0, VariantsPerLevel - 1);
        if (!string.IsNullOrEmpty(player.name))
            GetOrCreateProfile(player.name, GetDifficulty(player), player.cpuVariant);
    }

    /// <summary>List all variants for a character at a difficulty (creates seeds if missing).</summary>
    public static List<Profile> GetVariants(string characterName, CpuDifficulty difficulty)
    {
        EnsureLoaded();
        List<Profile> list = new List<Profile>(VariantsPerLevel);
        for (int v = 0; v < VariantsPerLevel; v++)
            list.Add(GetOrCreateProfile(characterName, difficulty, v));
        return list;
    }

    static Profile GetOrCreateProfile(string characterName, CpuDifficulty difficulty, int variant)
    {
        string key = ProfileKey(characterName, difficulty, variant);
        if (!profiles.TryGetValue(key, out Profile profile))
        {
            profile = CreateSeededProfile(characterName, difficulty, variant, null);
            profiles[key] = profile;
        }
        return profile;
    }

    static Profile CreateSeededProfile(string characterName, CpuDifficulty difficulty, int variant, Profile migrateFrom)
    {
        variant = Mathf.Clamp(variant, 0, VariantsPerLevel - 1);
        Profile p = new Profile
        {
            key = ProfileKey(characterName, difficulty, variant),
            characterName = characterName ?? "global",
            difficulty = (int)difficulty,
            variant = variant,
        };

        if (migrateFrom != null)
        {
            p.skill = migrateFrom.skill;
            p.aggression = migrateFrom.aggression;
            p.interceptLead = migrateFrom.interceptLead;
            p.deadZone = migrateFrom.deadZone;
            p.centerPull = migrateFrom.centerPull;
            p.matchesPlayed = migrateFrom.matchesPlayed;
            p.wins = migrateFrom.wins;
            p.losses = migrateFrom.losses;
            p.paddleHits = migrateFrom.paddleHits;
            p.goalsAgainst = migrateFrom.goalsAgainst;
        }

        // Distinct playstyles within the same difficulty
        switch (variant)
        {
            case 0: // Balanced / patient
                p.aggressionBias = -0.06f;
                p.leadBias = 0.0f;
                p.noiseBias = 0.05f;
                p.centerBias = 0.04f;
                if (migrateFrom == null)
                {
                    p.aggression = 0.22f;
                    p.interceptLead = 0.50f;
                    p.deadZone = 0.48f;
                    p.centerPull = 0.24f;
                }
                break;
            case 1: // Aggressive rusher
                p.aggressionBias = 0.12f;
                p.leadBias = 0.08f;
                p.noiseBias = -0.05f;
                p.centerBias = -0.06f;
                if (migrateFrom == null)
                {
                    p.aggression = 0.38f;
                    p.interceptLead = 0.62f;
                    p.deadZone = 0.38f;
                    p.centerPull = 0.14f;
                    p.skill = 0.38f;
                }
                break;
            default: // Tricky / unpredictable
                p.aggressionBias = 0.02f;
                p.leadBias = -0.05f;
                p.noiseBias = 0.18f;
                p.centerBias = 0.0f;
                if (migrateFrom == null)
                {
                    p.aggression = 0.28f;
                    p.interceptLead = 0.48f;
                    p.deadZone = 0.42f;
                    p.centerPull = 0.18f;
                    p.skill = 0.32f;
                }
                break;
        }

        p.skill = Mathf.Clamp(p.skill, MinSkill, MaxSkill);
        return p;
    }

    static DifficultyTuning GetTuning(CpuDifficulty difficulty)
    {
        switch (difficulty)
        {
            case CpuDifficulty.Easy:
                return new DifficultyTuning
                {
                    skillFloor = 0.15f,
                    skillCap = 0.40f,
                    noiseMult = 1.65f,
                    aggressionMult = 0.55f,
                    leadMult = 0.70f,
                };
            case CpuDifficulty.Normal:
                return new DifficultyTuning
                {
                    skillFloor = 0.25f,
                    skillCap = 0.65f,
                    noiseMult = 1.0f,
                    aggressionMult = 1.0f,
                    leadMult = 1.0f,
                };
            case CpuDifficulty.Hard:
                return new DifficultyTuning
                {
                    skillFloor = 0.45f,
                    skillCap = 0.85f,
                    noiseMult = 0.60f,
                    aggressionMult = 1.20f,
                    leadMult = 1.10f,
                };
            case CpuDifficulty.Expert:
                return new DifficultyTuning
                {
                    skillFloor = 0.70f,
                    skillCap = MaxSkill,
                    noiseMult = 0.30f,
                    aggressionMult = 1.35f,
                    leadMult = 1.25f,
                };
            case CpuDifficulty.Impossible:
                return new DifficultyTuning
                {
                    skillFloor = 0.88f,
                    skillCap = MaxSkill,
                    noiseMult = 0.12f,
                    aggressionMult = 1.50f,
                    leadMult = 1.40f,
                };
            default: // Training — uncapped learned profile
                return new DifficultyTuning
                {
                    skillFloor = MinSkill,
                    skillCap = MaxSkill,
                    noiseMult = 1.0f,
                    aggressionMult = 1.0f,
                    leadMult = 1.0f,
                };
        }
    }

    /// <summary>
    /// Learned profile shaped by the player's CpuDifficulty + variant for this match.
    /// </summary>
    public static RuntimeBrain GetRuntimeBrain(Player player)
    {
        Profile profile = GetProfile(player);
        CpuDifficulty difficulty = GetDifficulty(player);
        DifficultyTuning tune = GetTuning(difficulty);

        float learned = Mathf.Clamp(profile.skill, MinSkill, MaxSkill);
        float skill = learned;
        if (difficulty != CpuDifficulty.Training)
        {
            float t = Mathf.InverseLerp(MinSkill, MaxSkill, learned);
            skill = Mathf.Lerp(tune.skillFloor, tune.skillCap, t);
        }

        float aggression = Mathf.Clamp(
            (profile.aggression + profile.aggressionBias) * tune.aggressionMult,
            0.05f,
            MaxAggression);
        float interceptLead = Mathf.Clamp(
            (profile.interceptLead + profile.leadBias) * tune.leadMult,
            0.15f,
            1.45f);
        float noiseMult = Mathf.Max(0.05f, tune.noiseMult + profile.noiseBias);
        float centerPull = Mathf.Clamp(profile.centerPull + profile.centerBias, 0.05f, 0.55f);
        float deadZone = Mathf.Clamp(profile.deadZone, 0.1f, 1.2f);

        return new RuntimeBrain
        {
            skill = skill,
            aggression = aggression,
            interceptLead = interceptLead,
            deadZone = deadZone,
            centerPull = centerPull,
            noiseMult = noiseMult,
            difficulty = difficulty,
            variant = profile.variant,
        };
    }

    /// <summary>
    /// Continuous decision for this frame. Movement is reactive to the ball,
    /// not a sticky random thought held for half a second.
    /// </summary>
    public static Decision Evaluate(Transform paddle, Player player, List<string> wallTags, float wallCheckDistance)
    {
        Decision decision = new Decision();
        if (paddle == null || player == null)
            return decision;

        EnsureLoaded();
        RuntimeBrain brain = GetRuntimeBrain(player);
        float skill = brain.skill;

        TickCooldowns(player.index);

        if (!TryGetThreat(paddle, player.facing, out Rigidbody ballRb, out Vector3 ballPos, out Vector3 ballVel))
        {
            decision.moveDir = MoveToward(paddle, GetLaneCenter(paddle, player.facing), brain.deadZone * 1.5f, wallTags, wallCheckDistance);
            lastDecision[player.index] = decision;
            return decision;
        }

        bool ballComingAtUs = IsBallComingToward(player.facing, ballVel, paddle.position, ballPos);
        Vector3 target = ballPos;

        if (ballComingAtUs)
        {
            float lead = Mathf.Lerp(0.15f, brain.interceptLead, skill);
            float distance = Vector3.Distance(paddle.position, ballPos);
            float speed = Mathf.Max(ballVel.magnitude, 0.01f);
            float time = Mathf.Clamp(distance / speed, 0.05f, 1.25f) * lead;
            target = ballPos + ballVel * time;
        }
        else
        {
            Vector3 center = GetLaneCenter(paddle, player.facing);
            target = Vector3.Lerp(ballPos, center, brain.centerPull + (1f - skill) * 0.35f);
        }

        float noiseAmp = Mathf.Lerp(1.1f, 0.05f, skill) * brain.noiseMult;
        Vector2 noise = UnityEngine.Random.insideUnitCircle * noiseAmp;
        target.x += noise.x;
        target.y += noise.y;

        float deadZone = Mathf.Lerp(brain.deadZone * 1.4f, 0.12f, skill);
        decision.moveDir = MoveToward(paddle, target, deadZone, wallTags, wallCheckDistance);

        float lateral = LateralError(paddle, target);
        float approach = ApproachDistance(player.facing, paddle.position, ballPos);

        if (ballComingAtUs && GetCooldown(bumpCooldown, player.index) <= 0f)
        {
            float bumpChance = brain.aggression * Mathf.Lerp(0.15f, 0.85f, skill);
            if (Mathf.Abs(lateral) < Mathf.Lerp(1.6f, 0.7f, skill) && approach < Mathf.Lerp(3.5f, 2.2f, skill))
            {
                if (UnityEngine.Random.value < bumpChance * Time.deltaTime * 8f && player.bump != null && player.bump.Enough())
                {
                    decision.wantBump = true;
                    bumpCooldown[player.index] = Mathf.Lerp(1.4f, 0.55f, skill);
                }
            }
        }

        if (ballComingAtUs && GetCooldown(superCooldown, player.index) <= 0f)
        {
            float superChance = brain.aggression * 0.45f * skill;
            if (approach > 2.5f && Mathf.Abs(lateral) > 1.25f
                && player.super != null
                && player.super.Enough())
            {
                if (UnityEngine.Random.value < superChance * Time.deltaTime * 3f)
                {
                    decision.wantSuper = true;
                    superCooldown[player.index] = Mathf.Lerp(3f, 1.25f, skill);
                }
            }
        }

        if (ballComingAtUs
            && decision.moveDir != 0
            && GetCooldown(dashCooldown, player.index) <= 0f
            && player.dash != null
            && player.dash.amount >= player.dash.cost
            && !WallInDirection(paddle, decision.moveDir, wallTags, wallCheckDistance))
        {
            float dashGap = Mathf.Lerp(1.8f, 1.0f, skill);
            float dashChance = Mathf.Lerp(0.2f, 0.75f, skill) * (0.5f + brain.aggression);
            if (Mathf.Abs(lateral) >= dashGap && approach < Mathf.Lerp(5.5f, 3.5f, skill))
            {
                if (UnityEngine.Random.value < dashChance * Time.deltaTime * 10f)
                {
                    decision.wantDash = true;
                    dashCooldown[player.index] = Mathf.Lerp(1.1f, 0.45f, skill);
                }
            }
        }

        lastDecision[player.index] = decision;
        return decision;
    }

    /// <summary>
    /// Free-roam AI (Nari): chase / intercept balls with per-player personality,
    /// sticky commits, and exploration so multiple Naris diverge and train on varied plays.
    /// </summary>
    public static FreeRoamDecision EvaluateFreeRoam(
        Transform body,
        Player player,
        Vector3 currentMoveDir,
        float arriveDistance,
        float dashMinDistance,
        Func<Vector3, bool> wallBlocked)
    {
        FreeRoamDecision decision = new FreeRoamDecision
        {
            moveDir = currentMoveDir.sqrMagnitude > 0.01f ? currentMoveDir : Vector3.up,
        };

        if (body == null || player == null)
            return decision;

        EnsureLoaded();
        RuntimeBrain brain = GetRuntimeBrain(player);
        float skill = brain.skill;
        FreeRoamPersonality personality = GetFreeRoamPersonality(player);
        TickCooldowns(player.index);

        // Still committed to a prior explorative / chase choice — keep it unless blocked
        if (Time.time < personality.commitUntil
            && personality.committedDir.sqrMagnitude > 0.01f
            && (wallBlocked == null || !wallBlocked(personality.committedDir)))
        {
            decision.moveDir = personality.committedDir;
            decision.hasTarget = personality.committedBallId != 0;
            decision.distToTarget = 0f;

            if (GetCooldown(dashCooldown, player.index) <= 0f
                && player.dash != null
                && player.dash.amount >= player.dash.cost
                && Roll(player) < personality.wanderBias * 0.08f)
            {
                decision.wantDash = true;
                dashCooldown[player.index] = Mathf.Lerp(1.4f, 0.55f, skill);
            }

            lastFreeRoam[player.index] = decision;
            MirrorFreeRoamToDecision(player.index, decision);
            return decision;
        }

        // Low skill explores more; high skill still keeps some chance so clones diverge
        float exploreChance = Mathf.Lerp(0.55f, 0.12f, skill) * (0.65f + personality.wanderBias);

        if (!TryGetFreeRoamBall(player, body.position, skill, brain, personality, out Vector3 target, out float dist, out int ballId))
        {
            // Idle: wander or drift home — not the same for every Nari
            if (Roll(player) < exploreChance)
            {
                decision.moveDir = PickRandomOpenDir(currentMoveDir, wallBlocked, player);
            }
            else
            {
                Vector3 center = body.position;
                center.x = Mathf.Lerp(0f, personality.axisBias * 2.5f, 0.5f);
                center.y = Mathf.Lerp(0f, (personality.styleSeed - 0.5f) * 3f, 0.5f);
                if (Vector3.Distance(body.position, center) > 1.0f)
                    decision.moveDir = PickCardinalToward(body.position, center, currentMoveDir, arriveDistance, wallBlocked, personality, player, exploreChance);
            }

            CommitFreeRoam(personality, decision.moveDir, 0, player, skill);
            lastFreeRoam[player.index] = decision;
            MirrorFreeRoamToDecision(player.index, decision);
            return decision;
        }

        decision.hasTarget = true;
        decision.target = target;
        decision.distToTarget = dist;

        // Chance to explore a suboptimal path instead of pure pursuit (feeds learning variety)
        if (Roll(player) < exploreChance)
        {
            float roll = Roll(player);
            if (roll < 0.35f)
                decision.moveDir = PickRandomOpenDir(currentMoveDir, wallBlocked, player);
            else if (roll < 0.7f)
                decision.moveDir = PickCardinalToward(body.position, target, currentMoveDir, arriveDistance, wallBlocked, personality, player, 1f); // force secondary bias
            else
                decision.moveDir = PickCardinalToward(body.position, target, currentMoveDir, arriveDistance, wallBlocked, personality, player, 0f);
        }
        else
        {
            decision.moveDir = PickCardinalToward(body.position, target, currentMoveDir, arriveDistance, wallBlocked, personality, player, exploreChance * 0.5f);
        }

        CommitFreeRoam(personality, decision.moveDir, ballId, player, skill);

        if (GetCooldown(dashCooldown, player.index) <= 0f
            && dist >= dashMinDistance * Mathf.Lerp(0.75f, 1.15f, personality.styleSeed)
            && player.dash != null
            && player.dash.amount >= player.dash.cost)
        {
            float dashChance = Mathf.Lerp(0.15f, 0.75f, skill) * (0.35f + brain.aggression) * (0.7f + personality.wanderBias);
            if (Roll(player) < dashChance * Time.deltaTime * 10f)
            {
                decision.wantDash = true;
                dashCooldown[player.index] = Mathf.Lerp(1.4f, 0.55f, skill);
            }
        }

        lastFreeRoam[player.index] = decision;
        MirrorFreeRoamToDecision(player.index, decision);
        return decision;
    }

    static void CommitFreeRoam(FreeRoamPersonality personality, Vector3 dir, int ballId, Player player, float skill)
    {
        // Stickier at low skill (more random walks), shorter commits when sharp
        float hold = Mathf.Lerp(0.55f, 0.18f, skill) * (0.7f + Roll(player) * 0.8f);
        personality.commitUntil = Time.time + hold;
        personality.committedDir = dir;
        personality.committedBallId = ballId;
    }

    static FreeRoamPersonality GetFreeRoamPersonality(Player player)
    {
        int id = player.index;
        if (freeRoamPersonality.TryGetValue(id, out FreeRoamPersonality existing))
            return existing;

        // Stable unique quirks per player slot + character + assigned variant
        int seed = id * 9176
            ^ (player.name != null ? player.name.GetHashCode() : 0)
            ^ (player.cpuVariant * 7919)
            ^ ((int)player.cpuDifficulty * 104729)
            ^ 0x5f3759df;
        System.Random rng = new System.Random(seed);
        FreeRoamPersonality p = new FreeRoamPersonality
        {
            axisBias = (float)(rng.NextDouble() * 2.0 - 1.0),
            wanderBias = 0.2f + (float)rng.NextDouble() * 0.55f,
            leadBias = 0.65f + (float)rng.NextDouble() * 0.7f,
            styleSeed = (float)rng.NextDouble(),
            committedDir = Vector3.zero,
            commitUntil = 0f,
            committedBallId = 0,
        };
        freeRoamPersonality[id] = p;
        playerRng[id] = new System.Random(seed ^ Environment.TickCount);
        return p;
    }

    static System.Random GetRng(Player player)
    {
        int id = player.index;
        if (!playerRng.TryGetValue(id, out System.Random rng))
        {
            GetFreeRoamPersonality(player);
            rng = playerRng[id];
        }
        return rng;
    }

    static float Roll(Player player)
    {
        return (float)GetRng(player).NextDouble();
    }

    static void MirrorFreeRoamToDecision(int playerIndex, FreeRoamDecision free)
    {
        Decision d = new Decision { wantDash = free.wantDash };
        if (Mathf.Abs(free.moveDir.x) >= Mathf.Abs(free.moveDir.y))
            d.moveDir = free.moveDir.x >= 0f ? 1 : -1;
        else
            d.moveDir = free.moveDir.y >= 0f ? 1 : -1;
        lastDecision[playerIndex] = d;
    }

    static bool TryGetFreeRoamBall(
        Player player,
        Vector3 from,
        float skill,
        RuntimeBrain brain,
        FreeRoamPersonality personality,
        out Vector3 target,
        out float dist,
        out int ballId)
    {
        target = from;
        dist = 0f;
        ballId = 0;

        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        if (balls == null || balls.Length == 0)
            return false;

        // Rank candidates, then weighted-random pick so clones don't all hunt the same ball
        var candidates = new List<(GameObject go, Vector3 pos, Vector3 vel, float score, int id)>(balls.Length);

        for (int i = 0; i < balls.Length; i++)
        {
            GameObject go = balls[i];
            if (go == null)
                continue;

            Rigidbody ballRb = go.GetComponent<Rigidbody>();
            if (ballRb == null)
                continue;

            Vector3 pos = go.transform.position;
            Vector3 vel = ballRb.linearVelocity;
            float d = Vector3.Distance(from, pos);
            float score = -d + vel.magnitude * 0.35f;

            // Per-player preference shuffle
            score += Mathf.Sin(player.index * 2.17f + i * 1.3f + personality.styleSeed * 6.28f) * 3.5f;
            score += (Roll(player) - 0.5f) * Mathf.Lerp(6f, 1.5f, skill);

            BallInfo info = go.GetComponent<BallInfo>();
            if (info != null && info.futureColisionPoints != null && info.futureColisionPoints.Count > 0)
            {
                int cpIndex = Mathf.Clamp(Mathf.FloorToInt(personality.styleSeed * info.futureColisionPoints.Count), 0, info.futureColisionPoints.Count - 1);
                // Sometimes use a random future point for variety
                if (Roll(player) < 0.4f)
                    cpIndex = GetRng(player).Next(0, info.futureColisionPoints.Count);
                Vector3 cp = info.futureColisionPoints[cpIndex];
                float cpDist = Vector3.Distance(from, cp);
                score += 20f - cpDist;
                pos = Vector3.Lerp(pos, cp, 0.4f + personality.styleSeed * 0.35f);
            }

            // Prefer sticking with previously committed ball sometimes
            if (personality.committedBallId != 0 && go.GetInstanceID() == personality.committedBallId)
                score += Mathf.Lerp(8f, 2f, skill);

            candidates.Add((go, pos, vel, score, go.GetInstanceID()));
        }

        if (candidates.Count == 0)
            return false;

        candidates.Sort((a, b) => b.score.CompareTo(a.score));

        // Softmax-ish pick among top few
        int topN = Mathf.Min(candidates.Count, 3);
        float weightSum = 0f;
        float[] weights = new float[topN];
        for (int i = 0; i < topN; i++)
        {
            weights[i] = Mathf.Exp(candidates[i].score * 0.15f);
            weightSum += weights[i];
        }

        float pick = Roll(player) * weightSum;
        int chosen = 0;
        for (int i = 0; i < topN; i++)
        {
            pick -= weights[i];
            if (pick <= 0f)
            {
                chosen = i;
                break;
            }
            chosen = i;
        }

        Vector3 bestPos = candidates[chosen].pos;
        Vector3 bestVel = candidates[chosen].vel;
        ballId = candidates[chosen].id;

        float lead = Mathf.Lerp(0.12f, brain.interceptLead, skill) * personality.leadBias;
        float speed = Mathf.Max(bestVel.magnitude, 0.01f);
        float travel = Vector3.Distance(from, bestPos);
        float time = Mathf.Clamp(travel / speed, 0.05f, 1.1f) * lead;
        target = bestPos + bestVel * time;

        // Personal aim offset — different Naris lead to different intercept points
        float noiseAmp = Mathf.Lerp(1.35f, 0.12f, skill) * brain.noiseMult;
        target.x += ((Roll(player) * 2f) - 1f) * noiseAmp + personality.axisBias * 0.6f;
        target.y += ((Roll(player) * 2f) - 1f) * noiseAmp + (personality.styleSeed - 0.5f) * 1.2f;
        target.z = from.z;

        dist = Vector3.Distance(from, target);
        return true;
    }

    static Vector3 PickCardinalToward(
        Vector3 from,
        Vector3 worldTarget,
        Vector3 currentMoveDir,
        float arriveDistance,
        Func<Vector3, bool> wallBlocked,
        FreeRoamPersonality personality,
        Player player,
        float secondaryBiasChance)
    {
        Vector3 fallback = currentMoveDir.sqrMagnitude > 0.01f ? currentMoveDir : Vector3.up;
        Vector3 delta = worldTarget - from;
        delta.z = 0f;
        if (delta.sqrMagnitude <= arriveDistance * arriveDistance)
            return fallback;

        // Personality can flip axis preference near ties
        float xWeight = Mathf.Abs(delta.x) + personality.axisBias * 0.35f;
        float yWeight = Mathf.Abs(delta.y) - personality.axisBias * 0.35f;

        Vector3 primary;
        Vector3 secondary;
        if (xWeight >= yWeight)
        {
            primary = delta.x >= 0f ? Vector3.right : Vector3.left;
            secondary = delta.y >= 0f ? Vector3.up : Vector3.down;
            if (Mathf.Abs(delta.y) < 0.05f)
                secondary = primary;
        }
        else
        {
            primary = delta.y >= 0f ? Vector3.up : Vector3.down;
            secondary = delta.x >= 0f ? Vector3.right : Vector3.left;
            if (Mathf.Abs(delta.x) < 0.05f)
                secondary = primary;
        }

        bool Blocked(Vector3 d) => wallBlocked != null && wallBlocked(d);

        // Chance to take the secondary (longer) route first — exploration
        if (secondary != primary && secondaryBiasChance > 0f && Roll(player) < secondaryBiasChance && !Blocked(secondary))
            return secondary;

        if (!Blocked(primary))
            return primary;
        if (secondary != primary && !Blocked(secondary))
            return secondary;
        if (!Blocked(fallback))
            return fallback;

        return PickRandomOpenDir(fallback, wallBlocked, player);
    }

    static Vector3 PickRandomOpenDir(Vector3 preferred, Func<Vector3, bool> wallBlocked, Player player)
    {
        Vector3[] dirs = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
        // Shuffle with player RNG
        for (int i = dirs.Length - 1; i > 0; i--)
        {
            int j = GetRng(player).Next(0, i + 1);
            Vector3 tmp = dirs[i];
            dirs[i] = dirs[j];
            dirs[j] = tmp;
        }

        bool Blocked(Vector3 d) => wallBlocked != null && wallBlocked(d);

        if (preferred.sqrMagnitude > 0.01f && !Blocked(preferred) && Roll(player) < 0.35f)
            return preferred;

        for (int i = 0; i < dirs.Length; i++)
        {
            if (!Blocked(dirs[i]))
                return dirs[i];
        }

        return preferred.sqrMagnitude > 0.01f ? preferred : Vector3.up;
    }

    public static Decision GetLastDecision(int playerIndex)
    {
        return lastDecision.TryGetValue(playerIndex, out Decision d) ? d : default;
    }

    public static FreeRoamDecision GetLastFreeRoamDecision(int playerIndex)
    {
        return lastFreeRoam.TryGetValue(playerIndex, out FreeRoamDecision d) ? d : default;
    }

    static int MoveToward(Transform paddle, Vector3 target, float deadZone, List<string> wallTags, float wallCheckDistance)
    {
        float error = LateralError(paddle, target);
        if (Mathf.Abs(error) <= deadZone)
            return 0;

        int dir = error > 0f ? 1 : -1;
        if (WallInDirection(paddle, dir, wallTags, wallCheckDistance))
            return 0;
        return dir;
    }

    static float LateralError(Transform paddle, Vector3 worldTarget)
    {
        Vector3 right = paddle.right;
        right.z = 0f;
        if (right.sqrMagnitude < 0.0001f)
            return 0f;
        right.Normalize();

        Vector3 delta = worldTarget - paddle.position;
        delta.z = 0f;
        return Vector3.Dot(delta, right);
    }

    static Vector3 GetLaneCenter(Transform paddle, Facing facing)
    {
        Vector3 p = paddle.position;
        switch (facing)
        {
            case Facing.Up:
            case Facing.Down:
                return new Vector3(0f, p.y, p.z);
            default:
                return new Vector3(p.x, 0f, p.z);
        }
    }

    static bool IsBallComingToward(Facing facing, Vector3 ballVel, Vector3 paddlePos, Vector3 ballPos)
    {
        switch (facing)
        {
            case Facing.Up: return ballVel.y < -0.05f;
            case Facing.Down: return ballVel.y > 0.05f;
            case Facing.Left: return ballVel.x > 0.05f;
            case Facing.Right: return ballVel.x < -0.05f;
            default: return true;
        }
    }

    static float ApproachDistance(Facing facing, Vector3 paddlePos, Vector3 ballPos)
    {
        switch (facing)
        {
            case Facing.Up:
            case Facing.Down:
                return Mathf.Abs(ballPos.y - paddlePos.y);
            default:
                return Mathf.Abs(ballPos.x - paddlePos.x);
        }
    }

    static bool TryGetThreat(Transform paddle, Facing facing, out Rigidbody ballRb, out Vector3 ballPos, out Vector3 ballVel)
    {
        ballRb = null;
        ballPos = Vector3.zero;
        ballVel = Vector3.zero;

        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        if (balls == null || balls.Length == 0)
            return false;

        float bestScore = float.NegativeInfinity;
        for (int i = 0; i < balls.Length; i++)
        {
            GameObject go = balls[i];
            if (go == null)
                continue;

            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null)
                continue;

            Vector3 pos = go.transform.position;
            Vector3 vel = rb.linearVelocity;
            float toward = IsBallComingToward(facing, vel, paddle.position, pos) ? 10f : 0f;
            float dist = Vector3.Distance(paddle.position, pos);
            float score = toward * 100f - dist + vel.magnitude;

            BallInfo info = go.GetComponent<BallInfo>();
            if (info != null && info.futureColisionPoints != null && info.futureColisionPoints.Count > 0)
            {
                Vector3 bestCp = info.futureColisionPoints[0];
                float bestCpScore = float.PositiveInfinity;
                for (int c = 0; c < info.futureColisionPoints.Count; c++)
                {
                    Vector3 cp = info.futureColisionPoints[c];
                    if (!IsPointOnOurSide(facing, paddle.position, cp))
                        continue;
                    float cpDist = Vector3.Distance(paddle.position, cp);
                    if (cpDist < bestCpScore)
                    {
                        bestCpScore = cpDist;
                        bestCp = cp;
                    }
                }
                if (bestCpScore < float.PositiveInfinity)
                {
                    score += 25f - bestCpScore;
                    pos = Vector3.Lerp(pos, bestCp, 0.65f);
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                ballRb = rb;
                ballPos = pos;
                ballVel = vel;
            }
        }

        return ballRb != null;
    }

    static bool IsPointOnOurSide(Facing facing, Vector3 paddlePos, Vector3 point)
    {
        switch (facing)
        {
            case Facing.Up: return point.y <= paddlePos.y + 1.5f;
            case Facing.Down: return point.y >= paddlePos.y - 1.5f;
            case Facing.Left: return point.x >= paddlePos.x - 1.5f;
            case Facing.Right: return point.x <= paddlePos.x + 1.5f;
            default: return true;
        }
    }

    public static bool WallInDirection(Transform paddle, int dir, List<string> wallTags, float distance)
    {
        return PaddleWall.WallInDirection(paddle, dir, wallTags, distance);
    }

    public static Thought ThoughtFromDecision(Decision decision)
    {
        if (decision.wantBump)
            return Thought.MoveUp;
        if (decision.wantSuper)
            return Thought.MoveDown;
        return Thought.Nothing;
    }

    // ----- Learning (always writes to the active character|difficulty|variant Profile) -----

    public static void OnPaddleHitBall(Player player)
    {
        if (player == null || !player.computer)
            return;

        Profile p = GetProfile(player);
        p.paddleHits++;
        p.skill = Mathf.MoveTowards(p.skill, MaxSkill, 0.004f);
        p.interceptLead = Mathf.Clamp(p.interceptLead + 0.002f, 0.2f, 1.1f);
        p.deadZone = Mathf.Clamp(p.deadZone - 0.0015f, 0.1f, 1.2f);
        MaybeAutosave(p);
    }

    public static void OnTookGoalDamage(Player player, int amount)
    {
        if (player == null || !player.computer || amount <= 0)
            return;

        Profile p = GetProfile(player);
        p.goalsAgainst++;
        p.skill = Mathf.MoveTowards(p.skill, MaxSkill, 0.012f * amount);
        p.interceptLead = Mathf.Clamp(p.interceptLead + 0.025f * amount, 0.2f, 1.2f);
        p.deadZone = Mathf.Clamp(p.deadZone - 0.035f * amount, 0.1f, 1.2f);
        p.aggression = Mathf.Clamp(p.aggression + 0.012f * amount, 0.05f, MaxAggression);
        p.centerPull = Mathf.Clamp(p.centerPull - 0.01f, 0.05f, 0.5f);
        Save();
    }

    public static void OnMatchEnd(List<Player> players)
    {
        if (players == null)
            return;

        EnsureLoaded();
        bool anyComputer = false;

        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            if (player == null || !player.computer)
                continue;

            anyComputer = true;
            Profile p = GetProfile(player);
            p.matchesPlayed++;

            if (player.won)
            {
                p.wins++;
                p.skill = Mathf.MoveTowards(p.skill, MaxSkill, 0.045f);
                p.aggression = Mathf.Clamp(p.aggression + 0.015f, 0.05f, MaxAggression);
            }
            else
            {
                p.losses++;
                // Losses sharpen intercept more so the same variant improves at its level
                p.skill = Mathf.MoveTowards(p.skill, MaxSkill, 0.03f);
                p.interceptLead = Mathf.Clamp(p.interceptLead + 0.04f, 0.2f, 1.2f);
                p.deadZone = Mathf.Clamp(p.deadZone - 0.045f, 0.1f, 1.2f);
                p.aggression = Mathf.Clamp(p.aggression + 0.02f, 0.05f, MaxAggression);
            }
        }

        if (anyComputer)
            Save();
    }

    static int autosaveCounter;
    static void MaybeAutosave(Profile _)
    {
        autosaveCounter++;
        if (autosaveCounter >= 8)
        {
            autosaveCounter = 0;
            Save();
        }
    }

    static void TickCooldowns(int playerIndex)
    {
        if (bumpCooldown.TryGetValue(playerIndex, out float b) && b > 0f)
            bumpCooldown[playerIndex] = b - Time.deltaTime;
        if (superCooldown.TryGetValue(playerIndex, out float s) && s > 0f)
            superCooldown[playerIndex] = s - Time.deltaTime;
        if (dashCooldown.TryGetValue(playerIndex, out float d) && d > 0f)
            dashCooldown[playerIndex] = d - Time.deltaTime;
    }

    static float GetCooldown(Dictionary<int, float> map, int index)
    {
        return map.TryGetValue(index, out float v) ? v : 0f;
    }

    public static void Load()
    {
        profiles.Clear();
        try
        {
            if (!File.Exists(SavePath))
                return;

            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data?.profiles == null)
                return;

            List<Profile> legacy = new List<Profile>();

            for (int i = 0; i < data.profiles.Count; i++)
            {
                Profile p = data.profiles[i];
                if (p == null || string.IsNullOrEmpty(p.key))
                    continue;

                p.skill = Mathf.Clamp(p.skill, MinSkill, MaxSkill);
                p.aggression = Mathf.Clamp(p.aggression, 0.05f, MaxAggression);

                // Old format: key was just character name (no pipes)
                if (!p.key.Contains("|"))
                {
                    legacy.Add(p);
                    continue;
                }

                // Fill metadata from key when missing
                if (TryParseKey(p.key, out string cn, out int d, out int v))
                {
                    if (string.IsNullOrEmpty(p.characterName))
                        p.characterName = cn;
                    p.difficulty = d;
                    p.variant = v;
                }

                profiles[p.key] = p;
            }

            // Migrate legacy single-key profiles into Training|variant0 and seed other variants
            for (int i = 0; i < legacy.Count; i++)
            {
                Profile old = legacy[i];
                string charName = old.key;
                Profile training0 = CreateSeededProfile(charName, CpuDifficulty.Training, 0, old);
                profiles[training0.key] = training0;

                for (int v = 1; v < VariantsPerLevel; v++)
                {
                    Profile seeded = CreateSeededProfile(charName, CpuDifficulty.Training, v, old);
                    // Keep shared skill progress but distinct style biases from CreateSeededProfile
                    seeded.skill = old.skill;
                    seeded.matchesPlayed = 0;
                    seeded.wins = 0;
                    seeded.losses = 0;
                    profiles[seeded.key] = seeded;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("ComputerAI Load failed: " + e.Message);
        }
    }

    static bool TryParseKey(string key, out string characterName, out int difficulty, out int variant)
    {
        characterName = "global";
        difficulty = 0;
        variant = 0;
        if (string.IsNullOrEmpty(key))
            return false;

        string[] parts = key.Split('|');
        if (parts.Length < 3)
            return false;

        characterName = parts[0];
        if (!int.TryParse(parts[1], out difficulty))
            return false;
        if (!int.TryParse(parts[2], out variant))
            return false;
        variant = Mathf.Clamp(variant, 0, VariantsPerLevel - 1);
        return true;
    }

    public static void Save()
    {
        try
        {
            SaveData data = new SaveData();
            foreach (var kvp in profiles)
                data.profiles.Add(kvp.Value);

            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning("ComputerAI Save failed: " + e.Message);
        }
    }

    // ─── Training coverage (player-count / field / matchups beyond profile keys) ───

    const string CoverageFile = "computer_ai_coverage.json";
    static readonly Dictionary<string, int> coverage = new Dictionary<string, int>();
    static bool coverageLoaded;

    [Serializable]
    class CoverageSave
    {
        public List<CoverageEntry> entries = new List<CoverageEntry>();
    }

    [Serializable]
    public class CoverageEntry
    {
        public string key = "";
        public int samples;
    }

    /// <summary>Proposed auto-training match lineup.</summary>
    public class TrainingMatchPlan
    {
        public int playerCount = 2;
        public int fieldIndex = -1;
        public string fieldName = "";
        public readonly List<string> characterNames = new List<string>();
        public readonly List<int> variants = new List<int>();
        public float priorityScore; // lower = more needed
    }

    static string CoveragePath => Path.Combine(Application.persistentDataPath, CoverageFile);

    public static void EnsureCoverageLoaded()
    {
        EnsureLoaded();
        if (coverageLoaded)
            return;
        LoadCoverage();
        coverageLoaded = true;
    }

    static void LoadCoverage()
    {
        coverage.Clear();
        try
        {
            if (!File.Exists(CoveragePath))
                return;
            CoverageSave data = JsonUtility.FromJson<CoverageSave>(File.ReadAllText(CoveragePath));
            if (data == null || data.entries == null)
                return;
            for (int i = 0; i < data.entries.Count; i++)
            {
                CoverageEntry e = data.entries[i];
                if (e == null || string.IsNullOrEmpty(e.key))
                    continue;
                coverage[e.key] = e.samples;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("ComputerAI Coverage Load failed: " + e.Message);
        }
    }

    public static void SaveCoverage()
    {
        try
        {
            CoverageSave data = new CoverageSave();
            foreach (var kvp in coverage)
                data.entries.Add(new CoverageEntry { key = kvp.Key, samples = kvp.Value });
            File.WriteAllText(CoveragePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning("ComputerAI Coverage Save failed: " + e.Message);
        }
    }

    public static int GetCoverage(string key)
    {
        EnsureCoverageLoaded();
        if (string.IsNullOrEmpty(key))
            return 0;
        return coverage.TryGetValue(key, out int n) ? n : 0;
    }

    public static void AddCoverage(string key, int amount = 1)
    {
        if (string.IsNullOrEmpty(key) || amount == 0)
            return;
        EnsureCoverageLoaded();
        coverage.TryGetValue(key, out int n);
        coverage[key] = n + amount;
    }

    public static int GetProfileMatches(string characterName, CpuDifficulty difficulty, int variant)
    {
        EnsureLoaded();
        Profile p = GetOrCreateProfile(characterName, difficulty, variant);
        return p != null ? p.matchesPlayed : 0;
    }

    public static string PlayersCoverageKey(int playerCount) => "players:" + playerCount;
    public static string FieldCoverageKey(string fieldName) => "field:" + (fieldName ?? "unknown");
    public static string MatchupCoverageKey(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) a = "?";
        if (string.IsNullOrEmpty(b)) b = "?";
        if (string.CompareOrdinal(a, b) > 0)
        {
            string t = a;
            a = b;
            b = t;
        }
        return "matchup:" + a + "|" + b;
    }

    /// <summary>
    /// Record extra axes after a finished training match (profiles already updated via OnMatchEnd).
    /// </summary>
    public static void RecordTrainingMatchCoverage(List<Player> players, string fieldName)
    {
        if (players == null || players.Count == 0)
            return;

        EnsureCoverageLoaded();
        AddCoverage(PlayersCoverageKey(players.Count));
        AddCoverage(FieldCoverageKey(fieldName));

        List<string> names = new List<string>();
        for (int i = 0; i < players.Count; i++)
        {
            Player p = players[i];
            if (p == null || string.IsNullOrEmpty(p.name))
                continue;
            names.Add(p.name);
        }

        for (int i = 0; i < names.Count; i++)
        {
            for (int j = i + 1; j < names.Count; j++)
                AddCoverage(MatchupCoverageKey(names[i], names[j]));
        }

        SaveCoverage();
    }

    /// <summary>
    /// Build a match that prioritizes low-sample characters, variants, player counts, fields, and matchups.
    /// Always uses CpuDifficulty.Training for AI authoring.
    /// </summary>
    public static TrainingMatchPlan PickUndertrainedMatch(
        List<Characters> roster,
        List<Field> fields,
        int minPlayers = 2,
        int maxPlayers = 8)
    {
        EnsureCoverageLoaded();

        TrainingMatchPlan best = null;
        List<Characters> chars = new List<Characters>();
        if (roster != null)
        {
            for (int i = 0; i < roster.Count; i++)
            {
                Characters c = roster[i];
                if (c == null || string.IsNullOrEmpty(c.name))
                    continue;
                if (c.character == null || c.character.prefabs == null)
                    continue;
                chars.Add(c);
            }
        }

        if (chars.Count == 0)
            return null;

        List<int> fieldIdx = new List<int>();
        if (fields != null)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i] != null)
                    fieldIdx.Add(i);
            }
        }

        minPlayers = Mathf.Clamp(minPlayers, 2, 8);
        maxPlayers = Mathf.Clamp(maxPlayers, minPlayers, 8);

        // Monte-carlo sample candidate lineups; keep the hungriest (lowest score).
        const int Samples = 48;
        for (int s = 0; s < Samples; s++)
        {
            TrainingMatchPlan plan = new TrainingMatchPlan();
            plan.playerCount = UnityEngine.Random.Range(minPlayers, maxPlayers + 1);

            if (fieldIdx.Count > 0)
            {
                plan.fieldIndex = fieldIdx[UnityEngine.Random.Range(0, fieldIdx.Count)];
                Field f = fields[plan.fieldIndex];
                plan.fieldName = f != null && !string.IsNullOrEmpty(f.name) ? f.name : ("Field" + plan.fieldIndex);
            }
            else
            {
                plan.fieldIndex = -1;
                plan.fieldName = "unknown";
            }

            // Bias toward characters with low Training-variant matches
            List<Characters> pool = new List<Characters>(chars);
            for (int p = 0; p < plan.playerCount; p++)
            {
                Characters pick = WeightedPickCharacter(pool);
                if (pick == null)
                    pick = chars[UnityEngine.Random.Range(0, chars.Count)];
                plan.characterNames.Add(pick.name);

                int bestVar = 0;
                int bestVarMatches = int.MaxValue;
                for (int v = 0; v < VariantsPerLevel; v++)
                {
                    int m = GetProfileMatches(pick.name, CpuDifficulty.Training, v);
                    if (m < bestVarMatches)
                    {
                        bestVarMatches = m;
                        bestVar = v;
                    }
                }
                plan.variants.Add(bestVar);
            }

            plan.priorityScore = ScorePlan(plan);
            if (best == null || plan.priorityScore < best.priorityScore)
                best = plan;
        }

        return best;
    }

    static Characters WeightedPickCharacter(List<Characters> pool)
    {
        if (pool == null || pool.Count == 0)
            return null;

        float total = 0f;
        float[] weights = new float[pool.Count];
        for (int i = 0; i < pool.Count; i++)
        {
            int played = 0;
            for (int v = 0; v < VariantsPerLevel; v++)
                played += GetProfileMatches(pool[i].name, CpuDifficulty.Training, v);
            // Inverse weight — never-played chars dominate
            weights[i] = 1f / (1f + played);
            total += weights[i];
        }

        float r = UnityEngine.Random.value * total;
        float acc = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            acc += weights[i];
            if (r <= acc)
                return pool[i];
        }
        return pool[pool.Count - 1];
    }

    static float ScorePlan(TrainingMatchPlan plan)
    {
        if (plan == null)
            return float.MaxValue;

        float score = 0f;
        score += GetCoverage(PlayersCoverageKey(plan.playerCount)) * 2.5f;
        score += GetCoverage(FieldCoverageKey(plan.fieldName)) * 1.5f;

        for (int i = 0; i < plan.characterNames.Count; i++)
        {
            string name = plan.characterNames[i];
            int v = i < plan.variants.Count ? plan.variants[i] : 0;
            score += GetProfileMatches(name, CpuDifficulty.Training, v) * 3f;

            for (int j = i + 1; j < plan.characterNames.Count; j++)
                score += GetCoverage(MatchupCoverageKey(name, plan.characterNames[j])) * 2f;
        }

        // Slight noise so ties don't lock forever
        score += UnityEngine.Random.Range(0f, 0.35f);
        return score;
    }

    public static string DescribeCoverageGaps(List<Characters> roster, int top = 6)
    {
        EnsureCoverageLoaded();
        List<string> lines = new List<string>();

        if (roster != null)
        {
            List<(string label, int n)> charGaps = new List<(string, int)>();
            for (int i = 0; i < roster.Count; i++)
            {
                Characters c = roster[i];
                if (c == null || string.IsNullOrEmpty(c.name))
                    continue;
                int played = 0;
                for (int v = 0; v < VariantsPerLevel; v++)
                    played += GetProfileMatches(c.name, CpuDifficulty.Training, v);
                charGaps.Add((c.name, played));
            }
            charGaps.Sort((a, b) => a.n.CompareTo(b.n));
            for (int i = 0; i < Mathf.Min(top, charGaps.Count); i++)
                lines.Add(charGaps[i].label + ":" + charGaps[i].n);
        }

        for (int n = 2; n <= 8; n++)
            lines.Add("P" + n + "=" + GetCoverage(PlayersCoverageKey(n)));

        return string.Join("  ", lines);
    }
}
