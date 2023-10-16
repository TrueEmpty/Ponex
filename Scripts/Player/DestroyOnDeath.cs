using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnDeath : MonoBehaviour
{
    PlayerGrab pg;
    public bool hideInstead = false;

    // Start is called before the first frame update
    void Start()
    {
        pg = GetComponent<PlayerGrab>();    
    }

    // Update is called once per frame
    void Update()
    {
        if(pg != null)
        {
            if(pg.player != null)
            {
                if (pg.player.currentHealth <= 0)
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
