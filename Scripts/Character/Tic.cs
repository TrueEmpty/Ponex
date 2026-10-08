using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tic — pinball machine: left/right flippers, plunger bump, charging bumper super.
/// Prefab children: Right, Left, Center (plunger). Optional LoadPoint / LaunchArea
/// are created at runtime if missing.
/// </summary>
public class Tic : MonoBehaviour
{
    Database db;
    PlayerGrab pg;

    [Header("Parts")]
    public Transform leftPaddle;
    public Transform rightPaddle;
    public Transform plunger;
    public Transform loadPoint;
    public TicLaunchArea launchArea;

    Rigidbody leftRb;
    Rigidbody rightRb;
    Rigidbody plungerRb;

    [Header("Flippers")]
    public float flipAngle = 72f;
    public float flipUpSpeed = 720f;
    public float flipDownSpeed = 420f;
    // Local axis to rotate paddles (pinball flip). Default matches old Tic mesh orientation.
    public Vector3 flipAxis = Vector3.right;

    float leftAngle;
    float rightAngle;
    Quaternion leftRestRot;
    Quaternion rightRestRot;

    [Header("Plunger")]
    public float plungerTravel = 1.6f;
    public float plungerShootSpeed = 48f;
    public float plungerReturnSpeed = 18f;
    public float plungerLaunchSpeed = 26f;
    public float bumpCooldown = 0.85f;
    float plungerT; // 0 rest … 1 extended
    int plungerPhase; // 0 idle, 1 shooting up, 2 returning
    float bumpReadyAt;

    [Header("Bumpers")]
    public GameObject bumperPrefab;
    public GameObject wingBumperPrefab;
    public float bumperFreezeDuration = 5f;
    public float facingGravity = 8f;
    public float launchedGravity = 1.4f;
    readonly List<TicBumper> liveBumpers = new List<TicBumper>();

    [Header("AI / tags")]
    public List<string> hitTags = new List<string> { "Wall", "Walls", "Obstacle" };
    public float dis = 1.38f;

    [SerializeField] Thought thought = Thought.Nothing;

    public int PlayerIndex => pg != null ? pg.playerIndex : -1;

    void Start()
    {
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;
        ResolveParts();
        SetupRigidbodies();
        EnsureAuxParts();

        // Flippers are script-driven; kill leftover movers
        DisableMover(leftPaddle);
        DisableMover(rightPaddle);
        DisableMover(plunger);

        if (leftPaddle != null)
            leftRestRot = leftPaddle.localRotation;
        if (rightPaddle != null)
            rightRestRot = rightPaddle.localRotation;
    }

    void ResolveParts()
    {
        if (rightPaddle == null)
            rightPaddle = FindChild("Right");
        if (leftPaddle == null)
            leftPaddle = FindChild("Left");
        if (plunger == null)
            plunger = FindChild("Center");
        if (loadPoint == null)
            loadPoint = FindChild("LoadPoint");
        if (launchArea == null)
        {
            Transform t = FindChild("LaunchArea");
            if (t != null)
                launchArea = t.GetComponent<TicLaunchArea>();
        }
    }

