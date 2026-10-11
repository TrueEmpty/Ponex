using UnityEngine;

/// <summary>
/// Tic pinball bumper.
/// Loading: rolls down into the plunger bay under strong local gravity.
/// Launched: ball-like motion with low local gravity (always some speed).
/// Entering another Tic's bay steals gravity/ownership unless same-side bays overlap.
/// </summary>
[RequireComponent(typeof(PlayerGrab))]
public class TicBumper : MonoBehaviour
{
    public enum Phase
    {
        Loading,
        Launched
    }

    [Header("Gravity")]
    public float loadGravity = 8f;
    public float launchedGravity = 1.4f;
    public float gravityStrength = 8f; // legacy; overwritten by phase
    public float minLaunchSpeed = 5f;
    public float maxSpeed = 14f;
    public float lateralDampLoad = 0.72f;
    public float bounceRetain = 0.55f;
    [Tooltip("Pull toward the plunger while feeding down the ramp / bay.")]
    public float plungerPull = 1.15f;
    [Tooltip("Seconds after a plunger launch before the bay can reclaim this bumper.")]
    public float launchGrace = 0.85f;
    [Tooltip("Seconds after a plunger launch before local gravity resumes.")]
    public float launchForcePause = 0.4f;

    PlayerGrab pg;
    public Rigidbody rb;
    public ConstantForce constantForceComponent;
    Tic ownerTic;
    int firerIndex = -1;
    bool frozen;
    bool inLaunchArea;
    bool hasEnteredBay;
    float freezeEnd = -1f;
    float launchGraceUntil = -1f;
    float constantForceDisabledUntil = -1f;
    Collider[] cols;
    Phase phase = Phase.Loading;
    TicBumperSpikes spikes;
    TicWingBumper wingForm;
    readonly System.Collections.Generic.List<Collider> ignoredHomeWalls = new System.Collections.Generic.List<Collider>();
    float nextBallKickAt = -1f;

    public int OwnerIndex => pg != null ? pg.playerIndex : -1;
    public int FirerIndex => firerIndex;
    public bool IsFrozen => frozen;
    public bool InLaunchArea => inLaunchArea;
    public bool InLaunchGrace => Time.time < launchGraceUntil;
    public Phase CurrentPhase => phase;
    public Tic OwnerTic => ownerTic;

    public bool CanBeFrozenBy(int playerIndex)
    {
        if (frozen || inLaunchArea || phase == Phase.Loading)
            return false;
        return pg != null && pg.playerIndex == playerIndex;
    }

    public void Init(Tic tic, int ownerIndex)
    {
        ownerTic = tic;
        firerIndex = ownerIndex;
        if (pg == null)
            pg = GetComponent<PlayerGrab>();
        pg.playerIndex = ownerIndex;
        hasEnteredBay = false;
        SetPhase(Phase.Loading);
        FlattenChildBodies();
        TagAsPaddle();
        ApplyHomeWallIgnore(true);
    }

    /// <summary>Kick the bumper down the feed ramp toward the plunger (call right after spawn).</summary>
    public void BeginFeedIntoBay(float kickSpeed = 3.5f)
    {
        SetPhase(Phase.Loading);
        hasEnteredBay = false;
        if (rb == null || ownerTic == null)
            return;

        Vector3 g = GravityDir();
        Vector3 kick = g * kickSpeed;

        // From a side cutout, also nudge toward the plunger / shaft center
        Vector3 rest = ownerTic.GetBayRestPosition();
        Vector3 to = rest - rb.position;
        to.z = 0f;
        Vector3 alongG = g * Vector3.Dot(to, g);
        Vector3 sideways = to - alongG;
        if (sideways.sqrMagnitude > 0.0001f)
            kick += sideways.normalized * (kickSpeed * 0.85f);

        kick.z = 0f;
        rb.isKinematic = false;
        rb.linearVelocity = kick;
    }

