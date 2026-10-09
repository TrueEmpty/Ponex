using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class OrbitSatellite : MonoBehaviour
{
    Rigidbody rb;
    Vector3 target;
    float pullStrength = 6f;
    float maxAccel = 22f;
    float damp = 0.85f;
    bool hasTarget;

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

    void FixedUpdate()
    {
        if (!hasTarget || rb == null)
            return;

        // Soft shepherding only — never snap or overwrite velocity.
        // Regular bumps / BallMovement keep working; pull gently re-centers over time.
        Vector3 toTarget = target - rb.position;
        toTarget.z = 0f;

        Vector3 vel = rb.linearVelocity;
        vel.z = 0f;

        Vector3 accel = toTarget * pullStrength - vel * damp;
        accel = Vector3.ClampMagnitude(accel, maxAccel);
        accel.z = 0f;
        rb.AddForce(accel, ForceMode.Acceleration);
    }
}
