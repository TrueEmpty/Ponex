using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Garmen's cannon shot. The first solid hit knocks a ball, then the shot sticks
/// to that contact and rides along. Three seconds later it pops in a small blast.
/// </summary>
public class GarmenStickyShot : MonoBehaviour
{
    public float stickSeconds = 3f;
    public float blastRadius = 1.5f;
    public float blastPush = 3.5f;
    public float smokeScale = 0.85f;
    public GameObject smokePoof;

    bool stuck;
    bool booming;
    float stuckFor;
    Transform stuckTo;
    Transform pendingTarget;
    Vector3 pendingLocalPoint;
    Vector3 pendingLocalNormal;
    Rigidbody body;
    ConstantForward flight;
    SphereCollider shell;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        flight = GetComponent<ConstantForward>();
        shell = GetComponent<SphereCollider>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (stuck || booming || pendingTarget != null || collision == null || collision.contactCount == 0)
            return;
        if (IsOwnRig(collision.collider))
            return;

        ContactPoint contact = collision.GetContact(0);
        Rigidbody hitBody = collision.rigidbody;
        pendingTarget = hitBody != null ? hitBody.transform : collision.transform;
        pendingLocalPoint = pendingTarget.InverseTransformPoint(contact.point);
        pendingLocalNormal = pendingTarget.InverseTransformDirection(contact.normal);
    }

    void LateUpdate()
    {
        if (!stuck && pendingTarget != null)
            Stick();

        if (!stuck || booming)
            return;

        if (stuckTo == null)
        {
            Boom();
            return;
        }

        stuckFor += Time.deltaTime;
        if (stuckFor >= stickSeconds)
            Boom();
    }

    void Stick()
    {
        stuck = true;
        stuckTo = pendingTarget;
        pendingTarget = null;

        if (flight != null)
            flight.enabled = false;

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        if (shell != null)
            shell.enabled = false;

        if (stuckTo == null)
            return;

        float radius = 0.28f;
        if (shell != null)
            radius = shell.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);

        Vector3 point = stuckTo.TransformPoint(pendingLocalPoint);
        Vector3 normal = stuckTo.TransformDirection(pendingLocalNormal);
        if (normal.sqrMagnitude < 0.0001f)
            normal = Vector3.up;
        normal.Normalize();

        transform.position = point + normal * radius;
        transform.SetParent(stuckTo, true);
    }

    bool IsOwnRig(Collider other)
    {
        if (other == null)
            return false;
        if (other.GetComponentInParent<GarmenStickyShot>() != null)
            return false;
        if (other.CompareTag("Ball"))
            return false;
        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Ball"))
            return false;

        PlayerGrab mine = GetComponent<PlayerGrab>();
        PlayerGrab theirs = other.GetComponentInParent<PlayerGrab>();
        return mine != null
            && theirs != null
            && mine.playerIndex >= 0
            && theirs.playerIndex == mine.playerIndex;
    }

    void Boom()
    {
        if (booming)
            return;
        booming = true;

        Vector3 center = transform.position;
        transform.SetParent(null, true);

        GameObject poof = smokePoof != null ? smokePoof : BallBlast.SmokePoofPrefab;
        BallBlast.SpawnSmokeFire(center, poof, smokeScale);
        PushNearbyBalls(center);

        Destroy(gameObject);
    }

    void PushNearbyBalls(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, blastRadius);
        HashSet<int> pushed = new HashSet<int>();
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
                continue;
            Rigidbody ball = hit.attachedRigidbody;
            bool taggedBall = hit.CompareTag("Ball") || (ball != null && ball.CompareTag("Ball"));
            if (!taggedBall || ball == null || ball.gameObject == gameObject)
                continue;
            if (!pushed.Add(ball.GetEntityId().GetHashCode()))
                continue;

            Vector3 away = ball.worldCenterOfMass - center;
            away.z = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = Random.insideUnitCircle;
            away.Normalize();

            Vector3 velocity = ball.linearVelocity;
            velocity.z = 0f;
            velocity += away * blastPush;
            ball.linearVelocity = velocity;
        }
    }
}
