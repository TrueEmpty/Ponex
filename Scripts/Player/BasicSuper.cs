using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicSuper : MonoBehaviour
{
    Player player;
    Rigidbody rb;

    public float superCooldown = 3;
    float timeTillReset = 0;

    public GameObject super;
    public Vector3 spawnOffset = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<Player>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
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

        player.player.superReadyPercent = sP;

        if (player.keys.KeyPressedDown(Direction.Super) && player.player.superAmount >= player.player.superCost && timeTillReset >= superCooldown)
        {
            if(super != null)
            {
                Vector3 spawnPos = transform.position;
                spawnPos += transform.up * spawnOffset.y;
                spawnPos += transform.right * spawnOffset.x;
                spawnPos += transform.forward * spawnOffset.z;

                GameObject go = Instantiate(super, spawnPos, transform.rotation) as GameObject;

                PlayerGrab pg = go.GetComponent<PlayerGrab>();

                if (pg != null)
                {
                    pg.player = player;
                }

                timeTillReset = 0;

                player.CostSuper(player.player.superCost);
            }
        }

        timeTillReset += Time.deltaTime;
    }
}
