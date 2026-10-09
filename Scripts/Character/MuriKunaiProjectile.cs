using UnityEngine;

/// <summary>
/// Flying / stuck kunai. Sweeps a ray from the tip; passes through balls, sticks to other solids.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MuriKunaiProjectile : MonoBehaviour
{
    public Vector3 StickNormal { get; private set; } = Vector3.up;
    public bool IsFlying => state == State.Flying;
    public bool IsStuck => state == State.Stuck;

    const float Skin = 0.04f;
    const float CastRadius = 0.04f;

    enum State { Flying, Stuck, Dead }

    MuriKunai owner;
    Rigidbody rb;
    Collider col;
    CapsuleCollider cap;
    PlayerGrab grab;
    Vector3 flyDir = Vector3.up;
    float speed = 22f;
    State state = State.Flying;
    Transform stickParent;
    Collider stickCollider;
    Vector3 localPos;
    Quaternion localRot;
    bool refunded;
    float nextCrushCheck;

    static readonly RaycastHit[] rayHits = new RaycastHit[24];
    static readonly Collider[] crushHits = new Collider[16];

    public void Launch(MuriKunai source, Vector3 direction, float launchSpeed)
    {
        owner = source;
        flyDir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up;
        flyDir.z = 0f;
        if (flyDir.sqrMagnitude < 0.0001f)
            flyDir = Vector3.up;
        flyDir.Normalize();
        speed = launchSpeed;
        state = State.Flying;
        EnsureRefs();
        if (col != null)
            col.isTrigger = true;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, flyDir);
        rb.rotation = transform.rotation;
        SyncChildGrabs();
    }

    void SyncChildGrabs()
    {
        if (grab == null)
            return;
        PlayerGrab[] grabs = GetComponentsInChildren<PlayerGrab>(true);
        for (int i = 0; i < grabs.Length; i++)
        {
            if (grabs[i] != null)
                grabs[i].playerIndex = grab.playerIndex;
        }
    }

    void Awake()
    {
        EnsureRefs();
    }

    void EnsureRefs()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (col == null)
            col = GetComponent<Collider>();
        if (cap == null)
            cap = col as CapsuleCollider;
        if (grab == null)
            grab = GetComponent<PlayerGrab>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionZ
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationY;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }

    float TipOffset()
    {
        if (cap != null)
            return Mathf.Max(0.02f, cap.height * 0.5f);
        return 0.09f;
    }

    void FixedUpdate()
    {
        if (state == State.Dead)
            return;

        if (state == State.Flying)
        {
            TickFlight();
            return;
        }

        if (state == State.Stuck)
        {
            if (stickParent == null || !stickParent.gameObject.activeInHierarchy)
            {
                Vanish();
                return;
            }

            Vector3 pos = stickParent.TransformPoint(localPos);
            Quaternion rot = stickParent.rotation * localRot;
            pos.z = FlyZ();
            transform.SetPositionAndRotation(pos, rot);
            rb.position = pos;
            rb.rotation = rot;

            if (Time.time >= nextCrushCheck)
            {
                nextCrushCheck = Time.time + 0.12f;
                if (IsCrushed())
                    Vanish();
            }
        }
    }

    void TickFlight()
    {
        float dt = Time.fixedDeltaTime;
        float step = speed * dt;
        Vector3 pos = rb.position;
        pos.z = FlyZ();
        Vector3 tip = pos + flyDir * TipOffset();
        Vector3 from = tip - flyDir * 0.02f;
        float castDist = step + Skin + 0.02f;

        int n = Physics.SphereCastNonAlloc(from, CastRadius, flyDir, rayHits, castDist, ~0, QueryTriggerInteraction.Ignore);
        float stickDist = float.PositiveInfinity;
        RaycastHit stickHit = default;
        bool stick = false;
        bool oob = false;

        for (int i = 0; i < n; i++)
        {
            RaycastHit hit = rayHits[i];
            if (hit.collider == null || IsOwner(hit.collider) || IsKunaiPart(hit.collider))
                continue;

            if (IsBall(hit.collider))
                continue;

            if (IsOutOfBounds(hit.collider))
            {
                oob = true;
                continue;
            }

            if (!IsStickable(hit.collider))
                continue;

            if (hit.distance < stickDist)
            {
                stickDist = hit.distance;
                stickHit = hit;
                stick = true;
            }
        }

        if (oob && !stick)
        {
            Vanish();
            return;
        }

        if (stick)
        {
            StickTo(stickHit);
            return;
        }

        pos += flyDir * step;
        pos.z = FlyZ();
        Quaternion rot = Quaternion.LookRotation(Vector3.forward, flyDir);
        transform.SetPositionAndRotation(pos, rot);
        rb.position = pos;
        rb.rotation = rot;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;
        if (IsBall(collision.collider) && owner != null)
            owner.OnKunaiHitBall();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null || IsOwner(other) || IsKunaiPart(other))
            return;
        if (IsBall(other) && owner != null)
            owner.OnKunaiHitBall();
        if (state == State.Flying && IsOutOfBounds(other))
            Vanish();
    }

    void StickTo(RaycastHit hit)
    {
        state = State.Stuck;
        stickCollider = hit.collider;
        stickParent = hit.collider.transform;
        Vector3 n = Flatten(hit.normal);
        if (n.sqrMagnitude < 0.0001f)
            n = -flyDir;
        n.Normalize();
        if (Vector3.Dot(n, -flyDir) < 0f)
            n = -n;
        StickNormal = n;

        float embed = 0.03f;
        Vector3 pos = hit.point - flyDir * (TipOffset() - embed);
        pos.z = FlyZ();
        Quaternion rot = Quaternion.LookRotation(Vector3.forward, flyDir);
        transform.SetParent(null, true);
        transform.SetPositionAndRotation(pos, rot);
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = pos;
        rb.rotation = rot;
        if (col != null)
            col.isTrigger = true;

        transform.SetParent(stickParent, true);
        localPos = transform.localPosition;
        localRot = transform.localRotation;
        nextCrushCheck = Time.time + 0.45f;
    }

    public void ConsumeForBlink()
    {
        Vanish();
    }

    void Vanish()
    {
        if (state == State.Dead)
            return;
        state = State.Dead;
        if (!refunded && owner != null)
        {
            refunded = true;
            owner.Refund(this);
        }
        Destroy(gameObject);
    }

    bool IsCrushed()
    {
        if (col == null)
            return false;
        Bounds b = col.bounds;
        int n = Physics.OverlapBoxNonAlloc(b.center, b.extents * 0.55f, crushHits, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        int extras = 0;
        for (int i = 0; i < n; i++)
        {
            Collider hit = crushHits[i];
            if (hit == null || IsKunaiPart(hit) || IsOwner(hit) || IsBall(hit) || hit.isTrigger)
                continue;
            if (IsStickFamily(hit))
                continue;
            extras++;
        }
        return extras >= 2;
    }

    bool IsStickFamily(Collider hit)
    {
        if (hit == stickCollider)
            return true;
        if (stickParent == null || hit == null)
            return false;
        Transform t = hit.transform;
        return t == stickParent || t.IsChildOf(stickParent) || stickParent.IsChildOf(t);
    }

    bool IsKunaiPart(Collider other)
    {
        return other != null && (other == col || other.transform == transform || other.transform.IsChildOf(transform));
    }

    bool IsOwner(Collider other)
    {
        if (owner == null || other == null)
            return false;
        return other.transform == owner.transform || other.transform.IsChildOf(owner.transform);
    }

    static bool IsBall(Collider other)
    {
        return other.CompareTag("Ball") || other.CompareTag("Ghost");
    }

    static bool IsOutOfBounds(Collider other)
    {
        return other.CompareTag("OutOfBounds");
    }

    static bool IsStickable(Collider other)
    {
        if (other.isTrigger)
            return false;
        if (IsBall(other) || IsOutOfBounds(other))
            return false;
        return true;
    }

    static Vector3 Flatten(Vector3 v)
    {
        v.z = 0f;
        return v;
    }

    float FlyZ()
    {
        Database db = Database.instance;
        return db != null ? db.FieldPlaySize : transform.position.z;
    }

    void OnDestroy()
    {
        if (state != State.Dead && !refunded && owner != null)
        {
            refunded = true;
            owner.Refund(this);
        }
    }
}
