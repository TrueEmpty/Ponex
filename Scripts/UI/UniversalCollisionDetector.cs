using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks overlap enter/exit against other colliders without requiring a Rigidbody.
/// Safe for UI (shared Canvas root) and objects moved via Transform / RectTransform.
/// </summary>
public class UniversalCollisionDetector : MonoBehaviour
{
    [Tooltip("Colliders currently overlapping this object's colliders.")]
    public List<Collider> trackedColliders = new List<Collider>();

    [SerializeField]
    private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Tooltip("If false, ignore the project Layer Collision Matrix (still respects excludeLayers).")]
    [SerializeField]
    private bool respectLayerMatrix = true;

    [Tooltip("Extra padding added to the broadphase search radius.")]
    [SerializeField]
    private float broadphasePadding = 0.25f;

    [SerializeField]
    private bool debugLogs = false;

    private Collider[] myColliders;
    private readonly HashSet<Collider> currentFrameColliders = new HashSet<Collider>();
    private readonly List<Collider> exitBuffer = new List<Collider>();
    static readonly Collider[] overlapBuffer = new Collider[48];
    Vector3 lastSyncPos;
    bool hasLastSyncPos;

    void Awake()
    {
        RefreshColliders();
    }

    void OnEnable()
    {
        RefreshColliders();
    }

    /// <summary>Call if colliders are added/removed on this object at runtime.</summary>
    public void RefreshColliders()
    {
        myColliders = GetComponents<Collider>();
    }

    // LateUpdate so RectTransform / Transform moves from Update are already applied
    void LateUpdate()
    {
        DetectOverlaps();
    }

    void DetectOverlaps()
    {
        if (myColliders == null || myColliders.Length == 0)
            return;

        // Only sync physics when the cursor actually moved
        Vector3 pos = transform.position;
        if (!hasLastSyncPos || (pos - lastSyncPos).sqrMagnitude > 0.0001f)
        {
            Physics.SyncTransforms();
            lastSyncPos = pos;
            hasLastSyncPos = true;
        }

        currentFrameColliders.Clear();

        if (!TryGetBroadphase(out Vector3 center, out float radius))
            return;

        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, overlapBuffer, ~0, triggerInteraction);

        for (int i = 0; i < hitCount; i++)
        {
            Collider other = overlapBuffer[i];
            if (other == null || !other.enabled || !other.gameObject.activeInHierarchy)
                continue;

            // Only skip our own colliders / children — NOT the whole Canvas/root hierarchy
            if (IsOwnCollider(other))
                continue;

            for (int c = 0; c < myColliders.Length; c++)
            {
                Collider mine = myColliders[c];
                if (mine == null || !mine.enabled || mine == other)
                    continue;

                if (!ShouldProcessCollision(mine, other))
                    continue;

                if (mine is MeshCollider meshMine && !meshMine.convex)
                    continue;
                if (other is MeshCollider meshOther && !meshOther.convex)
                    continue;

                bool overlapping = Physics.ComputePenetration(
                    mine, mine.transform.position, mine.transform.rotation,
                    other, other.transform.position, other.transform.rotation,
                    out _, out _
                );

                // Thin UI colliders (e.g. z=1 boxes) can sit flush; also accept bounds overlap
                if (!overlapping && mine.bounds.Intersects(other.bounds))
                    overlapping = true;

                if (overlapping)
                {
                    currentFrameColliders.Add(other);

                    if (!trackedColliders.Contains(other))
                    {
                        trackedColliders.Add(other);
                        OnManualCollisionEnter(other);
                    }

                    break;
                }
            }
        }

        exitBuffer.Clear();
        for (int i = 0; i < trackedColliders.Count; i++)
        {
            Collider tracked = trackedColliders[i];
            if (tracked == null
                || !tracked.enabled
                || !tracked.gameObject.activeInHierarchy
                || !currentFrameColliders.Contains(tracked))
            {
                exitBuffer.Add(tracked);
            }
        }

        for (int i = 0; i < exitBuffer.Count; i++)
        {
            Collider removed = exitBuffer[i];
            trackedColliders.Remove(removed);
            if (removed != null)
                OnManualCollisionExit(removed);
        }
    }

    private bool IsOwnCollider(Collider other)
    {
        if (other.transform == transform)
            return true;

        // Child of this object
        if (other.transform.IsChildOf(transform))
            return true;

        // Parent collider above us (rare, but avoid self-hierarchy false positives)
        if (transform.IsChildOf(other.transform))
            return true;

        return false;
    }

    private bool TryGetBroadphase(out Vector3 center, out float radius)
    {
        bool hasBounds = false;
        Bounds combined = new Bounds();

        for (int i = 0; i < myColliders.Length; i++)
        {
            Collider col = myColliders[i];
            if (col == null || !col.enabled)
                continue;

            if (!hasBounds)
            {
                combined = col.bounds;
                hasBounds = true;
            }
            else
            {
                combined.Encapsulate(col.bounds);
            }
        }

        if (!hasBounds)
        {
            center = transform.position;
            radius = 0f;
            return false;
        }

        center = combined.center;
        radius = combined.extents.magnitude + broadphasePadding;
        return radius > 0f;
    }

    private bool ShouldProcessCollision(Collider mine, Collider other)
    {
        int mineLayer = mine.gameObject.layer;
        int otherLayer = other.gameObject.layer;
        int mineLayerMask = 1 << mineLayer;
        int otherLayerMask = 1 << otherLayer;

        if ((mine.excludeLayers.value & otherLayerMask) != 0)
            return false;
        if ((other.excludeLayers.value & mineLayerMask) != 0)
            return false;

        bool mineIncludesOther = (mine.includeLayers.value & otherLayerMask) != 0;
        bool otherIncludesMine = (other.includeLayers.value & mineLayerMask) != 0;
        if (mineIncludesOther || otherIncludesMine)
            return true;

        if (!respectLayerMatrix)
            return true;

        return !Physics.GetIgnoreLayerCollision(mineLayer, otherLayer);
    }

    public bool IsTouching(Collider other)
    {
        return other != null && trackedColliders.Contains(other);
    }

    public bool IsTouchingAny()
    {
        return trackedColliders.Count > 0;
    }

    private void OnManualCollisionEnter(Collider other)
    {
        if (debugLogs)
            Debug.Log($"[Universal Enter] Intersected with: {other.name} ({other.GetType().Name})", this);

        gameObject.SendMessage("CollisionEntered", other, SendMessageOptions.DontRequireReceiver);
    }

    private void OnManualCollisionExit(Collider other)
    {
        if (debugLogs)
            Debug.Log($"[Universal Exit] Separated from: {other.name}", this);

        gameObject.SendMessage("CollisionExited", other, SendMessageOptions.DontRequireReceiver);
    }
}
