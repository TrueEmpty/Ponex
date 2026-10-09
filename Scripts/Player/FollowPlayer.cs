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

    [Tooltip("Bahrue body stacks. Slides local position to LerpOffset. Leave off for Tic.")]
    public bool useStackLerp = false;

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
        if (playerTransform == null)
            return;

        if (useStackLerp)
        {
            ApplyStackLerp();
            return;
        }

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

    void ApplyStackLerp()
    {
        Vector3 goal = offset;
        if ((lerpOffset - offset).sqrMagnitude > 0.0001f)
        {
            goal = Vector3.MoveTowards(transform.localPosition, lerpOffset, ttt * Time.deltaTime);
            if ((goal - lerpOffset).sqrMagnitude <= 0.0001f)
                offset = lerpOffset;
        }

        transform.localPosition = goal;
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
