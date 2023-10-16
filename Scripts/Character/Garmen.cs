using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Garmen : MonoBehaviour
{
    PlayerGrab pg;
    ButtonManager bm;
    Database db;

    GameObject ll;
    FollowPlayer fp;

    public Transform shootPoint;
    public GameObject projectile;
    float gbC = 0;

    public float cannonOffset = .07f;

    #region AI
    bool thinking = false;

    bool up = false;
    bool down = false;
    bool left = false;
    bool right = false;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        pg = GetComponent<PlayerGrab>();
        bm = ButtonManager.instance;
        db = Database.instance;
        fp = GetComponent<FollowPlayer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (ll != null)
        {
            Player p = pg.player;

            //Position Cannon
            transform.position = ll.transform.position + (ll.transform.up * cannonOffset);

            if (db.gameStart && p.currentHealth > 0)
            {
                if (bm.KeyDown(p.buttons.Down(p.facing)))
                {
                    p.super.readyPercent = p.super.readyPercent == 0 ? 1 : 0;
                }


                if (pg.player.CanBump)
                {
                    if (bm.KeyDown(p.buttons.Up(p.facing)) && p.bump.amount >= p.bump.cost && gbC >= .25f)
                    {
                        GameObject pro = Instantiate(projectile, shootPoint.position, shootPoint.rotation);
                        
                        PlayerGrab pPg = pro.GetComponent<PlayerGrab>();
                        
                        if(pPg != null)
                        {
                            pPg.playerIndex = pg.playerIndex;
                        }

                        p.bump.Spend();
                        gbC = 0;
                    }
                }

                gbC += Time.deltaTime;

                if(p.bump.amount < p.bump.max)
                {
                    p.bump.readyPercent += Time.deltaTime/3;

                    if(p.bump.readyPercent >= 1)
                    {
                        p.bump.Gain(1);
                        p.bump.readyPercent = 0;
                    }
                }
            }
        }
        else
        {
            ll = pg.player.spawnedLifeline;
        }
    }
}
