using UnityEngine;

public enum MothershipActive
{
    Lasers,
    Shield
}

/// <summary>
/// Immobile carrier. Progress climbs while she lives and unlocks weapons.
/// Public cooldown and progress fields are for the Player Info UI.
/// </summary>
public class Mothership : MonoBehaviour
{
    const float ShipScale = 525.28f;

    public GameObject fleetPrefab;

    [Header("Progress")]
    public float progress;
    public float progressPerSecond = 100f / 300f;
    public float progressLossOnHit = 8f;

    [Header("Active")]
    public MothershipActive active = MothershipActive.Lasers;

    [Header("Cooldowns")]
    public float laserCooldown = 3f;
    public float laserCooldownLeft;
    public float shieldCooldown = 6f;
    public float shieldCooldownLeft;
    public float fleetCooldown = 12f;
    public float fleetCooldownLeft;
    public float bumpCooldown;
    public float bumpCooldownLeft;
    public float rotationSpeed = 70f;

    public bool shieldUnlocked;
    public bool fleetUnlocked;
    public bool superCharging;
    public bool mainBeamFiring;

    PlayerGrab pg;
    Rigidbody body;
    Vector3 pinned;
    Quaternion authored;
    float spin;
    float hitLock;
    Collider pendingClaim;
    float fleetReturnDiscount;
    int fleetSide;
    readonly Transform[] sideLazers = new Transform[8];
    Transform mainPort;
    Transform fleetSpawn1;
    Transform fleetSpawn2;
    Transform fleetDespawn;
    MothershipBeam beam;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        body.constraints = RigidbodyConstraints.FreezePosition;
        FitCollider();
        if (GetComponent<DamageOnTagHit>() == null)
        {
            DamageOnTagHit dmg = gameObject.AddComponent<DamageOnTagHit>();
            dmg.tagHit = "Ball";
        }
        gameObject.layer = 3;
        transform.localScale = Vector3.one * ShipScale;
        CachePorts();
    }

    bool placed;

    void Start()
    {
        pg = GetComponent<PlayerGrab>();
        if (fleetPrefab == null)
            fleetPrefab = Resources.Load<GameObject>("Fleetship");
        TryPlace();
        Hold();
    }

    void TryPlace()
    {
        if (placed)
            return;
        if (pg == null || !pg.IsLinked() || pg.player == null || Database.instance == null)
            return;

        Vector3 into = PaddleWall.IntoField(pg.player.facing);
        float half = Mathf.Max(1f, Database.instance.FieldPlaySize * 0.5f);
        float planeZ = Database.instance.ActivePlayPlaneZ;
        Vector3 spot = new Vector3(0f, 0f, planeZ) - into * (half * 0.32f);
        spot.z = planeZ;
        transform.localScale = Vector3.one * ShipScale;
        transform.position = spot;
        pinned = spot;
        authored = Quaternion.Euler(180f, 0f, WallRoll(pg.player.facing));
        placed = true;
    }

    static float WallRoll(Facing facing)
    {
        switch (facing)
        {
            case Facing.Down: return 180f;
            case Facing.Left: return 90f;
            case Facing.Right: return 270f;
            default: return 0f;
        }
    }

    void CachePorts()
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            string n = all[i].name;
            if (n.StartsWith("Side Lazer ") && int.TryParse(n.Substring("Side Lazer ".Length), out int index)
                && index >= 1 && index <= sideLazers.Length)
                sideLazers[index - 1] = all[i];
            else if (n == "Main Lazer" || n == "Main Lazer Port") mainPort = all[i];
            else if (n == "Fleet Spawn 1") fleetSpawn1 = all[i];
            else if (n == "Fleet Spawn 2") fleetSpawn2 = all[i];
            else if (n == "Fleet Despawn") fleetDespawn = all[i];
        }
    }

    void FitCollider()
    {
        FitSphere(gameObject);
    }

    public static SphereCollider FitSphere(GameObject go)
    {
        Collider[] existing = go.GetComponents<Collider>();
        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] == null)
                continue;
            existing[i].enabled = false;
            Destroy(existing[i]);
        }
        SphereCollider sphere = go.AddComponent<SphereCollider>();
        if (!MeshSphere(go.transform, out Vector3 center, out float radius))
        {
            sphere.center = Vector3.zero;
            sphere.radius = 0.5f;
            return sphere;
        }
        sphere.center = center;
        sphere.radius = radius;
        return sphere;
    }

    public static bool MeshSphere(Transform root, out Vector3 center, out float radius)
    {
        center = Vector3.zero;
        radius = 0.5f;
        Renderer[] rends = root.GetComponentsInChildren<Renderer>();
        bool any = false;
        Bounds bounds = new Bounds(root.position, Vector3.zero);
        for (int i = 0; i < rends.Length; i++)
        {
            Renderer rend = rends[i];
            if (rend == null || rend is ParticleSystemRenderer || rend is LineRenderer)
                continue;
            if (!any)
            {
                bounds = rend.bounds;
                any = true;
            }
            else
                bounds.Encapsulate(rend.bounds);
        }
        if (!any)
            return false;
        center = root.InverseTransformPoint(bounds.center);
        float world = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
        float scale = Mathf.Max(
            Mathf.Abs(root.lossyScale.x),
            Mathf.Abs(root.lossyScale.y),
            Mathf.Abs(root.lossyScale.z));
        radius = world / Mathf.Max(0.001f, scale);
        return radius > 0.0001f;
    }

    void Update()
    {
        if (pg == null)
            pg = GetComponent<PlayerGrab>();
        if (pg == null)
            pg = GetComponentInParent<PlayerGrab>();
        if (pg == null || !pg.IsLinked() || pg.player == null)
            return;
        TryPlace();
        if (Database.instance == null || !Database.instance.gameStart || pg.player.currentHealth <= 0)
            return;

        TickProgress();
        RefreshStats();
        ReadInput();
        TickCooldowns();
        TickFleet();
    }

    void LateUpdate()
    {
        if (pendingClaim != null)
        {
            ClaimBall(pendingClaim, false);
            pendingClaim = null;
        }
        if (!placed)
            return;
        Hold();
    }

    void Hold()
    {
        transform.SetPositionAndRotation(pinned, Quaternion.AngleAxis(spin, Vector3.forward) * authored);
        if (body != null)
        {
            body.position = pinned;
            body.rotation = transform.rotation;
        }
    }

    void TickProgress()
    {
        progress = Mathf.Clamp(progress + progressPerSecond * Time.deltaTime, 0f, 100f);
    }

    void RefreshStats()
    {
        rotationSpeed = progress >= 5f ? 36f : 22f;
        laserCooldown = 3f;
        if (progress >= 45f) laserCooldown *= 0.75f;
        if (progress >= 75f) laserCooldown = 0.45f;

        shieldCooldown = 6f;
        if (progress >= 25f) shieldCooldown = 4.2f;
        if (progress >= 45f) shieldCooldown *= 0.75f;
        if (progress >= 65f) shieldCooldown = 2.4f;

        fleetCooldown = 14f;
        if (progress >= 60f) fleetCooldown = 9f;
        if (progress >= 70f) fleetCooldown = 6.5f;
        if (progress >= 80f) fleetCooldown = 4.5f;
        if (progress >= 90f) fleetCooldown = 3f;
        if (progress >= 100f) fleetCooldown = 1.1f;

        shieldUnlocked = progress >= 15f;
        fleetUnlocked = progress >= 30f;
        if (!shieldUnlocked && active == MothershipActive.Shield)
            active = MothershipActive.Lasers;
        bumpCooldown = active == MothershipActive.Shield ? shieldCooldown : laserCooldown;
        bumpCooldownLeft = active == MothershipActive.Shield ? shieldCooldownLeft : laserCooldownLeft;
    }

    void ReadInput()
    {
        float turn = 0f;
        if (pg.inp.left || pg.inp.dashLeft) turn += 1f;
        if (pg.inp.right || pg.inp.dashRight) turn -= 1f;
        spin += turn * rotationSpeed * Time.deltaTime;

        if (pg.inp.tf_up)
            active = MothershipActive.Lasers;
        if (pg.inp.tf_down && shieldUnlocked)
            active = MothershipActive.Shield;

        if (pg.inp.tf_bump)
            FireActive();

        if (pg.inp.tf_super && !superCharging && !mainBeamFiring && pg.player.super != null && pg.player.super.Enough())
            StartCoroutine(MainLaser());
    }

    void TickCooldowns()
    {
        if (laserCooldownLeft > 0f)
            laserCooldownLeft -= Time.deltaTime;
        if (shieldCooldownLeft > 0f)
            shieldCooldownLeft -= Time.deltaTime;
        if (fleetCooldownLeft > 0f)
            fleetCooldownLeft -= Time.deltaTime;
        if (hitLock > 0f)
            hitLock -= Time.deltaTime;
    }

    void FireActive()
    {
        if (active == MothershipActive.Shield && shieldUnlocked)
        {
            if (shieldCooldownLeft > 0f)
                return;
            shieldCooldownLeft = shieldCooldown;
            SpawnShield();
            return;
        }

        if (laserCooldownLeft > 0f)
            return;
        laserCooldownLeft = laserCooldown;
        FireSideLasers();
    }

    void FireSideLasers()
    {
        int ports = 2;
        if (progress >= 10f) ports = 4;
        if (progress >= 35f) ports = 6;
        if (progress >= 40f) ports = 8;
        int count = progress >= 95f ? 2 : 1;
        for (int i = 0; i < ports; i++)
            ShootPort(sideLazers[i], count, 40f);
    }

    void ShootPort(Transform port, int count, float span)
    {
        if (port == null)
            return;
        float homing = progress >= 20f ? 170f : 0f;
        if (count <= 1)
        {
            MothershipLaser.Shoot(this, port, port.up, homing, true);
            return;
        }
        float step = span / (count - 1);
        float start = -span * 0.5f;
        for (int i = 0; i < count; i++)
        {
            Vector3 dir = Quaternion.AngleAxis(start + step * i, Vector3.forward) * port.up;
            MothershipLaser.Shoot(this, port, dir, homing, true);
        }
    }

    System.Collections.IEnumerator MainLaser()
    {
        superCharging = true;
        if (pg.player.super != null)
            pg.player.super.Spend();
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "Main Lazer Charge";
        Collider orbCol = orb.GetComponent<Collider>();
        if (orbCol != null)
            Destroy(orbCol);
        orb.transform.SetParent(mainPort != null ? mainPort : transform, false);
        orb.transform.localPosition = Vector3.zero;
        orb.transform.localScale = Vector3.one * 0.15f;
        Renderer orbRend = orb.GetComponent<Renderer>();
        if (orbRend != null)
            orbRend.sharedMaterial = GlowMaterial(new Color(0.45f, 0.85f, 1f, 1f), 2.4f, false);
        float charge = 2f;
        while (charge > 0f)
        {
            charge -= Time.deltaTime;
            float p = 1f - Mathf.Clamp01(charge / 2f);
            orb.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.42f, p);
            yield return null;
        }
        Destroy(orb);
        superCharging = false;
        mainBeamFiring = true;
        if (beam == null)
        {
            GameObject go = new GameObject("Main Lazer");
            beam = go.AddComponent<MothershipBeam>();
        }
        beam.Begin(this, mainPort != null ? mainPort : transform, 5f);
        yield return new WaitForSeconds(5f);
        mainBeamFiring = false;
        if (beam != null)
            beam.End();
    }

    void SpawnShield()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Mothership Shield";
        go.transform.position = transform.position;
        go.transform.rotation = transform.rotation;
        Renderer rend = go.GetComponent<Renderer>();
        if (rend != null)
            rend.sharedMaterial = GlowMaterial(new Color(0.55f, 0.95f, 1f, 0.035f), 0f, true);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        SphereCollider sphere = go.GetComponent<SphereCollider>();
        sphere.isTrigger = true;
        MothershipShield shield = go.AddComponent<MothershipShield>();
        float radius = ShipRadius() * 1.15f;
        bool vamp = progress >= 55f;
        float hold = progress >= 85f ? 1f : 0.5f;
        shield.Begin(this, radius, 0.25f, hold, vamp);
    }

    public Vector3 GunAim
    {
        get
        {
            Transform port = sideLazers != null && sideLazers.Length > 0 ? sideLazers[0] : null;
            Vector3 aim = port != null ? port.up : transform.up;
            aim.z = 0f;
            if (aim.sqrMagnitude < 0.0001f)
                aim = Vector3.up;
            return aim.normalized;
        }
    }

    public bool CanSuper
    {
        get
        {
            if (superCharging || mainBeamFiring)
                return false;
            PlayerGrab grab = pg != null ? pg : GetComponent<PlayerGrab>();
            return grab != null && grab.IsLinked() && grab.player.super != null && grab.player.super.Enough();
        }
    }

    public float ShipRadius()
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
            return 1.2f;
        Bounds bounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
        {
            if (rends[i] != null)
                bounds.Encapsulate(rends[i].bounds);
        }
        return Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.05f;
    }

    void TickFleet()
    {
        if (!fleetUnlocked || fleetPrefab == null)
            return;
        if (fleetCooldownLeft > 0f)
            return;
        float wait = Mathf.Max(0.35f, fleetCooldown - fleetReturnDiscount);
        fleetReturnDiscount = 0f;
        fleetCooldownLeft = wait;
        SpawnFleet(fleetSide == 0 ? fleetSpawn1 : fleetSpawn2);
        fleetSide = 1 - fleetSide;
        if (progress >= 50f)
            StartCoroutine(SecondFleet());
    }

    System.Collections.IEnumerator SecondFleet()
    {
        yield return new WaitForSeconds(0.5f);
        SpawnFleet(fleetSide == 0 ? fleetSpawn1 : fleetSpawn2);
        fleetSide = 1 - fleetSide;
    }

    void SpawnFleet(Transform port)
    {
        if (fleetPrefab == null)
            return;
        Transform from = port != null ? port : transform;
        GameObject ship = Instantiate(fleetPrefab, from.position, MothershipFleet.Face(from.up));
        ship.transform.localScale = transform.lossyScale * 0.1f;
        MothershipFleet fleet = ship.GetComponent<MothershipFleet>();
        if (fleet == null)
            fleet = ship.AddComponent<MothershipFleet>();
        fleet.Begin(this, fleetDespawn != null ? fleetDespawn : transform, from);
    }

    public void FleetReturned()
    {
        fleetReturnDiscount = 2.4f;
    }

    public void AddSuper(float amount)
    {
        if (pg == null || pg.player == null || pg.player.super == null)
            return;
        Skill super = pg.player.super;
        super.amount = Mathf.Clamp(super.amount + amount, 0f, super.max);
    }

    public void ClaimBall(Collider col, bool grantSuper)
    {
        if (col == null || pg == null)
            return;
        BallInfo info = col.GetComponentInParent<BallInfo>();
        if (info == null && !col.CompareTag("Ball"))
            return;
        PlayerGrab[] grabs = BallGrabs(col);
        if (grabs.Length == 0)
            return;
        bool mine = false;
        for (int i = 0; i < grabs.Length; i++)
        {
            if (SameOwner(grabs[i]))
                mine = true;
        }
        for (int i = 0; i < grabs.Length; i++)
        {
            if (grabs[i] != null)
                grabs[i].playerIndex = pg.playerIndex;
        }
        if (!mine && grantSuper)
            AddSuper(30f);
    }

    bool SameOwner(PlayerGrab ballGrab)
    {
        if (ballGrab == null || pg == null || pg.playerIndex < 0)
            return false;
        if (ballGrab.playerIndex == pg.playerIndex)
            return true;
        return ballGrab.IsLinked() && pg.IsLinked() && ballGrab.player == pg.player;
    }

    static PlayerGrab[] BallGrabs(Collider col)
    {
        if (col == null)
            return new PlayerGrab[0];
        PlayerGrab[] grabs = col.GetComponentsInParent<PlayerGrab>(true);
        if (grabs.Length > 0)
            return grabs;
        if (col.attachedRigidbody == null)
            return grabs;
        return col.attachedRigidbody.GetComponents<PlayerGrab>();
    }

    bool OwnsBall(Collider col)
    {
        PlayerGrab[] grabs = BallGrabs(col);
        for (int i = 0; i < grabs.Length; i++)
        {
            if (SameOwner(grabs[i]))
                return true;
        }
        return false;
    }

    public void ApplyVamp(Collider col)
    {
        if (progress < 55f || col == null)
            return;
        BallInfo info = col.GetComponentInParent<BallInfo>();
        if (info == null)
            return;
        MothershipVamp vamp = info.GetComponent<MothershipVamp>();
        if (vamp == null)
            vamp = info.gameObject.AddComponent<MothershipVamp>();
        vamp.ownerIndex = pg != null ? pg.playerIndex : -1;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null || hitLock > 0f)
            return;
        if (!collision.collider.CompareTag("Ball") && collision.collider.GetComponentInParent<BallInfo>() == null)
            return;
        if (pg == null)
            pg = GetComponent<PlayerGrab>();
        if (pg == null)
            pg = GetComponentInParent<PlayerGrab>();
        bool owned = OwnsBall(collision.collider);
        hitLock = 0.25f;
        float loss = owned ? progressLossOnHit * 0.2f : progressLossOnHit;
        progress = Mathf.Clamp(progress - loss, 0f, 100f);
        if (!owned)
            pendingClaim = collision.collider;
    }

    public static Material GlowMaterial(Color color, float emission, bool transparent)
    {
        Shader shader = transparent
            ? Shader.Find("Universal Render Pipeline/Particles/Unlit")
            : Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emission);
        }
        if (transparent)
        {
            Shader clear = Shader.Find("Universal Render Pipeline/Unlit");
            if (clear == null)
                clear = Shader.Find("Sprites/Default");
            if (clear != null)
                mat = new Material(clear);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", Color.black);
        }
        return mat;
    }
}

