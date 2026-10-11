using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Player
{
    public string name = "";
    public UniqueId uid = new UniqueId();

    public ControllerLink cLink;

    /// <summary>
    /// Extra devices driving this slot (Character Creation routes all pads here).
    /// When empty, gameplay uses <see cref="cLink"/> only.
    /// </summary>
    [System.NonSerialized]
    public List<ControllerLink> inputLinks = new List<ControllerLink>();

    public int index = -1;

    /// <summary>Index into Database.playerColors for duplicate-character skins. -1 = use player index.</summary>
    public int skinColorIndex = -1; // -1 = this character's base color

    public bool ignoreFacing = false;

    public string nickName = "";
    public int team = 0;
    public int position = 0;

    public int currentHealth = 10;
    public int maxHealth = 10;

    public float movementSpeed = 5;
    public float pushBack = 0;

    public Facing facing = Facing.Up;

    public Skill bump;
    public Skill super;
    public Skill dash;

    public ObjectInfo character = null;

    public ObjectInfo lifeline = null;
    public GameObject selector = null;
    public GameObject playerInfo = null;

    public string superName;
    public string superDescription;

    public Color portraitColor = Color.cyan;

    public Texture portrait;
    public Texture icon;

    public bool active = false;
    public bool computer = false;

    /// <summary>
    /// Match CPU difficulty. Easy–Impossible for lobby play.
    /// Training is authoring-only (not offered in the CPU level cycle).
    /// </summary>
    public ComputerAI.CpuDifficulty cpuDifficulty = ComputerAI.CpuDifficulty.Easy;

    /// <summary>Style variant index within the difficulty (0..ComputerAI.VariantsPerLevel-1).</summary>
    public int cpuVariant = 0;

    public PlayerSelectorObj pso = null;

    public GameObject spawnedPlayer;
    public GameObject spawnedLifeline;

    /// <summary>Tic coop: this slot is the wing-bumper form (no machine).</summary>
    [System.NonSerialized] public bool ticWingForm;
    /// <summary>Tic coop: this slot is the fused double-HP machine host.</summary>
    [System.NonSerialized] public bool ticHostFused;
    /// <summary>Tic coop: lateral wall offset for side-by-side machines.</summary>
    [System.NonSerialized] public float ticWallSideOffset;
    /// <summary>Tic coop: partner player index (-1 none).</summary>
    [System.NonSerialized] public int ticPartnerIndex = -1;

    /// <summary>Celarus coop: this slot owns the shared moon/sun lifeline.</summary>
    [System.NonSerialized] public bool celarusShareHost;
    /// <summary>Celarus coop: this slot uses a partner's moon/sun (no second lifeline).</summary>
    [System.NonSerialized] public bool celarusShareGuest;
    /// <summary>Celarus coop: host player index (-1 none).</summary>
    [System.NonSerialized] public int celarusShareHostIndex = -1;

    #region Selections
    public bool characterSelected = false;
    /// <summary>Roster "?" pick — concrete character is rolled in StartGame and again each rematch.</summary>
    public bool wantRandomCharacter = false;
    public float lastGridUpdate = 0;
    public bool gridLock = false;
    public string state = "";
    /// <summary>Win screen: stats card this human is manually scrolling (null = free cursor).</summary>
    [System.NonSerialized] public GameResultBreakdown winScrollTarget;

    #endregion

    [SerializeField]
    public List<PlayerConstraints> canMove = new List<PlayerConstraints>();
    [SerializeField]
    public List<PlayerConstraints> canBump = new List<PlayerConstraints>();
    [SerializeField]
    public List<PlayerConstraints> canSuper = new List<PlayerConstraints>();
    [SerializeField]
    public List<PlayerConstraints> canDash = new List<PlayerConstraints>();
    [SerializeField]
    public List<MoveSpeedModifier> moveSpeedModifiers = new List<MoveSpeedModifier>();

    #region Gameplay Stats
    public bool won = false;
    public MatchStats match = new MatchStats();

    public int damageDealt { get => match.damageDealt; set => match.damageDealt = value; }
    public int damageTaken { get => match.damageTaken; set => match.damageTaken = value; }
    public int ballHits { get => match.ballHits; set => match.ballHits = value; }
    public int longestBallOwnership { get => match.longestBallOwnership; set => match.longestBallOwnership = value; }
    public int highestSingleDamgeDealt { get => match.highestSingleDamgeDealt; set => match.highestSingleDamgeDealt = value; }
    public int highestSingleDamageTaken { get => match.highestSingleDamageTaken; set => match.highestSingleDamageTaken = value; }
    public int ultsUsed { get => match.ultsUsed; set => match.ultsUsed = value; }
    public int numberOfDashes { get => match.numberOfDashes; set => match.numberOfDashes = value; }
    public int afterDeathHits { get => match.afterDeathHits; set => match.afterDeathHits = value; }
    public int afterDeathDamage { get => match.afterDeathDamage; set => match.afterDeathDamage = value; }

    public void ResetMatchStats()
    {
        if (match == null)
            match = new MatchStats();
        match.Reset();
    }
    #endregion

    public Player()
    {

    }

    public Player(Player p)
    {
        name = p.name;
        uid = new UniqueId();

        index = p.index;
        skinColorIndex = p.skinColorIndex;

        ignoreFacing = p.ignoreFacing;

        currentHealth = p.currentHealth;
        maxHealth = p.maxHealth;

        movementSpeed = p.movementSpeed;
        pushBack = p.pushBack;

        facing = p.facing;

        bump = new Skill(p.bump);
        super = new Skill(p.super);
        dash = new Skill(p.dash);

        character = new ObjectInfo(p.character);

        lifeline = new ObjectInfo(p.lifeline);
        selector = p.selector;
        playerInfo = p.playerInfo;

        superName = p.superName;
        superDescription = p.superDescription;

        portraitColor = p.portraitColor;

        portrait = p.portrait;
        icon = p.icon;

        active = p.active;
        computer = p.computer;
        cpuDifficulty = p.cpuDifficulty;
        cpuVariant = p.cpuVariant;
        wantRandomCharacter = p.wantRandomCharacter;
        characterSelected = p.characterSelected;

        spawnedPlayer = p.spawnedPlayer;
        spawnedLifeline = p.spawnedLifeline;
        match = new MatchStats();
    }

    /// <summary>Lobby placeholder for random roster pick (portrait shows "?" until StartGame).</summary>
    public void SetRandomCharacterPending()
    {
        wantRandomCharacter = true;
        name = "";
        portrait = null;
        icon = null;
        character = new ObjectInfo();
        lifeline = new ObjectInfo();
        selector = null;
        playerInfo = null;
        superName = "";
        superDescription = "";
    }

    public void SetUpCharacter(Characters p)
    {
        if (p == null)
            return;

        string previousName = name;
        name = p.name;

        if (computer && previousName != p.name)
            ComputerAI.ApplyRecommendedDifficulty(this);

        currentHealth = p.maxHealth;
        maxHealth = p.maxHealth;

        ignoreFacing = p.ignoreFacing;

        movementSpeed = p.movementSpeed;
        pushBack = p.pushBack;

        bump = new Skill(p.bump);
        super = new Skill(p.super);
        dash = new Skill(p.dash);

        // Always keep non-null ObjectInfo instances so StartGame never NREs on .prefabs
        character = p.character != null ? new ObjectInfo(p.character) : new ObjectInfo();
        lifeline = p.lifeline != null ? new ObjectInfo(p.lifeline) : new ObjectInfo();
        selector = p.selector;
        playerInfo = p.playerInfo;

        superName = p.superName;
        superDescription = p.superDescription;

        portraitColor = p.portraitColor;

        portrait = p.portrait;
        icon = p.icon;
    }

    public void CreateUID()
    {
        uid = new UniqueId(10, "pl_");
    }

    public void AddConstraint(GameObject caller, float length = -1, PlayerConstraint constraint = PlayerConstraint.Move)
    {
        switch (constraint)
        {
            case PlayerConstraint.Move:
                PlayerConstraints pC_M = canMove.Find(x => x.caller == caller);

                if (pC_M != null)
                {
                    pC_M.endTime = length >= 0 ? Time.time + length : -1;
                }
                else
                {
                    canMove.Add(new PlayerConstraints(caller, length >= 0 ? Time.time + length : -1));
                }
                break;
            case PlayerConstraint.Bump:
                PlayerConstraints pC_B = canBump.Find(x => x.caller == caller);

                if (pC_B != null)
                {
                    pC_B.endTime = length >= 0 ? Time.time + length : -1;
                }
                else
                {
                    canBump.Add(new PlayerConstraints(caller, length >= 0 ? Time.time + length : -1));
                }
                break;
            case PlayerConstraint.Super:
                PlayerConstraints pC_S = canSuper.Find(x => x.caller == caller);

                if (pC_S != null)
                {
                    pC_S.endTime = length >= 0 ? Time.time + length : -1;
                }
                else
                {
                    canSuper.Add(new PlayerConstraints(caller, length >= 0 ? Time.time + length : -1));
                }
                break;
            case PlayerConstraint.Dash:
                PlayerConstraints pC_D = canDash.Find(x => x.caller == caller);

                if (pC_D != null)
                {
                    pC_D.endTime = length >= 0 ? Time.time + length : -1;
                }
                else
                {
                    canDash.Add(new PlayerConstraints(caller, length >= 0 ? Time.time + length : -1));
                }
                break;
        }
    }

    public void RemoveConstraint(GameObject caller, PlayerConstraint constraint = PlayerConstraint.Move)
    {
        switch (constraint)
        {
            case PlayerConstraint.Move:
                canMove.RemoveAll(x => x != null && x.caller == caller);
                break;
            case PlayerConstraint.Bump:
                canBump.RemoveAll(x => x != null && x.caller == caller);
                break;
            case PlayerConstraint.Super:
                canSuper.RemoveAll(x => x != null && x.caller == caller);
                break;
            case PlayerConstraint.Dash:
                canDash.RemoveAll(x => x != null && x.caller == caller);
                break;
        }
    }

    public void AddMoveSpeedFactor(GameObject caller, float length, float factor)
    {
        if (caller == null)
            return;

        MoveSpeedModifier existing = moveSpeedModifiers.Find(x => x != null && x.caller == caller);
        float end = length >= 0 ? Time.time + length : -1f;
        if (existing != null)
        {
            existing.endTime = end;
            existing.factor = factor;
        }
        else
        {
            moveSpeedModifiers.Add(new MoveSpeedModifier(caller, end, factor));
        }
    }

    public void RemoveMoveSpeedFactor(GameObject caller)
    {
        moveSpeedModifiers.RemoveAll(x => x != null && x.caller == caller);
    }

    public bool CanMove
    {
        get
        {
            return !(canMove.Count > 0);
        }
    }

    public bool CanBump
    {
        get
        {
            return !(canBump.Count > 0);
        }
    }

    public bool CanSuper
    {
        get
        {
            return !(canSuper.Count > 0);
        }
    }

    public bool CanDash
    {
        get
        {
            return !(canDash.Count > 0);
        }
    }

    /// <summary>Combined move-speed multipliers from temporary effects (Shock Ball, etc.).</summary>
    public float MoveSpeedMultiplier
    {
        get
        {
            float m = 1f;
            for (int i = 0; i < moveSpeedModifiers.Count; i++)
            {
                MoveSpeedModifier mod = moveSpeedModifiers[i];
                if (mod == null)
                    continue;
                m *= mod.factor;
            }
            return m;
        }
    }

    public float EffectiveMovementSpeed => movementSpeed * MoveSpeedMultiplier;

    public bool WithinLastUpdate
    {
        get
        {
            return lastGridUpdate +.1f < Time.time;
        }
    }

    // Same-frame dedupe so multi-collider lifelines (e.g. Garmen hub+body) don't multi-hit
    int lastGoalDamageFrame = -1;
    int lastGoalDamageBallId = 0;
    float lastGoalDamageTime = -999f;

    /// <summary>Apply HP change. Positive = damage (records damage taken as actual HP lost). Returns HP lost.</summary>
    public int Damage(int amount)
    {
        if (amount > 0)
        {
            int floor = CharacterCreationManager.IsActive ? 1 : 0;
            int lost = Mathf.Min(amount, Mathf.Max(0, currentHealth - floor));
            currentHealth -= amount;
            if (currentHealth < floor)
                currentHealth = floor;
            int leftover = amount - lost;
            if (leftover > 0)
                Stats.pendingOverkill += leftover;

            if (lost > 0)
                RecordDamageTaken(lost);

            return lost;
        }

        // Heal (negative amount)
        currentHealth -= amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
        int healFloor = CharacterCreationManager.IsActive ? 1 : 0;
        if (currentHealth < healFloor)
            currentHealth = healFloor;
        return 0;
    }

    /// <summary>Goal/ball damage with per-ball dedupe. Returns actual HP lost.</summary>
    public int ApplyGoalDamage(int amount, int ballInstanceId)
    {
        if (amount <= 0)
            return 0;

        // Yuotay: one HP per catch/hit, never stack vamp / weapon-up / re-collision
        bool yuotay = !string.IsNullOrEmpty(name)
            && name.Equals("Yuotay", System.StringComparison.OrdinalIgnoreCase);
        if (yuotay)
            amount = 1;

        // Same ball already counted this frame or during the hold/re-contact window
        if (ballInstanceId != 0
            && lastGoalDamageBallId == ballInstanceId
            && (lastGoalDamageFrame == Time.frameCount || Time.time - lastGoalDamageTime < 0.5f))
            return 0;

        // Yuotay mit can re-trigger OnCollisionEnter while the ball is stuck — block bursts
        if (yuotay && Time.time - lastGoalDamageTime < 0.35f)
            return 0;

        lastGoalDamageFrame = Time.frameCount;
        lastGoalDamageBallId = ballInstanceId;
        lastGoalDamageTime = Time.time;

        if (spawnedPlayer != null)
        {
            MuriShield shield = spawnedPlayer.GetComponent<MuriShield>();
            if (shield != null && shield.TryAbsorb())
            {
                RecordShieldBlock(amount);
                return 0;
            }
        }

        return Damage(amount);
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
            return;
        int before = currentHealth;
        Damage(-amount);
        int gained = currentHealth - before;
        if (gained > 0)
            RecordHeal(gained);
    }

    #region Win-screen stat recording
    MatchStats Stats
    {
        get
        {
            if (match == null)
                match = new MatchStats();
            return match;
        }
    }

    public void RecordDamageTaken(int amount)
    {
        if (amount <= 0)
            return;

        MatchStats s = Stats;
        s.damageTaken += amount;
        if (amount > s.highestSingleDamageTaken)
            s.highestSingleDamageTaken = amount;
        if (currentHealth == 1)
            s.damageTakenAtOneHp += amount;
        if (s.lowestHpReached > currentHealth)
            s.lowestHpReached = currentHealth;
        s.currentUndamaged = 0f;

        if (currentHealth <= 0)
            RecordDeath(null);
    }

    public void RecordDamageDealt(int amount)
    {
        RecordDamageDealt(amount, null, StatSource.Ball);
    }

    public void RecordDamageDealt(int amount, Player victim)
    {
        RecordDamageDealt(amount, victim, StatSource.Ball);
    }

    public void RecordDamageDealt(int amount, Player victim, StatSource source)
    {
        if (amount <= 0)
            return;

        MatchStats s = Stats;
        s.damageDealt += amount;
        if (amount > s.highestSingleDamgeDealt)
            s.highestSingleDamgeDealt = amount;
        if (currentHealth <= 0)
            s.afterDeathDamage += amount;
        if (currentHealth == 1)
            s.damageDealtAtOneHp += amount;

        if (source == StatSource.Ability)
            s.abilityDamageDealt += amount;
        else if (source == StatSource.Hazard)
            s.hazardDamageDealt += amount;

        if (victim != null && victim.currentHealth <= 0)
            RecordKill(victim);
    }

    public void RecordGoalScored(int amount, Player victim = null)
    {
        if (amount <= 0)
            return;
        Stats.goalsScored++;
        MatchStatTicker.NoteGoal(this, victim);
    }

    public void RecordGoalConceded(int amount)
    {
        if (amount <= 0)
            return;
        Stats.goalsConceded++;
    }

    public void RecordHazardDamageTaken(int amount)
    {
        if (amount <= 0)
            return;
        Stats.hazardDamageTaken += amount;
    }

    public void RecordKill(Player victim)
    {
        MatchStats s = Stats;
        s.kills++;
        s.currentKillStreak++;
        if (s.currentKillStreak > s.maxKillStreak)
            s.maxKillStreak = s.currentKillStreak;
        if (Time.time - s.lastKillTime < 8f)
            s.multiKills++;
        s.lastKillTime = Time.time;
        if (s.timeToFirstKill < 0f)
            s.timeToFirstKill = MatchStatTicker.MatchTime;
        if (MatchStatTicker.NoteFirstBlood(index))
            s.firstBloods = 1;
        if (victim != null && s.lastKilledByIndex == victim.index)
            s.revengeKills++;
        if (victim != null)
        {
            if (victim.match != null && victim.match.pendingOverkill > 0)
            {
                s.overkillDamage += victim.match.pendingOverkill;
                victim.match.pendingOverkill = 0;
            }
            victim.RecordDeath(this);
        }
    }

    public void RecordDeath(Player killer)
    {
        MatchStats s = Stats;
        if (killer != null)
            s.lastKilledByIndex = killer.index;
        if (s.deathCountedThisLife)
            return;
        s.deathCountedThisLife = true;
        s.deaths++;
        s.currentKillStreak = 0;
        s.reachedOneHpThisLife = false;
        if (s.timeToFirstDeath < 0f)
            s.timeToFirstDeath = MatchStatTicker.MatchTime;
        if (s.currentLifeSeconds > 0f)
        {
            if (s.shortestLife < 0f || s.currentLifeSeconds < s.shortestLife)
                s.shortestLife = s.currentLifeSeconds;
        }
        s.currentLifeSeconds = 0f;
    }

    public void RecordBallHit()
    {
        RecordBallHit(0f, false, false, true, false, 0, 999f);
    }

    public void RecordBallHit(float speed, bool incoming, bool isBump, bool isPaddle, bool isLifeline, int ballId, float distanceToBall)
    {
        MatchStats s = Stats;
        s.ballHits++;
        if (currentHealth <= 0)
            s.afterDeathHits++;
        if (s.timeToFirstHit < 0f)
            s.timeToFirstHit = MatchStatTicker.MatchTime;

        if (isPaddle)
            s.paddleHits++;
        if (isBump)
            s.bumpHits++;
        if (isLifeline)
            s.lifelineTouches++;
        if (incoming)
            s.ballsReturned++;
        else
            s.ballsRedirected++;
        if (MatchStatTicker.IsSmash(speed))
            s.smashHits++;
        if (speed > s.fastestBallHit)
            s.fastestBallHit = speed;
        if (speed > 0.05f && (s.slowestBallHit <= 0f || speed < s.slowestBallHit))
            s.slowestBallHit = speed;

        if (ballId != 0 && ballId == s.lastHitBallId && Time.time - s.lastHitTime < 0.45f)
            s.doubleTouches++;
        s.lastHitBallId = ballId;
        s.lastHitTime = Time.time;
        s.currentRallyTouches++;
        if (s.currentRallyTouches > s.maxRallyTouches)
            s.maxRallyTouches = s.currentRallyTouches;

        MatchStatTicker.NoteClutchSave(this, distanceToBall, speed);
    }

    public void RecordBallOwnershipSeconds(int seconds)
    {
        if (seconds > longestBallOwnership)
            longestBallOwnership = seconds;
    }

    public void RecordBallOwnershipTick(float dt, bool newPossession)
    {
        Stats.totalBallOwnership += dt;
        if (newPossession)
            Stats.timesGainedPossession++;
    }

    public void RecordUltUsed()
    {
        ultsUsed++;
    }

    public void RecordDash()
    {
        numberOfDashes++;
    }

    public void RecordBump()
    {
        Stats.bumpsUsed++;
    }

    public void RecordShieldBlock(int blockedAmount)
    {
        Stats.shieldBlocks++;
        if (blockedAmount > 0)
            Stats.damageBlocked += blockedAmount;
    }

    public void RecordHeal(int amount)
    {
        if (amount <= 0)
            return;
        Stats.healingReceived += amount;
        Stats.damageHealed += amount;
    }

    public void RecordWallBounceAfterHit()
    {
        Stats.wallBouncesAfterHit++;
        Stats.currentRallyTouches++;
        if (Stats.currentRallyTouches > Stats.maxRallyTouches)
            Stats.maxRallyTouches = Stats.currentRallyTouches;
    }

    public void RecordAce()
    {
        Stats.aces++;
    }

    public void RecordLastTouchGoal()
    {
        Stats.lastTouchGoals++;
    }

    public void RecordVolley()
    {
        Stats.volleys++;
    }
    #endregion
}

