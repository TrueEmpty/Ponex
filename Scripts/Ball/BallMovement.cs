using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallMovement : MonoBehaviour
{
    Rigidbody rb;
    BallInfo bI;

    bool moveReady = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        bI = GetComponent<BallInfo>();
    }

    public void BeginMovement()
    {
        Vector3 tor = new Vector3(0, 0, Random.Range(-bI.ball.startSpeed * 10, bI.ball.startSpeed * 10) * bI.ball.speedIncrease);
        rb.AddTorque(tor);
        Vector3 dir = new Vector3(Random.Range(-bI.ball.startSpeed, bI.ball.startSpeed) * (bI.ball.speedIncrease + 1), Random.Range(-bI.ball.startSpeed, bI.ball.startSpeed) * (bI.ball.speedIncrease + 1), 0);
        rb.AddRelativeForce(dir);

        moveReady = true;
    }

    // Update is called once per frame
    void Update()
    {
        if(bI.ballReady)
        {
            if(moveReady)
            {
                DeadZoneCheck();
                MinCheck();
                MaxCheck();
            }
            else
            {
                BeginMovement();
            }
        }        
    }

    void DeadZoneCheck()
    {
        float halfStartSpeed = (bI.ball.startSpeed / 2);

        if (Mathf.Abs(rb.velocity.x) < halfStartSpeed && Mathf.Abs(rb.velocity.y) < halfStartSpeed)
        {
            rb.velocity *= 2;
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

    private void OnCollisionEnter(Collision collision)
    {
        //Vector3 initalSpeed = rb.velocity;
        switch (collision.transform.tag)
        {
            case "Player":
                rb.velocity *= (bI.ball.speedIncrease * 1.2f);
                break;
            case "Lifeline":
                rb.velocity *= (bI.ball.speedIncrease / 2);
                break;
            case "Ball":
                break;
            case "Paddle":
                PlayerGrab pG = collision.gameObject.GetComponent<PlayerGrab>();
                if(pG != null)
                {
                    rb.velocity *= (bI.ball.speedIncrease + pG.player.BumpSpeed);
                }
                break;
            case "Wall":
                rb.velocity *= (bI.ball.speedIncrease/1.25f);
                break;
        }

        if(transform.childCount > 0)
        {
            for(int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).SendMessage("Ball_Hit", collision.gameObject,SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
