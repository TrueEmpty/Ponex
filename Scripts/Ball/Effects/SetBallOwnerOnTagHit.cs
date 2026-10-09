using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetBallOwnerOnTagHit : MonoBehaviour
{
    PlayerGrab pG;
    public float delay = 0;
    public List<string> targetTags = new List<string>();
    PlayerGrab target;
    int ppg = -1;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;
        TryClaim(other.gameObject, other.tag);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (TagListMatcher.Contains(targetTags, collision.transform.tag))
        {
            PlayerGrab tpG = collision.gameObject.GetComponent<PlayerGrab>();

            if (tpG != null && pG != null)
            {
                // Only claim ownership of balls — never overwrite Player/Lifeline indices
                if (!string.Equals(collision.transform.tag.Trim(), "ball", System.StringComparison.OrdinalIgnoreCase))
                    return;

                if (delay > 0)
                {
                    target = tpG;
                    ppg = tpG.playerIndex;
                    Invoke(nameof(SetTag), delay);
                }
                else
                {
                    tpG.playerIndex = pG.playerIndex;
                }
            }
        }
    }

    void TryClaim(GameObject hit, string tag)
    {
        if (!TagListMatcher.Contains(targetTags, tag))
            return;
        if (!string.Equals(tag.Trim(), "ball", System.StringComparison.OrdinalIgnoreCase))
            return;
        if (pG == null)
            pG = GetComponent<PlayerGrab>();
        PlayerGrab tpG = hit.GetComponent<PlayerGrab>();
        if (tpG == null || pG == null)
            return;
        tpG.playerIndex = pG.playerIndex;
    }

    void SetTag()
    {
        if (target != null)
        {
            if(ppg == target.playerIndex)
            {
                target.playerIndex = pG.playerIndex;
            }

            target = null;
            ppg = -1;
        }
    }
}