public enum StatSource
{
    Ball,
    Ability,
    Hazard
}

[System.Serializable]
public class ObjectInfo
{
    public GameObject prefabs;
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;

    public ObjectInfo()
    {

    }

    public ObjectInfo(ObjectInfo oI)
    {
        if (oI == null)
            return;

        prefabs = oI.prefabs;
        positionOffset = oI.positionOffset;
        rotationOffset = oI.rotationOffset;
    }

    public ObjectInfo(string fromString)
    {
        FromString(fromString);
    }

    public override string ToString()
    {
        string result = "";

        result += prefabs.name;
        result += "|";

        result += positionOffset.x.ToString();
        result += "|";
        result += positionOffset.y.ToString();
        result += "|";
        result += positionOffset.z.ToString();
        result += "|";

        result += rotationOffset.x.ToString();
        result += "|";
        result += rotationOffset.y.ToString();
        result += "|";
        result += rotationOffset.z.ToString();
        result += "|";

        return result;
    }

    public void FromString(string str)
    {
        int sai = 0;
        string[] sA = str.Split('|');

        if (sA[sai] != null && sA[sai] != "")
        {
            prefabs = (GameObject)Resources.Load("Parts/" + "/" + sA[sai]);
        }
        sai++;

        positionOffset.x = float.Parse(sA[sai]);
        sai++;
        positionOffset.y = float.Parse(sA[sai]);
        sai++;
        positionOffset.z = float.Parse(sA[sai]);
        sai++;

        rotationOffset.x = float.Parse(sA[sai]);
        sai++;
        rotationOffset.y = float.Parse(sA[sai]);
        sai++;
        rotationOffset.z = float.Parse(sA[sai]);
        sai++;
    }
}

