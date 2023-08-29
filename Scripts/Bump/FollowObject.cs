using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObject : MonoBehaviour
{
    PlayerGrab pG;
    public Transform target;
    public string playerChildName = null;
    public bool followParent = false;

    public Vector3 offset = Vector3.zero;
    public bool autoOffset = true;
    public bool reverseFollow = false;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();

        if (target != null && autoOffset)
        {
            if (reverseFollow)
            {
                offset = transform.position - target.position;
            }
            else
            {
                offset = target.position - transform.position;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (target != null)
        {
            if (reverseFollow)
            {
                target.position = transform.position - (transform.right * offset.x) - (transform.up * offset.y) - (transform.forward * offset.z);
            }
            else
            {
                transform.position = target.position - (target.right * offset.x) - (target.up * offset.y) - (target.forward * offset.z);
            }
        }
        else
        {
            if (playerChildName != null && !followParent)
            {
                target = pG.player.transform.Find(playerChildName);
            }
            else if(followParent)
            {
                target = pG.player.transform;
            }


            if (target != null && autoOffset)
            {
                if (reverseFollow)
                {
                    offset = transform.position - target.position;
                }
                else
                {
                    offset = target.position - transform.position;
                }
            }
        }
    }
}
