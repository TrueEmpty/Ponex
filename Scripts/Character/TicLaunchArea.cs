using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tic plunger bay volume. Trigger so balls/bumpers can enter; bumpers also ignore
/// any leftover solid children. Tracks contents for plunger launch + stack placement.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TicLaunchArea : MonoBehaviour
{
    public Tic owner;

    readonly HashSet<TicBumper> inside = new HashSet<TicBumper>();
    readonly HashSet<Rigidbody> heldBalls = new HashSet<Rigidbody>();
    readonly HashSet<TicBumper> currentInside = new HashSet<TicBumper>();
    readonly HashSet<int> launchedIds = new HashSet<int>();
    readonly List<Rigidbody> deadBodies = new List<Rigidbody>();
    readonly List<Rigidbody> heldBallCopy = new List<Rigidbody>();
    Collider[] selfCols;
    Collider[] overlapBuffer = new Collider[64];

    [Tooltip("Minimum height above the plunger tip for an empty bay stack slot.")]
    public float emptyStackHeight = 0.55f;
    public float stackGap = 0.08f;
    [Tooltip("Balls larger than this fraction of the bay's smallest axis won't load — they get ejected.")]
    [Range(0.35f, 1.25f)]
    public float maxFitFraction = 0.85f;

    void Awake()
    {
        selfCols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < selfCols.Length; i++)
        {
            if (selfCols[i] == null)
                continue;
            // Bay volume must not block balls — keep as trigger
            selfCols[i].isTrigger = true;
        }
    }

    public void SetOwner(Tic tic)
    {
        owner = tic;
    }

    public void RegisterBumperPassThrough(TicBumper bumper)
    {
        if (bumper == null)
            return;
        bumper.IgnoreColliders(selfCols, true);
    }

    void FixedUpdate()
    {
        if (owner == null)
            return;

        RefreshContents();
        HoldTrackedBalls();
    }

    void RefreshContents()
    {
        int hitCount = OverlapBay(0.35f);

        currentInside.Clear();
        for (int i = 0; i < hitCount; i++)
        {
            Collider c = overlapBuffer[i];
            if (c == null)
                continue;

            TicBumper bumper = c.GetComponentInParent<TicBumper>();
            if (bumper != null)
            {
                if (!InShaft(bumper))
                    continue;
                currentInside.Add(bumper);
                RegisterBumperPassThrough(bumper);
                bumper.SetInLaunchArea(true, owner);
                continue;
            }

            // Ignore ball ↔ bay solid leftovers if any appear later
            if (c.CompareTag("Ball"))
                IgnoreBall(c);
        }

        foreach (TicBumper b in inside)
        {
            if (b != null && !currentInside.Contains(b))
                b.SetInLaunchArea(false, owner);
        }
        inside.Clear();
        foreach (TicBumper b in currentInside)
            inside.Add(b);
    }

    void IgnoreBall(Collider ballCol)
    {
        if (ballCol == null || selfCols == null)
            return;
        for (int i = 0; i < selfCols.Length; i++)
        {
            if (selfCols[i] == null)
                continue;
            Physics.IgnoreCollision(selfCols[i], ballCol, true);
        }
    }

    void HoldTrackedBalls()
    {
        if (heldBalls.Count == 0)
            return;

        deadBodies.Clear();
        foreach (Rigidbody rb in heldBalls)
        {
            if (rb == null)
            {
                deadBodies.Add(rb);
                continue;
            }
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (deadBodies.Count > 0)
        {
            for (int i = 0; i < deadBodies.Count; i++)
                heldBalls.Remove(deadBodies[i]);
        }
    }

    public void HoldBall(Rigidbody ballRb)
    {
        if (ballRb == null)
            return;
        heldBalls.Add(ballRb);
        BallMovement move = ballRb.GetComponent<BallMovement>();
        if (move != null)
            move.SetLauncherHold(true);
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
    }

    public void ReleaseBall(Rigidbody ballRb)
    {
        if (ballRb == null)
            return;
        heldBalls.Remove(ballRb);
        BallMovement move = ballRb.GetComponent<BallMovement>();
        if (move != null)
            move.SetLauncherHold(false);
    }

    /// <summary>True when a ball of this radius can sit in the plunger bay.</summary>
    public bool CanFitBall(float objectRadius)
    {
        return objectRadius <= BayMinHalfExtent() * maxFitFraction;
    }

    float BayMinHalfExtent()
    {
        Vector3 lossy = transform.lossyScale;
        // Box collider size is 1×1×1 scaled by transform
        float hx = Mathf.Abs(lossy.x) * 0.5f;
        float hy = Mathf.Abs(lossy.y) * 0.5f;
        float hz = Mathf.Abs(lossy.z) * 0.5f;
        return Mathf.Max(0.25f, Mathf.Min(hx, Mathf.Min(hy, hz)));
    }

    /// <summary>World point at the mouth / top of the launch bay (along Tic up).</summary>
    public Vector3 GetLaunchMouthPosition(float objectRadius)
    {
        Transform ticT = owner != null ? owner.transform : transform;
        Vector3 up = ticT.up.normalized;
        Collider col = GetComponent<Collider>();
        Vector3 center = col != null ? col.bounds.center : transform.position;
        float halfUp;
        if (col != null)
        {
            Vector3 e = col.bounds.extents;
            halfUp = Mathf.Abs(up.x) * e.x + Mathf.Abs(up.y) * e.y + Mathf.Abs(up.z) * e.z;
        }
        else
        {
            halfUp = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y) * 0.5f;
        }
        return center + up * (halfUp + Mathf.Max(0.2f, objectRadius) + 0.15f);
    }

    /// <summary>
    /// Random velocity out of the bay (along Tic up + slight sideways), like a plunger fire.
    /// </summary>
    public Vector3 GetEjectVelocity()
    {
        Transform ticT = owner != null ? owner.transform : transform;
        Vector3 up = ticT.up.normalized;
        Vector3 right = ticT.right.normalized;
        float speed = owner != null ? owner.plungerLaunchSpeed : 18f;
        // Mostly out of the bay, with a random lateral kick
        Vector3 dir = (up + right * Random.Range(-0.45f, 0.45f)).normalized;
        float mul = Random.Range(0.85f, 1.15f);
        return dir * (speed * mul);
    }

    /// <summary>
    /// Next free slot above the plunger along Tic's up, stacking over bumpers / held balls.
    /// </summary>
    public Vector3 GetNextStackPosition(float objectRadius)
    {
        Transform plunger = owner != null ? owner.plunger : null;
        Transform ticT = owner != null ? owner.transform : transform;
        Vector3 up = ticT.up.normalized;
        Vector3 origin = plunger != null ? plunger.position : transform.position;

        float top = emptyStackHeight;
        float pad = Mathf.Max(0.05f, objectRadius) + stackGap;

        foreach (TicBumper b in inside)
        {
            if (b == null)
                continue;
            top = Mathf.Max(top, ExtentAlongUp(b.transform, origin, up) + pad);
        }

        foreach (Rigidbody rb in heldBalls)
        {
            if (rb == null)
                continue;
            top = Mathf.Max(top, ExtentAlongUp(rb.transform, origin, up) + pad);
        }

        // Also scan live overlaps for balls/bumpers not yet tracked
        Vector3 center = transform.position;
        float scanR = Mathf.Max(transform.lossyScale.magnitude * 0.6f, 1f);
        int hitCount = Overlap(center, scanR);
        for (int i = 0; i < hitCount; i++)
        {
            Collider c = overlapBuffer[i];
            if (c == null || c.transform.IsChildOf(ticT))
                continue;
            if (c.GetComponentInParent<TicBumper>() == null && !c.CompareTag("Ball"))
                continue;
            float r = EstimateRadius(c);
            top = Mathf.Max(top, ExtentAlongUp(c.transform, origin, up) + r + stackGap + objectRadius);
        }

        return origin + up * (top + objectRadius);
    }

    static float ExtentAlongUp(Transform t, Vector3 origin, Vector3 up)
    {
        if (t == null)
            return 0f;
        Collider col = t.GetComponentInChildren<Collider>();
        float along = Vector3.Dot(t.position - origin, up);
        if (col != null)
            along += EstimateRadius(col);
        return along;
    }

    static float EstimateRadius(Collider c)
    {
        if (c == null)
            return 0.4f;
        Vector3 e = c.bounds.extents;
        return Mathf.Max(0.15f, Mathf.Max(e.x, Mathf.Max(e.y, e.z)));
    }

    /// <summary>Launch every non-frozen body currently in the bay along owner facing.</summary>
    public void PlungerLaunch(Vector3 velocity, float radiusExtra)
    {
        if (owner == null)
            return;

        int hitCount = OverlapBay(radiusExtra);

        launchedIds.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider c = overlapBuffer[i];
            if (c == null)
                continue;

            TicBumper bumper = c.GetComponentInParent<TicBumper>();
            if (bumper != null)
            {
                if (bumper.IsFrozen || !InShaft(bumper))
                    continue;
                int id = bumper.GetEntityId().GetHashCode();
                if (!launchedIds.Add(id))
                    continue;
                bumper.SetInLaunchArea(true, owner);
                bumper.Launch(velocity);
                continue;
            }

            if (c.transform.IsChildOf(owner.transform) || c.transform == owner.transform)
                continue;

            Rigidbody body = c.attachedRigidbody;
            if (body == null || body.isKinematic)
                continue;

            int bid = body.GetEntityId().GetHashCode();
            if (!launchedIds.Add(bid))
                continue;

            ReleaseBall(body);
            body.linearVelocity = velocity;
            body.angularVelocity = Vector3.zero;
        }

        // Held balls that overlap scan missed
        if (heldBalls.Count > 0)
        {
            heldBallCopy.Clear();
            foreach (Rigidbody body in heldBalls)
                heldBallCopy.Add(body);
            for (int i = 0; i < heldBallCopy.Count; i++)
            {
                Rigidbody body = heldBallCopy[i];
                if (body == null)
                    continue;
                ReleaseBall(body);
                body.linearVelocity = velocity;
                body.angularVelocity = Vector3.zero;
            }
        }
    }

    int Overlap(Vector3 center, float radius)
    {
        int count;
        while ((count = Physics.OverlapSphereNonAlloc(
            center, radius, overlapBuffer, ~0, QueryTriggerInteraction.Collide)) == overlapBuffer.Length)
        {
            overlapBuffer = new Collider[overlapBuffer.Length * 2];
        }
        return count;
    }

    /// <summary>
    /// Cover the authored launch box plus the playfield-safe plunger rest so bumpers
    /// sitting in the shaft are still found after the cabinet is snapped flush.
    /// </summary>
    int OverlapBay(float radiusExtra)
    {
        Collider col = GetComponent<Collider>();
        Vector3 center = col != null ? col.bounds.center : transform.position;
        float radius = 1.25f;
        if (col != null)
            radius = col.bounds.extents.magnitude + Mathf.Max(0.25f, radiusExtra);
        else
            radius = Mathf.Max(transform.lossyScale.magnitude * 0.6f, 1.25f) + radiusExtra;

        if (owner != null)
        {
            Vector3 rest = owner.GetBayRestPosition();
            radius = Mathf.Max(radius, Vector3.Distance(center, rest) + 0.7f);
            if (owner.loadPoint != null)
                radius = Mathf.Max(radius, Vector3.Distance(center, owner.loadPoint.position) + 0.5f);
        }

        return Overlap(center, radius);
    }

    bool InShaft(TicBumper bumper)
    {
        if (bumper == null || owner == null)
            return bumper != null;
        Vector3 rest = owner.GetBayRestPosition();
        Vector3 to = bumper.transform.position - rest;
        to.z = 0f;
        float along = Vector3.Dot(to, owner.transform.up);
        float side = Mathf.Abs(Vector3.Dot(to, owner.transform.right));
        if (along < -0.7f)
            return false;
        if (along > 3.4f)
            return false;
        if (side > 1.7f)
            return false;
        return true;
    }

    void OnDestroy()
    {
        foreach (TicBumper b in inside)
        {
            if (b != null)
                b.SetInLaunchArea(false, null);
        }
        inside.Clear();

        foreach (Rigidbody rb in heldBalls)
        {
            if (rb == null)
                continue;
            BallMovement move = rb.GetComponent<BallMovement>();
            if (move != null)
                move.SetLauncherHold(false);
        }
        heldBalls.Clear();
    }
}
