using UnityEngine;

/// <summary>
/// Wing-form Tic: flap to move. Right wing = CCW turn, left = CW, both = forward push.
/// Floaty flight only after leaving the launch bay.
/// </summary>
[RequireComponent(typeof(TicBumper))]
[RequireComponent(typeof(PlayerGrab))]
[DefaultExecutionOrder(50)] // After TicBumper physics so super freeze sticks
public class TicWingBumper : MonoBehaviour
{
    public const float BodyScale = 0.38f;
    // Same silhouette as body; thinner on Z so it sits hidden inside
    public static readonly Vector3 InnerBodyScale = new Vector3(0.38f, 0.38f, 0.1f);
    public const float HubRadius = 0.2f;
    public const float WingScale = 0.085f;
    public const float WingScaleZ = 0.055f; // shorten long axis
    // Just past the body rim so paddles peek out
    public const float WingOffsetX = 0.18f;

    [Header("Parts")]
    public Transform mainBody;
    public Transform innerBumper;
    public Transform leftWing;
    public Transform rightWing;

    [Header("Flap flight")]
    public float flapThrust = 7.5f;
    public float bothThrustMul = 1.55f;
    public float turnTorque = 9f;
    public float coastDrag = 0.92f;       // must keep flapping
    public float flapDrag = 0.988f;      // while flapping
    public float maxFlightSpeed = 8f;
    public float floatGravity = 0.3f;
    public float bayGravity = 8f;        // while still in / clearing bay

    [Header("Paddle flap")]
    public float flipAngle = 72f;
    public float flipUpSpeed = 720f;
    public float flipDownSpeed = 420f;
    public Vector3 flipAxis = Vector3.right;

    [Header("Bump")]
    public float bumpCooldown = 0.75f;

    TicBumper bumper;
    PlayerGrab pg;
    Database db;
    TicBumperSpikes spikes;
    Vector3 leftWingRestScale = Vector3.one;
    Vector3 rightWingRestScale = Vector3.one;
    Vector3 leftWingRestPos;
    Vector3 rightWingRestPos;
    Quaternion leftWingRestRot = Quaternion.identity;
    Quaternion rightWingRestRot = Quaternion.identity;
    float leftAngle;
    float rightAngle;
    float bumpReadyAt;
    bool superHolding;
    Vector3 pausedVelocity;
    Vector3 freezePos;
    Tic hostTic;
    bool sized;
    Collider[] leftWingColliders;
    Collider[] rightWingColliders;
    BallInfo cachedNearestBall;
    float nextBallTargetRefresh;

    public bool IsHoldingSuper => superHolding;
    /// <summary>True once fully clear of the bay — floaty flight allowed.</summary>
    public bool FreeFlight => bumper != null && !bumper.InLaunchArea && !bumper.InLaunchGrace;

    bool InBay => bumper != null && bumper.InLaunchArea;

    public void SetHost(Tic host)
    {
        hostTic = host;
        if (bumper != null && host != null)
            bumper.Init(host, pg != null ? pg.playerIndex : host.PlayerIndex);
    }

    void Awake()
    {
        bumper = GetComponent<TicBumper>();
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;
        spikes = GetComponent<TicBumperSpikes>();
        if (spikes == null)
            spikes = gameObject.AddComponent<TicBumperSpikes>();
        EnsureVisuals();
        ApplyNormalBumperSize();
        spikes.EnsureBuilt();
        CacheRest();
        CacheWingColliders();
        ForceStowWings();
    }

    void Start()
    {
        if (db == null)
            db = Database.instance;
        if (hostTic == null && pg != null && pg.player != null)
            hostTic = FindHostTic(pg.player);
        if (hostTic != null)
        {
            bumper.Init(hostTic, pg.playerIndex);
            if (hostTic.launchArea != null)
                hostTic.launchArea.RegisterBumperPassThrough(bumper);
        }
        ApplyBayOrFlightGravity();
        ForceStowWings();
        PlayerSkin.Apply(gameObject, pg != null ? pg.player : null, db);
    }

    void ApplyBayOrFlightGravity()
    {
        if (bumper == null)
            return;
        bumper.loadGravity = bayGravity;
        // Floaty gravity only when free of the bay; while launched-but-still-in-bay keep bay gravity
        bumper.launchedGravity = FreeFlight ? floatGravity : bayGravity;
        bumper.minLaunchSpeed = FreeFlight ? 0.5f : 5f;
        bumper.maxSpeed = maxFlightSpeed;
        bumper.bounceRetain = FreeFlight ? 0.45f : 0.85f;
    }

