using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CelarusPhaseLock
{
    Cycle = 0,
    ForceSun = 1,
    ForceMoon = 2,
}

public class Celarus : MonoBehaviour
{
    Rigidbody rb;
    Database db;
    PlayerGrab pg;

    Transform spinPoint;

    public bool day = false;
    public float dayCycle = 20;
    public float spinSpeed = 50;

    [Tooltip("Cycle = normal day/night. ForceSun / ForceMoon = Sunshine / Moonlight Celarus.")]
    public CelarusPhaseLock phaseLock = CelarusPhaseLock.Cycle;

    [SerializeField]
    float cycle = 0;

    bool phaseLockApplied;

    DamageOnTagHit sunHit;
    PullObjectIn sunGravity;

    DamageOnTagHit moonHit;
    PullObjectIn moonGravity;

    [SerializeField]
    bool moving = false;

    public Vector3 leavePoint = new Vector3(0,-1,-4.5f);
    public float leaveSpeed = 50;

    public Vector3 solarFlareSpeed;
    public GameObject solarFlare;
    public float sunRight = 0;
    public float sunMiddle = 0;
    public float sunLeft = 0;

    public Vector3 sFsideDir = new Vector3(3.5f,3.5f);
    public float sFResetCount = 2;
    public float sFoffset = 4;
    public float sFoffsetAngle = 35;
    [Tooltip("Legacy field — moon skate uses moonPullAccel instead.")]
    public float moonGravityMultiplier = .5f;

    [Header("Moon Skate")]
    public float moonPullAccel = 55f;
    public float skateAccel = 70f;
    public float airSteerAccel = 48f;
    public float maxSkateSpeed = 40f;
    [Tooltip("Air max tangential speed as a fraction of maxSkateSpeed (1 = full skate speed left/right).")]
    public float airSpeedScale = 1f;
    public float starRadius = 0f;
    public float surfaceRide = -0.12f;
    public float spinDuration = 0.35f;
    public float spinCooldownTime = 1.1f;
    public float slamSpeed = 48f;
    public float slamCooldownTime = 1.25f;
    [Header("Star Dash")]
    [Tooltip("Short left/right burst while skating. Cooldown blocks spam.")]
    public float dashSpeed = 34f;
    public float dashDuration = 0.12f;
    public float dashCooldownTime = 1.4f;
    public float bounceRestitution = 0.9f;
    public float maxBounceBoost = 1.55f;
    public float slamBounceBonus = 1.25f;
    public float groundFriction = 0.15f;
    public float launchAssist = 1.15f;
    public float launchImpulse = 3.2f;
    public float launchSpeedFactor = 0.055f;
    public float maxLaunchOutSpeed = 12.5f;
    public float launchGraceTime = 0.12f;
    public float peelLift = 8f;
    public float peelMinSpeed = 8f;
    public float maxOuterOutSpeed = 5.5f;
    [Tooltip("Core gravity well radius. 0 = use Moon PullObjectIn range.")]
    public float pullRadius = 0f;
    [Tooltip("Pull always applied outside/far from the moon (fraction of moonPullAccel).")]
    public float outerPullScale = 0.28f;
    [Tooltip("How fast inner gravity ramps up once inside the well (higher = quicker climb).")]
    public float pullClimbRate = 2.8f;

    [Header("Moon Residue")]
    public float spinResidueDuration = 1.5f;
    public float dropTrailDuration = 0.75f;
    [Tooltip("Distance between drop-trail samples as a multiple of star diameter.")]
    public float dropTrailSpacingScale = 0.85f;
    public float dropTrailPullStrength = 2600f;
    public float dropTrailPullRadiusScale = 3.5f;

    bool onMoon;
    bool slamming;
    bool dashing;
    float spinLeft;
    float spinCooldown;
    float slamCooldown;
    float dashCooldown;
    float dashLeft;
    int dashDir;
    float launchGrace;
    float pullBlend;
    Transform visual;
    float slamPathAcc;
    bool wasSlamming;
    Vector3 lastDropEmitPos;
    readonly System.Collections.Generic.List<CelarusResidue> activeDropResidues = new System.Collections.Generic.List<CelarusResidue>(16);

    // Buffered from Update so FixedUpdate never misses press edges
    int pendingMoveDir;
    bool pendingSpin;
    bool pendingSlam;
    int pendingDashDir;

    GameObject sharedLifelineOverride;
    bool shareDeathHandled;

    [SerializeField]
    Thought thought = Thought.Nothing;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;

        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        // Massive authored mass fights fluid skate forces — keep Acceleration-friendly
        if (rb.mass > 50f)
            rb.mass = 5f;