    void Awake()
    {
        pg = GetComponent<PlayerGrab>();
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        if (constantForceComponent == null)
            constantForceComponent = GetComponent<ConstantForce>();
        if (constantForceComponent == null)
            constantForceComponent = gameObject.AddComponent<ConstantForce>();
        FlattenChildBodies();
        TagAsPaddle();
        EnsureHubCollider();
        cols = GetComponentsInChildren<Collider>(true);
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezePositionZ
            | RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationY;

        // Four-point piston expand (same for basic + wing bumpers)
        spikes = GetComponent<TicBumperSpikes>();
        if (spikes == null)
            spikes = gameObject.AddComponent<TicBumperSpikes>();
        // Match flat Tic bumper body proportions
        spikes.spikeScale = new Vector3(0.28f, 0.09f, 0.28f);
        spikes.outDistance = 0.34f;
        spikes.EnsureBuilt();
        wingForm = GetComponent<TicWingBumper>();
    }

    void FixedUpdate()
    {
        if (!Database.MatchPlayActive)
        {
            DisableConstantForce();
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            return;
        }

        if (frozen)
        {
            DisableConstantForce();
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            if (freezeEnd > 0f && Time.time >= freezeEnd)
                Destroy(gameObject);
            return;
        }

        if (rb == null || rb.isKinematic)
        {
            DisableConstantForce();
            return;
        }

        // Wing super freeze — do not overwrite with gravity / min-speed
        if (wingForm != null && wingForm.IsHoldingSuper)
        {
            DisableConstantForce();
            return;
        }

        // Stay Loading in the shaft even if the bay overlap flickers. Only free-fly
        // after a plunger launch or after clearly leaving the machine.
        if (phase == Phase.Loading && !inLaunchArea && !InLaunchGrace && hasEnteredBay && ClearlyLeftBay())
            SetPhase(Phase.Launched);

        Vector3 g = GravityDir();
        float dt = Time.fixedDeltaTime;
        float grav = phase == Phase.Loading ? loadGravity : launchedGravity;
        gravityStrength = grav;
        ApplyConstantForce(g, grav);

        Vector3 v = rb.linearVelocity;
        v.z = 0f;

        if (phase == Phase.Loading)
        {
            // Roll down the shaft / side cutout toward the plunger like a ball
            float along = Vector3.Dot(v, g);
            Vector3 lateral = v - g * along;
            if (along < 0f)
                along = 0f;
            lateral *= lateralDampLoad;
            v = g * along + lateral;

            if (ownerTic != null)
            {
                Vector3 toPlunger = ownerTic.GetBayRestPosition() - rb.position;
                toPlunger.z = 0f;
                Vector3 alongG = g * Vector3.Dot(toPlunger, g);
                Vector3 sideways = toPlunger - alongG;

                // Strong lateral pull so side-ramp spawns slide into the shaft
                if (sideways.sqrMagnitude > 0.0001f)
                    v += sideways.normalized * (grav * plungerPull * dt);

                // Also ease along gravity toward the rest if we're above it on the ramp
                float alongTo = Vector3.Dot(toPlunger, g);
                if (alongTo > 0.05f)
                    v += g * (grav * 0.35f * dt);
            }
        }
        else
        {
            // Wing form: keep ball-like floor speed until fully free of the bay
            bool wingFree = wingForm != null && wingForm.FreeFlight;
            if (wingForm == null || !wingFree)
            {
                float speed = v.magnitude;
                float floor = wingForm != null ? Mathf.Max(4f, minLaunchSpeed) : minLaunchSpeed;
                if (speed < floor)
                {
                    Vector3 dir = speed > 0.01f ? v.normalized : (Vector3.Cross(g, Vector3.forward).normalized);
                    if (dir.sqrMagnitude < 0.01f)
                        dir = Vector3.right;
                    // Prefer out of bay along owner up when still clearing
                    if (wingForm != null && ownerTic != null && inLaunchArea)
                        dir = ownerTic.transform.up;
                    v = dir * floor;
                }
            }
        }

        if (v.sqrMagnitude > maxSpeed * maxSpeed)
            v = v.normalized * maxSpeed;

        rb.linearVelocity = v;
    }

