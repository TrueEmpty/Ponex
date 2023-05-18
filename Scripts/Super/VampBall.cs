using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGrab))]
public class VampBall : MonoBehaviour
{
    PlayerGrab pG;
    public GameObject vampEffect;
    string tagHit = "Ball";

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.tag.ToLower().Trim() == tagHit.ToLower().Trim())
        {
            if(vampEffect != null)
            {
                GameObject go = Instantiate(vampEffect, collision.transform);

                if(go != null)
                {
                    go.transform.localPosition = Vector3.zero;
                    PlayerGrab vPG = go.GetComponent<PlayerGrab>();

                    if(vPG != null)
                    {
                        vPG.player = pG.player;
                    }
                }
            }
        }
    }
}
