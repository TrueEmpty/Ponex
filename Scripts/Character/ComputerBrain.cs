using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared computer controller. Drop on any player root (or auto-added for CPU players).
/// Character scripts read Decision / FreeRoam / Candle intents instead of each calling Evaluate.
/// New characters: add this + set Mode — move/dash/super intents come for free.
/// </summary>
[DefaultExecutionOrder(-80)]
[RequireComponent(typeof(PlayerGrab))]
public class ComputerBrain : MonoBehaviour
{
    public enum Mode
    {
        LanePaddle,
        FreeRoam,
        Candle,
        Kunai,
        Skylaar,
        Mothership
    }

    public Mode mode = Mode.LanePaddle;
    public List<string> hitTags = new List<string> { "Wall", "Walls", "Obstacle" };
    public float wallStopDistance = 1.38f;
    public float freeRoamArrive = 0.55f;
    public float freeRoamDashMin = 1.6f;

    PlayerGrab pg;
    Database db;
    Rigidbody rb;
    IyolitMovement iyolit;
    MuriKunai muriKunai;

    public ComputerAI.Decision Lane { get; private set; }
    public ComputerAI.FreeRoamDecision FreeRoam { get; private set; }
    public Thought Thought { get; private set; } = Thought.Nothing;

    public int MoveDir => Lane.moveDir;
    public bool WantBump => Lane.wantBump;
    public bool WantSuper => Lane.wantSuper;
    public bool WantDash => mode == Mode.FreeRoam ? FreeRoam.wantDash : Lane.wantDash;

    void Awake()
    {
        pg = GetComponent<PlayerGrab>();
        rb = GetComponent<Rigidbody>();
        iyolit = GetComponent<IyolitMovement>();
        muriKunai = GetComponent<MuriKunai>();
        mode = DetectMode(mode);
    }

    void Start()
    {
        db = Database.instance;
    }

    void Update()
    {
        if (db == null)
            db = Database.instance;
        if (db == null || !db.gameStart || pg == null || !pg.IsLinked() || pg.player == null)
            return;
        if (!pg.player.computer || pg.player.currentHealth <= 0)
            return;

        switch (mode)
        {
            case Mode.LanePaddle:
                TickLane();
                break;
            case Mode.FreeRoam:
                // NariMove already runs EvaluateFreeRoam — don't double-tick
                if (GetComponent<NariMove>() == null)
                    TickFreeRoam();
                break;
            case Mode.Candle:
                TickCandle();
                break;
            case Mode.Kunai:
                TickKunai();
                break;
            case Mode.Skylaar:
                TickSkylaar();
                break;
            case Mode.Mothership:
                TickMothership();
                break;
        }
    }

    void TickLane()
    {
        Lane = ComputerAI.Evaluate(transform, pg.player, hitTags, wallStopDistance);
        if (Lane.wantBump)
            Thought = Thought.MoveUp;
        else if (Lane.wantSuper)
            Thought = Thought.MoveDown;
        else
            Thought = Thought.Nothing;
    }

    void TickFreeRoam()
    {
        Vector3 cur = rb != null ? rb.linearVelocity.normalized : Vector3.up;
        FreeRoam = ComputerAI.EvaluateFreeRoam(
            transform, pg.player, cur, freeRoamArrive, freeRoamDashMin,
            dir => PaddleWall.WallInDirection(transform, dir.x >= 0f ? 1 : -1, hitTags, wallStopDistance));
        Lane = ComputerAI.GetLastDecision(pg.playerIndex);
        Thought = Thought.Nothing;
    }

    void TickCandle()
    {
        // Iyolit: sit on candles; pick the candle nearest the AI threat aim point
        if (iyolit == null || iyolit.positions == null || iyolit.positions.Count == 0)
            return;
        if (iyolit.moving)
            return;

        Lane = ComputerAI.Evaluate(transform, pg.player, hitTags, wallStopDistance);
        Thought = Lane.wantSuper ? Thought.MoveDown : (Lane.wantBump ? Thought.MoveUp : Thought.Nothing);

        // Prefer predicted bounce on our side via future points already on BallInfo
        Vector3 aim = transform.position;
        if (ComputerAI.TryGetAimPoint(pg.player, transform, out Vector3 threatAim))
            aim = threatAim;

        int best = iyolit.currentPosition;
        float bestScore = float.PositiveInfinity;
        for (int i = 0; i < iyolit.positions.Count; i++)
        {
            Transform t = iyolit.positions[i];
            if (t == null)
                continue;
            float d = (t.position - aim).sqrMagnitude;
            if (d < bestScore)
            {
                bestScore = d;
                best = i;
            }
        }

        if (best != iyolit.currentPosition && !iyolit.moving)
            iyolit.currentPosition = best;

        if (Lane.wantSuper && iyolit != null)
            iyolit.ActivateSuper();
    }

