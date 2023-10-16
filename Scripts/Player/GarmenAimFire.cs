using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarmenAimFire : MonoBehaviour
{
    PlayerGrab pg;
    ButtonManager bm;
    Database db;

    public Transform hub;
    public float speed = 1;
    public float rotAmount = 0;

    // Start is called before the first frame update
    void Start()
    {
        pg = GetComponent<PlayerGrab>();
        bm = ButtonManager.instance;
        db = Database.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if(db.gameStart && pg.player.currentHealth > 0 && pg.player.super.readyPercent == 0)
        {
            if(pg.player.CanMove)
            {
                int rotDir = 0;

                if (bm.KeyPressed(pg.player.buttons.Right(pg.player.facing)) && rotAmount > -85)
                {
                    rotDir -= 1;
                }

                if (bm.KeyPressed(pg.player.buttons.Left(pg.player.facing)) && rotAmount < 85)
                {
                    rotDir += 1;
                }

                hub.Rotate(Vector3.forward * rotDir * Time.deltaTime * speed, Space.Self);
                rotAmount += Time.deltaTime * speed * rotDir;
            }
        }
    }
}
