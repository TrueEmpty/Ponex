using UnityEngine;

/// <summary>Slowly wanders the playfield, steering away from stationary obstacles and borders.</summary>
[RequireComponent(typeof(Rigidbody))]
public class WanderingWagon : MonoBehaviour
{
    public float speed = 1.35f;
    public float turnSpeed = 1.8f;
    public float avoidDistance = 2.2f;
    public float halfPlay = 10f;
    public float borderPadding = 2.5f;

    Rigidbody rb;
    Vector2 dir;
    float repathTimer;
    readonly Collider[] overlap = new Collider[16];

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        dir = Random.insideUnitCircle.normalized;
        if (dir.sqrMagnitude < 0.01f)
            dir = Vector2.right;
        if (FieldCameraFit.TryMeasureWallOuterHalf(transform.root, out _, out float center))
            halfPlay = Mathf.Max(5f, center - borderPadding);
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;

        repathTimer -= Time.fixedDeltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = Random.Range(1.2f, 2.8f);
            dir = (dir + Random.insideUnitCircle * 0.6f).normalized;
        }

        Vector3 avoid = ComputeAvoidance();
        Vector2 desired = dir;
        if (avoid.sqrMagnitude > 0.01f)
            desired = (desired + new Vector2(avoid.x, avoid.y) * 1.6f).normalized;

        // Soft border push
        Vector3 p = rb.position;
        float limit = halfPlay;
        if (p.x > limit) desired.x = -Mathf.Abs(desired.x);
        if (p.x < -limit) desired.x = Mathf.Abs(desired.x);
        if (p.y > limit) desired.y = -Mathf.Abs(desired.y);
        if (p.y < -limit) desired.y = Mathf.Abs(desired.y);

        dir = Vector2.Lerp(dir, desired, turnSpeed * Time.fixedDeltaTime).normalized;
        float z = Database.instance != null ? Database.instance.FieldPlaySize : p.z;
        Vector3 vel = new Vector3(dir.x, dir.y, 0f) * speed;
        rb.linearVelocity = vel;
        p.z = z;
        rb.position = p;

        if (dir.sqrMagnitude > 0.01f)
        {
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rb.MoveRotation(Quaternion.Euler(0f, 0f, ang));
        }
    }

    Vector3 ComputeAvoidance()
    {
        Vector3 sum = Vector3.zero;
        int n = Physics.OverlapSphereNonAlloc(transform.position, avoidDistance, overlap, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            Collider c = overlap[i];
            if (c == null || c.transform == transform || c.transform.IsChildOf(transform))
                continue;
            // Skip other wandering wagons so they can pass
            if (c.GetComponentInParent<WanderingWagon>() != null)
                continue;
            // Prefer pushing off stationary obstacles / walls
            Vector3 away = transform.position - c.ClosestPoint(transform.position);
            away.z = 0f;
            float d = away.magnitude;
            if (d < 0.01f)
                away = Random.onUnitSphere;
            away.z = 0f;
            float w = 1f - Mathf.Clamp01(d / avoidDistance);
            sum += away.normalized * w;
        }
        return sum;
    }
}