    void TickKunai()
    {
        if (muriKunai == null)
            muriKunai = GetComponent<MuriKunai>();

        Thought = Thought.Nothing;
        if (!ComputerAI.TryGetAimPoint(pg.player, transform, out Vector3 aim))
            return;

        MuriKunaiProjectile stuck = muriKunai != null ? muriKunai.ClosestStuckTo(aim) : null;
        if (stuck != null)
        {
            Vector3 toKunai = stuck.transform.position - transform.position;
            toKunai.z = 0f;
            float ax = Mathf.Abs(toKunai.x);
            float ay = Mathf.Abs(toKunai.y);
            if (ax >= ay)
                Thought = toKunai.x >= 0f ? Thought.MoveRight : Thought.MoveLeft;
            else
                Thought = Thought.MoveUp;
            return;
        }

        Thought = Thought.MoveDown;
    }

    void TickSkylaar()
    {
        Skylaar sky = GetComponent<Skylaar>();
        if (sky == null || !sky.IsReady || pg.inp == null)
            return;
        ClearInputs();
        if (sky.IsFlying)
            DriveFlight(sky);
        else
            DriveFoot(sky);
    }

    void DriveFlight(Skylaar sky)
    {
        Vector3 into = sky.WallInto;
        Vector3 lateral = sky.WallLateral;
        float alt = sky.Altitude;
        float pitch = sky.Pitch;
        if (!ComputerAI.TryGetAimPoint(pg.player, transform, out Vector3 ball))
            return;

        Vector3 to = ball - transform.position;
        to.z = 0f;
        float ahead = Vector3.Dot(to, lateral) * sky.PatrolSign;
        float heightErr = Vector3.Dot(to, into);
        if (ahead < -1.1f && pitch < 155f)
        {
            pg.inp.up = true;
            Thought = Thought.MoveUp;
            return;
        }
        if (pitch >= 155f && pitch < 330f && ahead < 0.5f)
        {
            Thought = Thought.MoveDown;
            return;
        }

        if (to.magnitude < 2.5f && ahead > -0.4f && sky.CanRoll)
        {
            pg.inp.right = true;
            pg.inp.tf_right = true;
            Thought = Thought.MoveRight;
        }

        float aimPitch = 0f;
        if (ahead > 0.35f)
            aimPitch = Mathf.Clamp(Mathf.Atan2(heightErr, Mathf.Max(0.45f, ahead)) * Mathf.Rad2Deg, -45f, 70f);
        if (alt < 1.4f || aimPitch > pitch + 8f)
        {
            pg.inp.up = true;
            Thought = Thought.MoveUp;
        }
        else if (aimPitch < pitch - 8f && alt > 1.8f)
        {
            pg.inp.down = true;
            Thought = Thought.MoveDown;
        }
    }

    void DriveFoot(Skylaar sky)
    {
        float lane = 0f;
        bool toDock = sky.IsCarrying && sky.TryDockLane(out lane);
        bool toScrap = !sky.IsCarrying && sky.TryNearestScrap(out lane);
        if (toDock || toScrap)
        {
            if (lane > 0.3f)
                pg.inp.right = true;
            else if (lane < -0.3f)
                pg.inp.left = true;
            if (toDock && Mathf.Abs(lane) < 1.5f)
                pg.inp.tf_down = true;
            else if (toScrap && Mathf.Abs(lane) < 2.1f)
                pg.inp.tf_down = true;
        }

        if (!ComputerAI.TryGetAimPoint(pg.player, transform, out Vector3 ball))
            return;
        Vector3 to = ball - transform.position;
        to.z = 0f;
        float intoGap = Vector3.Dot(to, sky.WallInto);
        if (to.magnitude > 2.3f || intoGap < -0.3f || intoGap > 2.6f)
            return;
        float side = Vector3.Dot(to, sky.WallLateral);
        if (sky.CanRoll)
        {
            if (side >= 0f)
            {
                pg.inp.dashLeft = true;
                pg.inp.tf_dashLeft = true;
            }
            else
            {
                pg.inp.dashRight = true;
                pg.inp.tf_dashRight = true;
            }
        }
        pg.inp.bumpHeld = true;
        pg.inp.tf_bump = true;
        Thought = Thought.MoveUp;
    }