        if (transform.childCount > 0)
            visual = transform.GetChild(0);
    }

    // Update is called once per frame
    void Update()
    {
        if (db == null || pg == null || pg.player == null)
            return;

        if (pg.player.currentHealth <= 0)
        {
            HandleShareDeath();
            return;
        }

        // Lifelines spawn during countdown (startingGame) before gameStart —
        // snap Sunshine/Moonlight visuals immediately so the moon isn't shown pre-match.
        if (!db.gameStart)
        {
            if (db.startingGame)
                PreparePhaseVisuals();
            if (sunGravity != null)
                sunGravity.active = false;
            if (moonGravity != null)
                moonGravity.active = false;
            return;
        }

        if (spinPoint != null && sunHit != null && sunGravity != null && moonHit != null && moonGravity != null)
        {
            EnsurePhaseLock();

            if (pg.player.computer)
            {
                ComputerAI.Decision d = ComputerAI.Evaluate(transform, pg.player, null, 1.5f);
                if (d.wantBump) thought = Thought.MoveUp;
                else if (d.wantSuper) thought = Thought.MoveDown;
                else if (d.moveDir > 0) thought = Thought.MoveRight;
                else if (d.moveDir < 0) thought = Thought.MoveLeft;
                else thought = Thought.Nothing;
            }

            SyncSharePhaseFromHost();
            UpdateDayAndNight();
            PlanetsUpdate();
            SunAttacks();
            BufferMoonInput();
            TickSpin();
            if (spinCooldown > 0f)
                spinCooldown -= Time.deltaTime;
            if (slamCooldown > 0f)
                slamCooldown -= Time.deltaTime;
            if (dashCooldown > 0f)
                dashCooldown -= Time.deltaTime;
        }
        else
        {
            GetLifelineObjs();
        }
    }

    void PreparePhaseVisuals()
    {
        if (spinPoint == null || sunHit == null || sunGravity == null || moonHit == null || moonGravity == null)
            GetLifelineObjs();
        else
            EnsurePhaseLock();
    }

    void FixedUpdate()
    {
        if (db == null || pg == null || pg.player == null)
            return;
        if (!db.gameStart || pg.player.currentHealth <= 0)
            return;
        if (spinPoint == null || OrbitBody() == null)
            return;

        CelarusMove();
    }

    void BufferMoonInput()
    {
        if (moving || !CanSkateNow())
        {
            pendingMoveDir = 0;
            pendingDashDir = 0;
            return;
        }

        Player p = pg.player;
        int moveDir = 0;
        if (p.CanMove)
        {
            if (pg.inp.right || thought == Thought.MoveRight)
                moveDir += 1;
            if (pg.inp.left || thought == Thought.MoveLeft)
                moveDir -= 1;

            switch (p.facing)
            {
                case Facing.Down:
                case Facing.Right:
                    moveDir *= -1;
                    break;
            }
        }
        pendingMoveDir = moveDir;

        // Edges + held Jump/Interact so FixedUpdate never misses
        if (p.CanBump && (pg.inp.tf_bump || thought == Thought.MoveUp || JumpPressed()))
            pendingSpin = true;
        if (p.CanSuper && (pg.inp.tf_super || thought == Thought.MoveDown || InteractPressed()))
            pendingSlam = true;

        int dash = 0;
        if (p.CanDash)
        {
            if (p.computer)
            {
                ComputerAI.Decision ai = ComputerAI.GetLastDecision(p.index);
                if (ai.wantDash && ai.moveDir != 0)
                    dash = ai.moveDir > 0 ? 1 : -1;
            }
            else
            {
                if (pg.inp.tf_dashRight)
                    dash = 1;
                if (pg.inp.tf_dashLeft)
                    dash = -1;
            }

            switch (p.facing)
            {
                case Facing.Down:
                case Facing.Right:
                    dash *= -1;
                    break;
            }
        }
        pendingDashDir = dash;
    }

    bool JumpPressed()
    {
        ControllerLink cL = pg.player != null ? pg.player.cLink : null;
        if (cL == null)
            return false;
        ControllerButtons b = cL["Jump"];
        return b != null && b.wasPressedThisFrame;
    }

    bool InteractPressed()
    {
        ControllerLink cL = pg.player != null ? pg.player.cLink : null;
        if (cL == null)
            return false;
        ControllerButtons b = cL["Interact"];
        return b != null && b.wasPressedThisFrame;
    }

    void CelarusMove()
    {
        if (moving || !CanSkateNow() || OrbitBody() == null || rb == null)
            return;

        Vector3 moonPos = OrbitBody().transform.position;
        moonPos.z = transform.position.z;

        float moonRadius = GetMoonRadius();
        float rideRadius = moonRadius + starRadius + surfaceRide;

        Vector3 toStar = transform.position - moonPos;
        toStar.z = 0f;
        float dist = toStar.magnitude;
        if (dist < 0.0001f)
        {
            toStar = Vector3.up;
            dist = 0.0001f;
        }

        Vector3 radialOut = toStar / dist;
        Vector3 radialIn = -radialOut;
        Vector3 tangentRight = Vector3.Cross(radialOut, Vector3.forward).normalized;

        if (launchGrace > 0f)
            launchGrace -= Time.fixedDeltaTime;

        // Hard surface: keep star out of the moon (skip while launching so ramps can leave)
        bool deepInside = dist < rideRadius * 0.9f;
        if (dist < rideRadius && (launchGrace <= 0f || deepInside))
        {
            Vector3 fixedPos = moonPos + radialOut * rideRadius;
            fixedPos.z = transform.position.z;
            rb.position = fixedPos;
            transform.position = fixedPos;
            dist = rideRadius;
            toStar = radialOut * rideRadius;
        }

        float clearance = dist - rideRadius;
        onMoon = launchGrace <= 0f && clearance <= 0.12f;

        // Bump spin locks the star in place (ground or air)
        TryStartSpin();
        TryStartDash();
        if (spinLeft > 0f)
        {
            pendingSlam = false;
            rb.linearVelocity = Vector3.zero;
            Vector3 holdPos = moonPos + radialOut * Mathf.Max(dist, rideRadius);
            holdPos.z = transform.position.z;
            rb.position = holdPos;
            transform.position = holdPos;
            UpdateMoonVisual(tangentRight, 0f);
            return;
        }

        Vector3 v = rb.linearVelocity;
        v.z = 0f;

        int moveDir = pendingMoveDir;
        if (dashing)
            moveDir = dashDir;
        float tanSpeed = Vector3.Dot(v, tangentRight);
        float radSpeed = Vector3.Dot(v, radialOut); // + = leaving moon
        bool peeling = onMoon && moveDir != 0 && Mathf.Abs(tanSpeed) >= peelMinSpeed && !slamming;

        // Kill inward velocity while riding — but allow peel-away when holding L/R with speed
        if (onMoon && !peeling)
        {
            float inward = Vector3.Dot(v, radialIn);
            if (inward > 0f)
            {
                v -= radialIn * inward;
                radSpeed = Vector3.Dot(v, radialOut);
            }
        }

        // Skate / air steer — air keeps full L/R along the moon path while gravity pulls in
        if (moveDir != 0)
        {
            float accel = onMoon ? skateAccel : airSteerAccel;
            tanSpeed += moveDir * accel * Time.fixedDeltaTime;
        }
        else if (onMoon && !slamming)
        {
            tanSpeed = Mathf.MoveTowards(tanSpeed, 0f, groundFriction * maxSkateSpeed * Time.fixedDeltaTime);
        }
        else if (!onMoon && moveDir == 0 && !slamming)
        {
            // Light coast in air — don't kill side travel while orbiting the moon path
            tanSpeed = Mathf.MoveTowards(tanSpeed, 0f, airSteerAccel * 0.12f * Time.fixedDeltaTime);
        }

        // Free left/right travel in air (no short leash) — still pulled down radially
        float tanCap = maxSkateSpeed * ((onMoon || launchGrace > 0f) ? 1f : Mathf.Max(airSpeedScale, 1f));
        tanSpeed = Mathf.Clamp(tanSpeed, -tanCap, tanCap);

        float centripetalNeeded = (tanSpeed * tanSpeed) / Mathf.Max(0.1f, rideRadius);
        float coreRadius = GetMoonPullRadius(rideRadius);
        bool inCore = dist <= coreRadius;

        // Inner gravity climbs quickly (not an instant snap) when inside the well
        float blendTarget = inCore ? 1f : 0f;
        float blendRate = pullClimbRate * (inCore ? 1f : 0.65f);
        pullBlend = Mathf.MoveTowards(pullBlend, blendTarget, blendRate * Time.fixedDeltaTime);

        // Always pull toward the moon — solid far pull, stronger in-core after climb
        float farFade = 1f / (1f + Mathf.Max(0f, dist - rideRadius) * 0.025f);
        float coreFade = inCore
            ? Mathf.SmoothStep(1f, 0.55f, Mathf.InverseLerp(rideRadius, coreRadius, dist))
            : 0f;
        float outerPull = moonPullAccel * outerPullScale * farFade;
        float innerPull = moonPullAccel * pullBlend * coreFade;
        float pull = outerPull + innerPull;
        if (slamming)
            pull *= 3f;
        if (launchGrace > 0f)
            pull *= 0.35f;

        // Holding L/R with speed: gentle peel so you can leave sides and cut back in
        if (peeling)
        {
            float peelT = Mathf.InverseLerp(peelMinSpeed, maxSkateSpeed, Mathf.Abs(tanSpeed));
            radSpeed += peelLift * Mathf.Lerp(0.25f, 0.85f, peelT) * Time.fixedDeltaTime;
            if (radSpeed > 0.35f)
            {
                onMoon = false;
                launchGrace = Mathf.Max(launchGrace, launchGraceTime * 0.45f);
            }
        }

        // Ramp launch — modest hop at skate speed (enough to clear core, not a rocket)
        if (onMoon && !slamming && centripetalNeeded > moonPullAccel * launchAssist
            && Mathf.Abs(tanSpeed) > maxSkateSpeed * 0.4f)
        {
            float launchOut = launchImpulse + Mathf.Abs(tanSpeed) * launchSpeedFactor;
            launchOut = Mathf.Min(launchOut, maxLaunchOutSpeed);
            radSpeed = Mathf.Max(radSpeed, launchOut);
            onMoon = false;
            launchGrace = launchGraceTime;
        }
        else if (!slamming)
        {
            radSpeed -= pull * Time.fixedDeltaTime;
            if (onMoon && !peeling && radSpeed < 0f)
                radSpeed = 0f;
        }

        // Outside core gravity: don't keep shooting outward — soft cap exit speed
        if (!inCore && !slamming && radSpeed > maxOuterOutSpeed)
            radSpeed = Mathf.MoveTowards(radSpeed, maxOuterOutSpeed, 40f * Time.fixedDeltaTime);

        if (dashing)
        {
            tanSpeed = dashDir * dashSpeed;
            if (dashLeft > 0f)
                dashLeft -= Time.fixedDeltaTime;
            if (dashLeft <= 0f)
                dashing = false;
        }

        // Super slam only in the air
        if (!onMoon && !dashing)
        {
            ApplyAirSlam(ref radSpeed);
        }
        else
        {
            if (wasSlamming || slamming)
                FinishDropTrail(transform.position);
            slamming = false;
            pendingSlam = false;
        }

        // No overall speed leash on air L/R — only cap slam/boost spikes while on/near moon
        Vector3 newVel = tangentRight * tanSpeed + radialOut * radSpeed;
        if (onMoon || slamming)
        {
            float speedCap = maxSkateSpeed * (slamming ? 1.6f : 1.35f);
            if (newVel.magnitude > speedCap)
                newVel = newVel.normalized * speedCap;
        }
        newVel.z = 0f;
        rb.linearVelocity = newVel;

        if (slamming || dashing)
            EmitDropTrailAlongFall(transform.position);

        wasSlamming = slamming;
        UpdateMoonVisual(tangentRight, tanSpeed);
    }

    float GetMoonPullRadius(float rideRadius)
    {
        if (pullRadius > 0.1f)
            return pullRadius;

        PullObjectIn puller = OrbitBody();
        if (puller != null)
        {
            float fromPuller = puller.distance * Mathf.Max(0.1f, puller.disScale);
            // Stay a bit beyond the surface so you can peel and cut back in
            return Mathf.Max(rideRadius + 1.25f, fromPuller);
        }

        return rideRadius + 1.5f;
    }

    void TryStartSpin()
    {
        if (!pendingSpin)
            return;

        pendingSpin = false;
        if (spinLeft > 0f || spinCooldown > 0f)
            return;

        spinLeft = spinDuration;
        spinCooldown = spinCooldownTime;
        rb.linearVelocity = Vector3.zero;
        SpawnSpinResidue(transform.position);
    }

    void TryStartDash()
    {
        if (pendingDashDir == 0)
            return;

        int dir = pendingDashDir;
        pendingDashDir = 0;
        if (dashing || dashCooldown > 0f || spinLeft > 0f || slamming)
            return;

        dashing = true;
        dashDir = dir > 0 ? 1 : -1;
        dashLeft = dashDuration;
        dashCooldown = dashCooldownTime;
        slamPathAcc = 0f;
        lastDropEmitPos = transform.position;
        SpawnSpinResidue(transform.position);
        if (pg.player != null)
            pg.player.RecordDash();
    }

    void ApplyAirSlam(ref float radSpeed)
    {
        if (pendingSlam && slamCooldown <= 0f)
        {
            slamming = true;
            slamCooldown = slamCooldownTime;
            radSpeed = -slamSpeed;
            slamPathAcc = 0f;
            activeDropResidues.Clear();
            lastDropEmitPos = transform.position;
            // Spawn the first trail collider immediately as the drop begins
            SpawnDropResidue(transform.position, ProjectDropAim(transform.position));
            pg.player.RecordUltUsed();
        }
        pendingSlam = false;

        if (slamming)
            radSpeed = Mathf.Min(radSpeed, -slamSpeed * 0.85f);
    }

    /// <summary>Leave trail colliders while falling — not after landing.</summary>
    void EmitDropTrailAlongFall(Vector3 pos)
    {
        float starR = GetStarHitRadius();
        float spacing = Mathf.Max(0.15f, starR * 2f * dropTrailSpacingScale);
        float step = Vector3.Distance(lastDropEmitPos, pos);
        slamPathAcc += step;
        if (slamPathAcc < spacing)
            return;

        slamPathAcc = 0f;
        lastDropEmitPos = pos;
        if (dashing)
            SpawnSpinResidue(pos);
        else
            SpawnDropResidue(pos, ProjectDropAim(pos));
    }

    void SpawnDropResidue(Vector3 pos, Vector3 pullAim)
    {
        if (pg == null)
            return;

        float starR = GetStarHitRadius();
        Color c = PlayerSkin.GetColor(pg.player, db);
        CelarusResidue residue = CelarusResidue.Spawn(
            pos,
            pg.playerIndex,
            dropTrailDuration,
            starR,
            c,
            true,
            pullAim,
            dropTrailPullStrength,
            starR * dropTrailPullRadiusScale);

        if (residue != null)
            activeDropResidues.Add(residue);
    }

    Vector3 ProjectDropAim(Vector3 from)
    {
        PullObjectIn body = OrbitBody();
        if (body == null)
            return from;

        Vector3 moonPos = body.transform.position;
        moonPos.z = from.z;
        Vector3 outward = from - moonPos;
        outward.z = 0f;
        if (outward.sqrMagnitude < 0.0001f)
            outward = Vector3.up;
        outward.Normalize();

        float ride = GetMoonRadius() + starRadius + surfaceRide;
        Vector3 aim = moonPos + outward * Mathf.Max(0.05f, ride);
        aim.z = from.z;
        return aim;
    }

    float GetStarHitRadius()
    {
        Collider[] cols = GetComponentsInChildren<Collider>();
        float best = 0f;
        for (int i = 0; i < cols.Length; i++)
        {
            Collider c = cols[i];
            if (c == null || !c.enabled || c.isTrigger)
                continue;
            // Prefer planar size (ignore thin Z on flat stars)
            Vector3 e = c.bounds.extents;
            float planar = Mathf.Max(e.x, e.y);
            if (planar > best)
                best = planar;
        }

        if (best > 0.05f)
            return best;

        Renderer[] rends = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < rends.Length; i++)
        {
            if (rends[i] == null)
                continue;
            Vector3 e = rends[i].bounds.extents;
            float planar = Mathf.Max(e.x, e.y);
            if (planar > best)
                best = planar;
        }

        return Mathf.Max(0.35f, best > 0f ? best : 0.5f);
    }

    void SpawnSpinResidue(Vector3 pos)
    {
        if (pg == null)
            return;

        Color c = PlayerSkin.GetColor(pg.player, db);
        CelarusResidue.Spawn(
            pos,
            pg.playerIndex,
            spinResidueDuration,
            GetStarHitRadius(),
            c,
            false);
    }

    void FinishDropTrail(Vector3 dropPos)
    {
        // Trail already spawned during the fall — just retarget pull to the real drop point
        if (!slamming && !wasSlamming && activeDropResidues.Count == 0)
            return;

        wasSlamming = false;
        slamming = false;

        for (int i = activeDropResidues.Count - 1; i >= 0; i--)
        {
            CelarusResidue r = activeDropResidues[i];
            if (r == null)
            {
                activeDropResidues.RemoveAt(i);
                continue;
            }
            r.SetPullTarget(dropPos);
        }

        // Landing puff at the impact point
        if (pg != null)
            SpawnDropResidue(dropPos, dropPos);

        activeDropResidues.Clear();
        slamPathAcc = 0f;
    }

    void TickSpin()
    {
        if (spinLeft <= 0f)
            return;

        float step = 360f * (Time.deltaTime / Mathf.Max(0.01f, spinDuration));
        if (visual != null)
            visual.Rotate(0f, 0f, step, Space.Self);
        else
            transform.Rotate(0f, 0f, step, Space.Self);

        spinLeft -= Time.deltaTime;
        if (spinLeft <= 0f && visual != null)
            visual.localRotation = Quaternion.Euler(0f, 180f, 0f);
    }

    void UpdateMoonVisual(Vector3 tangentRight, float tanSpeed)
    {
        if (visual == null || spinLeft > 0f)
            return;

        float lean = Mathf.Clamp(tanSpeed * 1.8f, -60f, 60f);
        visual.localRotation = Quaternion.Euler(0f, 180f, lean);
    }

    float GetMoonRadius()
    {
        PullObjectIn body = OrbitBody();
        if (body == null)
            return 3f;

        SphereCollider sc = body.GetComponent<SphereCollider>();
        if (sc != null)
        {
            float s = Mathf.Max(body.transform.lossyScale.x, body.transform.lossyScale.y);
            return sc.radius * s;
        }

        return 0.5f * Mathf.Max(body.transform.lossyScale.x, 3f);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!CanSkateNow() || moving || OrbitBody() == null || rb == null)
            return;
        if (!IsMoonCollision(collision))
            return;

        ResolveMoonLanding(true);
    }

    void OnCollisionStay(Collision collision)
    {
        if (!CanSkateNow() || moving || OrbitBody() == null || rb == null)
            return;
        if (!IsMoonCollision(collision))
            return;

        // Keep from sinking while physics contacts
        Vector3 moonPos = OrbitBody().transform.position;
        moonPos.z = transform.position.z;
        float rideRadius = GetMoonRadius() + starRadius + surfaceRide;
        Vector3 toStar = transform.position - moonPos;
        toStar.z = 0f;
        if (toStar.magnitude < rideRadius)
        {
            Vector3 pos = moonPos + toStar.normalized * rideRadius;
            pos.z = transform.position.z;
            rb.position = pos;
        }
    }

    void ResolveMoonLanding(bool fromImpact)
    {
        PullObjectIn body = OrbitBody();
        if (body == null)
            return;

        Vector3 moonPos = body.transform.position;
        moonPos.z = transform.position.z;
        Vector3 radialOut = transform.position - moonPos;
        radialOut.z = 0f;
        if (radialOut.sqrMagnitude < 0.0001f)
            radialOut = Vector3.up;
        radialOut.Normalize();

        float rideRadius = GetMoonRadius() + starRadius + surfaceRide;
        Vector3 pos = moonPos + radialOut * rideRadius;
        pos.z = transform.position.z;
        rb.position = pos;

        Vector3 v = rb.linearVelocity;
        v.z = 0f;
        Vector3 tangentRight = Vector3.Cross(radialOut, Vector3.forward).normalized;
        Vector3 vTan = tangentRight * Vector3.Dot(v, tangentRight);
        float intoMoon = Mathf.Max(0f, Vector3.Dot(v, -radialOut));

        bool dropped = slamming || wasSlamming;
        if (dropped)
            FinishDropTrail(pos);

        if (!fromImpact && intoMoon < 1f && !dropped)
        {
            rb.linearVelocity = vTan;
            slamming = false;
            wasSlamming = false;
            onMoon = true;
            return;
        }

        float tanSpeed = vTan.magnitude;
        float speed = v.magnitude + 0.001f;
        float skimQuality = Mathf.Clamp01(tanSpeed / speed);
        float boost = Mathf.Lerp(1f, maxBounceBoost, skimQuality);
        if (dropped)
            boost *= slamBounceBonus;

        Vector3 bounced = vTan + radialOut * (intoMoon * bounceRestitution);
        if (bounced.sqrMagnitude < 0.01f)
            bounced = (tanSpeed > 0.1f ? vTan : tangentRight * 8f);

        bounced *= boost;
        float cap = maxSkateSpeed * (dropped ? 1.4f : 1.25f);
        if (bounced.magnitude > cap)
            bounced = bounced.normalized * cap;

        rb.linearVelocity = bounced;
        slamming = false;
        wasSlamming = false;
        onMoon = true;
    }

    bool IsMoonCollision(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return false;

        Transform t = collision.collider.transform;
        PullObjectIn body = OrbitBody();
        if (body != null && (t == body.transform || t.IsChildOf(body.transform)))
            return true;

        string n = t.name.ToLowerInvariant();
        if (n.Contains("moon"))
            return true;
        return ShouldSkateOnSun() && n.Contains("sun");
    }

    void SunAttacks()
    {
        Player p = pg.player;

        if (p.CanBump && day && sunGravity != null && !CanSkateNow())
        {
            Vector3 spPo = sunGravity.transform.position + (spinPoint.parent.transform.up * sFoffset);

            // Match moon skate: top/right seats have mirrored left/right
            bool wantLeft = pg.inp.tf_left || thought == Thought.MoveLeft;
            bool wantRight = pg.inp.tf_right || thought == Thought.MoveRight;
            bool wantMiddle = pg.inp.tf_up || thought == Thought.MoveUp;
            switch (p.facing)
            {
                case Facing.Down:
                case Facing.Right:
                    {
                        bool swap = wantLeft;
                        wantLeft = wantRight;
                        wantRight = swap;
                    }
                    break;
            }

            if (sunLeft >= 0)
            {
                //Spawn Ready Bubbles

                //Send Solar Flare
                if (wantLeft)
                {
                    GameObject ls = Instantiate(solarFlare, spPo + (-spinPoint.parent.transform.right * sFoffset), spinPoint.parent.rotation);

                    Vector3 ro = ls.transform.rotation.eulerAngles;
                    ro.z += sFoffsetAngle;
                    ls.transform.rotation = Quaternion.Euler(ro);

                    PlayerGrab lsPG = ls.GetComponent<PlayerGrab>();

                    if(lsPG != null)
                    {
                        lsPG.playerIndex = pg.playerIndex;
                    }

                    ConstantSpin lsCS = ls.transform.GetChild(0).GetComponent<ConstantSpin>();

                    if (lsCS != null)
                    {
                        lsCS.spinSpeed = Random.Range(solarFlareSpeed.x,solarFlareSpeed.y);
                    }

                    Rigidbody lsRB = ls.GetComponent<Rigidbody>();

                    if(lsRB != null)
                    {
                        lsRB.AddForce(new Vector3(sFsideDir.x, sFsideDir.y, sFsideDir.z) * solarFlareSpeed.z, ForceMode.Force);
                    }

                    ResetFlares lsRF = ls.transform.GetChild(0).GetComponent<ResetFlares>();

                    if (lsRF != null)
                    {
                        lsRF.script = this;
                    }

                   sunLeft = -sFResetCount;
                }
            }

            if (sunMiddle >= 0)
            {
                //Spawn Ready Bubbles

                //Send Solar Flare
                if (wantMiddle)
                {
                    GameObject ls = Instantiate(solarFlare, spPo, spinPoint.parent.rotation);

                    PlayerGrab lsPG = ls.GetComponent<PlayerGrab>();

                    if (lsPG != null)
                    {
                        lsPG.playerIndex = pg.playerIndex;
                    }

                    ConstantSpin lsCS = ls.GetComponent<ConstantSpin>();

                    if (lsCS != null)
                    {
                        lsCS.spinSpeed = Random.Range(solarFlareSpeed.x, solarFlareSpeed.y);
                    }

                    Rigidbody lsRB = ls.GetComponent<Rigidbody>();

                    if (lsRB != null)
                    {
                        lsRB.AddForce(new Vector3(sFsideDir.x, sFsideDir.y, sFsideDir.z) * solarFlareSpeed.z, ForceMode.Force);
                    }

                    sunMiddle = -sFResetCount;
                }
            }

            if (sunRight >= 0)
            {
                //Spawn Ready Bubbles

                //Send Solar Flare
                if (wantRight)
                {
                    GameObject ls = Instantiate(solarFlare, spPo + (spinPoint.parent.transform.right * sFoffset), spinPoint.parent.rotation);

                    Vector3 ro = ls.transform.rotation.eulerAngles;
                    ro.z -= sFoffsetAngle;
                    ls.transform.rotation = Quaternion.Euler(ro);

                    PlayerGrab lsPG = ls.GetComponent<PlayerGrab>();

                    if (lsPG != null)
                    {
                        lsPG.playerIndex = pg.playerIndex;
                    }

                    ConstantSpin lsCS = ls.GetComponent<ConstantSpin>();

                    if (lsCS != null)
                    {
                        lsCS.spinSpeed = Random.Range(solarFlareSpeed.x, solarFlareSpeed.y);
                    }

                    Rigidbody lsRB = ls.GetComponent<Rigidbody>();

                    if (lsRB != null)
                    {
                        lsRB.AddForce(new Vector3(sFsideDir.x, sFsideDir.y, sFsideDir.z) * solarFlareSpeed.z,ForceMode.Force);
                    }

                    sunRight = -sFResetCount;
                }
            }

            sunLeft += Time.deltaTime;
            sunMiddle += Time.deltaTime;
            sunRight += Time.deltaTime;
        }
    }

    void PlanetsUpdate()
    {
        if (sunHit == null || sunGravity == null || moonHit == null || moonGravity == null)
            return;

        bool skateSun = ShouldSkateOnSun();
        bool sunOn = !moving && sunGravity.gameObject.activeInHierarchy && (day || skateSun);
        bool moonOn = !moving && !day && !skateSun && moonGravity.gameObject.activeInHierarchy;

        sunHit.enabled = sunOn;
        sunGravity.active = sunOn;

        moonHit.enabled = moonOn;
        moonGravity.active = moonOn;
    }

    void UpdateDayAndNight()
    {
        // Sunshine / Moonlight variants stay in one phase
        if (phaseLock != CelarusPhaseLock.Cycle)
            return;
        if (ShareGuestFollowingHostCycle())
            return;

        if (!moving)
        {
            if (cycle >= dayCycle)
            {
                day = !day;
                moving = true;
                StartCoroutine(SwapCycle());
            }

            cycle += Time.deltaTime;
        }
    }

    void EnsurePhaseLock()
    {
        if (phaseLockApplied || phaseLock == CelarusPhaseLock.Cycle)
            return;
        if (spinPoint == null || sunHit == null || sunGravity == null || moonHit == null || moonGravity == null)
            return;
        ApplyPhaseLock();
    }

    /// <summary>
    /// Locks day/night for Sunshine (sun flares only) / Moonlight (moon skate only).
    /// </summary>
    void ApplyPhaseLock()
    {
        phaseLockApplied = true;

        if (phaseLock == CelarusPhaseLock.Cycle)
            return;

        if (ShareActive())
        {
            ApplySharedOpeningStar();
            return;
        }

        moving = false;
        cycle = 0f;

        if (phaseLock == CelarusPhaseLock.ForceSun)
        {
            day = true;
            // Prefab starts moon-forward (z=0). Snap to sun-forward immediately and hide the moon.
            if (spinPoint != null)
                spinPoint.localRotation = Quaternion.Euler(0f, 0f, 180f);
            SetCelestialActive(sun: true, moon: false);
            ParkStarForSun();
        }
        else
        {
            day = false;
            if (spinPoint != null)
                spinPoint.localRotation = Quaternion.Euler(0f, 0f, 0f);
            SetCelestialActive(sun: false, moon: true);
            PlaceStarOnMoonSurface();
        }

        PlanetsUpdate();
    }

    void SetCelestialActive(bool sun, bool moon)
    {
        if (sunGravity != null)
            sunGravity.gameObject.SetActive(sun);
        if (moonGravity != null)
            moonGravity.gameObject.SetActive(moon);

        // Keep component refs usable while inactive parents are toggled
        if (sunHit != null)
            sunHit.enabled = sun && day && !moving;
        if (sunGravity != null)
            sunGravity.active = sun && day && !moving;
        if (moonHit != null)
            moonHit.enabled = moon && !day && !moving;
        if (moonGravity != null)
            moonGravity.active = moon && !day && !moving;
    }

    void ParkStarForSun()
    {
        PullObjectIn body = sunGravity != null ? sunGravity : moonGravity;
        if (body == null)
            return;

        float playZ = body.transform.position.z;
        Vector3 park = body.transform.position;
        park.x += leavePoint.x;
        park.y += leavePoint.y;
        park.z = playZ;

        transform.position = park;
        if (rb != null)
        {
            rb.position = park;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        onMoon = false;
        slamming = false;
        launchGrace = 0f;
        pendingSpin = false;
        pendingSlam = false;
        pendingDashDir = 0;
        dashing = false;
        spinLeft = 0f;

        // Norm + Moon: only Moonlight's star stays in play during sun.
        if (phaseLock == CelarusPhaseLock.Cycle && ShareActive() && ShareHasLock(CelarusPhaseLock.ForceMoon))
        {
            park.z = playZ - 6f;
            transform.position = park;
            if (rb != null)
                rb.position = park;
            SetStarVisible(false);
        }
    }

    IEnumerator SwapCycle()
    {
        if (ShareActive())
        {
            yield return SharedSwapCycle();
            yield break;
        }

        float rot = 0;
        int dir = 1;
        float playZ = moonGravity != null ? moonGravity.transform.position.z : transform.position.z;

        // day was already toggled: true = entering sun, false = returning to moon
        if (day)
        {
            rot = 180;
            dir = -1;

            float leaveTime = 0;
            float timeframe = .75f;

            while (leaveTime < 1)
            {
                Vector3 p = transform.position + transform.up * Time.deltaTime;
                p.z = playZ;
                transform.position = p;

                leaveTime += (1f / timeframe) * Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
        }

        Vector3 curRot = spinPoint.transform.localRotation.eulerAngles;

        while (Mathf.Abs(Mathf.DeltaAngle(curRot.z, rot)) > 1f)
        {
            spinPoint.transform.localRotation = Quaternion.Euler(0, 0, curRot.z + (dir * spinSpeed * Time.deltaTime));

            // Park in the playfield plane (leavePoint.z used to yank the star off-field)
            Vector3 park = moonGravity.transform.position;
            park.x += leavePoint.x;
            park.y += leavePoint.y;
            park.z = playZ;
            transform.position = park;
            if (rb != null)
            {
                rb.position = park;
                rb.linearVelocity = Vector3.zero;
            }

            yield return new WaitForEndOfFrame();
            curRot = spinPoint.transform.localRotation.eulerAngles;
        }

        spinPoint.transform.localRotation = Quaternion.Euler(0, 0, rot);
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        yield return null;

        // Back to moon phase — seat the star on the moon in-play
        if (!day)
        {
            PlaceStarOnMoonSurface();
            yield return null;
        }

        cycle = 0;
        moving = false;
        yield return null;
    }

    void PlaceStarOnMoonSurface()
    {
        PullObjectIn body = OrbitBody();
        if (body == null)
            return;

        Vector3 moonPos = body.transform.position;
        float ride = GetMoonRadius() + starRadius + surfaceRide;

        Vector3 outDir = body.transform.up;
        outDir.z = 0f;
        if (outDir.sqrMagnitude < 0.0001f)
            outDir = Vector3.up;
        outDir.Normalize();

        Vector3 pos = moonPos + outDir * Mathf.Max(0.05f, ride);
        pos.z = moonPos.z;

        transform.position = pos;
        if (rb != null)
        {
            rb.position = pos;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        onMoon = true;
        slamming = false;
        launchGrace = 0f;
        pullBlend = 1f;
        pendingSpin = false;
        pendingSlam = false;
        pendingDashDir = 0;
        dashing = false;
        spinLeft = 0f;
        SetStarVisible(true);
    }

    void SetStarVisible(bool on)
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++)
        {
            if (rends[i] != null)
                rends[i].enabled = on;
        }

        Collider[] cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null && !cols[i].isTrigger)
                cols[i].enabled = on;
        }
    }

    void GetLifelineObjs()
    {
        Player p = pg.player;

        if (p != null)
        {
            GameObject sll = ResolveLifeline(p);

            if (sll != null)
            {
                if(sll.transform.childCount >= 1)
                {
                    spinPoint = sll.transform.GetChild(0);

                    if(spinPoint != null)
                    {
                        if(spinPoint.childCount > 0)
                        {
                            for(int i = 0; i < spinPoint.childCount; i++)
                            {
                                Transform spc = spinPoint.GetChild(i);

                                if(spc != null)
                                {
                                    if(spc.name.ToLower().Trim() == "moon")
                                    {
                                        moonHit = spc.GetComponent<DamageOnTagHit>();
                                        moonGravity = spc.GetComponent<PullObjectIn>();
                                        // Keep moon from being shoved by the skating star
                                        Rigidbody moonRb = spc.GetComponent<Rigidbody>();
                                        if (moonRb != null)
                                        {
                                            moonRb.isKinematic = true;
                                            moonRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                                        }
                                    }
                                    else if(spc.name.ToLower().Trim() == "sun")
                                    {
                                        sunHit = spc.GetComponent<DamageOnTagHit>();
                                        sunGravity = spc.GetComponent<PullObjectIn>();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Snap Sunshine/Moonlight phase as soon as the lifeline exists (don't wait for Update)
        if (!ShareActive())
            EnsurePhaseLock();
    }

    public void BindSharedLifeline(GameObject lifeline, List<int> members)
    {
        if (members == null)
            members = new List<int>();
        sharedLifelineOverride = lifeline;
        phaseLockApplied = true;
        GetLifelineObjs();

        Player p = pg != null ? pg.player : null;
        if (p != null && p.celarusShareHost)
            ApplySharedOpeningPhase();
        else if (phaseLock == CelarusPhaseLock.Cycle && ShareHasSun())
            day = true;
        ApplySharedOpeningStar();
    }

    public void LockShareToMoon()
    {
        day = false;
        moving = false;
        cycle = 0f;
        if (spinPoint != null)
            spinPoint.localRotation = Quaternion.Euler(0f, 0f, 0f);
        SetCelestialActive(sun: false, moon: true);
        PlaceStarOnMoonSurface();
        PlanetsUpdate();
    }

    void HandleShareDeath()
    {
        if (shareDeathHandled)
            return;
        shareDeathHandled = true;
        if (!ShareActive() && (pg == null || pg.player == null || pg.player.celarusShareHostIndex < 0))
            return;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        CelarusCoopLayout.OnShareMemberDied(pg.player);
        gameObject.SetActive(false);
    }

    GameObject ResolveLifeline(Player p)
    {
        if (sharedLifelineOverride != null)
            return sharedLifelineOverride;
        if (p == null)
            return null;
        if (p.spawnedLifeline != null)
            return p.spawnedLifeline;
        if (p.celarusShareHostIndex < 0 || db == null || db.players == null)
            return null;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player other = db.players[i];
            if (other != null && other.index == p.celarusShareHostIndex)
                return other.spawnedLifeline;
        }
        return null;
    }

    PullObjectIn OrbitBody()
    {
        if (ShouldSkateOnSun() && sunGravity != null)
            return sunGravity;
        if (moonGravity != null && moonGravity.gameObject.activeInHierarchy)
            return moonGravity;
        if (sunGravity != null && sunGravity.gameObject.activeInHierarchy)
            return sunGravity;
        if (moonGravity != null)
            return moonGravity;
        return sunGravity;
    }

    bool CanSkateNow()
    {
        return !ShouldParkForSun();
    }

    bool ShouldParkForSun()
    {
        if (phaseLock == CelarusPhaseLock.ForceSun)
            return true;
        // Normal Celarus always plays sun phase herself (park + flares), even beside Moonlight.
        if (phaseLock == CelarusPhaseLock.Cycle && day)
            return true;
        return false;
    }

    bool ShouldSkateOnSun()
    {
        if (phaseLock == CelarusPhaseLock.ForceSun)
            return false;
        if (phaseLock == CelarusPhaseLock.Cycle)
            return ShareHasSun() && !day;
        if (phaseLock == CelarusPhaseLock.ForceMoon)
            return ShareHasSun() || ShareHostIsInSunPhase();
        return false;
    }

    bool ShareHostIsInSunPhase()
    {
        if (ShareHasSun())
            return true;
        Celarus host = GetShareHostCelarus();
        return host != null && host.phaseLock == CelarusPhaseLock.Cycle && host.day;
    }

    bool ShareActive()
    {
        Player p = pg != null ? pg.player : null;
        if (p == null || p.celarusShareHostIndex < 0)
            return false;
        return HasLivingSharePartner();
    }

    bool HasLivingSharePartner()
    {
        Player self = pg != null ? pg.player : null;
        if (self == null || db == null || db.players == null)
            return false;
        int host = self.celarusShareHostIndex >= 0 ? self.celarusShareHostIndex : self.index;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player other = db.players[i];
            if (other == null || other == self || other.currentHealth <= 0)
                continue;
            int otherHost = other.celarusShareHostIndex >= 0 ? other.celarusShareHostIndex : -1;
            if (otherHost == host || other.index == host)
                return true;
        }
        return false;
    }

    bool ShareHasSun()
    {
        return ShareHasLock(CelarusPhaseLock.ForceSun);
    }

    bool ShareHasLock(CelarusPhaseLock lockMode)
    {
        if (db == null || db.players == null || pg == null || pg.player == null)
            return false;
        int host = pg.player.celarusShareHostIndex;
        if (host < 0)
            return false;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player other = db.players[i];
            if (other == null || other.currentHealth <= 0)
                continue;
            int otherHost = other.celarusShareHostIndex >= 0 ? other.celarusShareHostIndex : -1;
            if (otherHost != host && other.index != host)
                continue;
            if (CelarusCoopLayout.ReadPhaseLock(other) == lockMode)
                return true;
        }
        return false;
    }

    bool ShareGuestFollowingHostCycle()
    {
        Player p = pg != null ? pg.player : null;
        if (p == null || p.celarusShareHost || phaseLock != CelarusPhaseLock.Cycle)
            return false;
        Celarus host = GetShareHostCelarus();
        return host != null && host.phaseLock == CelarusPhaseLock.Cycle;
    }

    Celarus GetShareHostCelarus()
    {
        Player p = pg != null ? pg.player : null;
        if (p == null || db == null || db.players == null)
            return null;
        int host = p.celarusShareHostIndex;
        if (host < 0)
            return null;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player other = db.players[i];
            if (other == null || other.index != host || other.spawnedPlayer == null)
                continue;
            return other.spawnedPlayer.GetComponent<Celarus>();
        }
        return null;
    }

    void SyncSharePhaseFromHost()
    {
        Celarus host = GetShareHostCelarus();
        if (host == null || host == this || host.phaseLock != CelarusPhaseLock.Cycle)
            return;
        moving = host.moving;
        if (phaseLock == CelarusPhaseLock.Cycle && day != host.day && !host.moving)
        {
            day = host.day;
            ApplySharedOpeningStar();
        }
    }

    void ApplySharedOpeningPhase()
    {
        moving = false;
        cycle = 0f;
        if (ShareHasSun())
        {
            if (spinPoint != null)
                spinPoint.localRotation = Quaternion.Euler(0f, 0f, 180f);
            SetCelestialActive(sun: true, moon: false);
            if (phaseLock == CelarusPhaseLock.Cycle)
                day = true;
        }
        else
        {
            if (spinPoint != null)
                spinPoint.localRotation = Quaternion.Euler(0f, 0f, 0f);
            SetCelestialActive(sun: false, moon: true);
            if (phaseLock == CelarusPhaseLock.Cycle)
                day = false;
        }
        PlanetsUpdate();
    }

    void ApplySharedOpeningStar()
    {
        if (ShouldParkForSun())
            ParkStarForSun();
        else
            PlaceStarOnMoonSurface();
    }

    IEnumerator SharedSwapCycle()
    {
        if (ShareHasSun())
        {
            if (ShouldParkForSun())
                ParkStarForSun();
            else
                PlaceStarOnMoonSurface();
            ReseatShareStars();
            cycle = 0f;
            moving = false;
            yield break;
        }

        float rot = day ? 180f : 0f;
        int dir = day ? -1 : 1;
        float playZ = moonGravity != null ? moonGravity.transform.position.z : transform.position.z;

        // Norm enters sun like a solo swap — her star leaves. Moonlight's star stays in play.
        if (day && phaseLock == CelarusPhaseLock.Cycle)
        {
            float leaveTime = 0f;
            const float timeframe = 0.75f;
            while (leaveTime < 1f)
            {
                Vector3 p = transform.position + transform.up * Time.deltaTime;
                p.z = playZ;
                transform.position = p;
                if (rb != null)
                    rb.position = p;
                leaveTime += (1f / timeframe) * Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
        }

        if (spinPoint != null)
        {
            Vector3 curRot = spinPoint.localRotation.eulerAngles;
            while (Mathf.Abs(Mathf.DeltaAngle(curRot.z, rot)) > 1f)
            {
                spinPoint.localRotation = Quaternion.Euler(0f, 0f, curRot.z + (dir * spinSpeed * Time.deltaTime));
                if (phaseLock == CelarusPhaseLock.Cycle && day)
                {
                    Vector3 park = (sunGravity != null ? sunGravity.transform.position : moonGravity.transform.position);
                    park.x += leavePoint.x;
                    park.y += leavePoint.y;
                    park.z = playZ;
                    transform.position = park;
                    if (rb != null)
                    {
                        rb.position = park;
                        rb.linearVelocity = Vector3.zero;
                    }
                }
                yield return new WaitForEndOfFrame();
                curRot = spinPoint.localRotation.eulerAngles;
            }
            spinPoint.localRotation = Quaternion.Euler(0f, 0f, rot);
        }

        SetCelestialActive(sun: day, moon: !day);
        ReseatShareStars();
        cycle = 0f;
        moving = false;
        yield return null;
    }

    void ReseatShareStars()
    {
        if (db == null || db.players == null || pg == null || pg.player == null)
        {
            ApplySharedOpeningStar();
            return;
        }

        int host = pg.player.celarusShareHostIndex;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player other = db.players[i];
            if (other == null || other.currentHealth <= 0 || other.spawnedPlayer == null)
                continue;
            int otherHost = other.celarusShareHostIndex >= 0 ? other.celarusShareHostIndex : -1;
            if (otherHost != host && other.index != host)
                continue;
            Celarus c = other.spawnedPlayer.GetComponent<Celarus>();
            if (c != null)
                c.ApplySharedOpeningStar();
        }
    }
}
