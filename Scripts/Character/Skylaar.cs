using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Skylaar rides Fairwing above her wall. Up loops, down dives, left and right barrel-roll
/// through the ball. Hits peel the plane apart. On foot she collects the pieces and dies if the ball hits her.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Skylaar : MonoBehaviour
{
    public GameObject fairwingPrefab;

    public bool AbsorbsBallHit => flying && !rolling;
    public bool IsReady => ready;
    public bool IsFlying => flying;
    public bool IsCarrying => carrying != null;
    public bool CanRoll => rollLeft <= 0f && rollCooldown <= 0f;
    public float Pitch => pitch;
    public float PatrolSign => patrolSign;
    public float Altitude => Vector3.Dot(transform.position - floorAnchor, Into());
    public Vector3 WallInto => Into();
    public Vector3 WallLateral => Lateral();

    public bool TryNearestScrap(out float lane)
    {
        lane = 0f;
        SkylaarScrap nearest = null;
        float best = float.PositiveInfinity;
        Vector3 lateral = Lateral();
        for (int i = 0; i < loose.Count; i++)
        {
            SkylaarScrap scrap = loose[i];
            if (scrap == null)
                continue;
            float gap = Vector3.Dot(scrap.transform.position - transform.position, lateral);
            if (Mathf.Abs(gap) < best)
            {
                best = Mathf.Abs(gap);
                lane = gap;
                nearest = scrap;
            }
        }
        return nearest != null;
    }

    public bool TryDockLane(out float lane)
    {
        lane = 0f;
        if (dock == null)
            return false;
        lane = Vector3.Dot(dock.position - transform.position, Lateral());
        return true;
    }

    const float PlaneLength = 1.55f;
    const float BodyRadius = 0.16f;
    const float BodyHeight = 0.42f;
    const float CruiseAltitude = 4.15f;
    const float SineAmp = 0.42f;
    const float CruiseSpeed = 3.4f;
    const float FootSpeed = 1.35f;

    static readonly string[] BreakOrder = { "Front", "Jet", "Wing", "Rocket", "Base" };

    Rigidbody rb;
    PlayerGrab pg;
    CapsuleCollider body;
    Transform planeRoot;
    readonly List<PartState> parts = new List<PartState>();
    readonly List<SkylaarScrap> loose = new List<SkylaarScrap>();

    bool flying = true;
    bool ready;
    float patrolSign = 1f;
    float pitch;
    float sinePhase;
    bool upWasHeld;
    float rollLeft;
    float rollDuration = 0.48f;
    float rollCooldown;
    int rollDir;
    float chipCooldown;
    float hop;
    float jumpBuffer;
    Vector3 home;
    Vector3 floorAnchor;
    Vector3 noseLocal = Vector3.right;
    bool hidLifeline;
    SkylaarScrap carrying;
    Transform dock;
    float laneMin = -8f;
    float laneMax = 8f;
    float ceilingAlt = 16f;

    bool rolling => rollLeft > 0f;

    class PartState
    {
        public string key;
        public Transform transform;
        public Vector3 localPos;
        public Quaternion localRot;
        public Vector3 localScale;
        public bool attached = true;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        body = GetComponent<CapsuleCollider>();
        if (body == null)
            body = gameObject.AddComponent<CapsuleCollider>();
        body.direction = 1;
        body.radius = BodyRadius;
        body.height = BodyHeight;
        body.center = new Vector3(0f, BodyHeight * 0.5f, 0f);
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        if (GetComponent<MeshRenderer>() == null)
            BuildBodyVisual();
        if (fairwingPrefab == null)
            fairwingPrefab = Resources.Load<GameObject>("Fairwing in Pieces");
    }

    void Start()
    {
        home = transform.position;
        floorAnchor = FindFloor(home, Into());
        home = floorAnchor;
        SpawnPlane();
        Vector3 into = Into();
        Vector3 lifted = floorAnchor + into * CruiseAltitude;
        lifted.z = PlayZ();
        transform.position = lifted;
        rb.position = lifted;
        FaceBody(into);
        FacePlane(into, Lateral());
        ready = true;
    }

    void BuildBodyVisual()
    {
        GameObject view = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        view.name = "Body";
        view.transform.SetParent(transform, false);
        view.transform.localPosition = new Vector3(0f, BodyHeight * 0.5f, 0f);
        view.transform.localScale = new Vector3(BodyRadius * 2f, BodyHeight * 0.5f, BodyRadius * 2f);
        Collider extra = view.GetComponent<Collider>();
        if (extra != null)
            Destroy(extra);
        Renderer rend = view.GetComponent<Renderer>();
        if (rend != null)
        {
            Shader shader = Shader.Find("Standard");
            if (shader != null)
            {
                Material mat = new Material(shader);
                mat.color = new Color(0.95f, 0.62f, 0.12f);
                rend.sharedMaterial = mat;
            }
        }
    }

    void SpawnPlane()
    {
        if (fairwingPrefab == null)
            return;
        GameObject go = Instantiate(fairwingPrefab);
        go.name = "Fairwing";
        planeRoot = go.transform;
        planeRoot.SetParent(null, false);
        planeRoot.position = transform.position;
        planeRoot.rotation = Quaternion.identity;
        planeRoot.localScale = Vector3.one;
        FitPlaneScale();
        CacheParts();
        PaintOwned(planeRoot.gameObject);
        planeRoot.SetParent(transform, true);
        planeRoot.localPosition = new Vector3(0f, 0.05f, 0f);
        planeRoot.localRotation = Quaternion.identity;
        BoxCollider hull = go.GetComponent<BoxCollider>();
        if (hull == null)
            hull = go.AddComponent<BoxCollider>();
        FitHull(hull);
    }

    void FitPlaneScale()
    {
        Renderer[] rends = planeRoot.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
            return;
        Bounds bounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            bounds.Encapsulate(rends[i].bounds);
        float span = Mathf.Max(bounds.size.x, bounds.size.y);
        if (span < 0.01f)
            return;
        planeRoot.localScale = Vector3.one * (PlaneLength / span);
    }

    void CacheParts()
    {
        parts.Clear();
        Transform[] kids = planeRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < kids.Length; i++)
        {
            if (kids[i] == planeRoot)
                continue;
            string key = KeyFor(kids[i].name);
            if (key == null)
                continue;
            parts.Add(new PartState
            {
                key = key,
                transform = kids[i],
                localPos = kids[i].localPosition,
                localRot = kids[i].localRotation,
                localScale = kids[i].localScale
            });
        }
        CacheNose();
    }

    void CacheNose()
    {
        Transform front = null;
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i].key == "Front" && parts[i].transform != null)
                front = parts[i].transform;
        }
        if (front == null || planeRoot == null)
            return;
        Vector3 local = planeRoot.InverseTransformPoint(front.position);
        local.z = 0f;
        if (local.sqrMagnitude > 0.0001f)
            noseLocal = local.normalized;
    }

    static string KeyFor(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        for (int i = 0; i < BreakOrder.Length; i++)
        {
            if (name.StartsWith(BreakOrder[i], System.StringComparison.OrdinalIgnoreCase))
                return BreakOrder[i];
        }
        return null;
    }

    void StripColliders(GameObject go)
    {
        Collider[] cols = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null)
                Destroy(cols[i]);
        }
    }

    public void PaintOwned(GameObject root)
    {
        if (root == null || pg == null || pg.player == null)
            return;
        PlayerSkin.Apply(root, pg.player, Database.instance);
    }

    void FadeShip(Transform root, float alpha)
    {
        if (root == null)
            return;
        Renderer[] rends = root.GetComponentsInChildren<Renderer>(true);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        for (int i = 0; i < rends.Length; i++)
        {
            Renderer rend = rends[i];
            if (rend == null)
                continue;
            Material mat = rend.material;
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;

            rend.GetPropertyBlock(block);
            Color color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
            if (block.HasColor("_BaseColor"))
                color = block.GetColor("_BaseColor");
            else if (block.HasColor("_Color"))
                color = block.GetColor("_Color");
            color.a = alpha;
            if (mat.HasProperty("_BaseColor"))
                block.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                block.SetColor("_Color", color);
            rend.SetPropertyBlock(block);
        }
    }

    public static void GlowMaterial(Renderer rend, float alpha)
    {
        if (rend == null)
            return;
        Material mat = rend.material;
        Color glow = new Color(1f, 0.62f, 0.18f, alpha);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", glow);
        if (mat.HasProperty("_Color"))
            mat.color = glow;
        if (alpha < 0.95f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                Material ghost = new Material(shader);
                ghost.color = glow;
                if (ghost.HasProperty("_BaseColor"))
                    ghost.SetColor("_BaseColor", glow);
                if (ghost.HasProperty("_EmissionColor"))
                {
                    ghost.EnableKeyword("_EMISSION");
                    ghost.SetColor("_EmissionColor", new Color(1f, 0.48f, 0.08f) * 3.2f);
                }
                rend.material = ghost;
                return;
            }
        }
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1f, 0.48f, 0.08f) * (alpha < 0.9f ? 3.2f : 1.6f));
        }
        if (alpha < 0.95f)
        {
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
    }

    void FitHull(BoxCollider hull)
    {
        Renderer[] rends = planeRoot.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
            return;
        Bounds bounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            bounds.Encapsulate(rends[i].bounds);
        hull.center = planeRoot.InverseTransformPoint(bounds.center);
        Vector3 size = bounds.size;
        Vector3 scale = planeRoot.lossyScale;
        hull.size = new Vector3(
            size.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            size.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
            size.z / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
    }

    void Update()
    {
        if (!ready || pg == null || pg.player == null)
            return;
        if (Database.instance != null && !Database.instance.gameStart)
            return;
        if (pg.player.currentHealth <= 0)
            return;

        HideOwnLifeline();
        if (rollCooldown > 0f)
            rollCooldown -= Time.deltaTime;
        if (chipCooldown > 0f)
            chipCooldown -= Time.deltaTime;

        if (flying)
            ReadFlightInput();
        else
            ReadFootInput();
    }

    void ReadFlightInput()
    {
        bool up = pg.inp.up;
        bool down = pg.inp.down;
        if (up)
            pitch += 70f * Grip() * Time.deltaTime;
        else if (down)
            pitch -= 80f * Time.deltaTime;
        else
            pitch = Mathf.MoveTowards(pitch, 0f, 28f * Time.deltaTime);

        if (!up && upWasHeld && pitch > 155f && pitch < 330f)
        {
            patrolSign *= -1f;
            pitch = 0f;
        }
        upWasHeld = up;
        if (pitch >= 360f)
            pitch -= 360f;
        if (pitch < -80f)
            pitch = -80f;

        if (rollLeft <= 0f && rollCooldown <= 0f)
        {
            if (pg.inp.tf_left)
                StartRoll(-1);
            else if (pg.inp.tf_right)
                StartRoll(1);
        }
    }

    void ReadFootInput()
    {
        if (pg.inp.tf_up || pg.inp.tf_bump || Pressed("Jump"))
            jumpBuffer = 0.25f;
        if (GrabPressed())
            Interact();
        if (rollLeft > 0f || rollCooldown > 0f)
            return;
        if (pg.inp.tf_dashRight)
            StartFootRoll(1);
        else if (pg.inp.tf_dashLeft)
            StartFootRoll(-1);
    }

    void FixedUpdate()
    {
        if (!ready || pg == null || pg.player == null || pg.player.currentHealth <= 0)
            return;
        if (Database.instance != null && !Database.instance.gameStart)
            return;

        Vector3 into = Into();
        Vector3 lateral = Lateral();
        MeasureLane(lateral);
        MeasureCeiling(into);

        if (flying)
            Fly(into, lateral);
        else
            Run(into, lateral);

        Vector3 pos = transform.position;
        pos.z = PlayZ();
        transform.position = pos;
        rb.position = pos;
        FaceBody(into);
        if (flying)
            FacePlane(into, lateral);

        if (rolling)
        {
            rollLeft -= Time.fixedDeltaTime;
            IgnoreBalls(true);
            if (rollLeft <= 0f)
            {
                IgnoreBalls(false);
                rollCooldown = footRoll ? 2.75f : 0.65f * (1f + Missing() * 0.8f);
                footRoll = false;
            }
        }
    }

    void Fly(Vector3 into, Vector3 lateral)
    {
        sinePhase += Time.fixedDeltaTime * 1.7f;
        Vector3 pos = rb.position;
        float along = Vector3.Dot(pos, lateral);
        if (Mathf.Abs(pitch) < 12f)
        {
            if (along > laneMax - 0.02f)
                patrolSign = -1f;
            else if (along < laneMin + 0.02f)
                patrolSign = 1f;
        }

        float speed = CruiseSpeed * (pg.inp.down ? 1.45f : 1f);
        float rad = pitch * Mathf.Deg2Rad;
        Vector3 wish = lateral * patrolSign * Mathf.Cos(rad) * speed + into * Mathf.Sin(rad) * speed;
        float alt = Vector3.Dot(pos - floorAnchor, into);
        if (!pg.inp.up && !pg.inp.down && Mathf.Abs(pitch) < 18f)
        {
            float target = CruiseAltitude + Mathf.Sin(sinePhase) * SineAmp;
            float drift = Mathf.Clamp(target - alt, -0.7f, 0.7f);
            wish += into * drift;
        }

        Vector3 next = pos + wish * Time.fixedDeltaTime;
        float lane = Vector3.Dot(next, lateral);
        float clamped = Mathf.Clamp(lane, laneMin, laneMax);
        next += lateral * (clamped - lane);
        next = ClampUnderCeiling(next, into);
        next.z = PlayZ();
        if (alt < 0.42f)
        {
            ShatterAll();
            return;
        }
        rb.MovePosition(next);
        transform.position = next;
    }

    void Run(Vector3 into, Vector3 lateral)
    {
        Vector3 pos = rb.position;
        float input = 0f;
        if (pg.inp.right) input += 1f;
        if (pg.inp.left) input -= 1f;
        float alt = Vector3.Dot(pos - floorAnchor, into);
        if (jumpBuffer > 0f)
            jumpBuffer -= Time.fixedDeltaTime;
        bool feetDown = alt <= 0.5f && hop <= 0.75f;
        if (jumpBuffer > 0f && feetDown)
        {
            hop = 13f;
            jumpBuffer = 0f;
        }
        else if (alt > 0.5f)
            hop -= 26f * Time.fixedDeltaTime;
        else if (hop < 0f)
            hop = 0f;
        Vector3 wish = lateral * input * FootSpeed + into * hop;
        if (!flying && rollLeft > 0f)
            wish += lateral * rollDir * 3.2f;
        if (alt <= 0.5f && hop <= 0f && input != 0f)
            wish += Climb(pos, lateral * input, into);

        Vector3 next = pos + wish * Time.fixedDeltaTime;
        float lane = Vector3.Dot(next, lateral);
        lane = Mathf.Clamp(lane, laneMin, laneMax);
        next += lateral * (lane - Vector3.Dot(next, lateral));
        float floorGap = Vector3.Dot(next - floorAnchor, into);
        if (alt <= 0.5f && hop <= 0f)
            next -= into * floorGap;
        else if (floorGap < 0f)
            next -= into * floorGap;
        next = ClampUnderCeiling(next, into);
        next.z = PlayZ();
        rb.MovePosition(next);
        transform.position = next;
        if (carrying != null)
            carrying.transform.position = transform.position + into * 0.7f;
    }

    Quaternion FlightPose(Vector3 into)
    {
        // Upright on the wall reads as straight up. 90 degrees on Z lays her along the lane.
        return Quaternion.LookRotation(Vector3.forward, into) * Quaternion.Euler(0f, 0f, 0f);
    }

    Quaternion BuildPose(Vector3 into)
    {
        // Points up off her floor, then 90 degrees on Y while the ship is being rebuilt.
        return Quaternion.LookRotation(Vector3.forward, into) * Quaternion.Euler(0f, -90f, 90f);
    }

    void FaceBody(Vector3 into)
    {
        Quaternion pose = FlightPose(into);
        if (!flying && rollLeft > 0f)
        {
            float spun = (1f - Mathf.Clamp01(rollLeft / Mathf.Max(0.05f, rollDuration))) * 360f * rollDir;
            pose = Quaternion.AngleAxis(spun, Vector3.forward) * pose;
        }
        transform.rotation = pose;
        rb.rotation = pose;
    }

    void FacePlane(Vector3 into, Vector3 lateral)
    {
        if (planeRoot == null || (!flying && dock != null))
            return;
        planeRoot.rotation = AimAlongTravel(into, lateral, true);
    }

    float BobPitch()
    {
        if (pg != null && (pg.inp.up || pg.inp.down))
            return 0f;
        if (Mathf.Abs(pitch) >= 12f)
            return 0f;
        float vertical = Mathf.Cos(sinePhase) * SineAmp * 1.7f;
        return Mathf.Atan2(vertical, CruiseSpeed) * Mathf.Rad2Deg;
    }

    Quaternion AimAlongTravel(Vector3 into, Vector3 lateral, bool barrel)
    {
        float aimPitch = pitch + BobPitch();
        float rad = aimPitch * Mathf.Deg2Rad;
        Vector3 travel = lateral * patrolSign * Mathf.Cos(rad) + into * Mathf.Sin(rad);
        travel.z = 0f;
        if (travel.sqrMagnitude < 0.0001f)
            travel = lateral * patrolSign;
        travel.Normalize();

        Quaternion basePose = FlightPose(into);
        Vector3 authored = basePose * noseLocal;
        authored.z = 0f;
        if (authored.sqrMagnitude < 0.0001f)
            authored = lateral;
        authored.Normalize();
        Quaternion pose = Quaternion.FromToRotation(authored, travel) * basePose;
        if (!barrel || !rolling)
            return pose;
        float spun = (1f - Mathf.Clamp01(rollLeft / Mathf.Max(0.05f, rollDuration))) * 360f * rollDir;
        return Quaternion.AngleAxis(spun, travel) * pose;
    }

    Vector3 Climb(Vector3 pos, Vector3 move, Vector3 into)
    {
        if (move.sqrMagnitude < 0.01f)
            return Vector3.zero;
        RaycastHit[] hits = Physics.SphereCastAll(pos, BodyRadius, move.normalized, 0.45f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!SameTeamBlocker(hits[i].collider))
                continue;
            return into * FootSpeed * 0.85f;
        }
        return Vector3.zero;
    }

    bool StandingOnTeammate(Vector3 pos, Vector3 into)
    {
        RaycastHit[] hits = Physics.RaycastAll(pos, -into, 0.7f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (SameTeamBlocker(hits[i].collider))
                return true;
        }
        return false;
    }

    void Interact()
    {
        Vector3 into = Into();
        if (carrying != null)
        {
            if (dock != null && LaneGap(dock.position) < 1.7f)
                Install(carrying);
            else
                Throw(carrying, into);
            return;
        }

        SkylaarScrap nearest = null;
        float best = 2.4f;
        for (int i = 0; i < loose.Count; i++)
        {
            SkylaarScrap scrap = loose[i];
            if (scrap == null)
                continue;
            float gap = LaneGap(scrap.transform.position);
            if (gap < best)
            {
                best = gap;
                nearest = scrap;
            }
        }
        if (nearest == null)
            return;
        carrying = nearest;
        loose.Remove(nearest);
        nearest.Carry();
    }

    float LaneGap(Vector3 world)
    {
        return Mathf.Abs(Vector3.Dot(world - transform.position, Lateral()));
    }

    bool GrabPressed()
    {
        return pg.inp.tf_down || pg.inp.tf_super || Pressed("Interact");
    }

    bool Pressed(string action)
    {
        if (pg == null || pg.player == null)
            return false;
        Player player = pg.player;
        if (Edge(player.cLink, action))
            return true;
        if (player.inputLinks == null)
            return false;
        for (int i = 0; i < player.inputLinks.Count; i++)
        {
            if (Edge(player.inputLinks[i], action))
                return true;
        }
        return false;
    }

    static bool Edge(ControllerLink link, string action)
    {
        if (link == null)
            return false;
        ControllerButtons button = link[action];
        return button != null && button.wasPressedThisFrame;
    }

    void HideOwnLifeline()
    {
        if (hidLifeline || pg == null || pg.player == null)
            return;
        hidLifeline = true;
        if (pg.player.lifeline != null)
            pg.player.lifeline.prefabs = null;
        if (pg.player.spawnedLifeline != null)
        {
            Destroy(pg.player.spawnedLifeline);
            pg.player.spawnedLifeline = null;
        }
    }

    Vector3 FindFloor(Vector3 from, Vector3 into)
    {
        into.z = 0f;
        if (into.sqrMagnitude < 0.0001f)
            into = Vector3.up;
        into.Normalize();
        Collider[] cols = Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude);
        float bestInner = float.NegativeInfinity;
        bool found = false;
        float fromAlong = Vector3.Dot(from, into);
        for (int i = 0; i < cols.Length; i++)
        {
            Collider col = cols[i];
            if (col == null || col.isTrigger)
                continue;
            string tag = col.tag;
            if (tag != "Wall" && tag != "Walls")
                continue;
            Bounds bounds = col.bounds;
            Vector3 ext = bounds.extents;
            float intoExtent = ext.x * Mathf.Abs(into.x) + ext.y * Mathf.Abs(into.y);
            float lateralExtent = ext.x * Mathf.Abs(into.y) + ext.y * Mathf.Abs(into.x);
            if (lateralExtent < intoExtent + 0.05f)
                continue;
            float inner = Vector3.Dot(bounds.center, into) + intoExtent;
            if (inner > fromAlong + 0.35f)
                continue;
            if (fromAlong - inner > 6f)
                continue;
            if (inner > bestInner)
            {
                bestInner = inner;
                found = true;
            }
        }
        if (!found)
            return from;
        Vector3 floor = from + into * (bestInner - fromAlong);
        floor.z = PlayZ();
        return floor;
    }

    void MarkForMatchEnd(GameObject go)
    {
        if (go != null && go.GetComponent<ClearAfterTheGame>() == null)
            go.AddComponent<ClearAfterTheGame>();
    }

    void Install(SkylaarScrap scrap)
    {
        PartState part = FindPart(scrap.PartKey);
        if (part == null || planeRoot == null)
        {
            carrying = null;
            return;
        }
        scrap.Attach(planeRoot, part.localPos, part.localRot, part.localScale);
        ShowInstalledPart(scrap.transform, part);
        part.attached = true;
        part.transform = scrap.transform;
        carrying = null;
        loose.Remove(scrap);
        if (Missing() == 0)
            LaunchAgain();
    }

    void ShowInstalledPart(Transform piece, PartState part)
    {
        if (piece == null)
            return;
        Transform slot = GhostSlot(part.key);
        if (slot != null)
        {
            piece.SetPositionAndRotation(slot.position, slot.rotation);
            piece.SetParent(planeRoot, true);
            slot.gameObject.SetActive(false);
        }
        Renderer[] rends = piece.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++)
        {
            if (rends[i] == null)
                continue;
            rends[i].enabled = true;
        }
        PaintOwned(piece.gameObject);
    }

    Transform GhostSlot(string key)
    {
        Transform ghost = GhostRoot();
        if (ghost == null || string.IsNullOrEmpty(key))
            return null;
        Transform[] kids = ghost.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < kids.Length; i++)
        {
            if (kids[i] == null)
                continue;
            string found = KeyFor(kids[i].name);
            if (found != null && found == key)
                return kids[i];
        }
        return null;
    }

    Transform GhostRoot()
    {
        if (dock == null)
            return null;
        Transform named = dock.Find("Bay Glow");
        return named;
    }

    void Throw(SkylaarScrap scrap, Vector3 into)
    {
        carrying = null;
        loose.Add(scrap);
        scrap.Throw(into * 7.5f + Lateral() * Random.Range(-1.2f, 1.2f), -into);
    }

    void LaunchAgain()
    {
        flying = true;
        pitch = 0f;
        if (planeRoot != null)
        {
            planeRoot.SetParent(transform, true);
            planeRoot.localPosition = new Vector3(0f, 0.05f, 0f);
            planeRoot.localRotation = Quaternion.identity;
            PaintOwned(planeRoot.gameObject);
            BoxCollider hull = planeRoot.GetComponent<BoxCollider>();
            if (hull == null)
                hull = planeRoot.gameObject.AddComponent<BoxCollider>();
            FitHull(hull);
        }
        if (dock != null)
        {
            Destroy(dock.gameObject);
            dock = null;
        }
        Vector3 lifted = floorAnchor + Into() * CruiseAltitude;
        lifted.z = PlayZ();
        transform.position = lifted;
        rb.position = lifted;
        home = floorAnchor;
    }

    public void NotifyBallHit()
    {
        if (!flying || rolling || chipCooldown > 0f)
            return;
        chipCooldown = 0.18f;
        PartState next = NextAttached();
        if (next == null)
        {
            DropToFoot();
            return;
        }
        Break(next);
        if (next.key == "Base" || Missing() >= parts.Count)
            DropToFoot();
    }

    void ShatterAll()
    {
        if (!flying)
            return;
        crashSpread = true;
        for (int i = 0; i < BreakOrder.Length; i++)
        {
            PartState part = FindPart(BreakOrder[i]);
            if (part != null && part.attached)
                Break(part);
        }
        crashSpread = false;
        DropToFoot();
    }

    bool crashSpread;

    void Break(PartState part)
    {
        if (part == null || !part.attached || part.transform == null)
            return;
        part.attached = false;
        Vector3 into = Into();
        SkylaarScrap scrap = part.transform.gameObject.GetComponent<SkylaarScrap>();
        if (scrap == null)
            scrap = part.transform.gameObject.AddComponent<SkylaarScrap>();
        scrap.Setup(this, part.key, -into);
        scrap.Glow();
        MarkForMatchEnd(scrap.gameObject);
        Vector3 lateral = Lateral();
        float side = crashSpread ? Random.Range(-14f, 14f) : Random.Range(-5f, 5f);
        float pop = crashSpread ? Random.Range(4f, 8f) : Random.Range(3.2f, 5.5f);
        Vector3 toss = into * pop + lateral * side;
        scrap.Toss(toss, -into);
        loose.Add(scrap);
    }

    void EnsureDock()
    {
        if (dock != null)
            return;
        Vector3 into = Into();
        GameObject bay = new GameObject("Fairwing Bay");
        MarkForMatchEnd(bay);
        dock = bay.transform;
        Vector3 spot = home + into * 0.45f;
        spot.z = PlayZ();
        dock.position = spot;
        dock.rotation = BuildPose(into);

        if (fairwingPrefab != null)
        {
            GameObject ghost = Instantiate(fairwingPrefab, dock);
            ghost.name = "Bay Glow";
            ghost.transform.localPosition = Vector3.zero;
            ghost.transform.localRotation = Quaternion.identity;
            ghost.transform.localScale = planeRoot != null ? planeRoot.localScale : Vector3.one;
            StripColliders(ghost);
            PaintOwned(ghost);
            FadeShip(ghost.transform, 0.22f);
        }

        if (planeRoot != null)
        {
            StripColliders(planeRoot.gameObject);
            planeRoot.SetParent(dock, false);
            planeRoot.localPosition = Vector3.zero;
            planeRoot.localRotation = Quaternion.identity;
        }

        RaiseDockHalfHeight(into);
    }

    void RaiseDockHalfHeight(Vector3 into)
    {
        if (dock == null)
            return;
        Renderer[] rends = dock.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
            return;
        float min = float.PositiveInfinity;
        float max = float.NegativeInfinity;
        for (int i = 0; i < rends.Length; i++)
        {
            if (rends[i] == null)
                continue;
            Bounds bounds = rends[i].bounds;
            Vector3 center = bounds.center;
            Vector3 extent = bounds.extents;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(extent, new Vector3(x, y, z));
                        float along = Vector3.Dot(corner, into);
                        if (along < min)
                            min = along;
                        if (along > max)
                            max = along;
                    }
                }
            }
        }
        if (max <= min)
            return;
        dock.position += into * ((max - min) * 0.5f);
    }

    void DropToFoot()
    {
        flying = false;
        pitch = 0f;
        rollLeft = 0f;
        hop = 0f;
        IgnoreBalls(false);
        EnsureDock();
    }

    bool footRoll;

    void StartRoll(int dir)
    {
        rollDir = dir;
        footRoll = false;
        rollDuration = 0.48f;
        rollLeft = rollDuration;
        IgnoreBalls(true);
    }

    void StartFootRoll(int dir)
    {
        rollDir = dir;
        footRoll = true;
        rollDuration = 0.42f;
        rollLeft = rollDuration;
    }

    void MeasureLane(Vector3 lateral)
    {
        lateral.z = 0f;
        if (lateral.sqrMagnitude < 0.0001f)
            lateral = Vector3.right;
        lateral.Normalize();
        Vector3 origin = floorAnchor + Into() * 0.4f;
        origin.z = PlayZ();
        float pos = RayToLaneEnd(origin, lateral);
        float neg = RayToLaneEnd(origin, -lateral);
        float here = Vector3.Dot(origin, lateral);
        if (pos < 0.5f || neg < 0.5f)
        {
            float half = 12f;
            Database db = Database.instance;
            if (db != null)
                half = Mathf.Max(8f, db.FieldPlaySize * 0.5f);
            laneMin = here - half;
            laneMax = here + half;
            return;
        }
        laneMin = here - neg - 0.2f;
        laneMax = here + pos + 0.2f;
    }

    void MeasureCeiling(Vector3 into)
    {
        into.z = 0f;
        if (into.sqrMagnitude < 0.0001f)
            into = Vector3.up;
        into.Normalize();
        Vector3 origin = floorAnchor + into * 0.35f;
        origin.z = PlayZ();
        float dist = RayToLaneEnd(origin, into);
        if (dist < 1f)
        {
            float span = 20f;
            Database db = Database.instance;
            if (db != null)
                span = Mathf.Max(10f, db.FieldPlaySize);
            ceilingAlt = span - 1.25f;
            return;
        }
        ceilingAlt = 0.35f + dist - 1.25f;
    }

    Vector3 ClampUnderCeiling(Vector3 next, Vector3 into)
    {
        float alt = Vector3.Dot(next - floorAnchor, into);
        if (alt > ceilingAlt)
            next -= into * (alt - ceilingAlt);
        return next;
    }

    static float RayToLaneEnd(Vector3 origin, Vector3 dir)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, 80f, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return -1f;
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        Vector3 axis = dir.normalized;
        float best = -1f;
        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].transform;
            if (t == null)
                continue;
            if (!PaddleWall.IsBoundaryWallTag(t.tag))
                continue;
            if (hits[i].distance < 0.35f)
                continue;
            if (Mathf.Abs(Vector3.Dot(hits[i].normal, axis)) < 0.35f)
                continue;
            best = hits[i].distance;
            break;
        }
        return best;
    }

    void IgnoreBalls(bool ignore)
    {
        Collider[] cols = GetComponentsInChildren<Collider>();
        Collider[] all = Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null)
                continue;
            for (int n = 0; n < all.Length; n++)
            {
                Collider other = all[n];
                if (other == null || other == cols[i])
                    continue;
                if (!IsBall(other))
                    continue;
                Physics.IgnoreCollision(cols[i], other, ignore);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;
        if (IsBall(collision.collider))
        {
            if (flying)
                NotifyBallHit();
            return;
        }
        if (!flying)
            return;
        if (IsHomeFloor(collision.collider) || Slam(collision.collider))
            ShatterAll();
    }

    bool IsHomeFloor(Collider col)
    {
        return PaddleWall.IsHomeBackWall(col, transform);
    }

    bool Slam(Collider col)
    {
        if (col == null)
            return false;
        if (col.GetComponentInParent<PullObjectIn>() != null && SameTeam(col))
            return Vector3.Dot(rb.position - home, Into()) < CruiseAltitude * 0.55f;
        string tag = col.tag;
        if (tag != "Obstacle" && tag != "Hazard")
            return false;
        return Vector3.Dot((rb.position - transform.position), Into()) <= 0f
            && Vector3.Dot(rb.position - home, Into()) < CruiseAltitude * 0.45f;
    }

    int Missing()
    {
        int n = 0;
        for (int i = 0; i < parts.Count; i++)
        {
            if (!parts[i].attached)
                n++;
        }
        return n;
    }

    float Grip()
    {
        return 1f / (1f + Missing() * 0.6f);
    }

    PartState NextAttached()
    {
        for (int i = 0; i < BreakOrder.Length; i++)
        {
            PartState part = FindPart(BreakOrder[i]);
            if (part != null && part.attached)
                return part;
        }
        return null;
    }

    PartState FindPart(string key)
    {
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i].key == key)
                return parts[i];
        }
        return null;
    }

    Vector3 Into()
    {
        if (pg != null && pg.player != null)
            return PaddleWall.IntoField(pg.player.facing);
        Vector3 up = transform.up;
        up.z = 0f;
        if (up.sqrMagnitude < 0.0001f)
            return Vector3.up;
        return up.normalized;
    }

    Vector3 Lateral()
    {
        Vector3 into = Into();
        if (Mathf.Abs(into.y) >= Mathf.Abs(into.x))
            return Vector3.right;
        return Vector3.up;
    }

    float PlayZ()
    {
        Database db = Database.instance;
        return db != null ? db.FieldPlaySize : transform.position.z;
    }

    bool SameTeam(Collider col)
    {
        if (pg == null || !pg.IsLinked() || col == null)
            return false;
        PlayerGrab other = col.GetComponentInParent<PlayerGrab>();
        if (other == null || !other.IsLinked() || other.player == null)
            return false;
        return other.player.team == pg.player.team;
    }

    public bool GuideScrap(Rigidbody piece)
    {
        if (piece == null)
            return false;
        Vector3 into = Into();
        Vector3 lateral = Lateral();
        Vector3 pos = piece.position;
        float lane = Vector3.Dot(pos, lateral);
        float clamped = Mathf.Clamp(lane, laneMin, laneMax);
        pos += lateral * (clamped - lane);
        float alt = Vector3.Dot(pos - floorAnchor, into);
        if (alt < 0.08f)
            pos += into * (0.08f - alt);
        pos.z = PlayZ();
        piece.position = pos;

        Vector3 vel = piece.linearVelocity;
        vel.z = 0f;
        float laneVel = Vector3.Dot(vel, lateral);
        if (clamped <= laneMin + 0.35f && laneVel < 0f)
            laneVel = -laneVel * Random.Range(0.45f, 0.75f);
        if (clamped >= laneMax - 0.35f && laneVel > 0f)
            laneVel = -laneVel * Random.Range(0.45f, 0.75f);
        float intoVel = Vector3.Dot(vel, into);
        piece.linearVelocity = lateral * laneVel + into * intoVel;
        return alt < 0.4f && intoVel <= 0.35f;
    }

    public bool IsOwnFloor(Collider col)
    {
        if (col == null || !PaddleWall.IsBoundaryWallTag(col.tag))
            return false;
        Vector3 into = Into();
        Bounds bounds = col.bounds;
        Vector3 ext = bounds.extents;
        float intoExtent = ext.x * Mathf.Abs(into.x) + ext.y * Mathf.Abs(into.y);
        float lateralExtent = ext.x * Mathf.Abs(into.y) + ext.y * Mathf.Abs(into.x);
        if (lateralExtent < intoExtent + 0.05f)
            return false;
        float inner = Vector3.Dot(bounds.center, into) + intoExtent;
        float floorAlong = Vector3.Dot(floorAnchor, into);
        return Mathf.Abs(inner - floorAlong) < 1.25f;
    }

    public bool SharesSide(Collider col)
    {
        return SameTeamBlocker(col);
    }

    bool SameTeamBlocker(Collider col)
    {
        if (col == null)
            return false;
        if (col.transform == transform || col.transform.IsChildOf(transform))
            return false;
        bool lifeline = false;
        Transform p = col.transform;
        while (p != null)
        {
            if (p.CompareTag("Lifeline"))
            {
                lifeline = true;
                break;
            }
            p = p.parent;
        }
        bool celestial = col.GetComponentInParent<PullObjectIn>() != null;
        if (!lifeline && !celestial)
            return false;
        return SameTeam(col);
    }

    static bool IsBall(Collider col)
    {
        if (col == null)
            return false;
        if (col.CompareTag("Ball") || col.CompareTag("Ghost"))
            return true;
        return col.GetComponentInParent<BallInfo>() != null;
    }
}