public class MothershipLaser : MonoBehaviour
{
    const float Speed = 72f;

    Mothership owner;
    Vector3 head;
    Vector3 dir;
    float homing;
    bool grantSuper;
    bool untilWall;
    bool scored;
    float flown;
    float maxRange = 1600f;
    LineRenderer line;
    readonly System.Collections.Generic.HashSet<int> bounced = new System.Collections.Generic.HashSet<int>();

    public static void Shoot(Mothership owner, Transform port, Vector3 direction, float homing, bool grantSuper, bool untilWall = false)
    {
        Vector3 origin = port != null ? port.position : Vector3.zero;
        Shoot(owner, origin, direction, homing, grantSuper, untilWall, 1600f);
    }

    public static void Shoot(Mothership owner, Vector3 origin, Vector3 direction, float homing, bool grantSuper, bool untilWall, float maxRange)
    {
        GameObject go = new GameObject("Side Lazer");
        MothershipLaser laser = go.AddComponent<MothershipLaser>();
        laser.owner = owner;
        laser.dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up;
        laser.dir.z = 0f;
        if (laser.dir.sqrMagnitude < 0.0001f)
            laser.dir = Vector3.up;
        laser.dir.Normalize();
        origin.z = PlayZ(owner);
        laser.head = origin + laser.dir * 0.4f;
        laser.head.z = origin.z;
        laser.homing = homing;
        laser.grantSuper = grantSuper;
        laser.untilWall = untilWall;
        laser.maxRange = Mathf.Max(0.5f, maxRange);
        laser.line = go.AddComponent<LineRenderer>();
        laser.line.positionCount = 2;
        laser.line.startWidth = 0.07f;
        laser.line.endWidth = 0.04f;
        laser.line.material = Mothership.GlowMaterial(new Color(1f, 0.35f, 0.35f, 1f), 2.2f, false);
        laser.line.useWorldSpace = true;
    }

