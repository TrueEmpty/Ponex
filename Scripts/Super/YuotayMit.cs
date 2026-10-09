using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Yuotay's mit: always chases the ball on its own (human or CPU), slower than the bat.
/// Catch / throw / ownership are handled by StickOnCollision + DamageOnTagHit.
/// </summary>
public class YuotayMit : MonoBehaviour
{
    Rigidbody rb;
    Database db;
    PlayerGrab pg;
    StickOnCollision stick;

    public List<string> hitTags = new List<string>();

    public float dis = 1.38f;

    [Tooltip("Mit move speed as a fraction of the player's movementSpeed.")]
    public float speedScale = 0.28f;

    [Tooltip("How quickly lateral speed ramps toward the target (units/sec²-ish).")]
    public float accel = 8f;

    [Tooltip("Dead zone along the wall before the mit bothers moving.")]
    public float deadZone = 0.35f;

    [Tooltip("Nudge into the field so raised / extended wall segments sit behind the mit.")]
    public float fieldLift = 0.22f;

    PaddleWall.LaneLock lane;
    bool wallsIgnored;
    bool batIgnored;
    float laneMin;
    float laneMax;
    float laneRefresh;
    float currentSpeed;
    Vector3 cachedChaseTarget;
    bool hasCachedChaseTarget;
    float nextTargetRefreshAt;
    const float TargetRefreshInterval = 0.05f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        stick = GetComponent<StickOnCollision>();
        db = Database.instance;

        if (pg != null && pg.player != null)
            lane.Ensure(rb, pg.player.facing);

        if (rb != null)
        {
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            if (fieldLift != 0f)
            {
                Vector3 lifted = rb.position + transform.up.normalized * fieldLift;
                lifted.z = rb.position.z;
                rb.position = lifted;
                transform.position = lifted;
            }
        }

        IgnoreStadiumAndBat();

        if (GetComponent<DestroyOnDeath>() == null)
            gameObject.AddComponent<DestroyOnDeath>();

