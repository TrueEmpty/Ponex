using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarmenAimFire : MonoBehaviour
{
    PlayerGrab pg;
    Database db;
    Garmen g;

    public Transform hub;
    public float speed = 1;
    public float rotAmount = 0;

    // Start is called before the first frame update
    void Start()
    {
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if(db.gameStart)
        {
            Player p = db.players[pg.playerIndex];

            if(pg.player.currentHealth > 0 && pg.player.super.readyPercent == 0)
            {
                if (g == null)
                {
                    g = p.spawnedPlayer.GetComponent<Garmen>();
                }

                if (pg.player.CanMove)
                {
                    int rotDir = 0;

                    if ((pg.inp.right || g.thought == Thought.MoveRight) && rotAmount > -85)
                    {
                        rotDir -= 1;
                    }

                    if ((pg.inp.left || g.thought == Thought.MoveLeft) && rotAmount < 85)
                    {
                        rotDir += 1;
                    }

                    hub.Rotate(Vector3.forward * rotDir * Time.deltaTime * speed, Space.Self);
                    rotAmount += Time.deltaTime * speed * rotDir;
                }
            }            
        }
    }
}
