using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallMovement : MonoBehaviour
{
    Rigidbody rb;
    BallInfo bI;
    Database db;

    bool moveReady = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        bI = GetComponent<BallInfo>();
        db = Database.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if(bI.ballReady && db.gameStart)
        {
            if(moveReady)
            {
                DeadZoneCheck();

                if(bI.speedCap)
                {
                    MinCheck();
                    MaxCheck();
                }
            }
            else
            {
                BeginMovement();
            }
        }        
    }

    public void BeginMovement()
    {
        Vector3 dir = new Vector3(Random.Range(-bI.ball.startSpeed * 2, bI.ball.startSpeed * 2), Random.Range(-bI.ball.startSpeed * 2, bI.ball.startSpeed * 2), 0);
        rb.linearVelocity = dir;

        moveReady = true;
    }

    void DeadZoneCheck()
    {
        float halfStartSpeed = (bI.ball.startSpeed / 3);

        if (Mathf.Abs(rb.linearVelocity.x) < halfStartSpeed && Mathf.Abs(rb.linearVelocity.y) < halfStartSpeed)
        {
            rb.linearVelocity *= 2 * Time.deltaTime;
        }
    }

    void MinCheck()
    {
        Vector3 newVelocity = rb.linearVelocity;

        if (rb.linearVelocity.x < -bI.ball.maxSpeed)
        {
            newVelocity.x = -bI.ball.maxSpeed;
        }

        if (rb.linearVelocity.y < -bI.ball.maxSpeed)
        {
            newVelocity.y = -bI.ball.maxSpeed;
        }

        rb.linearVelocity = newVelocity;
    }

    void MaxCheck()
    {
        Vector3 newVelocity = rb.linearVelocity;

        if(rb.linearVelocity.x > bI.ball.maxSpeed)
        {
            newVelocity.x = bI.ball.maxSpeed;
        }

        if(rb.linearVelocity.y > bI.ball.maxSpeed)
        {
            newVelocity.y = bI.ball.maxSpeed;
        }

        rb.linearVelocity = newVelocity;
    }

    private void OnCollisionExit(Collision collision)
    {
        PlayerGrab pG = collision.gameObject.GetComponent<PlayerGrab>();

        switch (collision.transform.tag)
        {
            case "Player":
                rb.linearVelocity *= (bI.ball.speedIncrease * 1.2f);

                if (pG != null)
                {
                    if (pG.IsLinked())
                    {
                        Player p = db.players[pG.playerIndex];
                        p.ballHits++;
                    }
                }
                break;
            case "Lifeline":
                rb.linearVelocity *= (bI.ball.speedIncrease * 2);

                if (pG != null)
                {
                    if (pG.IsLinked())
                    {
                        Player p = db.players[pG.playerIndex];
                        p.ballHits++;                        
                    }
                }
                break;
            case "Ball":
                break;
            case "Paddle":
                if(pG != null)
                {
                    rb.linearVelocity *= (pG.player.pushBack / 100) * 3;

                    if(pG.IsLinked())
                    {
                        Player p = db.players[pG.playerIndex];
                        p.ballHits++;
                    }
                }
                break;
            case "Wall":
                rb.linearVelocity *= (bI.ball.speedIncrease*1.25f);
                break;
        }

        if (bI.documentColisions)
        {
            bI.futureColisions.Add(collision);
            bI.futureColisionPoints.Add(collision.collider.transform.position);
        }
        else
        {
            if (transform.childCount > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    transform.GetChild(i).SendMessage("Ball_Hit", collision.gameObject, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }
}