    void ApplyConstantForce(Vector3 gravityDirection, float acceleration)
    {
        if (constantForceComponent == null || rb == null)
            return;

        if (Time.time < constantForceDisabledUntil)
        {
            DisableConstantForce();
            return;
        }

        constantForceComponent.force = gravityDirection * acceleration * rb.mass;
        constantForceComponent.relativeForce = Vector3.zero;
        constantForceComponent.torque = Vector3.zero;
        constantForceComponent.relativeTorque = Vector3.zero;
        constantForceComponent.enabled = true;
    }

    void DisableConstantForce()
    {
        if (constantForceComponent == null)
            return;
        constantForceComponent.force = Vector3.zero;
        constantForceComponent.relativeForce = Vector3.zero;
        constantForceComponent.torque = Vector3.zero;
        constantForceComponent.relativeTorque = Vector3.zero;
        constantForceComponent.enabled = false;
    }

    public void SuspendConstantForce(float seconds = -1f)
    {
        float duration = seconds >= 0f ? seconds : launchForcePause;
        constantForceDisabledUntil = Mathf.Max(
            constantForceDisabledUntil,
            Time.time + Mathf.Max(0f, duration));
        DisableConstantForce();
    }

    public Vector3 GravityDir()
    {
        if (ownerTic != null)
            return (-ownerTic.transform.up).normalized;
        if (pg != null && pg.player != null && pg.player.spawnedPlayer != null)
            return (-pg.player.spawnedPlayer.transform.up).normalized;
        return Vector3.down;
    }

    public void SetPhase(Phase p)
    {
        phase = p;
        gravityStrength = p == Phase.Loading ? loadGravity : launchedGravity;
        ApplyHomeWallIgnore(p == Phase.Loading);
    }

    bool ClearlyLeftBay()
    {
        if (rb == null)
            return false;
        if (ownerTic == null)
            return hasEnteredBay && !inLaunchArea;
        Vector3 rest = ownerTic.GetBayRestPosition();
        float along = Vector3.Dot(rb.position - rest, ownerTic.transform.up);
        return along > 1.6f;
    }

