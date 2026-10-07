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

    float wallCoord;
    bool wallReady;
    float currentSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        stick = GetComponent<StickOnCollision>();
        db = Database.instance;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

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

        EnsureWallLock(pg.player);

        if (db.gameStart && pg.player.currentHealth > 0)
            OnMove();
        else if (rb != null)
        {
            currentSpeed = 0f;
            rb.linearVelocity = Vector3.zero;
        }

        ClampToWall(pg.player);
    }

    void FixedUpdate()
    {
        if (pg != null && pg.player != null)
            ClampToWall(pg.player);
    }

    void EnsureWallLock(Player p)
    {
        if (wallReady || p == null || rb == null)
            return;

        switch (p.facing)
        {
            case Facing.Left:
            case Facing.Right:
                rb.constraints = RigidbodyConstraints.FreezePositionX
                    | RigidbodyConstraints.FreezePositionZ
                    | RigidbodyConstraints.FreezeRotation;
                wallCoord = rb.position.x;
                break;
            default:
                rb.constraints = RigidbodyConstraints.FreezePositionY
                    | RigidbodyConstraints.FreezePositionZ
                    | RigidbodyConstraints.FreezeRotation;
                wallCoord = rb.position.y;
                break;
        }

        wallReady = true;
    }

    void ClampToWall(Player p)
    {
        if (!wallReady || p == null || rb == null)
            return;

        Vector3 pos = rb.position;
        Vector3 vel = rb.linearVelocity;

        switch (p.facing)
        {
            case Facing.Left:
            case Facing.Right:
                pos.x = wallCoord;
                vel.x = 0f;
                break;
            default:
                pos.y = wallCoord;
                vel.y = 0f;
                break;
        }

        rb.position = pos;
        rb.linearVelocity = vel;
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
            if (WallInDirection(dir))
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
        target = transform.position;
        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        if (balls == null || balls.Length == 0)
            return false;

        Vector3 myPos = transform.position;
        float bestScore = float.NegativeInfinity;
        bool found = false;

        for (int i = 0; i < balls.Length; i++)
        {
            GameObject go = balls[i];
            if (go == null)
                continue;

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
                target = pos + vel * 0.18f;
                found = true;
            }
        }

        return found;
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

    bool WallInDirection(int dir)
    {
        return PaddleWall.WallInDirection(transform, dir, hitTags, dis);
    }
}
