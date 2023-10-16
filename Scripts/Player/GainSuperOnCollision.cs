using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GainSuperOnCollision : MonoBehaviour
{
    PlayerGrab pG;
    public float amount = 1;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.tag.ToLower().Trim() == "ball")
        {
            pG.player.super.Gain(amount);
        }
    }
}