    public static float PlayZ(Mothership owner)
    {
        if (Database.instance != null)
            return Database.instance.ActivePlayPlaneZ;
        if (owner != null)
            return owner.transform.position.z;
        return 0f;
    }

    void Update()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }
        float step = Speed * Time.deltaTime;
        if (homing > 0f)
            Steer();
        head.z = PlayZ(owner);
        Vector3 next = head + dir * step;
        next.z = head.z;
        flown += step;
        if (flown > maxRange)
        {
            Destroy(gameObject);
            return;
        }
        if (Travel(head, next))
            return;
        head = next;
        Vector3 tail = head - dir * 2.4f;
        tail.z = head.z;
        line.SetPosition(0, tail);
        line.SetPosition(1, head);
    }

    bool Travel(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        float len = delta.magnitude;
        if (len < 0.001f)
            return false;
        RaycastHit[] hits = Physics.SphereCastAll(from, 0.1f, delta / len, len, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null || col.isTrigger)
                continue;
            if (col.GetComponentInParent<Mothership>() != null || col.GetComponentInParent<MothershipFleet>() != null)
                continue;
            if (col.GetComponentInParent<MothershipLaser>() != null)
                continue;
            if (IsBall(col))
            {
                BounceBall(col);
                continue;
            }
            if (!IsWallOrStructure(col))
                continue;
            if (untilWall && !IsWall(col))
                continue;
            Destroy(gameObject);
            return true;
        }
        return false;
    }

    static bool IsBall(Collider col)
    {
        return col.GetComponentInParent<BallInfo>() != null || col.CompareTag("Ball");
    }

    static bool IsWall(Collider col)
    {
        string tag = col.tag;
        return tag == "Wall" || tag == "Walls" || tag == "Obstacle";
    }

    static bool IsWallOrStructure(Collider col)
    {
        if (IsWall(col))
            return true;
        if (col.GetComponentInParent<PlayerGrab>() != null)
            return false;
        return col.GetComponentInParent<Renderer>() != null;
    }

    void BounceBall(Collider col)
    {
        int id = col.GetInstanceID();
        if (!bounced.Add(id))
            return;
        owner.ClaimBall(col, grantSuper && !scored);
        scored = true;
        Rigidbody rb = col.attachedRigidbody;
        if (rb == null)
            rb = col.GetComponentInParent<Rigidbody>();
        if (rb == null)
            return;
        Vector3 away = dir;
        away.z = 0f;
        if (away.sqrMagnitude < 0.0001f)
            away = Vector3.up;
        away.Normalize();
        Vector3 incoming = rb.linearVelocity;
        incoming.z = 0f;
        float speed = Mathf.Max(incoming.magnitude, 9f);
        rb.linearVelocity = away * speed;
        Vector3 pos = rb.position;
        pos.z = head.z;
        rb.position = pos + away * 0.15f;
    }

    void Steer()
    {
        BallInfo[] balls = Object.FindObjectsByType<BallInfo>(FindObjectsInactive.Exclude);
        float best = 28f;
        Vector3 aim = dir;
        for (int i = 0; i < balls.Length; i++)
        {
            if (balls[i] == null)
                continue;
            Vector3 to = balls[i].transform.position - head;
            to.z = 0f;
            float dist = to.magnitude;
            if (dist > best || dist < 0.05f)
                continue;
            if (Vector3.Angle(dir, to) > 110f)
                continue;
            best = dist;
            aim = to / dist;
        }
        if (aim != dir)
            dir = Vector3.RotateTowards(dir, aim, homing * Mathf.Deg2Rad * Time.deltaTime, 0f);
        dir.z = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.up;
        dir.Normalize();
    }
}

