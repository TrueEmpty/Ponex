using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGrab))]
public class PassPlayerGrabToChildren : MonoBehaviour
{
    PlayerGrab pg;

    // Start is called before the first frame update
    void Start()
    {
        pg = GetComponent<PlayerGrab>();
    }


    // Update is called once per frame
    void Update()
    {
        if(transform.childCount > 0)
        {
            for(int i = 0; i < transform.childCount; i++)
            {
                PlayerGrab cPG = transform.GetChild(i).gameObject.GetComponent<PlayerGrab>();

                if (cPG != null)
                {
                    cPG.playerIndex = pg.playerIndex;
                }
            }
        }            
    }
}
