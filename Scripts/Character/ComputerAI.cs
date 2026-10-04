using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Shared computer-player brain: continuous ball tracking (no sticky L/R)
/// plus persistent skill that improves across matches.
/// </summary>
public static class ComputerAI
{
    const string SaveFile = "computer_ai_profiles.json";
    const float MinSkill = 0.15f;
    const float MaxSkill = 0.95f;

    [Serializable]
    public class Profile
    {
        public string key = "global";
        public float skill = 0.35f;
        public float aggression = 0.25f;
        public float interceptLead = 0.55f;
        public float deadZone = 0.45f;
        public float centerPull = 0.2f;
        public int matchesPlayed;
        public int wins;
        public int losses;
        public int paddleHits;
        public int goalsAgainst;
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

    static readonly Dictionary<string, Profile> profiles = new Dictionary<string, Profile>();
    static readonly Dictionary<int, float> bumpCooldown = new Dictionary<int, float>();
    static readonly Dictionary<int, float> superCooldown = new Dictionary<int, float>();
    static readonly Dictionary<int, float> dashCooldown = new Dictionary<int, float>();
    static readonly Dictionary<int, Decision> lastDecision = new Dictionary<int, Decision>();
    static bool loaded;

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
            profile = new Profile { key = key };
            profiles[key] = profile;
        }
        return profile;
    }

    static string ProfileKey(Player player)
    {
        if (player == null || string.IsNullOrEmpty(player.name))
            return "global";
        return player.name;
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
        Profile profile = GetProfile(player);
        float skill = Mathf.Clamp(profile.skill, MinSkill, MaxSkill);

        TickCooldowns(player.index);

        if (!TryGetThreat(paddle, player.facing, out Rigidbody ballRb, out Vector3 ballPos, out Vector3 ballVel))
        {
            // No threat: ease toward center of own lane
            decision.moveDir = MoveToward(paddle, GetLaneCenter(paddle, player.facing), profile.deadZone * 1.5f, wallTags, wallCheckDistance);
            lastDecision[player.index] = decision;
            return decision;
        }

        bool ballComingAtUs = IsBallComingToward(player.facing, ballVel, paddle.position, ballPos);
        Vector3 target = ballPos;

        if (ballComingAtUs)
        {
            float lead = Mathf.Lerp(0.15f, profile.interceptLead, skill);
            float distance = Vector3.Distance(paddle.position, ballPos);
            float speed = Mathf.Max(ballVel.magnitude, 0.01f);
            float time = Mathf.Clamp(distance / speed, 0.05f, 1.25f) * lead;
            target = ballPos + ballVel * time;
        }
        else
        {
            // Ball leaving: drift toward center, lightly track X/Y for recovery
            Vector3 center = GetLaneCenter(paddle, player.facing);
            target = Vector3.Lerp(ballPos, center, profile.centerPull + (1f - skill) * 0.35f);
        }

        // Skill reduces jitter / low skill adds miss noise so they visibly improve
        float noiseAmp = Mathf.Lerp(1.1f, 0.05f, skill);
        Vector2 noise = UnityEngine.Random.insideUnitCircle * noiseAmp;
        target.x += noise.x;
        target.y += noise.y;

        float deadZone = Mathf.Lerp(profile.deadZone * 1.4f, 0.12f, skill);
        decision.moveDir = MoveToward(paddle, target, deadZone, wallTags, wallCheckDistance);

        // Bump / super: only when useful, gated by aggression + skill + cooldown
        float lateral = LateralError(paddle, target);
        float approach = ApproachDistance(player.facing, paddle.position, ballPos);

        if (ballComingAtUs && GetCooldown(bumpCooldown, player.index) <= 0f)
        {
            float bumpChance = profile.aggression * Mathf.Lerp(0.15f, 0.85f, skill);
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
            float superChance = profile.aggression * 0.45f * skill;
            if (approach > 2.5f && Mathf.Abs(lateral) > 1.25f
                && player.super != null
                && player.super.Enough()
                && player.super.readyPercent >= 1f)
            {
                if (UnityEngine.Random.value < superChance * Time.deltaTime * 3f)
                {
                    decision.wantSuper = true;
                    superCooldown[player.index] = Mathf.Lerp(3f, 1.25f, skill);
                }
            }
        }

        // Dash: cover large lateral gaps when the ball is incoming
        if (ballComingAtUs
            && decision.moveDir != 0
            && GetCooldown(dashCooldown, player.index) <= 0f
            && player.dash != null
            && player.dash.amount >= player.dash.cost
            && !WallInDirection(paddle, decision.moveDir, wallTags, wallCheckDistance))
        {
            float dashGap = Mathf.Lerp(1.8f, 1.0f, skill);
            float dashChance = Mathf.Lerp(0.2f, 0.75f, skill) * (0.5f + profile.aggression);
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

    public static Decision GetLastDecision(int playerIndex)
    {
        return lastDecision.TryGetValue(playerIndex, out Decision d) ? d : default;
    }

    static int MoveToward(Transform paddle, Vector3 target, float deadZone, List<string> wallTags, float wallCheckDistance)
    {
        float error = LateralError(paddle, target);
        if (Mathf.Abs(error) <= deadZone)
            return 0;

        int dir = error > 0f ? 1 : -1;
        if (WallInDirection(paddle, dir, wallTags, wallCheckDistance))
        {
            // Don't hold a blocked direction — stop or try the other way if somehow needed
            return 0;
        }
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

            // Prefer predicted intercepts when available
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
                    // Bias target toward that point via synthetic "position"
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
        if (paddle == null || distance <= 0f)
            return false;

        RaycastHit[] hits = Physics.RaycastAll(paddle.position, dir * paddle.right, distance);
        for (int i = 0; i < hits.Length; i++)
        {
            string tag = hits[i].transform.tag;
            if (wallTags == null || wallTags.Count == 0)
            {
                if (tag == "Wall" || tag == "Walls")
                    return true;
            }
            else if (wallTags.Exists(x => x.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }
        return false;
    }

    public static Thought ThoughtFromDecision(Decision decision)
    {
        if (decision.wantBump)
            return Thought.MoveUp;
        if (decision.wantSuper)
            return Thought.MoveDown;
        return Thought.Nothing;
    }

    // ----- Learning -----

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
        // Getting scored on: react earlier, tighter deadzone, a bit more aggression
        p.skill = Mathf.MoveTowards(p.skill, MaxSkill, 0.01f * amount);
        p.interceptLead = Mathf.Clamp(p.interceptLead + 0.02f * amount, 0.2f, 1.2f);
        p.deadZone = Mathf.Clamp(p.deadZone - 0.03f * amount, 0.1f, 1.2f);
        p.aggression = Mathf.Clamp(p.aggression + 0.015f * amount, 0.05f, 0.9f);
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
                p.aggression = Mathf.Clamp(p.aggression + 0.02f, 0.05f, 0.9f);
            }
            else
            {
                p.losses++;
                // Still improve on losses — learn from mistakes
                p.skill = Mathf.MoveTowards(p.skill, MaxSkill, 0.025f);
                p.interceptLead = Mathf.Clamp(p.interceptLead + 0.03f, 0.2f, 1.2f);
                p.deadZone = Mathf.Clamp(p.deadZone - 0.04f, 0.1f, 1.2f);
                p.aggression = Mathf.Clamp(p.aggression + 0.03f, 0.05f, 0.9f);
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

            for (int i = 0; i < data.profiles.Count; i++)
            {
                Profile p = data.profiles[i];
                if (p == null || string.IsNullOrEmpty(p.key))
                    continue;
                p.skill = Mathf.Clamp(p.skill, MinSkill, MaxSkill);
                profiles[p.key] = p;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("ComputerAI Load failed: " + e.Message);
        }
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
}