    void ApplyNormalBumperSize()
    {
        if (sized)
            return;
        sized = true;

        SphereCollider hub = GetComponent<SphereCollider>();
        if (hub != null)
            hub.radius = HubRadius;

        if (mainBody != null)
        {
            // Match normal Tic bumper flat capsule look
            mainBody.localScale = new Vector3(BodyScale, 0.115f, BodyScale);
            mainBody.localPosition = Vector3.zero;
            mainBody.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        if (innerBumper != null)
        {
            // Same shape as body, thinner on Z — hidden inside; spikes are the bump expand
            innerBumper.localScale = new Vector3(BodyScale * 0.92f, 0.09f, BodyScale * 0.35f);
            innerBumper.localPosition = Vector3.zero;
            innerBumper.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ApplyPistonMaterial(innerBumper);
            // Keep rendered but short on Z so it doesn't poke out of the body
            Renderer ir = innerBumper.GetComponent<Renderer>();
            if (ir != null)
                ir.enabled = true;
        }

        leftWingRestRot = Quaternion.Euler(0f, -90f, 0f);
        rightWingRestRot = Quaternion.Euler(0f, 90f, 0f);
        Vector3 wingScale = new Vector3(WingScale, WingScale, WingScaleZ);

        if (leftWing != null)
        {
            leftWing.localScale = wingScale;
            leftWing.localPosition = new Vector3(-WingOffsetX, 0f, 0f);
            leftWing.localRotation = leftWingRestRot;
        }
        if (rightWing != null)
        {
            rightWing.localScale = wingScale;
            rightWing.localPosition = new Vector3(WingOffsetX, 0f, 0f);
            rightWing.localRotation = rightWingRestRot;
        }

        Collider[] childCols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < childCols.Length; i++)
        {
            Collider c = childCols[i];
            if (c == null || c.gameObject == gameObject)
                continue;
            if (c.transform == mainBody || c.transform == innerBumper)
                c.enabled = false;
        }
    }

    static void ApplyPistonMaterial(Transform t)
    {
        if (t == null) return;
        Material mat = null;
#if UNITY_EDITOR
        mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Materials/Colors/Characters/Tic Piston.mat");
#endif
        if (mat == null)
            return;
        Renderer r = t.GetComponent<Renderer>();
        if (r != null)
            r.sharedMaterial = mat;
    }

    void CacheRest()
    {
        if (leftWing != null)
        {
            leftWingRestScale = leftWing.localScale;
            leftWingRestPos = leftWing.localPosition;
            leftWingRestRot = leftWing.localRotation;
        }
        if (rightWing != null)
        {
            rightWingRestScale = rightWing.localScale;
            rightWingRestPos = rightWing.localPosition;
            rightWingRestRot = rightWing.localRotation;
        }
    }

    void Update()
    {
        if (db == null || pg == null || pg.player == null)
            return;
        if (!db.gameStart || pg.player.currentHealth <= 0)
            return;

        ApplyBayOrFlightGravity();

        if (InBay)
        {
            ForceStowWings();
            if (superHolding)
                ClearSuperHold();
            if (pg.player.bump != null) pg.player.bump.Charge();
            if (pg.player.super != null) pg.player.super.Charge();
            return;
        }

        // Keep freeze locked even if other systems run this frame
        if (superHolding)
        {
            ApplySuperFreeze();
            if (pg.player.bump != null) pg.player.bump.Charge();
            if (pg.player.super != null) pg.player.super.Charge();
            HandleSuperHold(); // release check only
            return;
        }

        DeployWings();

        // Still in launch grace / clearing bay — keep Loading-like ball physics, no float controls
        if (!FreeFlight)
        {
            if (pg.player.bump != null) pg.player.bump.Charge();
            if (pg.player.super != null) pg.player.super.Charge();
            // Allow flap visuals only; light thrust to help clear bay
            HandleFlaps(bayAssist: true);
            HandleBump();
            HandleSuperHold();
            return;
        }

        if (bumper.CurrentPhase == TicBumper.Phase.Loading)
            bumper.SetPhase(TicBumper.Phase.Launched);

        if (pg.player.bump != null) pg.player.bump.Charge();
        if (pg.player.super != null) pg.player.super.Charge();

        HandleFlaps(bayAssist: false);
        HandleBump();
        HandleSuperHold();
    }