    void TickMothership()
    {
        Mothership ship = GetComponent<Mothership>();
        if (ship == null || pg.inp == null)
            return;
        ClearInputs();
        bool threat = ComputerAI.TryGetAimPoint(pg.player, transform, out Vector3 ballPos);
        if (!NearestBall(threat ? ballPos : transform.position, out Vector3 nearPos, out Vector3 ballVel))
        {
            if (!threat)
                return;
            ballVel = Vector3.zero;
        }
        else if (!threat)
            ballPos = nearPos;

        Vector3 to = ballPos - transform.position;
        to.z = 0f;
        float dist = to.magnitude;
        if (dist < 0.05f)
            return;
        Vector3 dir = to / dist;
        float signed = Vector3.SignedAngle(ship.GunAim, dir, Vector3.forward);
        if (signed > 6f)
            pg.inp.left = true;
        else if (signed < -6f)
            pg.inp.right = true;

        Vector3 into = PaddleWall.IntoField(pg.player.facing);
        float gap = Vector3.Dot(to, into);
        float reach = db != null ? Mathf.Max(4f, db.FieldPlaySize * 0.28f) : 6f;
        ballVel.z = 0f;
        bool incoming = ballVel.sqrMagnitude > 0.2f && Vector3.Dot(ballVel, -dir) > 0f;
        bool close = gap > 0f && gap < reach && incoming;
        if (close && ship.shieldUnlocked)
        {
            pg.inp.down = true;
            pg.inp.tf_down = true;
            if (ship.shieldCooldownLeft <= 0f)
                pg.inp.tf_bump = true;
            Thought = Thought.MoveDown;
            return;
        }

        pg.inp.up = true;
        pg.inp.tf_up = true;
        if (Mathf.Abs(signed) < 18f && ship.laserCooldownLeft <= 0f)
            pg.inp.tf_bump = true;
        if (Mathf.Abs(signed) < 26f && gap > reach && ship.CanSuper)
            pg.inp.tf_super = true;
        Thought = Thought.MoveUp;
    }

    void ClearInputs()
    {
        Inputs inp = pg.inp;
        inp.left = false;
        inp.right = false;
        inp.up = false;
        inp.down = false;
        inp.dashLeft = false;
        inp.dashRight = false;
        inp.superHeld = false;
        inp.bumpHeld = false;
        inp.tf_left = false;
        inp.tf_right = false;
        inp.tf_up = false;
        inp.tf_down = false;
        inp.tf_dashLeft = false;
        inp.tf_dashRight = false;
        inp.tf_bump = false;
        inp.tf_super = false;
    }

    static bool NearestBall(Vector3 from, out Vector3 pos, out Vector3 vel)
    {
        pos = Vector3.zero;
        vel = Vector3.zero;
        BallInfo[] balls = Object.FindObjectsByType<BallInfo>(FindObjectsInactive.Exclude);
        float best = float.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < balls.Length; i++)
        {
            if (balls[i] == null)
                continue;
            Vector3 p = balls[i].transform.position;
            float d = (p - from).sqrMagnitude;
            Rigidbody body = balls[i].GetComponent<Rigidbody>();
            if (body == null)
                body = balls[i].GetComponentInParent<Rigidbody>();
            if (!found || d < best)
            {
                best = d;
                pos = p;
                vel = body != null ? body.linearVelocity : Vector3.zero;
                found = true;
            }
        }
        return found;
    }

    Mode DetectMode(Mode fallback)
    {
        if (GetComponent<IyolitMovement>() != null)
            return Mode.Candle;
        if (GetComponent<NariMove>() != null)
            return Mode.FreeRoam;
        if (GetComponent<MuriMove>() != null)
            return Mode.Kunai;
        if (GetComponent<Skylaar>() != null)
            return Mode.Skylaar;
        if (GetComponent<Mothership>() != null)
            return Mode.Mothership;
        return fallback;
    }

    /// <summary>Ensure a brain exists on a spawned player (called from Database).</summary>
    public static ComputerBrain Ensure(GameObject root, Mode preferred = Mode.LanePaddle)
    {
        if (root == null)
            return null;
        ComputerBrain brain = root.GetComponent<ComputerBrain>();
        if (brain == null)
            brain = root.AddComponent<ComputerBrain>();
        brain.mode = brain.DetectMode(preferred);
        return brain;
    }
}