public class MothershipBeam : MonoBehaviour
{
    Mothership owner;
    Transform port;
    float life;
    BoxCollider box;

    public void Begin(Mothership ship, Transform port, float seconds)
    {
        owner = ship;
        this.port = port;
        life = seconds;
        transform.SetParent(null, true);
        if (box == null)
            box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.5f, 12f, 0.4f);
        LineRenderer line = GetComponent<LineRenderer>();
        if (line == null)
            line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 1.15f;
        line.endWidth = 1.35f;
        line.material = Mothership.GlowMaterial(new Color(0.55f, 0.9f, 1f, 0.85f), 2.5f, true);
        line.useWorldSpace = true;
        gameObject.SetActive(true);
    }

    public void End()
    {
        life = 0f;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (life <= 0f || port == null)
            return;
        life -= Time.deltaTime;
        float z = MothershipLaser.PlayZ(owner);
        Vector3 start = port.position;
        start.z = z;
        Vector3 aim = port.up;
        aim.z = 0f;
        if (aim.sqrMagnitude < 0.0001f)
            aim = Vector3.up;
        aim.Normalize();
        LineRenderer line = GetComponent<LineRenderer>();
        if (line != null)
        {
            line.SetPosition(0, start);
            line.SetPosition(1, start + aim * 12f);
        }
        transform.position = start + aim * 6f;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, aim);
        if (life <= 0f)
            End();
    }

    void OnTriggerStay(Collider other)
    {
        if (owner == null || other == null)
            return;
        if (other.GetComponentInParent<BallInfo>() == null && !other.CompareTag("Ball"))
            return;
        owner.ClaimBall(other, false);
    }
}