    void FixedUpdate()
    {
        if (bumper == null || bumper.rb == null || bumper.IsFrozen)
            return;

        if (superHolding)
        {
            bumper.rb.isKinematic = true;
            bumper.rb.position = freezePos;
            transform.position = freezePos;
            bumper.rb.linearVelocity = Vector3.zero;
            bumper.rb.angularVelocity = Vector3.zero;
            return;
        }

        if (!FreeFlight || bumper.rb.isKinematic)
            return;

        // Coast hard unless flapping this frame (set in HandleFlaps via lastFlap)
        Vector3 v = bumper.rb.linearVelocity;
        v.z = 0f;
        v *= lastFlap ? flapDrag : coastDrag;
        if (v.sqrMagnitude > maxFlightSpeed * maxFlightSpeed)
            v = v.normalized * maxFlightSpeed;
        bumper.rb.linearVelocity = v;
    }

    bool lastFlap;

    void HandleFlaps(bool bayAssist)
    {
        lastFlap = false;
        if (pg.player == null || !pg.player.CanMove || superHolding)
            return;

        bool leftHeld = false;
        bool rightHeld = false;
        if (pg.player.computer)
        {
            BallInfo ball = FindNearestBall();
            if (ball != null)
            {
                Vector3 to = ball.transform.position - transform.position;
                float side = Vector3.Dot(to, transform.right);
                leftHeld = side < -0.2f;
                rightHeld = side > 0.2f;
                if (Mathf.Abs(side) < 0.2f)
                {
                    leftHeld = true;
                    rightHeld = true;
                }
            }
        }
        else if (pg.inp != null)
        {
            leftHeld = pg.inp.left;
            rightHeld = pg.inp.right;
            // Controller: dash = both paddles (can't hold stick left+right at once)
            if (pg.inp.dashLeft || pg.inp.dashRight)
            {
                leftHeld = true;
                rightHeld = true;
            }
        }

        float dt = Time.deltaTime;
        leftAngle = Mathf.MoveTowards(leftAngle, leftHeld ? flipAngle : 0f,
            (leftHeld ? flipUpSpeed : flipDownSpeed) * dt);
        rightAngle = Mathf.MoveTowards(rightAngle, rightHeld ? flipAngle : 0f,
            (rightHeld ? flipUpSpeed : flipDownSpeed) * dt);

        Vector3 axis = flipAxis.sqrMagnitude > 0.01f ? flipAxis.normalized : Vector3.right;
        if (leftWing != null)
            leftWing.localRotation = leftWingRestRot * Quaternion.AngleAxis(leftAngle, axis);
        if (rightWing != null)
            rightWing.localRotation = rightWingRestRot * Quaternion.AngleAxis(rightAngle, axis);

        if (!leftHeld && !rightHeld)
            return;
        if (bumper.rb == null || bumper.rb.isKinematic)
            return;

        lastFlap = true;

        // Forward = bumper's local up in the play plane (turns with the body)
        Vector3 forward = transform.up;
        forward.z = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.up;
        forward.Normalize();

        float thrust = flapThrust * (bayAssist ? 0.55f : 1f);
        if (leftHeld && rightHeld)
            thrust *= bothThrustMul;
        else
            thrust *= 0.7f; // single wing = less push, more turn

        bumper.rb.AddForce(forward * thrust, ForceMode.Acceleration);

        // Right wing → slow CCW (+Z torque in Unity 2D play plane), left → CW
        float torque = 0f;
        if (rightHeld && !leftHeld) torque = turnTorque;
        if (leftHeld && !rightHeld) torque = -turnTorque;
        if (Mathf.Abs(torque) > 0.01f)
            bumper.rb.AddTorque(Vector3.forward * torque * (bayAssist ? 0.5f : 1f), ForceMode.Acceleration);
    }

    void HandleBump()
    {
        if (superHolding || Time.time < bumpReadyAt)
            return;

        bool want = false;
        if (!pg.player.computer && pg.inp != null)
            want = pg.inp.tf_bump || pg.inp.bump;

        if (!want)
            return;

        Skill b = pg.player.bump;
        if (b == null || !b.Enough())
            return;

        b.Spend();
        b.readyPercent = 0f;
        bumpReadyAt = Time.time + bumpCooldown;
        if (spikes != null)
            spikes.Pop();
    }

