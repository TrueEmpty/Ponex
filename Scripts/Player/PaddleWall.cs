using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared wall checks + depenetration for paddles that set Rigidbody velocity in Update.
/// Fast dashes can overshoot the soft WallInDirection stop and lock against heavy wall colliders;
/// Unstick pushes out of overlaps and clears into-wall velocity so leave-input always works.
/// </summary>
public static class PaddleWall
{
    const float DefaultSphereRadius = 0.08f;
    const float Skin = 0.03f;
    const float MaxPushPerFrame = 0.35f;
    static readonly RaycastHit[] castHits = new RaycastHit[32];
    static readonly Collider[] overlapHits = new Collider[64];
    static readonly Dictionary<int, SelfColliderCache> selfColliderCache = new Dictionary<int, SelfColliderCache>(16);

    struct SelfColliderCache
    {
        public Transform root;
        public Collider[] colliders;
    }

    public static bool MatchesWallTag(string tag, List<string> wallTags)
    {
        if (string.IsNullOrEmpty(tag))
            return false;

        bool isObstacle = tag.Equals("Obstacle", StringComparison.OrdinalIgnoreCase);
        bool isWall = tag.Equals("Wall", StringComparison.OrdinalIgnoreCase)
            || tag.Equals("Walls", StringComparison.OrdinalIgnoreCase);

        if (wallTags == null || wallTags.Count == 0)
            return isWall || isObstacle;

        // "Walls" in hitTags also blocks field obstacles (logs, etc.)
        for (int i = 0; i < wallTags.Count; i++)
        {
            string wallTag = wallTags[i];
            if (wallTag == null)
                continue;
            if (wallTag.Equals(tag, StringComparison.OrdinalIgnoreCase))
                return true;
            if (isObstacle
                && (wallTag.Equals("Walls", StringComparison.OrdinalIgnoreCase)
                    || wallTag.Equals("Wall", StringComparison.OrdinalIgnoreCase)
                    || wallTag.Equals("Obstacle", StringComparison.OrdinalIgnoreCase)))
                return true;
        }
        return false;
    }

    public static bool WallInDirection(Transform paddle, int dir, List<string> wallTags, float distance, float radius = DefaultSphereRadius)
    {
        if (paddle == null || dir == 0 || distance <= 0f)
            return false;

        // Must match movement: velocity uses AbsAxes(right) * moveDir, NOT raw transform.right.
        // Using paddle.right here flips the block on half the field (can only move into the wall).
        Vector3 direction = AbsAxes(paddle.right) * Mathf.Sign(dir);
        Vector3 origin = paddle.position;

        // Already overlapping: never block the direction that gets us out.
        if (TryGetLateralEscape(paddle, wallTags, out Vector3 escape)
            && Vector3.Dot(direction, escape) > 0.05f)
            return false;

        // Thin sphere helps thick paddles; shorten cast so stop distance matches the old ray feel
        float castDist = Mathf.Max(0.01f, distance - radius);
        int hitCount = Physics.SphereCastNonAlloc(
            origin, radius, direction, castHits, castDist, ~0, QueryTriggerInteraction.UseGlobal);
        if (hitCount == castHits.Length)
        {
            if (HasWallHit(Physics.SphereCastAll(origin, radius, direction, castDist), paddle, wallTags))
                return true;
        }
        else if (HasWallHit(castHits, hitCount, paddle, wallTags))
            return true;

        hitCount = Physics.RaycastNonAlloc(
            origin, direction, castHits, distance, ~0, QueryTriggerInteraction.UseGlobal);
        return hitCount == castHits.Length
            ? HasWallHit(Physics.RaycastAll(origin, direction, distance), paddle, wallTags)
            : HasWallHit(castHits, hitCount, paddle, wallTags);
    }

    static bool HasWallHit(RaycastHit[] hits, Transform paddle, List<string> wallTags)
    {
        return hits != null && HasWallHit(hits, hits.Length, paddle, wallTags);
    }

