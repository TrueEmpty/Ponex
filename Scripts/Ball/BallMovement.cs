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
        rb.velocity = dir;

        moveReady = true;
    }

    void DeadZoneCheck()
    {
        float halfStartSpeed = (bI.ball.startSpeed / 3);

        if (Mathf.Abs(rb.velocity.x) < halfStartSpeed && Mathf.Abs(rb.velocity.y) < halfStartSpeed)
        {
            rb.velocity *= 2 * Time.deltaTime;
        }
    }

    void MinCheck()
    {
        Vector3 newVelocity = rb.velocity;

        if (rb.velocity.x < -bI.ball.maxSpeed)
        {
            newVelocity.x = -bI.ball.maxSpeed;
        }

        if (rb.velocity.y < -bI.ball.maxSpeed)
        {
            newVelocity.y = -bI.ball.maxSpeed;
        }

        rb.velocity = newVelocity;
    }

    void MaxCheck()
    {
        Vector3 newVelocity = rb.velocity;

        if(rb.velocity.x > bI.ball.maxSpeed)
        {
            newVelocity.x = bI.ball.maxSpeed;
        }

        if(rb.velocity.y > bI.ball.maxSpeed)
        {
            newVelocity.y = bI.ball.maxSpeed;
        }

        rb.velocity = newVelocity;
    }

    private void OnCollisionExit(Collision collision)
    {
        switch (collision.transform.tag)
        {
            case "Player":
                rb.velocity *= (bI.ball.speedIncrease * 1.2f);
                break;
            case "Lifeline":
                rb.velocity *= (bI.ball.speedIncrease * 2);
                break;
            case "Ball":
                break;
            case "Paddle":
                PlayerGrab pG = collision.gameObject.GetComponent<PlayerGrab>();
                if(pG != null)
                {
                    rb.velocity *= (pG.player.pushBack / 100) * 3;
                }
                break;
            case "Wall":
                rb.velocity *= (bI.ball.speedIncrease*1.25f);
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
