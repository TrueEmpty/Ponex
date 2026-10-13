using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks trigger overlap against other colliders via OnTriggerEnter/Exit.
/// The selector needs a kinematic Rigidbody; buttons need trigger colliders.
/// </summary>
public class UniversalCollisionDetector : MonoBehaviour
{
    [Tooltip("Colliders currently overlapping this object's colliders.")]
    public List<Collider> trackedColliders = new List<Collider>();

    [SerializeField]
    private bool debugLogs = false;

    readonly HashSet<Collider> trackedLookup = new HashSet<Collider>(32);
    readonly HashSet<Collider> seen = new HashSet<Collider>(32);
    readonly List<Collider> stale = new List<Collider>(8);
    static readonly Collider[] overlapBuffer = new Collider[48];

    void OnDisable()
    {
        for (int i = 0; i < trackedColliders.Count; i++)
        {
            Collider other = trackedColliders[i];
            if (other != null)
                OnManualCollisionExit(other);
        }

        trackedColliders.Clear();
        trackedLookup.Clear();
    }

    void LateUpdate()
    {
        if (trackedColliders.Count == 0)
            return;

        stale.Clear();
        for (int i = 0; i < trackedColliders.Count; i++)
        {
            Collider tracked = trackedColliders[i];
            if (tracked == null || !tracked.enabled || !tracked.gameObject.activeInHierarchy)
                stale.Add(tracked);
        }

        for (int i = 0; i < stale.Count; i++)
        {
            Collider removed = stale[i];
            trackedColliders.Remove(removed);
            trackedLookup.Remove(removed);
            if (removed != null)
                OnManualCollisionExit(removed);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsTrackable(other))
            return;
        if (!trackedLookup.Add(other))
            return;

        trackedColliders.Add(other);
        OnManualCollisionEnter(other);
    }

    void OnTriggerExit(Collider other)
    {
        if (other == null)
            return;
        if (!trackedLookup.Remove(other))
            return;

        trackedColliders.Remove(other);
        OnManualCollisionExit(other);
    }

    bool IsTrackable(Collider other)
    {
        if (other == null || !other.enabled || !other.gameObject.activeInHierarchy)
            return false;
        if (other.transform == transform)
            return false;
        if (other.transform.IsChildOf(transform) || transform.IsChildOf(other.transform))
            return false;
        return true;
    }

    public void RefreshColliders()
    {
        // Kept for callers that used the old overlap detector.
    }

    /// <summary>
    /// Physics does not step while Time.timeScale is 0, so trigger enter/exit
    /// never fires for a cursor that is moved by hand. Query overlaps directly.
    /// </summary>
    public void PollOverlaps()
    {
        Collider self = GetComponent<Collider>();
        if (self == null || !self.enabled || !self.gameObject.activeInHierarchy)
            return;

        Physics.SyncTransforms();

        int mask = self.includeLayers.value;
        if (mask == 0)
            mask = Physics.AllLayers;

        Vector3 center = self.bounds.center;
        float radius = Mathf.Max(self.bounds.extents.x, self.bounds.extents.y);
        if (radius < 0.01f)
            radius = 0.01f;

        int count = Physics.OverlapSphereNonAlloc(
            center,
            radius,
            overlapBuffer,
            mask,
            QueryTriggerInteraction.Collide);

        seen.Clear();
        for (int i = 0; i < count; i++)
        {
            Collider other = overlapBuffer[i];
            overlapBuffer[i] = null;
            if (!IsTrackable(other))
                continue;
            if (!seen.Add(other))
                continue;
            if (!trackedLookup.Add(other))
                continue;

            trackedColliders.Add(other);
            OnManualCollisionEnter(other);
        }

        stale.Clear();
        for (int i = 0; i < trackedColliders.Count; i++)
        {
            Collider tracked = trackedColliders[i];
            if (tracked == null || !seen.Contains(tracked))
                stale.Add(tracked);
        }

        for (int i = 0; i < stale.Count; i++)
        {
            Collider removed = stale[i];
            trackedColliders.Remove(removed);
            trackedLookup.Remove(removed);
            if (removed != null)
                OnManualCollisionExit(removed);
        }
    }

    public bool IsTouching(Collider other)
    {
        return other != null && trackedLookup.Contains(other);
    }

    public bool IsTouchingAny()
    {
        return trackedColliders.Count > 0;
    }

    void OnManualCollisionEnter(Collider other)
    {
        if (debugLogs)
            Debug.Log($"[Universal Enter] Intersected with: {other.name} ({other.GetType().Name})", this);

        gameObject.SendMessage("CollisionEntered", other, SendMessageOptions.DontRequireReceiver);
    }

    void OnManualCollisionExit(Collider other)
    {
        if (debugLogs)
            Debug.Log($"[Universal Exit] Separated from: {other.name}", this);

        gameObject.SendMessage("CollisionExited", other, SendMessageOptions.DontRequireReceiver);
    }
}
