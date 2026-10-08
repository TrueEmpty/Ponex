using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stretches this object along the configured axes until it fills the gap between
/// tagged walls — then stops. Centers on the span so it does not overshoot.
/// Used by Master Pong's lifeline (scale X to left/right Walls).
/// </summary>
public class ScaleUntilCollision : MonoBehaviour
{
    PlayerGrab pG;
    public Vector3 scaleAmount = Vector3.one;
    public float speed = 5;

    public List<string> tags = new List<string>();

    public Vector3 raycastOffset = Vector3.zero;

    [Tooltip("World-space inset so the mesh sits flush inside the wall faces.")]
    public float wallSkin = 0.04f;

    [Tooltip("Max ray distance when searching for walls.")]
    public float maxRayDistance = 500f;

    bool fitted;
    bool xMinHit;
    bool xMaxHit;
    bool yMinHit;
    bool yMaxHit;
    bool zMinHit;
    bool zMaxHit;

    void Start()
    {
        pG = GetComponent<PlayerGrab>();
        if (tags == null)
            tags = new List<string>();
        if (tags.Count == 0)
        {
            tags.Add("Walls");
            tags.Add("Wall");
            tags.Add("Obstacle");
        }

        // Walls finish spawning a frame after lifelines — fit next frame, then backup grow.
        Invoke(nameof(TryFitExact), 0.05f);
    }

    void Update()
    {
        if (fitted || pG == null)
            return;

        // Prefer an exact wall-span snap every frame (no overshoot)
        TryFitExact();
        if (fitted)
            return;

        MarkDisabledAxes();

        // Walls not ready yet — grow slowly until we can measure a span
        transform.localScale += scaleAmount * speed * Time.deltaTime;

        Vector3 pos = RayOrigin();
        ProbeAxis(ref xMinHit, ref xMaxHit, scaleAmount.x, transform.right, pos);
        ProbeAxis(ref yMinHit, ref yMaxHit, scaleAmount.y, transform.up, pos);
        ProbeAxis(ref zMinHit, ref zMaxHit, scaleAmount.z, transform.forward, pos);
    }

    void MarkDisabledAxes()
    {
        if (Mathf.Abs(scaleAmount.x) < 0.0001f) { xMinHit = true; xMaxHit = true; }
        if (Mathf.Abs(scaleAmount.y) < 0.0001f) { yMinHit = true; yMaxHit = true; }
        if (Mathf.Abs(scaleAmount.z) < 0.0001f) { zMinHit = true; zMaxHit = true; }
    }

    void ProbeAxis(ref bool minHit, ref bool maxHit, float amount, Vector3 axis, Vector3 pos)
    {
        if (Mathf.Abs(amount) < 0.0001f || (minHit && maxHit))
            return;

        // Half-extent along this axis in world space (cube mesh is 1 unit)
        float half = AxisHalfExtent(axis) + wallSkin;

        if (!maxHit && TagHit(pos, axis, half))
            maxHit = true;
        if (!minHit && TagHit(pos, -axis, half))
            minHit = true;
    }

    float AxisHalfExtent(Vector3 localAxis)
    {
        // Prefer lossy scale projected onto the axis direction
        Vector3 s = transform.lossyScale;
        Vector3 a = localAxis.normalized;
        // Object-aligned: for right/up/forward use matching scale component
        if (Vector3.Dot(a, transform.right) > 0.9f || Vector3.Dot(a, -transform.right) > 0.9f)
            return Mathf.Abs(s.x) * 0.5f;
        if (Vector3.Dot(a, transform.up) > 0.9f || Vector3.Dot(a, -transform.up) > 0.9f)
            return Mathf.Abs(s.y) * 0.5f;
        if (Vector3.Dot(a, transform.forward) > 0.9f || Vector3.Dot(a, -transform.forward) > 0.9f)
            return Mathf.Abs(s.z) * 0.5f;
        return Mathf.Max(s.x, s.y, s.z) * 0.5f;
    }

    Vector3 RayOrigin()
    {
        Vector3 pos = transform.position;
        pos += transform.right * raycastOffset.x;
        pos += transform.up * raycastOffset.y;
        pos += transform.forward * raycastOffset.z;
        return pos;
    }