        if (stick != null)
        {
            stick.onCatch += OnMitCatch;
            stick.onThrow += OnMitThrow;
        }
    }

    void OnDestroy()
    {
        if (stick != null)
        {
            stick.onCatch -= OnMitCatch;
            stick.onThrow -= OnMitThrow;
        }
    }

    void OnMitCatch()
    {
        YuotaySfx.PlayCatch();
    }

    void OnMitThrow()
    {
        YuotaySfx.PlayThrow();
    }

    void Update()
    {
        if (db == null || pg == null || pg.player == null)
            return;

        lane.Ensure(rb, pg.player.facing);

        if (pg.player.currentHealth <= 0)
        {
            Destroy(gameObject);
            return;
        }

        if (db.gameStart && pg.player.currentHealth > 0)
        {
            IgnoreStadiumAndBat();
            OnMove();
        }
        else if (rb != null)
        {
            currentSpeed = 0f;
            rb.linearVelocity = Vector3.zero;
        }

        ClampToSide();
    }

    void FixedUpdate()
    {
        if (rb != null)
            ClampToSide();
    }

    void ClampToSide()
    {
        lane.Clamp(rb);
        if (pg != null && pg.player != null)
            PaddleWall.ClampToLaneLimits(rb, transform, pg.player.facing, ref laneMin, ref laneMax, ref laneRefresh);
    }

    void IgnoreStadiumAndBat()
    {
        Collider[] self = GetComponentsInChildren<Collider>(true);
        if (!wallsIgnored)
        {
            PaddleWall.IgnoreBoundaryWallCollisions(transform, self);
            wallsIgnored = true;
        }

        if (batIgnored || pg == null || pg.player == null || pg.player.spawnedPlayer == null)
            return;

        Collider[] batCols = pg.player.spawnedPlayer.GetComponentsInChildren<Collider>(true);
        PaddleWall.IgnoreColliderSets(self, batCols);
        batIgnored = true;
    }

    void OnMove()
    {
        Player p = pg.player;
        if (p == null || rb == null)
            return;

        // Holding a catch: ease to a stop
        if (stick != null && stick.stuckObjects.Count > 0)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, accel * 2f * Time.deltaTime);
            ApplySlideVelocity(currentSpeed);
            return;
        }

        float targetSpeed = DesiredSlideSpeed(p);

        if (Mathf.Abs(targetSpeed) > 0.01f)
        {
            int dir = targetSpeed > 0f ? 1 : -1;
            if (AtLaneEnd(dir))
                targetSpeed = 0f;
        }

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.deltaTime);
        ApplySlideVelocity(currentSpeed);
    }

    void ApplySlideVelocity(float signedSpeed)
    {
        Vector3 axis = PaddleWall.AbsAxes(transform.right);
        rb.linearVelocity = axis * signedSpeed;
    }

    /// <summary>
    /// Continuous target speed toward the ball along the wall (not ±1 snaps).
    /// </summary>
    float DesiredSlideSpeed(Player p)
    {
        if (!TryGetChaseTarget(p, out Vector3 target))
            return 0f;

        Vector3 axis = PaddleWall.AbsAxes(transform.right);
        float delta = Vector3.Dot(target - transform.position, axis);
        if (Mathf.Abs(delta) <= deadZone)
            return 0f;

        float maxSpeed = p.EffectiveMovementSpeed * speedScale;
        // Ease off as we near the target so it doesn't jitter at the catch point
        float approach = Mathf.Clamp01(Mathf.Abs(delta) / 1.5f);
        float speed = maxSpeed * Mathf.SmoothStep(0.15f, 1f, approach);
        return Mathf.Sign(delta) * speed;
    }

    bool TryGetChaseTarget(Player p, out Vector3 target)
    {
        if (Time.time < nextTargetRefreshAt)
        {
            target = cachedChaseTarget;
            return hasCachedChaseTarget;
        }

        nextTargetRefreshAt = Time.time + TargetRefreshInterval;
        cachedChaseTarget = transform.position;
        hasCachedChaseTarget = false;

        Vector3 myPos = transform.position;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < LiveBallRegistry.Count; i++)
        {
            BallInfo info = LiveBallRegistry.GetAt(i);
            if (info == null || info.gameObject == null || !info.gameObject.activeInHierarchy)
                continue;
            GameObject go = info.gameObject;

            if (stick != null && stick.stuckObjects.Contains(go))
                continue;

            Rigidbody ballRb = go.GetComponent<Rigidbody>();
            Vector3 pos = go.transform.position;
            Vector3 vel = ballRb != null ? ballRb.linearVelocity : Vector3.zero;

            float toward = BallComingTowardWall(p.facing, vel) ? 40f : 0f;
            float dist = Vector3.Distance(myPos, pos);
            float score = toward - dist + vel.magnitude * 0.1f;

            if (score > bestScore)
            {
                bestScore = score;
                cachedChaseTarget = pos + vel * 0.18f;
                hasCachedChaseTarget = true;
            }
        }

        target = cachedChaseTarget;
        return hasCachedChaseTarget;
    }

    /// <summary>Matches ComputerAI facing conventions (Up = bottom seat, etc.).</summary>
    static bool BallComingTowardWall(Facing facing, Vector3 vel)
    {
        switch (facing)
        {
            case Facing.Up: return vel.y < -0.05f;
            case Facing.Down: return vel.y > 0.05f;
            case Facing.Left: return vel.x > 0.05f;
            case Facing.Right: return vel.x < -0.05f;
            default: return true;
        }
    }

    bool AtLaneEnd(int dir)
    {
        if (pg == null || pg.player == null)
            return false;
        return PaddleWall.LaneEndInDirection(
            transform, pg.player.facing, dir, ref laneMin, ref laneMax, ref laneRefresh);
    }
}
