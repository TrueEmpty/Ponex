using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicSuper : MonoBehaviour
{
    PlayerInfo pG;
    Rigidbody rb;
    ButtonManager bm;

    public float superCooldown = 3;
    float timeTillReset = 0;

    public GameObject super;
    public Vector3 spawnOffset = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerInfo>();
        rb = GetComponent<Rigidbody>();
        bm = ButtonManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (!pG.player.selection)
        {
            float sP = timeTillReset / superCooldown;

            if (sP > 1)
            {
                sP = 1;
            }
            else if (sP < 0)
            {
                sP = 0;
            }

            pG.player.player.superReadyPercent = sP;

            if (bm.KeyDown(pG.player.keys.super) && pG.player.player.superAmount >= pG.player.player.superCost && timeTillReset >= superCooldown)
            {
                if (super != null)
                {
                    Vector3 spawnPos = transform.position;
                    spawnPos += transform.up * spawnOffset.y;
                    spawnPos += transform.right * spawnOffset.x;
                    spawnPos += transform.forward * spawnOffset.z;

                    GameObject go = Instantiate(super, spawnPos, transform.rotation) as GameObject;

                    PlayerGrab pg = go.GetComponent<PlayerGrab>();

                    if (pg != null)
                    {
                        pg.player = pG;
                    }

                    timeTillReset = 0;

                    pG.CostSuper(pG.player.player.superCost);
                }
            }

            timeTillReset += Time.deltaTime;
        }        
    }
}
