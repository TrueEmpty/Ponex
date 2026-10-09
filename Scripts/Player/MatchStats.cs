using System.Text;
using UnityEngine;

/// <summary>Per-player end-of-match stat card. Reset between matches.</summary>
[System.Serializable]
public class MatchStats
{
    // Existing card stats
    public int damageDealt;
    public int damageTaken;
    public int ballHits;
    public int longestBallOwnership;
    public int highestSingleDamgeDealt;
    public int highestSingleDamageTaken;
    public int ultsUsed;
    public int numberOfDashes;
    public int afterDeathHits;
    public int afterDeathDamage;

    // Combat
    public int goalsScored;
    public int goalsConceded;
    public int kills;
    public int deaths;
    public int firstBloods;
    public int finishingBlows;
    public int maxKillStreak;
    public int multiKills;
    public int revengeKills;
    public int overkillDamage;
    public int damageHealed;
    public int healingReceived;
    public int hazardDamageTaken;
    public int hazardDamageDealt;
    public int abilityDamageDealt;
    public int damageDealtAtOneHp;
    public int damageTakenAtOneHp;
    public int lowestHpReached = 999;
    public int timesReachedOneHp;
    public int recoveriesFromOneHp;
    public int unansweredGoals;
    public int maxUnansweredGoals;
    public int hpRemaining;
    public int comebacks;

    // Ball
    public float totalBallOwnership;
    public int timesGainedPossession;
    public float fastestBallHit;
    public float fastestBallFaced;
    public float slowestBallHit;
    public int paddleHits;
    public int bumpHits;
    public int lifelineTouches;
    public int smashHits;
    public int volleys;
    public int aces;
    public int lastTouchGoals;
    public int errors;
    public int doubleTouches;
    public int maxRallyTouches;
    public int wallBouncesAfterHit;
    public int ballsReturned;
    public int ballsRedirected;
    public int closeCalls;
    public int clutchSaves;
    public int lastSecondSaves;

    // Movement / time
    public float distanceTraveled;
    public float timeAlive;
    public float timeDead;
    public float timeMoving;
    public float timeIdle;
    public float timeAtOneHp;
    public float timeAtFullHp;
    public float maxMoveSpeed;
    public int directionChanges;
    public int bumpsUsed;
    public int shieldBlocks;
    public int damageBlocked;
    public float matchSeconds;
    public float timeToFirstHit = -1f;
    public float timeToFirstGoal = -1f;
    public float timeToFirstDeath = -1f;
    public float timeToFirstKill = -1f;
    public float longestLife;
    public float shortestLife = -1f;
    public float longestUndamagedStreak;

    // Runtime (not shown raw)
    public int currentKillStreak;
    public float lastKillTime = -999f;
    public int lastKilledByIndex = -1;
    public int lastHitBallId;
    public float lastHitTime = -999f;
    public int currentRallyTouches;
    public float currentLifeSeconds;
    public float currentUndamaged;
    public bool deathCountedThisLife;
    public bool reachedOneHpThisLife;
    public int pendingOverkill;

    public void Reset()
    {
        damageDealt = 0;
        damageTaken = 0;
        ballHits = 0;
        longestBallOwnership = 0;
        highestSingleDamgeDealt = 0;
        highestSingleDamageTaken = 0;
        ultsUsed = 0;
        numberOfDashes = 0;
        afterDeathHits = 0;
        afterDeathDamage = 0;
        goalsScored = 0;
        goalsConceded = 0;
        kills = 0;
        deaths = 0;
        firstBloods = 0;
        finishingBlows = 0;
        maxKillStreak = 0;
        multiKills = 0;
        revengeKills = 0;
        overkillDamage = 0;
        damageHealed = 0;
        healingReceived = 0;
        hazardDamageTaken = 0;
        hazardDamageDealt = 0;
        abilityDamageDealt = 0;
        damageDealtAtOneHp = 0;
        damageTakenAtOneHp = 0;
        lowestHpReached = 999;
        timesReachedOneHp = 0;
        recoveriesFromOneHp = 0;
        unansweredGoals = 0;
        maxUnansweredGoals = 0;
        hpRemaining = 0;
        comebacks = 0;
        totalBallOwnership = 0f;
        timesGainedPossession = 0;
        fastestBallHit = 0f;
        fastestBallFaced = 0f;
        slowestBallHit = 0f;
        paddleHits = 0;
        bumpHits = 0;
        lifelineTouches = 0;
        smashHits = 0;
        volleys = 0;
        aces = 0;
        lastTouchGoals = 0;
        errors = 0;
        doubleTouches = 0;
        maxRallyTouches = 0;
        wallBouncesAfterHit = 0;
        ballsReturned = 0;
        ballsRedirected = 0;
        closeCalls = 0;
        clutchSaves = 0;
        lastSecondSaves = 0;
        distanceTraveled = 0f;
        timeAlive = 0f;
        timeDead = 0f;
        timeMoving = 0f;
        timeIdle = 0f;
        timeAtOneHp = 0f;
        timeAtFullHp = 0f;
        maxMoveSpeed = 0f;
        directionChanges = 0;
        bumpsUsed = 0;
        shieldBlocks = 0;
        damageBlocked = 0;
        matchSeconds = 0f;
        timeToFirstHit = -1f;
        timeToFirstGoal = -1f;
        timeToFirstDeath = -1f;
        timeToFirstKill = -1f;
        longestLife = 0f;
        shortestLife = -1f;
        longestUndamagedStreak = 0f;
        currentKillStreak = 0;
        lastKillTime = -999f;
        lastKilledByIndex = -1;
        lastHitBallId = 0;
        lastHitTime = -999f;
        currentRallyTouches = 0;
        currentLifeSeconds = 0f;
        currentUndamaged = 0f;
        deathCountedThisLife = false;
        reachedOneHpThisLife = false;
        pendingOverkill = 0;
    }

