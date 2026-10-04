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

    private void OnCollisionEnter(Collision collision)
    {
        if (targetTags.Exists(x => x.ToLower().Trim() == collision.transform.tag.ToLower().Trim()))
        {
            PlayerGrab tpG = collision.gameObject.GetComponent<PlayerGrab>();

            if (tpG != null && pG != null)
            {
                // Only claim ownership of balls — never overwrite Player/Lifeline indices
                if (collision.transform.tag.ToLower().Trim() != "ball")
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