    static bool HasWallHit(RaycastHit[] hits, int count, Transform paddle, List<string> wallTags)
    {
        if (hits == null || count <= 0)
            return false;

        for (int i = 0; i < count; i++)
        {
            Transform t = hits[i].transform;
            if (t == null || t == paddle || t.IsChildOf(paddle))
                continue;

            if (MatchesWallTag(t.tag, wallTags))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Push out of overlapping wall colliders and kill velocity driven into them.
    /// </summary>
    public static void Unstick(Rigidbody rb, Transform paddle, List<string> wallTags, float searchRadius = 2.5f)
    {
        Collider[] selfCols = GetSelfColliders(paddle);
        Unstick(rb, paddle, wallTags, selfCols, searchRadius);
    }

    static Collider[] GetSelfColliders(Transform paddle)
    {
        if (paddle == null)
            return null;

        int id = paddle.GetEntityId().GetHashCode();
        if (selfColliderCache.TryGetValue(id, out SelfColliderCache cached)
            && cached.root == paddle
            && cached.colliders != null)
        {
            bool valid = true;
            for (int i = 0; i < cached.colliders.Length; i++)
            {
                if (cached.colliders[i] == null)
                {
                    valid = false;
                    break;
                }
            }
            if (valid)
                return cached.colliders;
        }

        Collider[] colliders = paddle.GetComponentsInChildren<Collider>();
        selfColliderCache[id] = new SelfColliderCache { root = paddle, colliders = colliders };
        return colliders;
    }

    /// <summary>
    /// Cached-collider overload for paddles that call this every frame.
    /// </summary>
    public static void Unstick(
        Rigidbody rb,
        Transform paddle,
        List<string> wallTags,
        Collider[] selfCols,
        float searchRadius = 2.5f)
    {
        if (rb == null || paddle == null)
            return;

        if (selfCols == null || selfCols.Length == 0)
            return;

        int nearbyCount = Physics.OverlapSphereNonAlloc(
            paddle.position, searchRadius, overlapHits, ~0, QueryTriggerInteraction.UseGlobal);
        Collider[] nearby = overlapHits;
        if (nearbyCount == overlapHits.Length)
        {
            nearby = Physics.OverlapSphere(paddle.position, searchRadius);
            nearbyCount = nearby.Length;
        }
        if (nearbyCount == 0)
            return;

        Vector3 lateral = AbsAxes(paddle.right);
        Vector3 totalPush = Vector3.zero;

        for (int n = 0; n < nearbyCount; n++)
        {
            Collider wall = nearby[n];
            if (wall == null || wall.isTrigger)
                continue;
            if (wall.transform == paddle || wall.transform.IsChildOf(paddle))
                continue;
            if (!MatchesWallTag(wall.tag, wallTags))
                continue;

            for (int s = 0; s < selfCols.Length; s++)
            {
                Collider self = selfCols[s];
                if (self == null || self.isTrigger || !self.enabled)
                    continue;

                if (!Physics.ComputePenetration(
                    self, self.transform.position, self.transform.rotation,
                    wall, wall.transform.position, wall.transform.rotation,
                    out Vector3 sepDir, out float sepDist))
                {
                    continue;
                }

                if (sepDist <= 0f)
                    continue;

                Vector3 push = sepDir * (sepDist + Skin);
                // Stay on the lifeline: only slide along the paddle, never off the wall.
                Vector3 lateralPush = Vector3.Project(push, lateral);
                if (lateralPush.sqrMagnitude > 0.00001f)
                    totalPush += lateralPush;

                float into = Vector3.Dot(rb.linearVelocity, -sepDir);
                if (into > 0f)
                    rb.linearVelocity += sepDir * into;
            }
        }

        if (totalPush.sqrMagnitude <= 0.00001f)
        {
            if (OverlapsEndWall(paddle, selfCols, wallTags, nearby, nearbyCount, lateral))
                NudgeAlongLane(rb, paddle, lateral, selfCols, wallTags);
            return;
        }

        if (totalPush.magnitude > MaxPushPerFrame)
            totalPush = totalPush.normalized * MaxPushPerFrame;

        rb.position += totalPush;
        if (rb.transform != null)
            rb.transform.position = rb.position;
    }

    static bool OverlapsEndWall(
        Transform paddle, Collider[] selfCols, List<string> wallTags, Collider[] nearby, int nearbyCount, Vector3 lateral)
    {
        Vector3 towardHome = paddle.up.sqrMagnitude > 0.0001f ? -paddle.up.normalized : Vector3.down;
        for (int n = 0; n < nearbyCount; n++)
        {
            Collider wall = nearby[n];
            if (wall == null || wall.isTrigger)
                continue;
            if (wall.transform == paddle || wall.transform.IsChildOf(paddle))
                continue;
            if (!MatchesWallTag(wall.tag, wallTags))
                continue;

            Vector3 toWall = wall.bounds.center - paddle.position;
            if (Mathf.Abs(Vector3.Dot(toWall, lateral)) <= Mathf.Abs(Vector3.Dot(toWall, towardHome)))
                continue;

            for (int s = 0; s < selfCols.Length; s++)
            {
                Collider self = selfCols[s];
                if (self == null || self.isTrigger || !self.enabled)
                    continue;
                if (self.bounds.Intersects(wall.bounds))
                    return true;
            }
        }
        return false;
    }

    static void NudgeAlongLane(
        Rigidbody rb, Transform paddle, Vector3 lateral, Collider[] selfCols, List<string> wallTags)
    {
        const float step = 0.12f;
        const int maxSteps = 10;
        Vector3 start = rb.position;

        for (int sign = -1; sign <= 1; sign += 2)
        {
            for (int i = 1; i <= maxSteps; i++)
            {
                Vector3 test = start + lateral * (sign * step * i);
                bool blocked = false;
                for (int s = 0; s < selfCols.Length && !blocked; s++)
                {
                    Collider self = selfCols[s];
                    if (self == null || self.isTrigger || !self.enabled)
                        continue;
                    Vector3 delta = test - start;
                    Vector3 world = self.transform.position + delta;
                    int hits = Physics.OverlapBoxNonAlloc(
                        world,
                        self.bounds.extents,
                        overlapHits,
                        self.transform.rotation,
                        ~0,
                        QueryTriggerInteraction.Ignore);
                    for (int h = 0; h < hits; h++)
                    {
                        Collider other = overlapHits[h];
                        if (other == null || other.isTrigger)
                            continue;
                        if (other.transform == paddle || other.transform.IsChildOf(paddle))
                            continue;
                        if (!MatchesWallTag(other.tag, wallTags))
                            continue;
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    rb.position = test;
                    if (rb.transform != null)
                        rb.transform.position = test;
                    return;
                }
            }
        }
    }

    public static bool TryGetLateralEscape(Transform paddle, List<string> wallTags, out Vector3 escape)
    {
        escape = Vector3.zero;
        if (paddle == null)
            return false;

        Collider[] selfCols = GetSelfColliders(paddle);
        if (selfCols == null || selfCols.Length == 0)
            return false;

        int nearbyCount = Physics.OverlapSphereNonAlloc(
            paddle.position, 2.5f, overlapHits, ~0, QueryTriggerInteraction.Ignore);
        if (nearbyCount <= 0)
            return false;

        Vector3 lateral = AbsAxes(paddle.right);
        Vector3 total = Vector3.zero;
        for (int n = 0; n < nearbyCount; n++)
        {
            Collider wall = overlapHits[n];
            if (wall == null || wall.isTrigger)
                continue;
            if (wall.transform == paddle || wall.transform.IsChildOf(paddle))
                continue;
            if (!MatchesWallTag(wall.tag, wallTags))
                continue;

            for (int s = 0; s < selfCols.Length; s++)
            {
                Collider self = selfCols[s];
                if (self == null || self.isTrigger || !self.enabled)
                    continue;
                if (!Physics.ComputePenetration(
                    self, self.transform.position, self.transform.rotation,
                    wall, wall.transform.position, wall.transform.rotation,
                    out Vector3 sepDir, out float sepDist)
                    || sepDist <= 0f)
                    continue;

                Vector3 lateralPush = Vector3.Project(sepDir * sepDist, lateral);
                if (lateralPush.sqrMagnitude > 0.00001f)
                    total += lateralPush;
            }
        }

        if (total.sqrMagnitude <= 0.00001f)
            return false;
        escape = total.normalized;
        return true;
    }

    public static Vector3 AbsAxes(Vector3 v)
    {
        v.x = Mathf.Abs(v.x);
        v.y = Mathf.Abs(v.y);
        v.z = Mathf.Abs(v.z);
        return v.sqrMagnitude > 0.0001f ? v.normalized : v;
    }

    public static Vector3 IntoField(Facing facing)
    {
        switch (facing)
        {
            case Facing.Up: return Vector3.up;
            case Facing.Down: return Vector3.down;
            case Facing.Left: return Vector3.left;
            case Facing.Right: return Vector3.right;
            default: return Vector3.up;
        }
    }

    /// <summary>
    /// Decorative / rear colliders sit inside the stadium mesh. Physics must not
    /// shove the paddle off its lifeline; lane stop + Unstick handle the ends.
    /// </summary>
    public static void IgnoreWallCollisions(Transform paddle, List<string> wallTags)
    {
        IgnoreHomeWallCollisions(paddle, wallTags, null);
    }

    /// <summary>
    /// Ignore only the home/rear wall so butts and bats can sit in the mesh.
    /// End walls stay solid so nobody tunnels in and gets stuck.
    /// </summary>
    public static void IgnoreHomeWallCollisions(Transform paddle, List<string> wallTags, Collider[] selfCols)
    {
        if (paddle == null)
            return;

        if (selfCols == null || selfCols.Length == 0)
            selfCols = paddle.GetComponentsInChildren<Collider>(true);
        if (selfCols == null || selfCols.Length == 0)
            return;

        Vector3 towardHome = -paddle.up;
        Vector3 lateral = AbsAxes(paddle.right);
        if (towardHome.sqrMagnitude < 0.0001f)
            towardHome = Vector3.down;
        else
            towardHome.Normalize();

        Collider[] walls = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude);
        for (int w = 0; w < walls.Length; w++)
        {
            Collider wall = walls[w];
            if (wall == null || wall.isTrigger)
                continue;
            if (wall.transform == paddle || wall.transform.IsChildOf(paddle))
                continue;
            if (!MatchesWallTag(wall.tag, wallTags))
                continue;

            Vector3 toWall = wall.bounds.center - paddle.position;
            float behind = Vector3.Dot(toWall, towardHome);
            float side = Mathf.Abs(Vector3.Dot(toWall, lateral));
            if (behind <= 0.05f || behind <= side)
                continue;

            for (int s = 0; s < selfCols.Length; s++)
            {
                Collider self = selfCols[s];
                if (self == null)
                    continue;
                Physics.IgnoreCollision(self, wall, true);
            }
        }
    }

    public static bool IsBoundaryWallTag(string tag)
    {
        if (string.IsNullOrEmpty(tag))
            return false;
        return tag.Equals("Wall", StringComparison.OrdinalIgnoreCase)
            || tag.Equals("Walls", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Pass through stadium boundary meshes. Obstacles (logs, wagons) stay solid.
    /// </summary>
    public static void IgnoreBoundaryWallCollisions(Transform root, Collider[] selfCols)
    {
        if (root == null)
            return;
        if (selfCols == null || selfCols.Length == 0)
            selfCols = root.GetComponentsInChildren<Collider>(true);
        if (selfCols == null || selfCols.Length == 0)
            return;

        Collider[] walls = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude);
        for (int w = 0; w < walls.Length; w++)
        {
            Collider wall = walls[w];
            if (wall == null || wall.isTrigger)
                continue;
            if (wall.transform == root || wall.transform.IsChildOf(root))
                continue;
            if (!IsBoundaryWallTag(wall.tag))
                continue;

            for (int s = 0; s < selfCols.Length; s++)
            {
                if (selfCols[s] == null)
                    continue;
                Physics.IgnoreCollision(selfCols[s], wall, true);
            }
        }
    }

    public static void IgnoreColliderSets(Collider[] a, Collider[] b)
    {
        if (a == null || b == null)
            return;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] == null)
                continue;
            for (int j = 0; j < b.Length; j++)
            {
                if (b[j] == null || a[i] == b[j])
                    continue;
                Physics.IgnoreCollision(a[i], b[j], true);
            }
        }
    }

