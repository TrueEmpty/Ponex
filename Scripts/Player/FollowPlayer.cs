using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    PlayerGrab pG;
    public Transform playerTransform;
    public Vector3 offset = Vector3.zero;
    Vector3 lerpOffset = Vector3.zero;
    public float ttt = 1.5f;

    public bool absolutePosition = false;
    public bool trueOffsetBasedOnRotation = false;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();

        if(playerTransform == null)
        {
            playerTransform = pG.player.spawnedPlayer.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(playerTransform != null)
        {
            Vector3 trueOffset = offset;

            if (trueOffsetBasedOnRotation)
            {
                trueOffset = (playerTransform.right * offset.x) + (playerTransform.up * offset.y) + (playerTransform.forward * offset.z);
            }

            if (absolutePosition)
            {
                transform.position = Vector3.MoveTowards(transform.position, playerTransform.position + trueOffset, ttt * Time.deltaTime);
            }
            else
            {
                transform.localPosition = trueOffset;

                if (trueOffset != lerpOffset)
                {
                    trueOffset = Vector3.MoveTowards(trueOffset, lerpOffset, ttt * Time.deltaTime);
                }
            }
        }
    }

    public void LerpOffset(Vector3 newPos,float timeToTake = 1.5f)
    {
        if(lerpOffset != newPos)
        {
            lerpOffset = newPos;
            ttt = timeToTake;
        }
    }
}
