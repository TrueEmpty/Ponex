using UnityEngine;

/// <summary>
/// Muri rides attached surfaces instead of walking. Gravity / facing follow the support.
/// Walls are concave meshes — never use Physics.ClosestPoint on them.
/// </summary>
[DefaultExecutionOrder(40)]
[RequireComponent(typeof(Rigidbody), typeof(PlayerGrab))]
public class MuriMove : MonoBehaviour
{
    public float bodyRadius = 0.22f;
    public float standOffset = 0f;
    public float fallAccel = 22f;
    public float maxFallSpeed = 16f;
    public float pushSpeed = 1.1f;

    public Transform Support => support;
    public Vector3 StandUp => standUp;
    public Vector3 GravityDir => gravityDir;
    public bool IsAttached => attached && support != null;

    Rigidbody rb;
    PlayerGrab pg;
    Database db;
    Collider bodyCol;

    Transform support;
    Transform preferredSupport;
    Vector3 localPos;
    Vector3 standUp = Vector3.up;
    Vector3 gravityDir = Vector3.down;
    Vector3 fallVel;
    bool attached;
    bool placed;
    bool supportIsWall;
    Vector3 spawnPos;
    Vector3 lastSafePos;
    bool haveSpawn;
    bool haveSafe;

    static readonly Collider[] overlap = new Collider[48];
    static readonly RaycastHit[] rayHits = new RaycastHit[24];

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        bodyCol = GetComponent<Collider>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezePositionZ
            | RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationY;
    }

    void Start()
    {
        db = Database.instance;
        ComputerBrain.Ensure(gameObject, ComputerBrain.Mode.Kunai);
        PlaceOnBestSupport();
    }

    public void PlaceOnBestSupport()
    {
        db = db != null ? db : Database.instance;
        Physics.SyncTransforms();
        float z = PlayZ();
        Vector3 origin = transform.position;
        origin.z = z;

        RaycastHit hit;
        if (TryFindBlockingHit(origin, out hit) || TryFindWallHit(origin, out hit))
        {
            Vector3 n = FaceIntoField(hit.normal, origin);
            Attach(hit.collider.transform, hit.point, n, true);
        }
        else
        {
            attached = false;
            standUp = InwardFromFacing();
            gravityDir = -standUp;
            fallVel = gravityDir * 2f;
            ApplyStandRotation();
        }

        placed = true;
        SnapZ();
        RememberSafeIfValid();
    }

    void FixedUpdate()
    {
        if (db == null)
            db = Database.instance;
        if (pg == null || !pg.IsLinked() || pg.player == null)
            return;
        if (db != null && !db.gameStart && !placed)
            return;
        if (pg.player.currentHealth <= 0)
        {
            rb.isKinematic = true;
            return;
        }

        if (!placed)
            PlaceOnBestSupport();

        float z = PlayZ();
        if (attached && !SupportValid(support))
        {
            if (SupportValid(preferredSupport) && preferredSupport != support)
                ReattachPreferred();
            else
                BeginFall();
        }

        if (attached && support != null)
        {
            rb.isKinematic = true;
            Vector3 desired = support.TransformPoint(localPos);
            Vector3 from = rb.position;
            from.z = z;
            desired.z = z;

            if (TryWallPin(from, desired, out Vector3 pin, out RaycastHit wallHit))
            {
                Attach(wallHit.collider.transform, pin, FaceIntoField(wallHit.normal, from), false);
                desired = pin;
            }

            desired.z = z;
            rb.position = desired;
            transform.position = desired;
            RefreshSurfaceAxes(desired);
            ApplyStandRotation();
            fallVel = Vector3.zero;
            RememberSafeIfValid();
        }
        else
        {
            rb.isKinematic = false;
            fallVel += gravityDir * fallAccel * Time.fixedDeltaTime;
            if (fallVel.sqrMagnitude > maxFallSpeed * maxFallSpeed)
                fallVel = fallVel.normalized * maxFallSpeed;

            Vector3 from = rb.position;
            Vector3 next = from + fallVel * Time.fixedDeltaTime;
            from.z = z;
            next.z = z;

            Vector3 delta = next - from;
            float dist = delta.magnitude;
            if (dist > 0.0001f && Physics.SphereCast(from, bodyRadius * 0.9f, delta / dist, out RaycastHit land, dist + standOffset, ~0, QueryTriggerInteraction.Ignore))
            {
                if (IsSolid(land.collider) && !IsSelf(land.collider))
                {
                    Attach(land.collider.transform, land.point, FaceIntoField(land.normal, from), false);
                    return;
                }
            }

            rb.MovePosition(next);
            ApplyStandRotation();
        }

        UpdatePlayerFacing();
        RecoverIfOutOfBounds();
    }

    void OnCollisionEnter(Collision collision)
    {
        TryTakePush(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        TryTakePush(collision);
    }

    void TryTakePush(Collision collision)
    {
        if (collision == null || collision.collider == null || collision.contactCount <= 0)
            return;
        if (IsSelf(collision.collider) || !IsSolid(collision.collider))
            return;
        if (support != null && collision.collider.transform.IsChildOf(support))
            return;

        ContactPoint contact = collision.GetContact(0);
        float intoUs = Vector3.Dot(collision.relativeVelocity, contact.normal);
        Rigidbody otherRb = collision.rigidbody;
        bool movingOther = otherRb != null && otherRb.linearVelocity.sqrMagnitude > pushSpeed * pushSpeed;
        if (intoUs < pushSpeed && !movingOther)
            return;

        Transform nextSupport = collision.collider.transform;
        if (attached && support != null && support != nextSupport && SupportValid(preferredSupport) && preferredSupport != nextSupport)
        {
            if (StillTouching(preferredSupport))
                return;
        }

        Attach(nextSupport, contact.point, FaceIntoField(contact.normal, transform.position), false);
    }

    void Attach(Transform next, Vector3 worldPoint, Vector3 normal, bool asPreferred)
    {
        if (next == null)
            return;

        standUp = Flatten(normal);
        if (standUp.sqrMagnitude < 0.0001f)
            standUp = InwardFromFacing();
        standUp.Normalize();
        gravityDir = -standUp;

        support = next;
        supportIsWall = HasWallTag(next) || IsNonConvexMesh(next);

        Vector3 pos = worldPoint + standUp * SitDistance(supportIsWall);
        pos.z = PlayZ();

        localPos = next.InverseTransformPoint(pos);
        attached = true;
        rb.isKinematic = true;
        rb.position = pos;
        transform.position = pos;
        ApplyStandRotation();
        UpdatePlayerFacing();

        if (asPreferred || preferredSupport == null)
            preferredSupport = next;
    }

    void ReattachPreferred()
    {
        Collider col = preferredSupport.GetComponentInChildren<Collider>();
        if (col == null)
        {
            BeginFall();
            return;
        }

        Vector3 from = transform.position + InwardFromFacing() * 1.5f;
        from.z = PlayZ();
        Vector3 target = SafeClosest(col, transform.position);
        Vector3 dir = target - from;
        float dist = dir.magnitude;
        if (dist > 0.0001f && Physics.Raycast(from, dir / dist, out RaycastHit hit, dist + 2f, ~0, QueryTriggerInteraction.Ignore)
            && !IsSelf(hit.collider))
        {
            Attach(preferredSupport, hit.point, FaceIntoField(hit.normal, from), true);
            return;
        }

        BeginFall();
    }

    void BeginFall()
    {
        attached = false;
        support = null;
        supportIsWall = false;
        rb.isKinematic = false;
        if (fallVel.sqrMagnitude < 0.01f)
            fallVel = gravityDir * 2f;
    }

    bool TryWallPin(Vector3 from, Vector3 to, out Vector3 pin, out RaycastHit wallHit)
    {
        pin = to;
        wallHit = default;
        Vector3 delta = to - from;
        float dist = delta.magnitude;
        if (dist < 0.0001f)
            return false;

        int n = Physics.SphereCastNonAlloc(from, bodyRadius * 0.85f, delta / dist, rayHits, dist, ~0, QueryTriggerInteraction.Ignore);
        float best = float.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < n; i++)
        {
            RaycastHit hit = rayHits[i];
            if (hit.collider == null || IsSelf(hit.collider))
                continue;
            if (!IsWall(hit.collider))
                continue;
            if (support != null && (hit.collider.transform == support || hit.collider.transform.IsChildOf(support)))
                continue;
            if (hit.distance < best)
            {
                best = hit.distance;
                wallHit = hit;
                found = true;
            }
        }

        if (!found)
            return false;

        Vector3 wallNormal = FaceIntoField(wallHit.normal, from);
        pin = wallHit.point + Flatten(wallNormal) * SitDistance(true);
        pin.z = PlayZ();
        return true;
    }

    bool TryFindBlockingHit(Vector3 origin, out RaycastHit bestHit)
    {
        bestHit = default;
        int n = Physics.OverlapSphereNonAlloc(origin, bodyRadius * 2.4f, overlap, ~0, QueryTriggerInteraction.Ignore);
        Collider best = null;
        float bestScore = float.PositiveInfinity;
        for (int i = 0; i < n; i++)
        {
            Collider col = overlap[i];
            if (col == null || IsSelf(col) || !IsSolid(col) || IsWall(col))
                continue;
            if (!OnOurSide(col.bounds.center))
                continue;

            Vector3 closest = SafeClosest(col, origin);
            float score = (closest - origin).sqrMagnitude;
            if (score < bestScore)
            {
                bestScore = score;
                best = col;
            }
        }

        if (best == null)
            return false;

        return RaycastOnto(best, origin, out bestHit);
    }

    bool TryFindWallHit(Vector3 origin, out RaycastHit bestHit)
    {
        bestHit = default;
        Vector3 inward = InwardFromFacing();
        if (inward.sqrMagnitude < 0.0001f)
            inward = Vector3.up;
        inward.Normalize();

        Vector3 from = origin + inward * 4f;
        from.z = PlayZ();
        Vector3 dir = -inward;
        int n = Physics.RaycastNonAlloc(from, dir, rayHits, 48f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < n; i++)
        {
            RaycastHit hit = rayHits[i];
            if (hit.collider == null || IsSelf(hit.collider))
                continue;
            if (!IsWall(hit.collider) && !IsSolid(hit.collider))
                continue;
            if (hit.distance < best)
            {
                best = hit.distance;
                bestHit = hit;
                found = true;
            }
        }
        return found;
    }

    bool RaycastOnto(Collider col, Vector3 origin, out RaycastHit hit)
    {
        hit = default;
        Vector3 inward = InwardFromFacing();
        Vector3 target = SafeClosest(col, origin);
        Vector3 from;
        // Inside a paddle/player, ClosestPoint returns the origin. Cast from the
        // field so we hit the real face instead of parenting to the object's center.
        if ((target - origin).sqrMagnitude < 0.0004f)
        {
            from = FieldCenter();
            from.z = PlayZ();
            target = origin;
        }
        else
        {
            from = origin + inward * 2.5f;
            from.z = PlayZ();
        }
        Vector3 delta = target - from;
        float dist = delta.magnitude;
        if (dist < 0.0001f)
            return false;

        int n = Physics.RaycastNonAlloc(from, delta / dist, rayHits, dist + 3f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < n; i++)
        {
            RaycastHit h = rayHits[i];
            if (h.collider == null || IsSelf(h.collider))
                continue;
            if (h.collider != col && !h.collider.transform.IsChildOf(col.transform) && !col.transform.IsChildOf(h.collider.transform))
                continue;
            if (h.distance < best)
            {
                best = h.distance;
                hit = h;
                found = true;
            }
        }
        return found;
    }

    void RefreshSurfaceAxes(Vector3 worldPos)
    {
        if (support == null)
            return;

        if (supportIsWall)
        {
            // Wall meshes are often authored at the field origin; radial from
            // that origin points into the wall. Always stand toward the field.
            Vector3 inward = Flatten(FieldCenter() - worldPos);
            if (inward.sqrMagnitude > 0.0004f)
                standUp = inward.normalized;
        }
        else if (IsFighterTransform(support))
        {
            // Keep the contact normal so she stays on the hit face, not the object's center.
        }
        else
        {
            Vector3 radial = Flatten(worldPos - support.position);
            if (radial.sqrMagnitude > 0.0004f)
                standUp = radial.normalized;
        }
        gravityDir = -standUp;
    }

    void ApplyStandRotation()
    {
        if (standUp.sqrMagnitude < 0.0001f)
            return;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, standUp);
        if (rb != null)
            rb.rotation = transform.rotation;
    }

    void UpdatePlayerFacing()
    {
        if (pg == null || pg.player == null)
            return;

        // Database spawn + PlayerGrab remap: bottom=Up, top=Down, right=Left, left=Right.
        // Use which side of the field she is on, not the surface normal (wall meshes
        // centered at the origin make that normal point the wrong way).
        Vector3 fromCenter = Flatten(transform.position - FieldCenter());
        if (fromCenter.sqrMagnitude < 0.0001f)
            return;

        Facing next;
        if (Mathf.Abs(fromCenter.y) >= Mathf.Abs(fromCenter.x))
            next = fromCenter.y <= 0f ? Facing.Up : Facing.Down;
        else
            next = fromCenter.x >= 0f ? Facing.Left : Facing.Right;

        pg.player.facing = next;
        pg.player.ignoreFacing = false;
    }

    Vector3 InwardFromFacing()
    {
        Facing facing = pg != null && pg.player != null ? pg.player.facing : Facing.Up;
        switch (facing)
        {
            case Facing.Down: return Vector3.down;
            case Facing.Left: return Vector3.left;
            case Facing.Right: return Vector3.right;
            default: return Vector3.up;
        }
    }

    Vector3 SideCenter()
    {
        float z = PlayZ();
        float half = db != null ? db.FieldPlaySize * 0.5f : 5f;
        Facing facing = pg != null && pg.player != null ? pg.player.facing : Facing.Up;
        switch (facing)
        {
            case Facing.Down: return new Vector3(0f, half * 0.45f, z);
            case Facing.Left: return new Vector3(half * 0.45f, 0f, z);
            case Facing.Right: return new Vector3(-half * 0.45f, 0f, z);
            default: return new Vector3(0f, -half * 0.45f, z);
        }
    }

    Vector3 FieldCenter()
    {
        return new Vector3(0f, 0f, PlayZ());
    }

    bool OnOurSide(Vector3 pos)
    {
        Facing facing = pg != null && pg.player != null ? pg.player.facing : Facing.Up;
        switch (facing)
        {
            case Facing.Down: return pos.y >= 0f;
            case Facing.Left: return pos.x >= 0f;
            case Facing.Right: return pos.x <= 0f;
            default: return pos.y <= 0f;
        }
    }

    bool StillTouching(Transform t)
    {
        if (t == null)
            return false;
        Collider col = t.GetComponentInChildren<Collider>();
        if (col == null)
            return false;
        Vector3 closest = SafeClosest(col, transform.position);
        return (closest - transform.position).sqrMagnitude <= (bodyRadius + 0.35f) * (bodyRadius + 0.35f);
    }

    Vector3 FaceIntoField(Vector3 normal, Vector3 from)
    {
        Vector3 n = Flatten(normal);
        if (n.sqrMagnitude < 0.0001f)
            n = InwardFromFacing();
        n.Normalize();
        Vector3 intoField = Flatten(FieldCenter() - from);
        if (intoField.sqrMagnitude > 0.0001f && Vector3.Dot(n, intoField) < 0f)
            n = -n;
        return n;
    }

    static Vector3 SafeClosest(Collider col, Vector3 origin)
    {
        if (col == null)
            return origin;
        if (SupportsClosestPoint(col))
            return col.ClosestPoint(origin);
        return col.bounds.ClosestPoint(origin);
    }

    static bool SupportsClosestPoint(Collider col)
    {
        if (col is BoxCollider || col is SphereCollider || col is CapsuleCollider)
            return true;
        MeshCollider mesh = col as MeshCollider;
        return mesh != null && mesh.convex;
    }

    public float SitDistance(bool wall)
    {
        // Wall hug: sit in the surface instead of a full-radius air gap.
        if (wall)
            return Mathf.Max(0.06f, bodyRadius - 0.1f);
        return Mathf.Max(0.06f, bodyRadius + standOffset);
    }

    bool SupportValid(Transform t)
    {
        return t != null && t.gameObject.activeInHierarchy;
    }

    bool IsSelf(Collider col)
    {
        return col != null && (col.transform == transform || col.transform.IsChildOf(transform) || col == bodyCol);
    }

    static bool HasWallTag(Transform t)
    {
        Transform p = t;
        while (p != null)
        {
            if (IsWallTag(p.tag))
                return true;
            p = p.parent;
        }
        return false;
    }

    static bool IsWallTag(string tag)
    {
        return tag == "Wall" || tag == "Walls" || tag == "Obstacle";
    }

    static bool IsWall(Collider col)
    {
        return col != null && HasWallTag(col.transform);
    }

    static bool IsNonConvexMesh(Transform t)
    {
        if (t == null)
            return false;
        MeshCollider mesh = t.GetComponent<MeshCollider>();
        if (mesh == null)
            mesh = t.GetComponentInChildren<MeshCollider>();
        return mesh != null && !mesh.convex;
    }

    static bool IsSolid(Collider col)
    {
        if (col == null || col.isTrigger)
            return false;
        string tag = col.tag;
        if (tag == "Ball" || tag == "Ghost" || tag == "OutOfBounds")
            return false;
        return true;
    }

    public static bool IsFighterCollider(Collider col)
    {
        return col != null && IsFighterTransform(col.transform);
    }

    public static bool IsFighterTransform(Transform t)
    {
        Transform p = t;
        while (p != null)
        {
            if (p.CompareTag("Player") || p.CompareTag("Paddle"))
                return true;
            p = p.parent;
        }
        return false;
    }

    public static bool IsOutsidePlayfield(Vector3 worldPos, Transform ignoreRoot = null)
    {
        Database db = Database.instance;
        float z = db != null ? db.FieldPlaySize : worldPos.z;
        Vector3 pos = worldPos;
        pos.z = z;
        Vector3 center = new Vector3(0f, 0f, z);

        int o = Physics.OverlapSphereNonAlloc(pos, 0.1f, overlap, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < o; i++)
        {
            Collider col = overlap[i];
            if (col == null)
                continue;
            if (ignoreRoot != null && (col.transform == ignoreRoot || col.transform.IsChildOf(ignoreRoot)))
                continue;
            if (col.CompareTag("OutOfBounds"))
                return true;
        }

        Vector3 delta = Flatten(pos - center);
        float dist = delta.magnitude;
        if (dist < 0.05f)
            return false;

        Vector3 dir = delta / dist;
        int n = Physics.RaycastNonAlloc(center, dir, rayHits, dist + 0.2f, ~0, QueryTriggerInteraction.Ignore);
        float wallDist = float.PositiveInfinity;
        for (int i = 0; i < n; i++)
        {
            RaycastHit hit = rayHits[i];
            if (hit.collider == null)
                continue;
            if (ignoreRoot != null && (hit.collider.transform == ignoreRoot || hit.collider.transform.IsChildOf(ignoreRoot)))
                continue;
            if (!IsWall(hit.collider))
                continue;
            if (hit.distance < wallDist)
                wallDist = hit.distance;
        }

        return wallDist < dist - 0.12f;
    }

    void RememberSafeIfValid()
    {
        Vector3 p = transform.position;
        p.z = PlayZ();
        if (IsOutsidePlayfield(p, transform))
            return;
        lastSafePos = p;
        haveSafe = true;
        if (!haveSpawn)
        {
            spawnPos = p;
            haveSpawn = true;
        }
    }

    public void RecoverIfOutOfBounds()
    {
        Vector3 p = transform.position;
        p.z = PlayZ();
        if (!IsOutsidePlayfield(p, transform))
            return;

        if (haveSafe && !IsOutsidePlayfield(lastSafePos, transform))
        {
            TeleportTo(lastSafePos);
            PlaceOnBestSupport();
            return;
        }

        if (haveSpawn && !IsOutsidePlayfield(spawnPos, transform))
        {
            TeleportTo(spawnPos);
            PlaceOnBestSupport();
            return;
        }

        Vector3 fallback = haveSpawn ? spawnPos : SideCenter();
        TeleportTo(fallback);
        PlaceOnBestSupport();
        if (IsOutsidePlayfield(transform.position, transform) && haveSafe)
            TeleportTo(lastSafePos);
    }

    void TeleportTo(Vector3 pos)
    {
        pos.z = PlayZ();
        transform.position = pos;
        if (rb != null)
            rb.position = pos;
    }

    static Vector3 Flatten(Vector3 v)
    {
        v.z = 0f;
        return v;
    }

    float PlayZ()
    {
        return db != null ? db.FieldPlaySize : transform.position.z;
    }

    void SnapZ()
    {
        Vector3 p = transform.position;
        p.z = PlayZ();
        transform.position = p;
        rb.position = p;
    }
}
