using UnityEngine;

/// <summary>
/// Pushes balls, players, and bumps around the dune. Strongest at the center.
/// Fast objects only curve; slow ones get turned aside. Nothing is stopped dead.
/// </summary>
[DefaultExecutionOrder(500)]
public class SandDuneDeflect : MonoBehaviour
{
    public float radius = 3.2f;
    public float strength = 14f;

    SphereCollider range;

    void Awake()
    {
        range = GetComponent<SphereCollider>();
        if (range == null)
            range = gameObject.AddComponent<SphereCollider>();
        range.isTrigger = true;
        float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        range.radius = scale > 0.01f ? radius / scale : radius;
    }

    void FixedUpdate()
    {
        Vector3 center = transform.position;
        float worldRadius = Mathf.Max(radius, transform.lossyScale.x * 0.55f);

        Collider[] hits = Physics.OverlapSphere(center, worldRadius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;
            if (!Affects(hit))
                continue;

            Rigidbody rb = hit.attachedRigidbody;
            if (rb == null)
                continue;

            Uwrenmaw uw = hit.GetComponent<Uwrenmaw>();
            if (uw == null)
                uw = hit.GetComponentInParent<Uwrenmaw>();
            if (uw != null)
                continue;

            Vector3 away = rb.worldCenterOfMass - center;
            away.z = 0f;
            float dist = away.magnitude;
            if (dist < 0.05f)
                away = Vector3.right;
            else
                away /= dist;

            float closeness = 1f - Mathf.Clamp01(dist / worldRadius);
            if (rb.isKinematic)
            {
                if (hit.GetComponentInParent<PlayerGrab>() == null)
                    continue;
                Vector3 slide = rb.position + away * (strength * closeness * closeness * 0.035f);
                slide.z = rb.position.z;
                rb.MovePosition(slide);
                continue;
            }

            Vector3 vel = rb.linearVelocity;
            vel.z = 0f;
            float speed = vel.magnitude;
            // Fast objects keep most of their direction. Slow ones near the middle get turned around the hill.
            float turn = closeness * closeness * Mathf.Clamp01(6.5f / (speed + 0.75f));
            Vector3 dir = speed < 0.05f ? away : Vector3.Slerp(vel.normalized, away, turn);
            float kept = Mathf.Max(speed, closeness * 1.4f);
            Vector3 next = dir * kept;
            next.z = rb.linearVelocity.z;
            rb.linearVelocity = next + away * (strength * closeness * closeness / (1f + speed * 0.45f)) * Time.fixedDeltaTime;
        }
    }

    /// <summary>Curves a flyer around nearby dunes. Uwrenmaw uses this so his direction actually turns.</summary>
    public static Vector3 Steer(Vector3 position, Vector3 direction, float speed)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return direction;
        direction.z = 0f;
        direction.Normalize();

        SandDuneDeflect[] dunes = Object.FindObjectsByType<SandDuneDeflect>(FindObjectsInactive.Exclude);
        for (int i = 0; i < dunes.Length; i++)
        {
            SandDuneDeflect dune = dunes[i];
            if (dune == null)
                continue;
            Vector3 away = position - dune.transform.position;
            away.z = 0f;
            float worldRadius = Mathf.Max(dune.radius, dune.transform.lossyScale.x * 0.55f);
            float dist = away.magnitude;
            if (dist > worldRadius)
                continue;
            if (dist < 0.05f)
                away = Vector3.right;
            else
                away /= dist;
            float closeness = 1f - Mathf.Clamp01(dist / worldRadius);
            float turn = closeness * closeness * Mathf.Clamp01(6.5f / (speed + 0.75f));
            direction = Vector3.Slerp(direction, away, turn);
            direction.z = 0f;
            direction.Normalize();
        }
        return direction;
    }

    static bool Affects(Collider hit)
    {
        string tag = hit.tag;
        if (tag == "Ball" || tag == "Player" || tag == "Paddle")
            return true;
        if (hit.GetComponent<BallInfo>() != null || hit.GetComponentInParent<BallInfo>() != null)
            return true;
        if (hit.GetComponent<PlayerGrab>() != null || hit.GetComponentInParent<PlayerGrab>() != null)
            return true;
        string n = hit.name;
        return n != null && n.IndexOf("Bump", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
