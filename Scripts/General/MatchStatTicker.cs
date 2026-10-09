using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-frame match stats that aren't event-driven: movement, clocks, close calls, bump spends.
/// </summary>
public static class MatchStatTicker
{
    const float CloseCallDistance = 1.35f;
    const float ClutchDistance = 2.1f;
    const float LastSecondDistance = 1.15f;
    const float MoveEpsilon = 0.04f;
    const float SmashSpeed = 14f;

    public static float MatchTime { get; private set; }
    static int firstBloodIndex = -1;
    static int lastScorerIndex = -1;
    static int lastScorerTeam = int.MinValue;
    static int unansweredRun;
    static bool finalized;

    class TickState
    {
        public Vector3 lastPos;
        public bool hasPos;
        public float lastBump = -1f;
        public int lastHealth = -999;
        public float lastLateral;
        public bool hasLateral;
        public int armedCloseBallId;
        public float armedCloseDist = 999f;
        public bool closeArmed;
    }

    static readonly Dictionary<int, TickState> states = new Dictionary<int, TickState>(8);

    public static void Reset()
    {
        MatchTime = 0f;
        firstBloodIndex = -1;
        lastScorerIndex = -1;
        lastScorerTeam = int.MinValue;
        unansweredRun = 0;
        finalized = false;
        states.Clear();
    }

    public static void Tick(Database db)
    {
        if (db == null || !db.gameStart || db.winnerScreen || db.LocalMatchPaused)
            return;
        if (CharacterCreationManager.IsActive)
            return;

        float dt = Time.deltaTime;
        MatchTime += dt;

        if (db.players == null)
            return;

        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null)
                continue;

            MatchStats s = p.match;
            if (s == null)
                continue;