    static readonly StringBuilder sb = new StringBuilder(2048);

    public string FormatWinScreen(Player p)
    {
        float minutes = Mathf.Max(matchSeconds / 60f, 1f / 60f);
        int dpm = Mathf.RoundToInt(damageDealt / minutes);
        int hpm = Mathf.RoundToInt(ballHits / minutes);
        bool perfect = damageTaken == 0 && (p == null || p.currentHealth > 0 || hpRemaining > 0);
        bool comeback = p != null && p.won && lowestHpReached <= 1 && deaths == 0;

        sb.Length = 0;
        Line("HP Remaining", hpRemaining > 0 ? hpRemaining : (p != null ? Mathf.Max(0, p.currentHealth) : 0));
        Line("Damage Dealt", damageDealt);
        Line("Damage Taken", damageTaken);
        Line("Damage / Min", dpm);
        Line("Biggest Hit Dealt", highestSingleDamgeDealt);
        Line("Biggest Hit Taken", highestSingleDamageTaken);
        Line("Goals Scored", goalsScored);
        Line("Goals Against", goalsConceded);
        Line("Aces", aces);
        Line("Score Run", unansweredGoals);
        Line("Best Score Run", maxUnansweredGoals);
        Line("Giveaways", errors);
        Line("Eliminations", kills);
        Line("Times KO'd", deaths);
        Line("Ball Hits", ballHits);
        Line("Hits / Min", hpm);
        Line("Paddle Hits", paddleHits);
        Line("Bump Hits", bumpHits);
        Line("Lifeline Contacts", lifelineTouches);
        Line("Smash Hits", smashHits);
        Line("Volleys", volleys);
        Line("Returns", ballsReturned);
        Line("Redirects", ballsRedirected);
        Line("Double Touches", doubleTouches);
        Line("Longest Rally", maxRallyTouches);
        Line("Banks After Hit", wallBouncesAfterHit);
        Line("Longest Ball Ownership", longestBallOwnership + "s");
        Line("Total Ball Ownership", Sec(totalBallOwnership));
        Line("Possessions", timesGainedPossession);
        Line("Fastest Ball Hit", Spd(fastestBallHit));
        Line("Slowest Ball Hit", slowestBallHit > 0f ? Spd(slowestBallHit) : "—");
        Line("Fastest Ball Faced", Spd(fastestBallFaced));
        Line("Close Calls", closeCalls);
        Line("Clutch Saves", clutchSaves);
        Line("Last-Second Saves", lastSecondSaves);
        Line("Ults used", ultsUsed);
        Line("Bumps Used", bumpsUsed);
        Line("# of Dashes", numberOfDashes);
        Line("Shield Blocks", shieldBlocks);
        Line("Damage Blocked", damageBlocked);
        Line("Healing Received", healingReceived);
        Line("Hazard Damage Taken", hazardDamageTaken);
        Line("Damage at 1 HP (dealt)", damageDealtAtOneHp);
        Line("Damage at 1 HP (taken)", damageTakenAtOneHp);
        Line("Lowest HP Reached", lowestHpReached >= 999 ? (p != null ? p.maxHealth : 0) : lowestHpReached);
        Line("Times at 1 HP", timesReachedOneHp);
        Line("1 HP Recoveries", recoveriesFromOneHp);
        Line("After Death Hits", afterDeathHits);
        Line("After Death Damage Dealt", afterDeathDamage);
        Line("Time Alive", Sec(timeAlive));
        Line("Time Moving", Sec(timeMoving));
        Line("Time Idle", Sec(timeIdle));
        Line("Time at 1 HP", Sec(timeAtOneHp));
        Line("Time at Full HP", Sec(timeAtFullHp));
        Line("Longest Clean Streak", Sec(longestUndamagedStreak));
        Line("Distance Traveled", distanceTraveled.ToString("0.0"));
        Line("Direction Changes", directionChanges);
        Line("Time to First Hit", timeToFirstHit >= 0f ? Sec(timeToFirstHit) : "—");
        Line("Time to First Goal", timeToFirstGoal >= 0f ? Sec(timeToFirstGoal) : "—");
        Line("Perfect Defense", perfect ? "Yes" : "No");
        Line("Comeback", comeback ? "Yes" : "No");
        return sb.ToString();
    }

    static void Line(string label, object value)
    {
        sb.Append(label);
        sb.Append(": ");
        sb.Append(value);
        sb.Append('\n');
    }

    static string Sec(float t) => Mathf.Max(0f, t).ToString("0.0") + "s";
    static string Spd(float s) => Mathf.Max(0f, s).ToString("0.0");
}