    /// <summary>
    /// Snap scale/position exactly to the wall span so we never grow past the walls.
    /// </summary>
    void TryFitExact()
    {
        if (fitted || pG == null)
            return;

        MarkDisabledAxes();

        Vector3 scale = transform.localScale;
        Vector3 pos = transform.position;
        Vector3 origin = RayOrigin();
        bool any = false;

        if (Mathf.Abs(scaleAmount.x) > 0.0001f)
        {
            if (TryMeasureSpan(origin, transform.right, out float left, out float right))
            {
                float span = left + right;
                float target = Mathf.Max(0.05f, span - wallSkin * 2f);
                // Re-center along right so both ends meet the walls
                float shift = (right - left) * 0.5f;
                pos += transform.right * shift;
                origin += transform.right * shift;
                scale.x = target / Mathf.Max(0.0001f, AxisScaleFactor(transform.right));
                any = true;
                xMinHit = true;
                xMaxHit = true;
            }
        }

        if (Mathf.Abs(scaleAmount.y) > 0.0001f)
        {
            if (TryMeasureSpan(origin, transform.up, out float lo, out float hi))
            {
                float span = lo + hi;
                float target = Mathf.Max(0.05f, span - wallSkin * 2f);
                pos += transform.up * ((hi - lo) * 0.5f);
                origin += transform.up * ((hi - lo) * 0.5f);
                scale.y = target / Mathf.Max(0.0001f, AxisScaleFactor(transform.up));
                any = true;
                yMinHit = true;
                yMaxHit = true;
            }
        }

        if (Mathf.Abs(scaleAmount.z) > 0.0001f)
        {
            if (TryMeasureSpan(origin, transform.forward, out float lo, out float hi))
            {
                float span = lo + hi;
                float target = Mathf.Max(0.05f, span - wallSkin * 2f);
                pos += transform.forward * ((hi - lo) * 0.5f);
                scale.z = target / Mathf.Max(0.0001f, AxisScaleFactor(transform.forward));
                any = true;
                zMinHit = true;
                zMaxHit = true;
            }
        }

        if (!any)
            return;

        transform.position = pos;
        transform.localScale = scale;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = pos;
            rb.rotation = transform.rotation;
        }

        fitted = true;
        enabled = false;
    }

    /// <summary>Parent lossy scale contribution along a local axis (1 for unparented).</summary>
    float AxisScaleFactor(Vector3 axis)
    {
        // localScale is what we set; lossyScale includes parents. Cube mesh size 1 → world size = lossy.
        // We assign localScale so world half-extent = local * parentFactor / 2.
        Transform parent = transform.parent;
        if (parent == null)
            return 1f;
        Vector3 pls = parent.lossyScale;
        Vector3 a = axis.normalized;
        if (Mathf.Abs(Vector3.Dot(a, parent.right)) > 0.9f) return Mathf.Abs(pls.x);
        if (Mathf.Abs(Vector3.Dot(a, parent.up)) > 0.9f) return Mathf.Abs(pls.y);
        if (Mathf.Abs(Vector3.Dot(a, parent.forward)) > 0.9f) return Mathf.Abs(pls.z);
        return 1f;
    }

    bool TryMeasureSpan(Vector3 origin, Vector3 axis, out float negDist, out float posDist)
    {
        negDist = RayDistance(origin, -axis);
        posDist = RayDistance(origin, axis);
        return negDist > 0.01f && posDist > 0.01f;
    }

    float RayDistance(Vector3 origin, Vector3 dir)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, maxRayDistance, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return -1f;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].transform;
            if (t == null || t == transform || t.IsChildOf(transform))
                continue;
            if (TagMatches(t.tag))
                return hits[i].distance;
        }
        return -1f;
    }

    bool TagHit(Vector3 pos, Vector3 dir, float distance)
    {
        RaycastHit[] hits = Physics.RaycastAll(pos, dir, distance, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return false;

        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].transform;
            if (t == null || t == transform || t.IsChildOf(transform))
                continue;
            if (TagMatches(t.tag))
                return true;
        }
        return false;
    }

    bool TagMatches(string tag)
    {
        if (string.IsNullOrEmpty(tag) || tags == null)
            return false;
        string t = tag.ToLowerInvariant().Trim();
        for (int i = 0; i < tags.Count; i++)
        {
            if (string.IsNullOrEmpty(tags[i]))
                continue;
            if (tags[i].ToLowerInvariant().Trim() == t)
                return true;
        }
        // Always accept common wall tags even if inspector list is incomplete
        return t == "walls" || t == "wall" || t == "obstacle";
    }
}