    static readonly float[] LaneLimitLifts = { 0.6f, 1.25f, 2f, 2.8f };

    /// <summary>
    /// Inner faces of the furthest end walls on this lane, as scalars along AbsAxes(right).
    /// </summary>
    public static bool TryMeasureLaneLimits(Transform paddle, Facing facing, out float minAlong, out float maxAlong)
    {
        minAlong = 0f;
        maxAlong = 0f;
        if (paddle == null)
            return false;

        Vector3 axis = AbsAxes(paddle.right);
        Vector3 into = IntoField(facing);
        Vector3 origin = paddle.position;
        origin.z = paddle.position.z;

        float bestSpan = -1f;
        float bestMin = 0f;
        float bestMax = 0f;
        for (int i = 0; i < LaneLimitLifts.Length; i++)
        {
            Vector3 sample = origin + into * LaneLimitLifts[i];
            float posDist = RayToEndWall(sample, axis, paddle);
            float negDist = RayToEndWall(sample, -axis, paddle);
            if (posDist < 0.25f || negDist < 0.25f)
                continue;

            float span = posDist + negDist;
            if (span <= bestSpan)
                continue;

            float along = Vector3.Dot(sample, axis);
            bestMin = along - negDist;
            bestMax = along + posDist;
            bestSpan = span;
        }

        if (bestSpan < 0.5f)
        {
            float half = 10f;
            if (Database.instance != null)
                half = Mathf.Max(6f, Database.instance.FieldPlaySize * 0.45f);
            minAlong = -half;
            maxAlong = half;
            return true;
        }

        minAlong = bestMin;
        maxAlong = bestMax;
        return true;
    }

