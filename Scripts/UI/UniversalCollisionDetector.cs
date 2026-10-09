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
    readonly List<Collider> stale = new List<Collider>(8);

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