public class MothershipShield : MonoBehaviour
{
    Mothership owner;
    float grow;
    float hold;
    float radius;
    bool vamp;
    bool grown;

    public void Begin(Mothership ship, float radius, float growTime, float holdTime, bool vamp)
    {
        owner = ship;
        this.radius = radius;
        grow = growTime;
        hold = holdTime;
        this.vamp = vamp;
        transform.localScale = Vector3.one * 0.15f;
        if (vamp)
            VampBits();
    }

    void VampBits()
    {
        GameObject bits = new GameObject("Vamp");
        bits.transform.SetParent(transform, false);
        ParticleSystem ps = bits.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.startColor = new Color(0.85f, 0.15f, 0.25f, 0.9f);
        main.startSize = 0.12f;
        main.startLifetime = 0.45f;
        main.loop = true;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 28f;
        ParticleSystemRenderer rend = bits.GetComponent<ParticleSystemRenderer>();
        rend.material = Mothership.GlowMaterial(new Color(0.9f, 0.2f, 0.3f, 0.8f), 1.6f, true);
    }

    void Update()
    {
        if (owner != null)
            transform.position = owner.transform.position;
        if (!grown)
        {
            grow -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(grow / 0.25f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.15f, radius * 2f, t);
            if (grow <= 0f)
                grown = true;
            return;
        }
        hold -= Time.deltaTime;
        if (hold <= 0f)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (owner == null || other == null)
            return;
        if (other.GetComponentInParent<BallInfo>() == null && !other.CompareTag("Ball"))
            return;
        owner.ClaimBall(other, false);
        if (vamp)
            owner.ApplyVamp(other);
    }
}