    void HandleSuperHold()
    {
        bool want = ReadSuperHeld();

        if (want && !superHolding)
        {
            // Can freeze anywhere outside the bay (not only after floaty free-flight)
            if (InBay)
                return;
            Skill s = pg.player.super;
            if (s == null || !s.Enough())
                return;
            s.Spend();
            s.readyPercent = 0f;
            BeginSuperFreeze();
            pg.player.RecordUltUsed();
        }
        else if (!want && superHolding)
        {
            ClearSuperHold();
        }
        else if (superHolding)
        {
            ApplySuperFreeze();
        }
    }

    bool ReadSuperHeld()
    {
        if (pg == null || pg.player == null || pg.player.computer)
            return false;
        if (pg.inp != null && (pg.inp.superHeld || pg.inp.super))
            return true;
        // Raw Interact hold (controller) — don't rely on facing remap alone
        if (pg.player.cLink != null)
        {
            ControllerButtons b = pg.player.cLink["Interact"];
            if (b != null && b.isPressed)
                return true;
        }
        if (pg.player.inputLinks != null)
        {
            for (int i = 0; i < pg.player.inputLinks.Count; i++)
            {
                ControllerLink cL = pg.player.inputLinks[i];
                if (cL == null) continue;
                ControllerButtons b = cL["Interact"];
                if (b != null && b.isPressed)
                    return true;
            }
        }
        return false;
    }

    void BeginSuperFreeze()
    {
        freezePos = transform.position;
        freezePos.z = bumper != null && bumper.rb != null ? bumper.rb.position.z : freezePos.z;
        pausedVelocity = bumper != null && bumper.rb != null ? bumper.rb.linearVelocity : Vector3.zero;
        pausedVelocity.z = 0f;
        superHolding = true;
        ApplySuperFreeze();
    }

    void ApplySuperFreeze()
    {
        if (bumper == null || bumper.rb == null)
            return;
        bumper.rb.isKinematic = true;
        bumper.rb.linearVelocity = Vector3.zero;
        bumper.rb.angularVelocity = Vector3.zero;
        bumper.rb.position = freezePos;
        bumper.rb.rotation = transform.rotation;
        transform.position = freezePos;
    }

    void LateUpdate()
    {
        // Hard lock after all physics / bumper scripts
        if (superHolding)
            ApplySuperFreeze();
    }

    void ClearSuperHold()
    {
        if (!superHolding)
            return;
        superHolding = false;
        if (bumper != null && bumper.rb != null && !bumper.IsFrozen)
        {
            bumper.rb.isKinematic = false;
            Vector3 v = pausedVelocity;
            v.z = 0f;
            if (v.sqrMagnitude < 0.25f)
                v = transform.up * 2f;
            bumper.rb.linearVelocity = v;
        }
        pausedVelocity = Vector3.zero;
    }

    void ForceStowWings()
    {
        leftAngle = 0f;
        rightAngle = 0f;
        SetWingActive(leftWing, leftWingColliders, false);
        SetWingActive(rightWing, rightWingColliders, false);
    }

    void DeployWings()
    {
        SetWingActive(leftWing, leftWingColliders, true);
        SetWingActive(rightWing, rightWingColliders, true);
        if (leftWing != null)
        {
            leftWing.localScale = leftWingRestScale;
            leftWing.localPosition = leftWingRestPos;
        }
        if (rightWing != null)
        {
            rightWing.localScale = rightWingRestScale;
            rightWing.localPosition = rightWingRestPos;
        }
    }

    void CacheWingColliders()
    {
        leftWingColliders = leftWing != null
            ? leftWing.GetComponentsInChildren<Collider>(true)
            : new Collider[0];
        rightWingColliders = rightWing != null
            ? rightWing.GetComponentsInChildren<Collider>(true)
            : new Collider[0];
    }

