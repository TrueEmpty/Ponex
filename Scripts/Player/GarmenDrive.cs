using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarmenDrive : MonoBehaviour
{
    PlayerGrab pg;
    ButtonManager bm;
    Database db;
    Rigidbody rb;

    float searchLength = 2.4f;
    public GameObject hub;
    [SerializeField]
    float liftRate = 1.2f;

    public List<string> hitTags = new List<string>();

    // Start is called before the first frame update
    void Start()
    {
        pg = GetComponent<PlayerGrab>();
        bm = ButtonManager.instance;
        db = Database.instance;
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if(db.gameStart && pg.player.currentHealth > 0 && pg.player.super.readyPercent == 1)
        {
            Action();

            //Raise Garmen
            if (hub.transform.localPosition.y < .5f)
            {
                hub.transform.localPosition += Vector3.up * Time.deltaTime * (liftRate * 2);
            }
            else
            {
                hub.transform.localPosition = new Vector3(0,.5f,0);
            }
        }
        else if(db.gameStart && pg.player.currentHealth > 0 && pg.player.super.readyPercent == 0)
        {
            rb.velocity = Vector3.zero;
            //Lower Garmen
            if (hub.transform.localPosition.y > 0)
            {
                hub.transform.localPosition -= Vector3.up * Time.deltaTime * (liftRate * 2);
            }
            else
            {
                hub.transform.localPosition = new Vector3(0, 0, 0);
            }
        }
    }

    void Action()
    {
        if (pg.player.CanMove && pg.player.super.amount >= pg.player.super.cost)
        {
            int moveDir = 0;

            if (bm.KeyPressed(pg.player.buttons.Right(pg.player.facing)) && !WallInDirection(1))
            {
                moveDir += 1;
            }

            if (bm.KeyPressed(pg.player.buttons.Left(pg.player.facing)) && !WallInDirection(-1))
            {
                moveDir -= 1;
            }

            Vector3 rotDir = transform.right;
            rotDir.x = Mathf.Abs(rotDir.x);
            rotDir.y = Mathf.Abs(rotDir.y);
            rotDir.z = Mathf.Abs(rotDir.z);

            rb.velocity = rotDir * moveDir * pg.player.movementSpeed;

            if(moveDir != 0)
            {
                pg.player.super.Spend(pg.player.super.cost * Time.deltaTime);
            }
        }
        else
        {
            rb.velocity = Vector3.zero;
        }
    }

    bool WallInDirection(int dir)
    {
        bool result = false;

        RaycastHit[] hits = Physics.RaycastAll(transform.position, dir * transform.right, searchLength);

        if (hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                if (hitTags.Exists(x => x.ToLower().Trim() == hits[i].transform.tag.ToLower().Trim()))
                {
                    result = true;
                    break;
                }
            }
        }

        return result;
    }
}