public class MothershipFleet : MonoBehaviour
{
    enum Run { Out, Arc, Return }

    Mothership owner;
    Transform home;
    Transform shotPort;
    Vector3 hub;
    Vector3 outDir;
    float arcRadius = 6f;
    float angle;
    float arcLeft = 180f;
    float arcSign = 1f;
    float speed = 24f;
    Run run = Run.Out;
    float shotWait = 0.15f;
    float bodyRadius = 0.2f;
    bool dying;

    public void Begin(Mothership ship, Transform despawn, Transform spawn)
    {
        owner = ship;
        home = despawn;
        CacheShotPort();
        hub = ship.transform.position;
        hub.z = MothershipLaser.PlayZ(ship);
        Vector3 fromHub = transform.position - hub;
        fromHub.z = 0f;
        if (fromHub.sqrMagnitude < 0.04f && spawn != null)
        {
            fromHub = spawn.up;
            fromHub.z = 0f;
        }
        if (fromHub.sqrMagnitude < 0.0001f)
            fromHub = Vector3.up;
        outDir = fromHub.normalized;
        Vector3 facing = ship.transform.up;
        facing.z = 0f;
        float side = facing.x * outDir.y - facing.y * outDir.x;
        arcSign = side >= 0f ? 1f : -1f;
        arcRadius = Mathf.Max(8f, ship.ShipRadius() * 2.6f);
        angle = Mathf.Atan2(outDir.y, outDir.x) * Mathf.Rad2Deg;
        speed = Mathf.Max(16f, arcRadius * 0.52f);
        SnapPlane();
        transform.rotation = Face(outDir);
        SphereCollider sphere = Mothership.FitSphere(gameObject);
        float scale = Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y),
            Mathf.Abs(transform.lossyScale.z));
        bodyRadius = Mathf.Max(0.05f, sphere.radius * scale);
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
    }

    void Update()
    {
        if (owner == null || dying)
        {
            if (owner == null)
                Destroy(gameObject);
            return;
        }
        float dt = Time.deltaTime;
        hub = owner.transform.position;
        hub.z = MothershipLaser.PlayZ(owner);
        if (run == Run.Out)
        {
            transform.position += outDir * speed * dt;
            transform.rotation = Face(outDir);
            Vector3 fromHub = transform.position - hub;
            fromHub.z = 0f;
            if (fromHub.magnitude >= arcRadius)
            {
                run = Run.Arc;
                angle = Mathf.Atan2(fromHub.y, fromHub.x) * Mathf.Rad2Deg;
            }
        }
        else if (run == Run.Arc)
        {
            float step = speed / Mathf.Max(0.5f, arcRadius) * Mathf.Rad2Deg * dt;
            angle += arcSign * step;
            arcLeft -= step;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 pos = hub + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * arcRadius;
            Vector3 tangent = arcSign * new Vector3(-Mathf.Sin(rad), Mathf.Cos(rad), 0f);
            transform.position = pos;
            if (tangent.sqrMagnitude > 0.001f)
                transform.rotation = Face(tangent);
            if (arcLeft <= 0f)
                run = Run.Return;
        }
        else if (home != null)
        {
            Vector3 goal = home.position;
            goal.z = hub.z;
            Vector3 next = Vector3.MoveTowards(transform.position, goal, speed * dt);
            Vector3 face = goal - transform.position;
            face.z = 0f;
            transform.position = next;
            if (face.sqrMagnitude > 0.001f)
                transform.rotation = Face(face);
            if (Vector3.Distance(transform.position, goal) <= Mathf.Max(1.6f, speed * dt))
            {
                owner.FleetReturned();
                Destroy(gameObject);
                return;
            }
        }
        SnapPlane();
        TryShoot();
        CheckCrash();
        shotWait -= dt;
    }

    void SnapPlane()
    {
        Vector3 pos = transform.position;
        pos.z = hub.z;
        transform.position = pos;
    }

    void TryShoot()
    {
        if (shotWait > 0f)
            return;
        shotWait = 0.7f;
        Vector3 aim = -transform.right;
        aim.z = 0f;
        if (aim.sqrMagnitude < 0.001f)
            aim = outDir;
        aim.Normalize();
        float nose = Mathf.Max(0.35f, bodyRadius * 0.92f);
        Vector3 origin = transform.position + aim * nose;
        float range = Mathf.Clamp(bodyRadius * 0.35f, 3.2f, 8f);
        MothershipLaser.Shoot(owner, origin, aim, 0f, true, true, range);
    }

    void CheckCrash()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, bodyRadius, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!IsForeign(hits[i]))
                continue;
            if (IsBall(hits[i]))
            {
                HitByBall(hits[i]);
                return;
            }
            if (run == Run.Out)
                continue;
            Destroy(gameObject);
            return;
        }
    }

    bool IsForeign(Collider col)
    {
        if (col == null || col.transform == transform || col.transform.IsChildOf(transform))
            return false;
        if (col.GetComponentInParent<MothershipFleet>() != null)
            return false;
        if (col.GetComponentInParent<Mothership>() != null)
            return false;
        if (col.GetComponentInParent<MothershipLaser>() != null)
            return false;
        return true;
    }

    static bool IsBall(Collider col)
    {
        return col.GetComponentInParent<BallInfo>() != null || col.CompareTag("Ball");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (dying || collision == null || collision.collider == null)
            return;
        if (!IsForeign(collision.collider))
            return;
        if (IsBall(collision.collider))
        {
            HitByBall(collision.collider);
            return;
        }
        if (run == Run.Out)
            return;
        Destroy(gameObject);
    }

    void HitByBall(Collider col)
    {
        if (dying)
            return;
        dying = true;
        Burst();
        BounceBall(col);
        if (owner != null)
            owner.ClaimBall(col, true);
        Destroy(gameObject);
    }

    void BounceBall(Collider col)
    {
        Rigidbody rb = col.attachedRigidbody;
        if (rb == null)
            rb = col.GetComponentInParent<Rigidbody>();
        if (rb == null)
            return;
        Vector3 away = rb.position - transform.position;
        away.z = 0f;
        if (away.sqrMagnitude < 0.0001f)
            away = transform.up;
        away.z = 0f;
        away.Normalize();
        Vector3 incoming = rb.linearVelocity;
        incoming.z = 0f;
        Vector3 bounce = Vector3.Reflect(incoming.sqrMagnitude > 0.01f ? incoming.normalized : -away, away);
        bounce.z = 0f;
        if (bounce.sqrMagnitude < 0.0001f)
            bounce = away;
        bounce.Normalize();
        float kept = Mathf.Max(incoming.magnitude, 9f);
        rb.linearVelocity = bounce * kept;
        Vector3 pos = rb.position;
        pos.z = hub.z;
        rb.position = pos + away * (bodyRadius + 0.35f);
    }

    void Burst()
    {
        GameObject go = new GameObject("Fleet Explosion");
        Vector3 pos = transform.position;
        pos.z = hub.z;
        go.transform.position = pos;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.duration = 0.25f;
        main.startLifetime = 0.35f;
        main.startSpeed = Mathf.Max(3f, bodyRadius * 0.8f);
        main.startSize = Mathf.Max(0.2f, bodyRadius * 0.18f);
        main.startColor = new Color(1f, 0.45f, 0.15f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 14) });
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(0.15f, bodyRadius * 0.25f);
        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.material = Mothership.GlowMaterial(new Color(1f, 0.4f, 0.12f, 1f), 2f, false);
        Destroy(go, 0.8f);
    }

    void CacheShotPort()
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == "Lazer")
            {
                shotPort = all[i];
                return;
            }
        }
    }

    public static Quaternion Face(Vector3 travel)
    {
        travel.z = 0f;
        if (travel.sqrMagnitude < 0.0001f)
            travel = Vector3.up;
        // The fleet mesh nose is local -X. Roll that nose onto local +Y, the travel direction.
        return Quaternion.LookRotation(Vector3.forward, travel.normalized) * Quaternion.Euler(0f, 0f, -90f);
    }
}

public class MothershipVamp : MonoBehaviour
{
    public int ownerIndex = -1;
    bool clearAfterHit;

    public void OnDealt(int dealerIndex, int amount)
    {
        if (amount <= 0 || dealerIndex != ownerIndex)
        {
            Destroy(this);
            return;
        }
        Database db = Database.instance;
        if (db != null && db.players != null && ownerIndex >= 0 && ownerIndex < db.players.Count)
        {
            Player owner = db.players[ownerIndex];
            if (owner != null)
                owner.Heal(amount);
        }
        Destroy(this);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;
        if (collision.collider.GetComponentInParent<PlayerGrab>() == null)
            return;
        if (collision.collider.GetComponentInParent<BallInfo>() != null)
            return;
        clearAfterHit = true;
    }

    void LateUpdate()
    {
        if (clearAfterHit)
            Destroy(this);
    }
}
