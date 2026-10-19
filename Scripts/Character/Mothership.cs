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
    float fleetReturnDiscount;
    int fleetSide;
    Transform lazer1;
    Transform lazer2;
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
        if (GetComponent<Collider>() == null)
            FitCollider();
        if (GetComponent<DamageOnTagHit>() == null)
        {
            DamageOnTagHit dmg = gameObject.AddComponent<DamageOnTagHit>();
            dmg.tagHit = "Ball";
        }
        gameObject.layer = 3;
        transform.localScale = new Vector3(131.32f, 131.32f, 131.32f);
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
        transform.localScale = new Vector3(131.32f, 131.32f, 131.32f);
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
            if (n == "Side Lazer 1") lazer1 = all[i];
            else if (n == "Side Lazer 2") lazer2 = all[i];
            else if (n == "Main Lazer Port") mainPort = all[i];
            else if (n == "Fleet Spawn 1") fleetSpawn1 = all[i];
            else if (n == "Fleet Spawn 2") fleetSpawn2 = all[i];
            else if (n == "Fleet Despawn") fleetDespawn = all[i];
        }
    }

    void FitCollider()
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
            return;
        Bounds bounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            bounds.Encapsulate(rends[i].bounds);
        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.center = transform.InverseTransformPoint(bounds.center);
        Vector3 size = bounds.size;
        Vector3 scale = transform.lossyScale;
        box.size = new Vector3(
            size.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            size.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
            size.z / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
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
        if (progress >= 10f) laserCooldown = 1.5f;
        if (progress >= 40f) laserCooldown = 1.05f;
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
        int count = 1;
        if (progress >= 95f) count = 5;
        else if (progress >= 35f) count = 2;
        float span = progress >= 95f ? 64f : 40f;
        ShootPort(lazer1, count, span);
        ShootPort(lazer2, count, span);
    }

    void ShootPort(Transform port, int count, float span)
    {
        if (port == null)
            return;
        float homing = progress >= 20f ? 55f : 0f;
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

    float ShipRadius()
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
        GameObject ship = Instantiate(fleetPrefab, from.position, Quaternion.LookRotation(Vector3.forward, from.up));
        ship.transform.localScale = transform.lossyScale * 0.1f;
        MothershipFleet fleet = ship.GetComponent<MothershipFleet>();
        if (fleet == null)
            fleet = ship.AddComponent<MothershipFleet>();
        fleet.Begin(this, fleetDespawn != null ? fleetDespawn : transform);
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
        PlayerGrab ball = col.GetComponentInParent<PlayerGrab>();
        if (ball == null)
            return;
        bool mine = ball.playerIndex == pg.playerIndex;
        ball.playerIndex = pg.playerIndex;
        if (!mine && grantSuper)
            AddSuper(22f);
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
        PlayerGrab ball = collision.collider.GetComponentInParent<PlayerGrab>();
        if (ball != null && pg != null && ball.playerIndex == pg.playerIndex)
            return;
        hitLock = 0.25f;
        progress = Mathf.Clamp(progress - progressLossOnHit, 0f, 100f);
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
    bool scored;
    LineRenderer line;
    readonly System.Collections.Generic.HashSet<int> bounced = new System.Collections.Generic.HashSet<int>();

    public static void Shoot(Mothership owner, Transform port, Vector3 direction, float homing, bool grantSuper)
    {
        GameObject go = new GameObject("Side Lazer");
        MothershipLaser laser = go.AddComponent<MothershipLaser>();
        laser.owner = owner;
        laser.dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up;
        laser.dir.z = 0f;
        if (laser.dir.sqrMagnitude < 0.0001f)
            laser.dir = Vector3.up;
        laser.dir.Normalize();
        laser.head = port.position + laser.dir * 0.4f;
        laser.head.z = port.position.z;
        laser.homing = homing;
        laser.grantSuper = grantSuper;
        laser.line = go.AddComponent<LineRenderer>();
        laser.line.positionCount = 2;
        laser.line.startWidth = 0.07f;
        laser.line.endWidth = 0.04f;
        laser.line.material = Mothership.GlowMaterial(new Color(1f, 0.35f, 0.35f, 1f), 2.2f, false);
        laser.line.useWorldSpace = true;
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
        Vector3 next = head + dir * step;
        next.z = head.z;
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
            Destroy(gameObject);
            return true;
        }
        return false;
    }

    static bool IsBall(Collider col)
    {
        return col.GetComponentInParent<BallInfo>() != null || col.CompareTag("Ball");
    }

    static bool IsWallOrStructure(Collider col)
    {
        string tag = col.tag;
        if (tag == "Wall" || tag == "Walls" || tag == "Obstacle")
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
        float best = 7f;
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
            if (Vector3.Angle(dir, to) > 70f)
                continue;
            best = dist;
            aim = to / dist;
        }
        if (aim != dir)
            dir = Vector3.RotateTowards(dir, aim, homing * Mathf.Deg2Rad * Time.deltaTime, 0f);
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
        LineRenderer line = GetComponent<LineRenderer>();
        if (line != null)
        {
            line.SetPosition(0, port.position);
            line.SetPosition(1, port.position + port.up * 12f);
        }
        transform.position = port.position + port.up * 6f;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, port.up);
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
    Mothership owner;
    Transform home;
    Vector3 center;
    float radius = 6f;
    float angle;
    float spin = 28f;
    bool returning;
    float shotWait = 1.5f;
    float traveled;
    bool onOrbit;

    public void Begin(Mothership ship, Transform despawn)
    {
        owner = ship;
        home = despawn;
        if (GaurdCursor.TryBounds(out Bounds box))
        {
            center = box.center;
            radius = Mathf.Min(box.extents.x, box.extents.y) * 0.72f;
        }
        else
            center = ship.transform.position;
        center.z = ship.transform.position.z;
        Vector3 offset = transform.position - center;
        offset.z = 0f;
        angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        if (GetComponent<Collider>() == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = 0.35f;
        }
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
    }

    void Update()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }
        float dt = Time.deltaTime;
        if (!returning && !onOrbit)
        {
            Vector3 outDir = transform.up;
            outDir.z = 0f;
            if (outDir.sqrMagnitude < 0.001f)
                outDir = Vector3.up;
            transform.position += outDir.normalized * 5.5f * dt;
            if (Vector3.Distance(transform.position, center) >= radius * 0.9f)
                onOrbit = true;
            TryShoot();
            CheckCrash();
            shotWait -= dt;
            return;
        }
        if (!returning)
        {
            angle += spin * dt;
            traveled += spin * dt;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f) * radius;
            pos.z = center.z;
            Vector3 tangent = new Vector3(-Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad), 0f);
            tangent = Avoid(pos, tangent);
            transform.position = pos;
            if (tangent.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(Vector3.forward, tangent);
            TryShoot();
            if (traveled > 300f)
                returning = true;
            CheckCrash();
        }
        else if (home != null)
        {
            Vector3 next = Vector3.MoveTowards(transform.position, home.position, 4.5f * dt);
            next.z = center.z;
            Vector3 face = home.position - transform.position;
            face.z = 0f;
            transform.position = next;
            if (face.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(Vector3.forward, face.normalized);
            if (Vector3.Distance(transform.position, home.position) < 0.55f)
            {
                owner.FleetReturned();
                Destroy(gameObject);
            }
        }
        shotWait -= dt;
    }

    Vector3 Avoid(Vector3 pos, Vector3 tangent)
    {
        if (Physics.Raycast(pos, tangent, out RaycastHit hit, 1.6f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && hit.collider.GetComponentInParent<MothershipFleet>() == null
                && hit.collider.GetComponentInParent<Mothership>() == null)
            {
                Vector3 inwards = center - pos;
                inwards.z = 0f;
                return (tangent + inwards.normalized * 1.4f).normalized;
            }
        }
        return tangent;
    }

    void TryShoot()
    {
        if (shotWait > 0f)
            return;
        BallInfo[] balls = Object.FindObjectsByType<BallInfo>(FindObjectsInactive.Exclude);
        Vector3 forward = transform.up;
        forward.z = 0f;
        for (int i = 0; i < balls.Length; i++)
        {
            if (balls[i] == null)
                continue;
            Vector3 to = balls[i].transform.position - transform.position;
            to.z = 0f;
            if (to.magnitude > 9f || Vector3.Angle(forward, to) > 18f)
                continue;
            shotWait = 1.5f;
            MothershipShot.Fire(owner, transform.position, to.normalized);
            return;
        }
    }

    void CheckCrash()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.35f, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null || hits[i].transform == transform || hits[i].transform.IsChildOf(transform))
                continue;
            if (hits[i].GetComponentInParent<MothershipFleet>() != null)
                continue;
            if (hits[i].GetComponentInParent<Mothership>() != null)
                continue;
            Destroy(gameObject);
            return;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;
        if (collision.collider.GetComponentInParent<MothershipFleet>() != null)
            return;
        if (collision.collider.GetComponentInParent<Mothership>() != null)
            return;
        Destroy(gameObject);
    }
}

public class MothershipShot : MonoBehaviour
{
    Mothership owner;
    Vector3 dir;
    float age;

    public static void Fire(Mothership owner, Vector3 pos, Vector3 dir)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Fleet Shot";
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.16f;
        Collider col = go.GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
        Renderer rend = go.GetComponent<Renderer>();
        if (rend != null)
            rend.sharedMaterial = Mothership.GlowMaterial(new Color(1f, 0.85f, 0.3f, 1f), 2f, false);
        MothershipShot shot = go.AddComponent<MothershipShot>();
        shot.owner = owner;
        shot.dir = dir;
    }

    void Update()
    {
        age += Time.deltaTime;
        transform.position += dir * 14f * Time.deltaTime;
        if (age > 1.4f)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (owner == null || other == null)
            return;
        if (other.GetComponentInParent<Mothership>() != null || other.GetComponentInParent<MothershipFleet>() != null)
            return;
        if (other.GetComponentInParent<BallInfo>() == null && !other.CompareTag("Ball"))
            return;
        owner.ClaimBall(other, true);
        Destroy(gameObject);
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
