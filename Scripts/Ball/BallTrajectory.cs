using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cheap bounce prediction for AI — no ghost Instantiate / physics sim.
/// Fills world hit points along the ball's path (walls, paddles, obstacles).
/// </summary>
public static class BallTrajectory
{
    static readonly RaycastHit[] hitBuffer = new RaycastHit[8];
    static readonly List<string> defaultTags = new List<string> { "Wall", "Walls", "Obstacle", "Paddle", "Player", "Lifeline" };

    /// <summary>
    /// Predict bounce contact points. Uses SphereCast + reflect — same data AI used from ghosts.
    /// </summary>
    public static void Predict(
        Vector3 origin,
        Vector3 velocity,
        float radius,
        List<Vector3> pointsOut,
        int maxBounces = 5,
        float maxDistance = 48f,
        List<string> solidTags = null)
    {
        if (pointsOut == null)
            return;
        pointsOut.Clear();

        Vector3 vel = velocity;
        vel.z = 0f;
        float speed = vel.magnitude;
        if (speed < 0.05f)
            return;

        Vector3 pos = origin;
        pos.z = origin.z;
        Vector3 dir = vel / speed;
        float remaining = maxDistance;
        List<string> tags = solidTags != null && solidTags.Count > 0 ? solidTags : defaultTags;

        for (int bounce = 0; bounce < maxBounces && remaining > 0.05f; bounce++)
        {
            int count = Physics.SphereCastNonAlloc(
                pos, Mathf.Max(0.05f, radius), dir, hitBuffer, remaining, ~0, QueryTriggerInteraction.Ignore);

            RaycastHit? best = null;
            float bestDist = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                RaycastHit h = hitBuffer[i];
                if (h.collider == null || h.distance < 0.001f)
                    continue;
                if (!TagMatches(h.collider, tags))
                    continue;
                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    best = h;
                }
            }

            if (best == null)
                break;

            RaycastHit hit = best.Value;
            Vector3 point = hit.point;
            point.z = origin.z;
            pointsOut.Add(point);

            Vector3 n = hit.normal;
            n.z = 0f;
            if (n.sqrMagnitude < 0.0001f)
                break;
            n.Normalize();

            dir = Vector3.Reflect(dir, n).normalized;
            pos = point + n * (radius + 0.02f);
            remaining -= hit.distance + 0.02f;
        }
    }

    static bool TagMatches(Collider col, List<string> tags)
    {
        if (TagListMatcher.Contains(tags, col.tag))
            return true;

        Transform p = col.transform.parent;
        return p != null && TagListMatcher.Contains(tags, p.tag);
    }
}
