using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetBallOwnerOnTagHit : MonoBehaviour
{
    PlayerGrab pG;

    public List<string> targetTags = new List<string>();

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (targetTags.Contains(collision.transform.tag))
        {
            PlayerGrab tpG = collision.gameObject.GetComponent<PlayerGrab>();

            if(tpG != null)
            {
                pG.player = tpG.player;
            }
        }
    }
}