[System.Serializable]
public class Skill
{
    public float amount = 0;
    public float max = 20;
    public float cost = 10;
    public float speed = 50;
    // 0 by default — for Garmen this flag means Drive is ON when >= 1
    public float readyPercent = 0;

    public Skill()
    {

    }

    public Skill(Skill s)
    {
        amount = s.amount;
        max = s.max;
        cost = s.cost;
        speed = s.speed;
        readyPercent = s.readyPercent;
    }

    public void Spend(float lose = float.NaN)
    {
        amount -= (float.IsNaN(lose)) ? cost : lose;
        
        if(amount < 0)
        {
            amount = 0;
        }

        if(amount > max)
        {
            amount = max;
        }
    }

    public void Gain(float gain)
    {
        amount += gain;

        if (amount < 0)
        {
            amount = 0;
        }

        if (amount > max)
        {
            amount = max;
        }
    }

    public void Charge()
    {
        if (!Database.MatchPlayActive)
            return;

        if (amount < max)
        {
            readyPercent += Time.deltaTime;

            if (readyPercent >= speed)
            {
                Gain(1);
                readyPercent = 0;
            }
        }
    }

    public bool Enough()
    {
        return (amount >= cost);
    }
}

[System.Serializable]
public class PlayerConstraints
{
    public GameObject caller;
    public float endTime = 0;

    public PlayerConstraints(GameObject caller, float endTime = -1)
    {
        this.caller = caller;
        this.endTime = endTime;
    }
}

[System.Serializable]
public class PlayerConstraintHolder
{
    public PlayerConstraint constraint;
    public float length = 0;

    public PlayerConstraintHolder(PlayerConstraint theConstraint, float theLength = -1)
    {
        constraint = theConstraint;
        length = theLength;
    }
}

public enum PlayerConstraint
{
    Move,
    Bump,
    Super,
    Dash
}

[System.Serializable]
public class MoveSpeedModifier
{
    public GameObject caller;
    public float endTime = -1f;
    public float factor = 1f;

    public MoveSpeedModifier(GameObject caller, float endTime = -1f, float factor = 1f)
    {
        this.caller = caller;
        this.endTime = endTime;
        this.factor = factor;
    }
}