    void FlattenChildBodies()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody child = bodies[i];
            if (child == null || child == rb)
                continue;
            child.detectCollisions = false;
            child.isKinematic = true;
            Destroy(child);
        }
        cols = GetComponentsInChildren<Collider>(true);
    }

    void EnsureHubCollider()
    {
        if (GetComponent<Collider>() != null)
            return;
        SphereCollider hub = gameObject.AddComponent<SphereCollider>();
        hub.radius = 0.22f;
        hub.center = Vector3.zero;
    }

    void TagAsPaddle()
    {
        gameObject.tag = "Paddle";
        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null)
                all[i].gameObject.tag = "Paddle";
        }
    }

    void ApplyHomeWallIgnore(bool ignore)
    {
        if (cols == null)
            cols = GetComponentsInChildren<Collider>(true);

        if (!ignore)
        {
            for (int i = 0; i < ignoredHomeWalls.Count; i++)
            {
                Collider wall = ignoredHomeWalls[i];
                if (wall == null)
                    continue;
                for (int c = 0; c < cols.Length; c++)
                {
                    if (cols[c] != null)
                        Physics.IgnoreCollision(cols[c], wall, false);
                }
            }
            ignoredHomeWalls.Clear();
            return;
        }

        if (ownerTic == null)
            return;

        Vector3 towardHome = -ownerTic.transform.up;
        Vector3 origin = ownerTic.GetBayRestPosition();
        Collider[] walls = Physics.OverlapSphere(origin, 2.4f, ~0, QueryTriggerInteraction.Ignore);
        for (int w = 0; w < walls.Length; w++)
        {
            Collider wall = walls[w];
            if (wall == null || wall.isTrigger)
                continue;
            if (wall.transform == ownerTic.transform || wall.transform.IsChildOf(ownerTic.transform))
                continue;
            if (wall.transform == transform || wall.transform.IsChildOf(transform))
                continue;
            string tag = wall.tag;
            if (tag != "Wall" && tag != "Walls" && tag != "Obstacle")
                continue;
            if (Vector3.Dot(wall.bounds.center - origin, towardHome) <= 0.05f)
                continue;
            if (ignoredHomeWalls.Contains(wall))
                continue;
            ignoredHomeWalls.Add(wall);
            for (int c = 0; c < cols.Length; c++)
            {
                if (cols[c] != null)
                    Physics.IgnoreCollision(cols[c], wall, true);
            }
        }
    }

    public void SetInLaunchArea(bool inside, Tic areaOwner)
    {
        bool wasInside = inLaunchArea;
        inLaunchArea = inside;
        if (inside)
            hasEnteredBay = true;

        if (!inside || areaOwner == null || pg == null)
            return;

        // Just launched — keep flying out; don't snap back to Loading / high gravity
        if (InLaunchGrace)
            return;

        if (!CanClaimOwnership(areaOwner))
            return;

        if (pg.playerIndex != areaOwner.PlayerIndex)
        {
            // Wing form players keep their own index (they are the controllable bumper)
            if (GetComponent<TicWingBumper>() == null)
            {
                pg.playerIndex = areaOwner.PlayerIndex;
                ownerTic = areaOwner;
            }
            else if (ownerTic == null)
            {
                ownerTic = areaOwner;
            }
        }
        else if (ownerTic == null)
        {
            ownerTic = areaOwner;
        }

        // Still punching out of the bay — don't reclaim until we settle / re-enter slowly
        if (phase == Phase.Launched && rb != null && ownerTic != null)
        {
            float outSpeed = Vector3.Dot(rb.linearVelocity, ownerTic.transform.up);
            if (outSpeed > 1.5f)
                return;
        }

        // Settling into a bay → loading again
        if (!wasInside || phase == Phase.Launched)
            SetPhase(Phase.Loading);
    }

    /// <summary>
    /// Same-side Tics with overlapping launch areas do not steal bumpers from each other.
    /// </summary>
    public static bool CanClaimOwnership(Tic currentOwner, Tic claimant)
    {
        if (claimant == null)
            return false;
        if (currentOwner == null || currentOwner == claimant)
            return true;

        Player a = currentOwner.GetComponent<PlayerGrab>() != null
            ? currentOwner.GetComponent<PlayerGrab>().player
            : null;
        Player b = claimant.GetComponent<PlayerGrab>() != null
            ? claimant.GetComponent<PlayerGrab>().player
            : null;

        if (a != null && b != null && a.facing == b.facing)
        {
            if (LaunchAreasOverlap(currentOwner.launchArea, claimant.launchArea))
                return false;
        }
        return true;
    }

    bool CanClaimOwnership(Tic claimant)
    {
        return CanClaimOwnership(ownerTic, claimant);
    }

    public static bool LaunchAreasOverlap(TicLaunchArea a, TicLaunchArea b)
    {
        if (a == null || b == null)
            return false;
        Collider ca = a.GetComponent<Collider>();
        Collider cb = b.GetComponent<Collider>();
        if (ca == null || cb == null)
            return false;
        return ca.bounds.Intersects(cb.bounds);
    }

    public bool TryFreeze(int byPlayerIndex, float duration)
    {
        if (!CanBeFrozenBy(byPlayerIndex))
            return false;

        frozen = true;
        freezeEnd = Time.time + duration;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        if (spikes == null)
            spikes = GetComponent<TicBumperSpikes>();
        if (spikes != null)
            spikes.Pop();
        return true;
    }

    public void Launch(Vector3 worldVelocity)
    {
        if (frozen)
            return;
        rb.isKinematic = false;
        SetPhase(Phase.Launched);
        launchGraceUntil = Time.time + launchGrace;
        SuspendConstantForce();
        worldVelocity.z = 0f;
        if (worldVelocity.magnitude < minLaunchSpeed)
            worldVelocity = worldVelocity.sqrMagnitude > 0.001f
                ? worldVelocity.normalized * minLaunchSpeed
                : GravityDir() * -minLaunchSpeed; // out of bay = opposite gravity
        // Out of bay is along owner up (away from wall)
        if (ownerTic != null && Vector3.Dot(worldVelocity, ownerTic.transform.up) < 0f)
            worldVelocity = ownerTic.transform.up * worldVelocity.magnitude;
        rb.linearVelocity = worldVelocity;
        inLaunchArea = false;
        ApplyHomeWallIgnore(false);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;

        if (collision.collider.tag == "Lifeline")
        {
            // Wing player form handles its own HP — regular bumpers die
            if (wingForm == null && GetComponent<TicWingBumper>() == null)
                Destroy(gameObject);
            return;
        }

        Rigidbody ballRb = collision.rigidbody;
        if (ballRb == null)
            ballRb = collision.collider.attachedRigidbody;
        bool isBall = collision.collider.CompareTag("Ball")
            || (ballRb != null && ballRb.CompareTag("Ball"))
            || collision.collider.GetComponentInParent<BallInfo>() != null
            || collision.collider.GetComponentInParent<BallMovement>() != null;

        // Frozen bumper: ball hit → spike pop + knock ball away fast
        if (frozen)
        {
            if (isBall && ballRb != null)
            {
                if (spikes == null)
                    spikes = GetComponent<TicBumperSpikes>();
                Vector3 hit = collision.contactCount > 0
                    ? collision.GetContact(0).point
                    : ballRb.position;
                if (spikes != null)
                    spikes.PopAndKnockBall(ballRb, hit);
            }
            return;
        }

        // Live bumper acts like a pinball bumper once it is in play
        if (isBall && ballRb != null && phase == Phase.Launched && Time.time >= nextBallKickAt)
        {
            nextBallKickAt = Time.time + 0.12f;
            if (spikes == null)
                spikes = GetComponent<TicBumperSpikes>();
            Vector3 hit = collision.contactCount > 0
                ? collision.GetContact(0).point
                : ballRb.position;
            if (spikes != null)
                spikes.PopAndKnockBall(ballRb, hit);
            else
            {
                Vector3 away = ballRb.position - transform.position;
                away.z = 0f;
                if (away.sqrMagnitude < 0.0001f)
                    away = Vector3.up;
                ballRb.linearVelocity = away.normalized * Mathf.Max(10f, ballRb.linearVelocity.magnitude);
            }
        }

        if (collision.contactCount <= 0 || rb == null || rb.isKinematic)
            return;

        Vector3 n = collision.GetContact(0).normal;
        n.z = 0f;
        Vector3 g = GravityDir();

        // Loading: kill bounce straight up the shaft, but still allow side / wall bounces
        if (phase == Phase.Loading && Vector3.Dot(n, -g) > 0.55f)
        {
            Vector3 v = rb.linearVelocity;
            float up = Vector3.Dot(v, -g);
            if (up > 0f)
                rb.linearVelocity = v - (-g) * up;
            // Keep a little lateral bounce so side cutouts don't stick
            float into = Vector3.Dot(rb.linearVelocity, n);
            if (into < 0f)
                rb.linearVelocity -= n * into * 0.35f;
            return;
        }

        // Stronger bounce while clearing the bay so wing/bumper doesn't stick on walls
        float retain = bounceRetain;
        if (inLaunchArea || InLaunchGrace)
            retain = Mathf.Max(retain, 0.9f);

        Vector3 vel = rb.linearVelocity;
        float intoN = Vector3.Dot(vel, n);
        if (intoN < 0f)
            rb.linearVelocity = vel - n * intoN * (1f + retain);
    }

    void OnDestroy()
    {
        if (ownerTic != null)
            ownerTic.UnregisterBumper(this);
    }

    public void IgnoreColliders(Collider[] other, bool ignore)
    {
        if (cols == null)
            cols = GetComponentsInChildren<Collider>(true);
        if (cols == null || other == null)
            return;
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null)
                continue;
            for (int j = 0; j < other.Length; j++)
            {
                if (other[j] == null)
                    continue;
                Physics.IgnoreCollision(cols[i], other[j], ignore);
            }
        }
    }
}
