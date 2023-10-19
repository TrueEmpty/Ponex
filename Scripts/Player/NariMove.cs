using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody),typeof(PlayerGrab))]
public class NariMove : MonoBehaviour
{
    Rigidbody rb;
    ButtonManager bm;
    Database db;
    PlayerGrab pg;
    Vector3 moveDir = Vector3.up;
    float hheadCur = 1.3f;
    public List<string> hitTags = new List<string>();
    bool startup = false;
    float trueSpeed = 1;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        bm = ButtonManager.instance;
        db = Database.instance;

        rb.useGravity = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (db.gameStart && pg.player.currentHealth > 0)
        {
            if (startup)
            {
                if (pg.player.CanMove)
                {
                    OnMove();
                }
            }
            else
            {
                switch (pg.player.facing)
                {
                    case Facing.Right:
                        moveDir = Vector3.right;
                        break;
                    case Facing.Left:
                        moveDir = Vector3.left;
                        break;
                    case Facing.Up:
                        moveDir = Vector3.up;
                        break;
                    case Facing.Down:
                        moveDir = Vector3.down;
                        break;
                }

                startup = true;
            }
        }
    }

    void OnMove()
    {
        if (bm.KeyDown(pg.player.buttons.right))
        {
            if (!WallInDirection(Vector3.right))
            {
                transform.rotation = Quaternion.Euler(0, 0, 90);
                moveDir = Vector3.right;
            }
        }
        else if (bm.KeyDown(pg.player.buttons.left))
        {
            if (!WallInDirection(Vector3.left))
            {
                transform.rotation = Quaternion.Euler(0, 0, 270);
                moveDir = Vector3.left;
            }
        }
        else if (bm.KeyDown(pg.player.buttons.up))
        {
            if (!WallInDirection(Vector3.up))
            {
                transform.rotation = Quaternion.Euler(0, 0, 180);
                moveDir = Vector3.up;
            }
        }
        else if (bm.KeyDown(pg.player.buttons.down))
        {
            if(!WallInDirection(Vector3.down))
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
                moveDir = Vector3.down;
            }    
        }

        if(trueSpeed > pg.player.movementSpeed)
        {
            trueSpeed = pg.player.movementSpeed;
        }
        else if(trueSpeed < pg.player.movementSpeed)
        {
            trueSpeed += Time.deltaTime;
        }

        rb.velocity = trueSpeed * moveDir;

        //Check if Wall is in dir if so bounce back
        if(WallInDirection(moveDir))
        {
            rb.velocity *= -1;
            moveDir *= -1;
            Vector3 nRot = transform.rotation.eulerAngles;
            nRot += new Vector3(0, 0, 180);
            transform.rotation = Quaternion.Euler(nRot);
        }
    }

    bool WallInDirection(Vector3 mD)
    {
        bool result = false;

        RaycastHit[] hits = Physics.RaycastAll(transform.position, mD, ((hheadCur/2)+.2f));

        if(hits.Length > 0)
        {
            for(int i = 0; i < hits.Length; i++)
            { 
                if(hitTags.Exists(x=> x.ToLower().Trim() == hits[i].transform.tag.ToLower().Trim()))
                {
                    result = true;
                    break;
                }
            }
        }

        return result;
    }
}