    static float RayToEndWall(Vector3 origin, Vector3 dir, Transform self)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, 80f, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return -1f;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        Vector3 axis = dir.sqrMagnitude > 0.0001f ? dir.normalized : dir;
        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].transform;
            if (t == null || t == self || t.IsChildOf(self))
                continue;
            if (!IsBoundaryWallTag(t.tag))
                continue;
            if (hits[i].distance < 0.4f)
                continue;
            if (Mathf.Abs(Vector3.Dot(hits[i].normal, axis)) < 0.35f)
                continue;
            return hits[i].distance;
        }
        return -1f;
    }

    /// <summary>How far from the paddle origin to stop before a side wall.</summary>
    public const float LineSideClearance = 0.42f;

    public static float LateralStopDistance(Transform paddle, float extraClearance = LineSideClearance)
    {
        if (paddle == null)
            return 0.6f + extraClearance;
        return Mathf.Max(0.25f, LaneHalfExtent(paddle, AbsAxes(paddle.right)) + extraClearance);
    }

    public static float LaneHalfExtent(Transform paddle, Vector3 axis)
    {
        float best = 0.25f;
        if (paddle == null)
            return best;

        Collider[] cols = GetSelfColliders(paddle);
        if (cols == null)
            return best;

        for (int i = 0; i < cols.Length; i++)
        {
            Collider col = cols[i];
            if (col == null || col.isTrigger || !col.enabled)
                continue;
            Vector3 e = col.bounds.extents;
            float along = Mathf.Abs(e.x * axis.x) + Mathf.Abs(e.y * axis.y) + Mathf.Abs(e.z * axis.z);
            if (along > best)
                best = along;
        }
        return best;
    }

    public static void ClampToLaneLimits(
        Rigidbody rb,
        Transform paddle,
        Facing facing,
        ref float cachedMin,
        ref float cachedMax,
        ref float nextRefresh,
        float extraSkin = 0.04f)
    {
        if (rb == null || paddle == null)
            return;

        if (!ReadyLaneCache(paddle, facing, ref cachedMin, ref cachedMax, ref nextRefresh))
            return;

        Vector3 axis = AbsAxes(paddle.right);
        float radius = LaneHalfExtent(paddle, axis) + extraSkin;
        float along = Vector3.Dot(rb.position, axis);
        float lo = cachedMin + radius;
        float hi = cachedMax - radius;
        if (lo > hi)
        {
            float mid = (cachedMin + cachedMax) * 0.5f;
            lo = hi = mid;
        }

        float clamped = Mathf.Clamp(along, lo, hi);
        if (Mathf.Abs(clamped - along) <= 0.0001f)
            return;

        Vector3 pos = rb.position + axis * (clamped - along);
        rb.position = pos;
        if (rb.transform != null)
            rb.transform.position = pos;

        float vAlong = Vector3.Dot(rb.linearVelocity, axis);
        if ((clamped <= lo + 0.001f && vAlong < 0f) || (clamped >= hi - 0.001f && vAlong > 0f))
            rb.linearVelocity -= axis * vAlong;
    }

    public static bool LaneEndInDirection(
        Transform paddle,
        Facing facing,
        int dir,
        ref float cachedMin,
        ref float cachedMax,
        ref float nextRefresh)
    {
        if (paddle == null || dir == 0)
            return false;
        if (!ReadyLaneCache(paddle, facing, ref cachedMin, ref cachedMax, ref nextRefresh))
            return false;

        Vector3 axis = AbsAxes(paddle.right);
        float radius = LaneHalfExtent(paddle, axis) + 0.05f;
        float along = Vector3.Dot(paddle.position, axis);
        if (dir > 0)
            return along >= cachedMax - radius;
        return along <= cachedMin + radius;
    }

    static bool ReadyLaneCache(Transform paddle, Facing facing, ref float cachedMin, ref float cachedMax, ref float nextRefresh)
    {
        if (Time.time < nextRefresh && cachedMax - cachedMin > 0.25f)
            return true;
        if (!TryMeasureLaneLimits(paddle, facing, out float minAlong, out float maxAlong))
            return cachedMax - cachedMin > 0.25f;

        cachedMin = minAlong;
        cachedMax = maxAlong;
        nextRefresh = Time.time + 0.35f;
        return true;
    }

    public static void KeepOnLifeline(Rigidbody rb, Transform paddle, Vector3 lanePoint)
    {
        if (paddle == null)
            return;

        Vector3 axis = AbsAxes(paddle.right);
        Vector3 pos = rb != null ? rb.position : paddle.position;
        Vector3 pinned = lanePoint + Vector3.Project(pos - lanePoint, axis);
        pinned.z = pos.z;
        paddle.position = pinned;
        if (rb == null)
            return;
        rb.position = pinned;
        rb.linearVelocity = Vector3.Project(rb.linearVelocity, axis);
    }

    /// <summary>
    /// Hard lock to the spawn wall: freeze the depth axis from facing and
    /// rewrite any off-lane position / velocity every physics tick.
    /// </summary>
    public struct LaneLock
    {
        public bool ready;
        public bool lockX;
        public float coord;

        public void Ensure(Rigidbody rb, Facing facing)
        {
            if (ready || rb == null)
                return;

            if (facing == Facing.Left || facing == Facing.Right)
            {
                rb.constraints = RigidbodyConstraints.FreezePositionX
                    | RigidbodyConstraints.FreezePositionZ
                    | RigidbodyConstraints.FreezeRotation;
                coord = rb.position.x;
                lockX = true;
            }
            else
            {
                rb.constraints = RigidbodyConstraints.FreezePositionY
                    | RigidbodyConstraints.FreezePositionZ
                    | RigidbodyConstraints.FreezeRotation;
                coord = rb.position.y;
                lockX = false;
            }

            ready = true;
        }

        public void Clamp(Rigidbody rb)
        {
            if (!ready || rb == null)
                return;

            Vector3 pos = rb.position;
            Vector3 vel = rb.linearVelocity;
            if (lockX)
            {
                pos.x = coord;
                vel.x = 0f;
            }
            else
            {
                pos.y = coord;
                vel.y = 0f;
            }

            rb.position = pos;
            rb.linearVelocity = vel;
            if (rb.transform != null)
                rb.transform.position = pos;
        }
    }
}
