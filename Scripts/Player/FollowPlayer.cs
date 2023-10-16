using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    PlayerGrab pG;
    public Transform playerTransform;
    public Vector3 offset = Vector3.zero;
    Vector3 lerpOffset = Vector3.zero;
    float ttt = 1.5f;

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
            transform.localPosition = offset;

            if(offset != lerpOffset)
            {
                offset = Vector3.MoveTowards(offset, lerpOffset, ttt * Time.deltaTime);
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