    static void SetWingActive(Transform wing, Collider[] cols, bool active)
    {
        if (wing == null) return;
        if (wing.gameObject.activeSelf != active)
            wing.gameObject.SetActive(active);
        if (cols == null)
            return;
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null)
                cols[i].enabled = active;
        }
    }

    BallInfo FindNearestBall()
    {
        if (Time.time < nextBallTargetRefresh
            && cachedNearestBall != null
            && cachedNearestBall.gameObject.activeInHierarchy
            && cachedNearestBall.ballReady)
        {
            return cachedNearestBall;
        }
        nextBallTargetRefresh = Time.time + 0.1f;

        BallInfo best = null;
        float bestSq = float.MaxValue;
        for (int i = 0; i < LiveBallRegistry.Count; i++)
        {
            BallInfo ball = LiveBallRegistry.GetAt(i);
            if (ball == null || !ball.gameObject.activeInHierarchy || !ball.ballReady) continue;
            float sq = (ball.transform.position - transform.position).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = ball; }
        }
        cachedNearestBall = best;
        return cachedNearestBall;
    }

    static Tic FindHostTic(Player wingPlayer)
    {
        if (wingPlayer == null || Database.instance == null) return null;
        var players = Database.instance.players;
        for (int i = 0; i < players.Count; i++)
        {
            Player p = players[i];
            if (p == null || p.spawnedPlayer == null || p.ticWingForm) continue;
            if (p.facing != wingPlayer.facing) continue;
            Tic tic = p.spawnedPlayer.GetComponent<Tic>();
            if (tic != null) return tic;
        }
        return null;
    }

    void EnsureVisuals()
    {
        if (mainBody != null && innerBumper != null && leftWing != null && rightWing != null)
            return;

        Material matMain = null;
        Material matInner = null;
        Material matWing = null;
        Mesh paddleMesh = null;

#if UNITY_EDITOR
        matMain = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Colors/Bump/Tic.mat");
        matInner = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Colors/Characters/Tic Piston.mat");
        matWing = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Colors/Characters/Tic.mat");
        GameObject ticPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/_Characters/Tic.prefab");
        if (ticPrefab != null)
        {
            MeshFilter[] filters = ticPrefab.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i] != null && filters[i].gameObject.name == "Paddle" && filters[i].sharedMesh != null)
                {
                    paddleMesh = filters[i].sharedMesh;
                    break;
                }
            }
        }
#endif

        if (mainBody == null)
            mainBody = CreateCapsulePart("MainBody", Vector3.one * BodyScale, matMain).transform;
        if (innerBumper == null)
            innerBumper = CreateCapsulePart("InnerBumper", InnerBodyScale, matInner).transform;
        if (leftWing == null)
            leftWing = CreateWing("LeftWing", new Vector3(-WingOffsetX, 0f, 0f), paddleMesh, matWing, left: true).transform;
        if (rightWing == null)
            rightWing = CreateWing("RightWing", new Vector3(WingOffsetX, 0f, 0f), paddleMesh, matWing, left: false).transform;
    }

    GameObject CreateCapsulePart(string name, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = scale;
        go.tag = "Paddle";
        Collider col = go.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Rigidbody childRb = go.GetComponent<Rigidbody>();
        if (childRb != null) Destroy(childRb);
        if (mat != null)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }
        PlayerGrab g = go.AddComponent<PlayerGrab>();
        g.playerIndex = pg != null ? pg.playerIndex : -1;
        return go;
    }

    GameObject CreateWing(string name, Vector3 localPos, Mesh mesh, Material mat, bool left)
    {
        GameObject go;
        Vector3 wingScale = new Vector3(WingScale, WingScale, WingScaleZ);
        if (mesh != null)
        {
            go = new GameObject(name);
            go.transform.SetParent(transform, false);
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;
            go.transform.localScale = wingScale;
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(WingScale * 0.35f, WingScale * 0.25f, WingScaleZ * 2f);
            Rigidbody childRb = go.GetComponent<Rigidbody>();
            if (childRb != null) Destroy(childRb);
            if (mat != null)
            {
                Renderer r = go.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
            }
        }
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, left ? -90f : 90f, 0f);
        go.tag = "Paddle";
        go.SetActive(false);
        PlayerGrab g = go.AddComponent<PlayerGrab>();
        g.playerIndex = pg != null ? pg.playerIndex : -1;
        return go;
    }

    public static GameObject CreateRuntimePrefabInstance()
    {
        GameObject root = new GameObject("TicWingBumper");
        root.tag = "Paddle";
        root.layer = 3;
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 0.8f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezePositionZ
            | RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationY;
        root.AddComponent<PlayerGrab>();
        root.AddComponent<ClearAfterTheGame>();
        TicBumper bumper = root.AddComponent<TicBumper>();
        bumper.loadGravity = 8f;
        bumper.launchedGravity = 8f;
        bumper.minLaunchSpeed = 5f;
        bumper.maxSpeed = 8f;
        root.AddComponent<TicBumperSpikes>();
        root.AddComponent<TicWingBumper>();
        SphereCollider sc = root.AddComponent<SphereCollider>();
        sc.radius = HubRadius;
        return root;
    }
}
