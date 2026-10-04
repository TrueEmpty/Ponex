using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarmenDrive : MonoBehaviour
{
    PlayerGrab pg;
    Database db;
    Rigidbody rb;
    Garmen g;

    float searchLength = 2.4f;
    public GameObject hub;
    [SerializeField]
    float liftRate = 1.2f;

    public List<string> hitTags = new List<string>();

    // Start is called before the first frame update
    void Start()
    {
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if(db.gameStart)
        {
            Player p = db.players[pg.playerIndex];

            if (g == null)
            {
                GameObject sp = p.spawnedPlayer;

                if(sp != null)
                {
                    g = sp.GetComponent<Garmen>();
                }
            }

            if(g != null)
            {
                bool inDrive = p.currentHealth > 0 && p.super.readyPercent >= 1f;

                if (inDrive)
                {
                    Action();

                    //Raise Garmen
                    if (hub.transform.localPosition.y < .5f)
                    {
                        hub.transform.localPosition += Vector3.up * Time.deltaTime * (liftRate * 2);
                    }
                    else
                    {
                        hub.transform.localPosition = new Vector3(0, .5f, 0);
                    }
                }
                else if (p.currentHealth > 0)
                {
                    rb.linearVelocity = Vector3.zero;
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
        }
    }

    void Action()
    {
        if (pg.player.CanMove && pg.player.super.amount >= pg.player.super.cost)
        {
            int moveDir = 0;

            if (pg.player.computer)
            {
                // Thought is refreshed every frame by Garmen from ComputerAI
                if (g.thought == Thought.MoveRight)
                    moveDir += 1;
                else if (g.thought == Thought.MoveLeft)
                    moveDir -= 1;
            }
            else
            {
                if (pg.inp.right)
                    moveDir += 1;
                if (pg.inp.left)
                    moveDir -= 1;
            }

            if (moveDir != 0 && WallInDirection(moveDir))
                moveDir = 0;

            Vector3 rotDir = PaddleWall.AbsAxes(transform.right);
            rb.linearVelocity = rotDir * moveDir * pg.player.movementSpeed;
            PaddleWall.Unstick(rb, transform, hitTags);

            if(moveDir != 0)
            {
                pg.player.super.Spend(pg.player.super.cost * Time.deltaTime);
            }
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    bool WallInDirection(int dir)
    {
        return PaddleWall.WallInDirection(transform, dir, hitTags, searchLength);
    }
}
