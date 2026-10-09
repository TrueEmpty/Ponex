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
                Vector3 lateralPush = Vector3.Project(push, lateral);
                totalPush += lateralPush.sqrMagnitude > 0.00001f ? lateralPush : push;

                float into = Vector3.Dot(rb.linearVelocity, -sepDir);
                if (into > 0f)
                    rb.linearVelocity += sepDir * into;
            }
        }

        if (totalPush.sqrMagnitude <= 0.00001f)
            return;

        if (totalPush.magnitude > MaxPushPerFrame)
            totalPush = totalPush.normalized * MaxPushPerFrame;

        rb.position += totalPush;
    }

    public static Vector3 AbsAxes(Vector3 v)
    {
        v.x = Mathf.Abs(v.x);
        v.y = Mathf.Abs(v.y);
        v.z = Mathf.Abs(v.z);
        return v.sqrMagnitude > 0.0001f ? v.normalized : v;
    }
}
