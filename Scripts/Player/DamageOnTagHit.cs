using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageOnTagHit : MonoBehaviour
{
    PlayerGrab pg;
    Database db;

    public string tagHit = "Ball";
    public int damageIncrease = 0;

    // Start is called before the first frame update
    void Start()
    {
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.tag.ToLower().Trim() == tagHit.ToLower().Trim())
        {
            //Check if you own the object
            PlayerGrab tpG = collision.gameObject.GetComponent<PlayerGrab>();
            BallInfo tbI = collision.gameObject.GetComponent<BallInfo>();
            bool pass = true;

            //Debug.Log("Got Hit By Ball! with your index at: " + pg.playerIndex + " & target at: " + tpG.playerIndex);

            if (tpG != null)
            {
                if(tpG.IsLinked())
                {
                    if(tpG.playerIndex == pg.playerIndex)
                    {
                        pass = false;
                    }
                    else
                    {
                        Player tp = db.players[tpG.playerIndex];
                        Player yp = db.players[pg.playerIndex];

                        if(tp.team == yp.team)
                        {
                            pass = false;
                        }
                    }
                }
            }

            if (pass)
            {
                int baseDamage = 0;

                if(tbI != null)
                {
                    baseDamage = tbI.ball.damage;
                }

                //Send out Ball hit to all
                pg.player.Damage(baseDamage + damageIncrease);
            }
        }
    }
}
