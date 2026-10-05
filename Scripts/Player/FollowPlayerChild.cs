using UnityEngine;

/// <summary>
/// Follows a named child on the linked player's character (used by Tic lifeline paddles).
/// Recreated from ported prefab fields after the original script was lost in Character Creator.
/// </summary>
public class FollowPlayerChild : MonoBehaviour
{
    public Transform target;
    public string playerChildName;
    public bool followParent;
    public Vector3 offset;
    public bool autoOffset = true;
    public bool reverseFollow;

    PlayerGrab pg;
    bool capturedOffset;

    void Start()
    {
        pg = GetComponent<PlayerGrab>();
        if (pg == null)
            pg = GetComponentInParent<PlayerGrab>();

        ResolveTarget();
        CaptureAutoOffset();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            ResolveTarget();
            CaptureAutoOffset();
        }

        Transform follow = GetFollowTransform();
        if (follow == null)
            return;

        Vector3 useOffset = reverseFollow ? -offset : offset;
        transform.position = follow.position + useOffset;
        transform.rotation = follow.rotation;
    }

    Transform GetFollowTransform()
    {
        if (target == null)
            return null;
        if (followParent && target.parent != null)
            return target.parent;
        return target;
    }

    void CaptureAutoOffset()
    {
        if (!autoOffset || capturedOffset)
            return;

        Transform follow = GetFollowTransform();
        if (follow == null)
            return;

        offset = transform.position - follow.position;
        capturedOffset = true;
    }

    void ResolveTarget()
    {
        if (target != null || string.IsNullOrEmpty(playerChildName))
            return;

        Transform searchRoot = null;
        if (pg != null && pg.IsLinked() && pg.player != null && pg.player.spawnedPlayer != null)
            searchRoot = pg.player.spawnedPlayer.transform;

        if (searchRoot == null)
            return;

        target = FindDeepChild(searchRoot, playerChildName);
    }

    static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChild(parent.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }
}