    Transform FindChild(string name)
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == name)
                return all[i];
        }
        return null;
    }

    void SetupRigidbodies()
    {
        leftRb = EnsureRb(leftPaddle, kinematic: true);
        rightRb = EnsureRb(rightPaddle, kinematic: true);
        plungerRb = EnsureRb(plunger, kinematic: true);

        Rigidbody root = GetComponent<Rigidbody>();
        if (root != null)
        {
            root.useGravity = false;
            root.isKinematic = true; // Tic body stays planted; only paddles/plunger animate
        }
    }

    static Rigidbody EnsureRb(Transform t, bool kinematic)
    {
        if (t == null)
            return null;
        Rigidbody rb = t.GetComponent<Rigidbody>();
        if (rb == null)
            rb = t.gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = kinematic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        return rb;
    }

    static void DisableMover(Transform t)
    {
        if (t == null)
            return;
        MoveUntilCollision m = t.GetComponent<MoveUntilCollision>();
        if (m != null)
            m.enabled = false;
    }

    void EnsureAuxParts()
    {
        if (loadPoint == null)
        {
            GameObject go = new GameObject("LoadPoint");
            go.transform.SetParent(transform, false);
            // Sit "above" plunger toward court so gravity rolls into bay
            go.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            loadPoint = go.transform;
        }

        if (launchArea == null)
        {
            GameObject go = new GameObject("LaunchArea");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = plunger != null ? plunger.localPosition : new Vector3(0f, 1.2f, 0f);
            go.transform.localScale = new Vector3(3.2f, 2.4f, 2f);
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = Vector3.one;
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            launchArea = go.AddComponent<TicLaunchArea>();
        }
        else
        {
            // Prefab bay must stay trigger so balls can enter
            Collider[] bayCols = launchArea.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < bayCols.Length; i++)
            {
                if (bayCols[i] != null)
                    bayCols[i].isTrigger = true;
            }
        }

        launchArea.SetOwner(this);

        PlayerGrab areaGrab = launchArea.GetComponent<PlayerGrab>();
        if (areaGrab == null)
            areaGrab = launchArea.gameObject.AddComponent<PlayerGrab>();
        areaGrab.playerIndex = pg != null ? pg.playerIndex : -1;

        if (launchArea.GetComponent<SetBallOwnerOnTagHit>() == null)
        {
            SetBallOwnerOnTagHit own = launchArea.gameObject.AddComponent<SetBallOwnerOnTagHit>();
            own.targetTags = new List<string> { "Ball" };
        }
    }

    void Update()
    {
        if (db == null || pg == null || pg.player == null)
            return;
        if (!db.gameStart || pg.player.currentHealth <= 0)
            return;

        if (pg.player.CanMove)
            UpdateFlippers();

        if (pg.player.CanBump)
            UpdatePlungerInput();

        UpdatePlungerMotion();

        // Super meter always charges; at full auto-loads a bumper into the hollow
        if (pg.player.super != null)
        {
            pg.player.super.Charge();
            TryAutoSpawnBumper();
        }

        if (pg.player.CanSuper)
            UpdateFreezeInput();

        if (pg.player.bump != null)
            pg.player.bump.Charge();

        // Pass index to parts
        SyncChildGrabs();
    }

    void SyncChildGrabs()
    {
        int idx = pg.playerIndex;
        SetGrab(leftPaddle, idx);
        SetGrab(rightPaddle, idx);
        SetGrab(plunger, idx);
        if (launchArea != null)
            SetGrab(launchArea.transform, idx);
    }

    static void SetGrab(Transform t, int idx)
    {
        if (t == null)
            return;
        PlayerGrab g = t.GetComponent<PlayerGrab>();
        if (g != null)
            g.playerIndex = idx;
    }

    void UpdateFlippers()
    {
        bool leftHeld;
        bool rightHeld;

        if (pg.player.computer)
        {
            ComputerAI.Decision d = ComputerAI.Evaluate(transform, pg.player, hitTags, dis);
            leftHeld = d.moveDir < 0 || d.wantBump;
            rightHeld = d.moveDir > 0;
            if (d.wantBump) thought = Thought.MoveUp;
            else if (d.wantSuper) thought = Thought.MoveDown;
            else thought = Thought.Nothing;
        }
        else
        {
            thought = Thought.Nothing;
            leftHeld = pg.inp.left;
            rightHeld = pg.inp.right;
        }

        if(pg.player.facing == Facing.Down)
        {
            bool lh = rightHeld;
            bool rh = leftHeld;

            leftHeld = lh;
            rightHeld = rh;
        }

        float dt = Time.deltaTime;
        leftAngle = MoveAngle(leftAngle, leftHeld ? flipAngle : 0f, leftHeld ? flipUpSpeed : flipDownSpeed, dt);
        rightAngle = MoveAngle(rightAngle, rightHeld ? flipAngle : 0f, rightHeld ? flipUpSpeed : flipDownSpeed, dt);

        // Both paddles flip the same local way (meshes are mirrored via Y rotation)
        ApplyFlip(leftPaddle, leftRb, leftRestRot, leftAngle, 1f);
        ApplyFlip(rightPaddle, rightRb, rightRestRot, rightAngle, 1f);
    }

    static float MoveAngle(float current, float target, float degPerSec, float dt)
    {
        return Mathf.MoveTowards(current, target, degPerSec * dt);
    }

    void ApplyFlip(Transform paddle, Rigidbody rb, Quaternion rest, float angle, float sign)
    {
        if (paddle == null)
            return;
        Quaternion goal = rest * Quaternion.AngleAxis(angle * sign, flipAxis.normalized);
        paddle.localRotation = goal;
        if (rb != null && rb.isKinematic)
            rb.MoveRotation(paddle.rotation);
    }

    void UpdatePlungerInput()
    {
        bool want = pg.player.computer
            ? thought == Thought.MoveUp
            : (pg.inp.tf_bump || pg.inp.bump);

        // Edge for human: prefer tf; hold OK for AI thought one-shot
        if (!pg.player.computer && !pg.inp.tf_bump)
            want = false;

        if (!want || plungerPhase != 0)
            return;
        if (Time.time < bumpReadyAt)
            return;

        Skill b = pg.player.bump;
        if (b == null || !b.Enough())
            return;

        // Capture rest only when firing so you can freely place the plunger in edit/play
        _plungerRestLocal = plunger.localPosition;
        plungerT = 0f;
        plungerPhase = 1;
        bumpReadyAt = Time.time + bumpCooldown;
        b.Spend();
        b.readyPercent = 0f;
        thought = Thought.Nothing;

        if (launchArea != null)
        {
            Vector3 vel = transform.up * plungerLaunchSpeed;
            launchArea.PlungerLaunch(vel, 0.75f);
        }
    }

    void UpdatePlungerMotion()
    {
        // Idle: do not touch transform — authoring / Character Creation can move it
        if (plunger == null || plungerPhase == 0)
            return;

        float dt = Time.deltaTime;
        if (plungerPhase == 1)
        {
            plungerT = Mathf.MoveTowards(plungerT, 1f, (plungerShootSpeed / Mathf.Max(0.01f, plungerTravel)) * dt);
            if (plungerT >= 1f)
                plungerPhase = 2;
        }
        else if (plungerPhase == 2)
        {
            plungerT = Mathf.MoveTowards(plungerT, 0f, (plungerReturnSpeed / Mathf.Max(0.01f, plungerTravel)) * dt);
            if (plungerT <= 0f)
            {
                plungerT = 0f;
                plungerPhase = 0;
                ApplyPlungerLocal(_plungerRestLocal);
                return;
            }
        }

        Vector3 pos = _plungerRestLocal + Vector3.up * (plungerTravel * plungerT);
        ApplyPlungerLocal(pos);
    }

    void ApplyPlungerLocal(Vector3 localPos)
    {
        if (plunger == null)
            return;
        plunger.localPosition = localPos;
        if (plungerRb != null && plungerRb.isKinematic)
            plungerRb.position = plunger.position;
    }

    Vector3 _plungerRestLocal;

    void TryAutoSpawnBumper()
    {
        Skill s = pg.player.super;
        if (s == null || !s.Enough())
            return;

        SpawnBumperIntoLoader();
        s.Spend();
        s.readyPercent = 0f;
    }

    void UpdateFreezeInput()
    {
        bool want = pg.player.computer
            ? thought == Thought.MoveDown
            : pg.inp.tf_super;

        if (!want)
            return;

        int frozen = 0;
        for (int i = liveBumpers.Count - 1; i >= 0; i--)
        {
            TicBumper b = liveBumpers[i];
            if (b == null)
            {
                liveBumpers.RemoveAt(i);
                continue;
            }
            if (b.TryFreeze(PlayerIndex, bumperFreezeDuration))
                frozen++;
        }

        if (frozen > 0)
        {
            pg.player.RecordUltUsed();
            thought = Thought.Nothing;
        }
    }

    void SpawnBumperIntoLoader()
    {
        Vector3 pos = loadPoint != null ? loadPoint.position : transform.position + transform.up * 2f;
        Quaternion rot = transform.rotation;

        GameObject go;
        if (bumperPrefab != null)
        {
            go = Instantiate(bumperPrefab, pos, rot);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "TicBumper";
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = Vector3.one * 0.85f;
            SphereCollider sc = go.GetComponent<SphereCollider>();
            if (sc != null)
                sc.material = null;
        }

        go.tag = "Paddle";
        if (go.GetComponent<ClearAfterTheGame>() == null)
            go.AddComponent<ClearAfterTheGame>();

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null)
            rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 0.8f;

        PlayerGrab grab = go.GetComponent<PlayerGrab>();
        if (grab == null)
            grab = go.AddComponent<PlayerGrab>();
        grab.playerIndex = PlayerIndex;

        if (go.GetComponent<SetBallOwnerOnTagHit>() == null)
        {
            SetBallOwnerOnTagHit own = go.AddComponent<SetBallOwnerOnTagHit>();
            own.targetTags = new List<string> { "Ball" };
        }

        TicBumper bumper = go.GetComponent<TicBumper>();
        if (bumper == null)
            bumper = go.AddComponent<TicBumper>();
        bumper.loadGravity = facingGravity;
        bumper.launchedGravity = launchedGravity;
        bumper.Init(this, PlayerIndex);

        RegisterLiveBumper(bumper);

        if (launchArea != null)
            launchArea.RegisterBumperPassThrough(bumper);

        // Feed ramp / side cutout sits outside the launch trigger — kick into Loading slide
        bumper.BeginFeedIntoBay(3.5f);

        PlayerSkin.Apply(go, pg.player, db);
    }

    public void RegisterLiveBumper(TicBumper bumper)
    {
        if (bumper == null || liveBumpers.Contains(bumper))
            return;
        liveBumpers.Add(bumper);
    }

    public void UnregisterBumper(TicBumper bumper)
    {
        liveBumpers.Remove(bumper);
    }
}