            s.matchSeconds = MatchTime;
            TickPlayer(p, s, dt);
        }

        TickBalls(db);
    }

    static TickState StateFor(int index)
    {
        if (!states.TryGetValue(index, out TickState st) || st == null)
        {
            st = new TickState();
            states[index] = st;
        }
        return st;
    }

    static void TickPlayer(Player p, MatchStats s, float dt)
    {
        TickState st = StateFor(p.index);
        bool alive = p.currentHealth > 0;

        if (alive)
        {
            s.timeAlive += dt;
            s.currentLifeSeconds += dt;
            if (s.currentLifeSeconds > s.longestLife)
                s.longestLife = s.currentLifeSeconds;
            s.deathCountedThisLife = false;
        }
        else
        {
            s.timeDead += dt;
        }

        if (alive)
        {
            s.currentUndamaged += dt;
            if (s.currentUndamaged > s.longestUndamagedStreak)
                s.longestUndamagedStreak = s.currentUndamaged;
        }

        if (p.currentHealth == 1)
            s.timeAtOneHp += dt;
        if (p.maxHealth > 0 && p.currentHealth >= p.maxHealth && alive)
            s.timeAtFullHp += dt;

        if (s.lowestHpReached > p.currentHealth)
            s.lowestHpReached = p.currentHealth;

        if (st.lastHealth == 1 && p.currentHealth > 1)
            s.recoveriesFromOneHp++;
        if (p.currentHealth == 1 && st.lastHealth != 1 && st.lastHealth != -999)
        {
            if (!s.reachedOneHpThisLife)
            {
                s.timesReachedOneHp++;
                s.reachedOneHpThisLife = true;
            }
        }
        st.lastHealth = p.currentHealth;

        if (p.bump != null)
        {
            float bump = p.bump.amount;
            if (st.lastBump >= 0f)
            {
                float drop = st.lastBump - bump;
                float threshold = p.bump.cost > 0.5f ? p.bump.cost * 0.45f : 0.5f;
                if (drop >= threshold)
                    p.RecordBump();
            }
            st.lastBump = bump;
        }

        Transform body = p.spawnedPlayer != null ? p.spawnedPlayer.transform : null;
        if (body == null)
        {
            st.hasPos = false;
            st.hasLateral = false;
            if (alive)
                s.timeIdle += dt;
            return;
        }

        Vector3 pos = body.position;
        pos.z = 0f;
        if (st.hasPos)
        {
            Vector3 delta = pos - st.lastPos;
            delta.z = 0f;
            float dist = delta.magnitude;
            float spd = dt > 0.0001f ? dist / dt : 0f;
            s.distanceTraveled += dist;
            if (spd > s.maxMoveSpeed)
                s.maxMoveSpeed = spd;

            if (dist > MoveEpsilon)
                s.timeMoving += dt;
            else
                s.timeIdle += dt;

            float lateral = LateralAlongWall(p, pos);
            if (st.hasLateral)
            {
                float step = lateral - st.lastLateral;
                if (Mathf.Abs(step) > 0.08f)
                {
                    float sign = Mathf.Sign(step);
                    if (st.lastLateral != 0f && sign != 0f && Mathf.Sign(st.lastLateral) != 0f
                        && Mathf.Sign(st.lastLateral) != sign && Mathf.Abs(st.lastLateral) > 0.08f)
                        s.directionChanges++;
                    st.lastLateral = step;
                }
            }
            else
            {
                st.lastLateral = 0f;
                st.hasLateral = true;
            }
        }
        else
        {
            st.hasPos = true;
        }
        st.lastPos = pos;
    }

    static float LateralAlongWall(Player p, Vector3 pos)
    {
        if (p.facing == Facing.Left || p.facing == Facing.Right)
            return pos.y;
        return pos.x;
    }

    static void TickBalls(Database db)
    {
        for (int b = 0; b < LiveBallRegistry.Count; b++)
        {
            BallInfo info = LiveBallRegistry.GetAt(b);
            if (info == null || !info.ballReady)
                continue;

            Rigidbody rb = info.GetComponent<Rigidbody>();
            if (rb == null)
                continue;

            Vector3 ballPos = info.transform.position;
            Vector3 vel = rb.linearVelocity;
            vel.z = 0f;
            float speed = vel.magnitude;

            for (int i = 0; i < db.players.Count; i++)
            {
                Player p = db.players[i];
                if (p == null || p.currentHealth <= 0 || p.spawnedPlayer == null)
                    continue;

                MatchStats s = p.match;
                TickState st = StateFor(p.index);
                Vector3 paddle = p.spawnedPlayer.transform.position;
                Vector3 toPaddle = paddle - ballPos;
                toPaddle.z = 0f;
                float dist = toPaddle.magnitude;

                if (speed > s.fastestBallFaced)
                    s.fastestBallFaced = speed;

                bool incoming = IsIncoming(p, vel, toPaddle);
                if (!incoming)
                {
                    if (st.closeArmed && st.armedCloseBallId == info.gameObject.GetEntityId().GetHashCode())
                    {
                        if (st.armedCloseDist < CloseCallDistance
                            && Time.time - s.lastHitTime > 0.2f)
                            s.closeCalls++;
                        st.closeArmed = false;
                    }
                    continue;
                }

                int ballId = info.gameObject.GetEntityId().GetHashCode();
                if (dist < st.armedCloseDist || !st.closeArmed || st.armedCloseBallId != ballId)
                {
                    st.armedCloseDist = dist;
                    st.armedCloseBallId = ballId;
                    st.closeArmed = true;
                }
            }
        }
    }

    static bool IsIncoming(Player p, Vector3 vel, Vector3 toPaddle)
    {
        Vector3 into = IntoField(p.facing);
        return Vector3.Dot(vel, -into) > 0.15f || Vector3.Dot(vel.normalized, toPaddle.normalized) > 0.2f;
    }

    public static Vector3 IntoField(Facing facing)
    {
        switch (facing)
        {
            case Facing.Down: return Vector3.up;
            case Facing.Left: return Vector3.right;
            case Facing.Right: return Vector3.left;
            default: return Vector3.down;
        }
    }

    public static bool NoteFirstBlood(int playerIndex)
    {
        if (firstBloodIndex >= 0)
            return false;
        firstBloodIndex = playerIndex;
        return true;
    }

    public static void NoteGoal(Player scorer, Player victim)
    {
        if (scorer == null)
            return;

        MatchStats s = scorer.match;
        if (s.timeToFirstGoal < 0f)
            s.timeToFirstGoal = MatchTime;

        bool sameRun = lastScorerIndex == scorer.index
            || (lastScorerTeam != int.MinValue && lastScorerTeam == scorer.team);
        if (sameRun)
            unansweredRun++;
        else
            unansweredRun = 1;

        lastScorerIndex = scorer.index;
        lastScorerTeam = scorer.team;
        s.unansweredGoals = unansweredRun;
        if (unansweredRun > s.maxUnansweredGoals)
            s.maxUnansweredGoals = unansweredRun;

        if (victim != null && victim.match != null && victim.match.lastHitBallId != 0
            && Time.time - victim.match.lastHitTime < 2.5f)
            victim.match.errors++;
    }

    public static void NoteClutchSave(Player p, float ballDistance, float ballSpeed)
    {
        if (p == null || p.match == null)
            return;
        if (ballDistance <= LastSecondDistance)
            p.match.lastSecondSaves++;
        else if (ballDistance <= ClutchDistance || ballSpeed >= SmashSpeed)
            p.match.clutchSaves++;
    }

    public static bool IsSmash(float speed) => speed >= SmashSpeed;

    public static void FinalizeMatch(Database db)
    {
        if (finalized || db == null || db.players == null)
            return;
        finalized = true;

        int lastKiller = -1;
        float lastKillAt = -1f;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null || p.match == null)
                continue;
            p.match.matchSeconds = MatchTime;
            p.match.hpRemaining = Mathf.Max(0, p.currentHealth);
            if (p.won && p.match.lowestHpReached <= 1 && p.match.deaths == 0)
                p.match.comebacks = 1;
            if (p.match.lastKillTime > lastKillAt && p.match.kills > 0)
            {
                lastKillAt = p.match.lastKillTime;
                lastKiller = p.index;
            }
        }

        if (lastKiller >= 0)
        {
            for (int i = 0; i < db.players.Count; i++)
            {
                Player p = db.players[i];
                if (p != null && p.index == lastKiller && p.won)
                    p.match.finishingBlows = 1;
            }
        }
    }
}
