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
    Collider[] selfCols;

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
        Vector3 center = transform.position;
        Vector3 half = Vector3.Scale(transform.lossyScale, new Vector3(0.55f, 0.55f, 0.55f));
        float radius = Mathf.Max(half.x, half.y, half.z);
        Collider[] hits = Physics.OverlapSphere(center, Mathf.Max(0.5f, radius), ~0, QueryTriggerInteraction.Collide);

        HashSet<TicBumper> now = new HashSet<TicBumper>();
        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (c == null)
                continue;

            TicBumper bumper = c.GetComponentInParent<TicBumper>();
            if (bumper != null)
            {
                now.Add(bumper);
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
            if (b != null && !now.Contains(b))
                b.SetInLaunchArea(false, owner);
        }
        inside.Clear();
        foreach (TicBumper b in now)
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

        List<Rigidbody> dead = null;
        foreach (Rigidbody rb in heldBalls)
        {
            if (rb == null)
            {
                if (dead == null) dead = new List<Rigidbody>();
                dead.Add(rb);
                continue;
            }
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (dead != null)
        {
            for (int i = 0; i < dead.Count; i++)
                heldBalls.Remove(dead[i]);
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
        Collider[] hits = Physics.OverlapSphere(center, scanR, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
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

        Vector3 center = transform.position;
        Vector3 half = transform.lossyScale * 0.5f;
        float radius = Mathf.Max(half.x, half.y, half.z) + radiusExtra;
        Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Collide);

        HashSet<int> launched = new HashSet<int>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (c == null)
                continue;

            TicBumper bumper = c.GetComponentInParent<TicBumper>();
            if (bumper != null)
            {
                if (bumper.IsFrozen)
                    continue;
                int id = bumper.GetEntityId().GetHashCode();
                if (!launched.Add(id))
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
            if (!launched.Add(bid))
                continue;

            ReleaseBall(body);
            body.linearVelocity = velocity;
            body.angularVelocity = Vector3.zero;
        }

        // Held balls that overlap scan missed
        if (heldBalls.Count > 0)
        {
            List<Rigidbody> copy = new List<Rigidbody>(heldBalls);
            for (int i = 0; i < copy.Count; i++)
            {
                Rigidbody body = copy[i];
                if (body == null)
                    continue;
                ReleaseBall(body);
                body.linearVelocity = velocity;
                body.angularVelocity = Vector3.zero;
            }
        }
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
