using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicBump : MonoBehaviour
{
    PlayerGrab pG;
    Rigidbody rb;

    public float bumpCooldown = 2;
    float timeTillReset = 0;

    public GameObject bump;
    public Vector3 spawnOffset = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        float bP = timeTillReset / bumpCooldown;

        if(bP > 1)
        {
            bP = 1;
        }
        else if(bP < 0)
        {
            bP = 0;
        }

        pG.player.player.bumpReadyPercent = bP;

        if (pG.player.keys.KeyPressedDown(Direction.Bump))
        {
            if (timeTillReset >= bumpCooldown && pG.player.CanBump)
            {
                Vector3 spawnPos = transform.position;
                spawnPos += transform.up * spawnOffset.y;
                spawnPos += transform.right * spawnOffset.x;
                spawnPos += transform.forward * spawnOffset.z;

                GameObject go = Instantiate(bump, spawnPos, transform.rotation) as GameObject;

                PlayerGrab pg = go.GetComponent<PlayerGrab>();

                if (pg != null)
                {
                    pg.player = pG.player;
                }

                timeTillReset = 0;
            }
        }

        timeTillReset += Time.deltaTime;
    }
}
