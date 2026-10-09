using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class OrbitSatellite : MonoBehaviour
{
    const float DisruptSeconds = 0.75f;
    const float RecoverSeconds = 0.6f;

    Rigidbody rb;
    Vector3 target;
    float pullStrength = 6f;
    float maxAccel = 22f;
    float damp = 0.85f;
    bool hasTarget;
    float disruptUntil = -999f;

    public void Initialize(OrbitBall owner, BallInfo coreInfo)
    {
        rb = GetComponent<Rigidbody>();
        BallInfo info = GetComponent<BallInfo>();
        if (info != null && coreInfo != null && coreInfo.ball != null)
        {
            info.ball = new Ball(coreInfo.ball);
            info.ball.name = "Orbit Ball";
            info.ball.damage = 1;
            info.ballReady = true;
            info.projectionOn = true;
            info.checkStuck = false;
            info.anchor = gameObject;
        }

        PlayerGrab own = owner != null ? owner.GetComponent<PlayerGrab>() : null;
        PlayerGrab mine = GetComponent<PlayerGrab>();
        if (own != null && mine != null && own.IsLinked())
            mine.playerIndex = own.playerIndex;
    }

    public void SetOrbitTarget(Vector3 worldTarget, float pull, float maxAcceleration)
    {
        target = worldTarget;
        pullStrength = pull;
        maxAccel = maxAcceleration;
        hasTarget = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision != null)
            TryDisrupt(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        TryDisrupt(other);
    }

    void TryDisrupt(Collider other)
    {
        if (other == null)
            return;
        if (other.transform == transform || other.transform.IsChildOf(transform))
            return;
        if (IsWall(other.tag))
            return;

        disruptUntil = Time.time + DisruptSeconds;
    }

    static bool IsWall(string tag)
    {
        if (string.IsNullOrEmpty(tag))
            return false;
        return tag.Equals("Wall", System.StringComparison.OrdinalIgnoreCase)
            || tag.Equals("Walls", System.StringComparison.OrdinalIgnoreCase)
            || tag.Equals("Obstacle", System.StringComparison.OrdinalIgnoreCase);
    }

    float PullScale()
    {
        if (Time.time < disruptUntil)
            return 0f;

        float since = Time.time - disruptUntil;
        if (since >= RecoverSeconds)
            return 1f;

        return Mathf.SmoothStep(0f, 1f, since / RecoverSeconds);
    }

    void FixedUpdate()
    {
        if (!hasTarget || rb == null)
            return;

        float scale = PullScale();
        if (scale <= 0.001f)
            return;

        // Soft shepherding only — never snap or overwrite velocity.
        // Regular bumps / BallMovement keep working; pull gently re-centers over time.
        Vector3 toTarget = target - rb.position;
        toTarget.z = 0f;

        Vector3 vel = rb.linearVelocity;
        vel.z = 0f;

        Vector3 accel = toTarget * (pullStrength * scale) - vel * (damp * scale);
        accel = Vector3.ClampMagnitude(accel, maxAccel * scale);
        accel.z = 0f;
        rb.AddForce(accel, ForceMode.Acceleration);
    }
}
