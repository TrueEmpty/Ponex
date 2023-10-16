using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitalVelocityMimic : MonoBehaviour
{
    PlayerGrab pG;
    Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
        Rigidbody rb = GetComponent<Rigidbody>();
        Rigidbody pRb = pG.player.spawnedPlayer.GetComponent<Rigidbody>();

        rb.velocity += pRb.velocity;
    }
}
