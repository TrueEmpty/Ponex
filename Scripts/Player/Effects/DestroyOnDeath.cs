using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnDeath : MonoBehaviour
{
    PlayerGrab pG;
    public bool hideInstead = false;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();    
    }

    // Update is called once per frame
    void Update()
    {
        if(pG != null)
        {
            if(pG.player != null)
            {
                if(!pG.player.player.selection)
                {
                    if (pG.player.player.player.currentHealth <= 0)
                    {
                        if (hideInstead)
                        {
                            gameObject.SetActive(false);
                        }
                        else
                        {
                            Destroy(gameObject);
                        }
                    }
                }
            }
        }
    }
}
