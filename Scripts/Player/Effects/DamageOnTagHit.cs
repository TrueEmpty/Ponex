using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageOnTagHit : MonoBehaviour
{
    PlayerGrab pG;

    public string tagHit = "Ball";
    public int damageIncrease = 0;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.tag.ToLower().Trim() == tagHit.ToLower().Trim())
        {
            //Check if you own the object
            PlayerGrab tpG = collision.gameObject.GetComponent<PlayerGrab>();
            BallInfo tbI = collision.gameObject.GetComponent<BallInfo>();
            bool pass = true;

            if(tpG != null)
            {
                if(tpG.player != null)
                {
                    if(tpG.player == pG.player)
                    {
                        pass = false;
                    }
                }
            }

            if (pG.player != null && pass)
            {
                int baseDamage = 0;

                if(tbI != null)
                {
                    baseDamage = tbI.ball.damage;
                }

                //Send out Ball hit to all
                pG.player.Damage(baseDamage + damageIncrease);
            }
        }
    }
}
