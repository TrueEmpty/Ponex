using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    PlayerGrab pG;
    Transform playerTransform;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();

        playerTransform = pG.player.gameObject.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if(playerTransform != null)
        {
            transform.position = playerTransform.position;
        }
    }
}
