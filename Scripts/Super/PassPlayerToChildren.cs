using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGrab))]
public class PassPlayerToChildren : MonoBehaviour
{
    PlayerInfo player;
    PlayerInfo oldPlayer;

    // Update is called once per frame
    void Update()
    {
        PlayerGrab pG = GetComponent<PlayerGrab>();

        if(pG != null)
        {
            player = pG.player;

            if (player != oldPlayer)
            {
                if(transform.childCount > 0)
                {
                    for(int i = 0; i < transform.childCount; i++)
                    {
                        PlayerGrab cPG = transform.GetChild(i).gameObject.GetComponent<PlayerGrab>();

                        if (cPG != null)
                        {
                            cPG.player = player;
                        }
                    }
                }

                oldPlayer = player;
            }
        }
    }
}
