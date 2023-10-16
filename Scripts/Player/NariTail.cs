using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NariTail : MonoBehaviour
{
    public int healthIndex = 1;
    public NariTailUpkeep nTU;
    PlayerGrab pG;
    Rigidbody rb;
    OffsetFollow oF;

    public string tagHit = "Ball";
    float destroy = -1;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
        rb = GetComponent<Rigidbody>();
        oF = GetComponent<OffsetFollow>();
    }

    // Update is called once per frame
    void Update()
    {
        if(pG.player != null && destroy < 0)
        {
            if(pG.player.currentHealth < healthIndex)
            {
                destroy = 0;
                nTU.tails.Remove(gameObject);
            }
        }
        else if (destroy > 1)
        {
            Destroy(gameObject);
        }
        else if(destroy >= 0)
        {
            destroy += Time.deltaTime*2;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (pG.player != null && destroy < 0)
        {
            if (collision.transform.tag.ToLower().Trim() == tagHit.ToLower().Trim())
            {
                //Check if you own the object
                PlayerGrab tpG = collision.gameObject.GetComponent<PlayerGrab>();
                BallInfo tbI = collision.gameObject.GetComponent<BallInfo>();
                bool pass = true;

                if (tpG != null)
                {
                    if (tpG.playerIndex >= 0)
                    {
                        if (tpG.playerIndex == pG.playerIndex)
                        {
                            pass = false;
                        }
                    }
                }

                if (pass)
                {
                    if (pG.player.currentHealth > healthIndex - 1)
                    {
                        pG.player.currentHealth = healthIndex - 1;
                        pG.player.maxHealth = healthIndex - 1;

                        destroy = 0;
                        nTU.tails.Remove(gameObject);
                        oF.parent = null;

                        rb.velocity = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
                    }
                }
            }
        }
    }
}