/// <summary>A Fairwing piece falling onto Skylaar's floor, or thrown at the ball.</summary>
public class SkylaarScrap : MonoBehaviour
{
    public string PartKey { get; private set; }
    public bool Settled { get; private set; }
    Vector3 restPos;
    float bobSeed;
    bool hasRest;

    Skylaar owner;
    Rigidbody body;
    Collider col;
    Vector3 floorDir = Vector3.down;
    bool thrown;
    float ignoreUntil;

    public void Setup(Skylaar skylaar, string key, Vector3 towardFloor)
    {
        owner = skylaar;
        PartKey = key;
        floorDir = towardFloor.sqrMagnitude > 0.0001f ? towardFloor.normalized : Vector3.down;
        if (body == null)
            body = gameObject.GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        if (col == null)
            col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                box.center = transform.InverseTransformPoint(rend.bounds.center);
                Vector3 size = rend.bounds.size;
                Vector3 scale = transform.lossyScale;
                box.size = new Vector3(
                    size.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                    size.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
                    size.z / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
            }
            col = box;
        }
        col.isTrigger = false;
        Settled = false;
        thrown = false;
        ignoreUntil = Time.time + 0.15f;
        ApplyIgnores(false);
        Glow();
    }

    public void Glow()
    {
        if (owner != null)
            owner.PaintOwned(gameObject);
    }

    public void Toss(Vector3 velocity, Vector3 towardFloor)
    {
        floorDir = towardFloor.normalized;
        thrown = false;
        Settled = false;
        hasRest = false;
        if (body != null)
        {
            body.isKinematic = false;
            body.linearVelocity = velocity;
        }
        transform.SetParent(null, true);
    }

    public void Throw(Vector3 velocity, Vector3 towardFloor)
    {
        Toss(velocity, towardFloor);
        thrown = true;
        Settled = false;
        ApplyIgnores(true);
    }

    public void Carry()
    {
        Settled = false;
        thrown = false;
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.isKinematic = true;
        }
    }

    public void Attach(Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale)
    {
        if (body != null)
            Destroy(body);
        if (col != null)
            Destroy(col);
        body = null;
        transform.SetParent(parent, false);
        transform.localPosition = localPos;
        transform.localRotation = localRot;
        transform.localScale = localScale;
        enabled = false;
    }

    Vector3 RestAboveFloor(Vector3 pivot)
    {
        float sink = 0.05f;
        float size = 0f;
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < rends.Length; i++)
        {
            if (rends[i] == null)
                continue;
            Bounds bounds = rends[i].bounds;
            size = Mathf.Max(size, bounds.size.x, bounds.size.y);
            Vector3 ext = bounds.extents;
            float toward = ext.x * Mathf.Abs(floorDir.x) + ext.y * Mathf.Abs(floorDir.y) + ext.z * Mathf.Abs(floorDir.z);
            float low = Vector3.Dot(bounds.center - pivot, floorDir) + toward;
            if (low > sink)
                sink = low;
        }
        float extra = size < 0.4f ? 0.42f : 0.16f;
        float lift = sink + extra;
        Vector3 rest = pivot - floorDir * lift;
        rest.z = pivot.z;
        return rest;
    }

    void Update()
    {
        if (!Settled || !hasRest)
            return;
        float wave = (Mathf.Sin(Time.time * 1.7f + bobSeed) + 1f) * 0.16f;
        Vector3 pos = restPos - floorDir * wave;
        pos.z = restPos.z;
        transform.position = pos;
        if (body != null)
            body.position = pos;
    }

    void FixedUpdate()
    {
        if (Settled || body == null || body.isKinematic)
            return;
        body.AddForce(floorDir * 22f, ForceMode.Acceleration);
        if (owner != null && owner.GuideScrap(body))
        {
            Vector3 rest = RestAboveFloor(body.position);
            body.position = rest;
            transform.position = rest;
            restPos = rest;
            bobSeed = Random.Range(0f, 6.28f);
            hasRest = true;
            body.linearVelocity = Vector3.zero;
            body.isKinematic = true;
            Settled = true;
            return;
        }
        Vector3 pos = body.position;
        Database db = Database.instance;
        if (db != null)
            pos.z = db.FieldPlaySize;
        body.position = pos;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null || Time.time < ignoreUntil)
            return;
        if (thrown && IsBall(collision.collider))
        {
            Rigidbody ball = collision.rigidbody;
            if (ball == null)
                ball = collision.collider.attachedRigidbody;
            if (ball != null)
            {
                Vector3 away = ball.position - transform.position;
                away.z = 0f;
                if (away.sqrMagnitude < 0.0001f)
                    away = Vector3.up;
                ball.linearVelocity = away.normalized * Mathf.Max(8f, ball.linearVelocity.magnitude);
            }
            if (body != null)
                body.linearVelocity = -floorDir * 3.5f + Random.insideUnitSphere * 2f;
            thrown = false;
            ApplyIgnores(false);
            return;
        }

        if (body != null && collision.collider != null && (owner == null || !owner.IsOwnFloor(collision.collider)))
        {
            if (IsStructure(collision.collider) && collision.contactCount > 0)
            {
                Vector3 normal = collision.GetContact(0).normal;
                normal.z = 0f;
                if (normal.sqrMagnitude < 0.0001f)
                    normal = Vector3.up;
                normal.Normalize();
                Vector3 bounced = Vector3.Reflect(body.linearVelocity, normal);
                bounced.z = 0f;
                bounced *= Random.Range(0.5f, 0.8f);
                bounced += new Vector3(Random.Range(-1.6f, 1.6f), Random.Range(-1.6f, 1.6f), 0f);
                float fall = Vector3.Dot(bounced, floorDir);
                if (fall < 2f)
                    bounced += floorDir * (2f - fall);
                body.linearVelocity = bounced;
            }
            return;
        }

        if (IsHerGround(collision.collider) && body != null && Vector3.Dot(body.linearVelocity, floorDir) >= -0.4f)
        {
            Vector3 rest = RestAboveFloor(body.position);
            body.position = rest;
            transform.position = rest;
            restPos = rest;
            bobSeed = Random.Range(0f, 6.28f);
            hasRest = true;
            body.linearVelocity = Vector3.zero;
            body.isKinematic = true;
            Settled = true;
            return;
        }
    }

    void ApplyIgnores(bool hitBalls)
    {
        if (col == null)
            return;
        Collider[] all = Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude);
        for (int i = 0; i < all.Length; i++)
        {
            Collider other = all[i];
            if (other == null || other == col)
                continue;
            bool ball = IsBall(other);
            bool ownFloor = owner != null && owner.IsOwnFloor(other);
            bool wall = IsStructure(other);
            bool ignore = !ownFloor && !wall && !(hitBalls && ball);
            Physics.IgnoreCollision(col, other, ignore);
        }
    }

    static bool IsBall(Collider col)
    {
        if (col == null)
            return false;
        if (col.CompareTag("Ball") || col.CompareTag("Ghost"))
            return true;
        return col.GetComponentInParent<BallInfo>() != null;
    }

    static bool IsStructure(Collider col)
    {
        string tag = col.tag;
        return tag == "Wall" || tag == "Walls" || tag == "Obstacle" || tag == "Hazard";
    }

    bool IsHerGround(Collider col)
    {
        return owner != null && owner.IsOwnFloor(col);
    }

    bool IsFloor(Collider col)
    {
        return IsHerGround(col);
    }
}
