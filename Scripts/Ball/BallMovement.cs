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
    Vector3 velocityBeforePhysics;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        bI = GetComponent<BallInfo>();
        db = Database.instance;
    }

    void FixedUpdate()
    {
        velocityBeforePhysics = rb.linearVelocity;
    }

    void Update()
    {
        if (bI.ballReady && db.gameStart)
        {
            if (moveReady)
            {
                EnforceSpeedLimits();
            }
            else
            {
                BeginMovement();
            }
        }
    }

    public void BeginMovement()
    {
        float speed = Mathf.Max(bI.ball.startSpeed, bI.ball.minSpeed);
        Vector2 dir = Random.insideUnitCircle;
        if (dir.sqrMagnitude < 0.001f)
            dir = Vector2.right;
        dir.Normalize();

        rb.linearVelocity = new Vector3(dir.x, dir.y, 0f) * speed;
        moveReady = true;
    }

    void EnforceSpeedLimits()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.z = 0f;

        float speed = velocity.magnitude;
        float minSpeed = Mathf.Max(0.01f, bI.ball.minSpeed);
        // Allow bumps to push up to 2x max; everything shares that ceiling
        float maxSpeed = Mathf.Max(minSpeed, bI.ball.BumpSpeedCap);

        if (speed < 0.0001f)
        {
            Vector2 dir = Random.insideUnitCircle;
            if (dir.sqrMagnitude < 0.001f)
                dir = Vector2.right;
            velocity = new Vector3(dir.x, dir.y, 0f) * minSpeed;
        }
        else if (bI.speedCap)
        {
            if (speed < minSpeed)
                velocity = velocity.normalized * minSpeed;
            else if (speed > maxSpeed)
                velocity = velocity.normalized * maxSpeed;
        }

        rb.linearVelocity = velocity;
    }

    void SetSpeedPreservingDirection(float newSpeed)
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.z = 0f;

        float minSpeed = Mathf.Max(0.01f, bI.ball.minSpeed);
        newSpeed = Mathf.Max(newSpeed, minSpeed);

        if (velocity.sqrMagnitude < 0.0001f)
        {
            Vector2 dir = Random.insideUnitCircle;
            if (dir.sqrMagnitude < 0.001f)
                dir = Vector2.right;
            rb.linearVelocity = new Vector3(dir.x, dir.y, 0f) * newSpeed;
            return;
        }

        rb.linearVelocity = velocity.normalized * newSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!moveReady || bI == null || !bI.ballReady)
            return;

        string tag = collision.transform.tag;

        // Clean planar bounce off walls (tag is "Walls" on field prefabs)
        if (tag == "Wall" || tag == "Walls")
        {
            ReflectOffContact(collision);
        }
    }

    void ReflectOffContact(Collision collision)
    {
        if (collision.contactCount <= 0)
            return;

        Vector3 normal = collision.GetContact(0).normal;
        normal.z = 0f;
        if (normal.sqrMagnitude < 0.0001f)
            return;
        normal.Normalize();

        // Use pre-solve velocity so we don't fight Unity's already-reflected result
        Vector3 incoming = velocityBeforePhysics;
        incoming.z = 0f;

        if (incoming.sqrMagnitude < 0.0001f)
            incoming = rb.linearVelocity;

        incoming.z = 0f;
        float speed = incoming.magnitude;

        if (Vector3.Dot(incoming, normal) < 0f)
        {
            Vector3 reflected = Vector3.Reflect(incoming.normalized, normal) * speed;
            reflected.z = 0f;
            rb.linearVelocity = reflected;
            velocityBeforePhysics = reflected;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        PlayerGrab pG = collision.gameObject.GetComponent<PlayerGrab>();
        string tag = collision.transform.tag;
        float speed = rb.linearVelocity.magnitude;

        switch (tag)
        {
            case "Player":
                SetSpeedPreservingDirection(speed * Mathf.Max(1f, bI.ball.speedIncrease * 1.2f));

                if (pG != null && pG.IsLinked())
                {
                    Player p = db.players[pG.playerIndex];
                    p.RecordBallHit();
                    ComputerAI.OnPaddleHitBall(p);
                }
                break;

            case "Lifeline":
                SetSpeedPreservingDirection(speed * Mathf.Max(1f, bI.ball.speedIncrease * 2f));

                if (pG != null && pG.IsLinked())
                {
                    Player p = db.players[pG.playerIndex];
                    p.RecordBallHit();
                }
                break;

            case "Ball":
                break;

            case "Paddle":
                // Bumps: raise speed toward 2x max ball speed (never slow the ball)
                {
                    float bumpCap = bI.ball.BumpSpeedCap;
                    float boostFactor = Mathf.Max(1.15f, bI.ball.speedIncrease);
                    float bumpedSpeed = Mathf.Min(Mathf.Max(speed, bI.ball.minSpeed) * boostFactor, bumpCap);
                    // Always move at least a bit closer to the bump cap
                    bumpedSpeed = Mathf.Max(bumpedSpeed, Mathf.MoveTowards(speed, bumpCap, bI.ball.maxSpeed * 0.15f));
                    bumpedSpeed = Mathf.Min(bumpedSpeed, bumpCap);
                    SetSpeedPreservingDirection(bumpedSpeed);

                    if (pG != null && pG.IsLinked())
                    {
                        Player p = db.players[pG.playerIndex];
                        p.RecordBallHit();
                        ComputerAI.OnPaddleHitBall(p);
                    }
                }
                break;

            case "Wall":
            case "Walls":
                SetSpeedPreservingDirection(speed * Mathf.Max(1f, bI.ball.speedIncrease * 1.25f));
                break;
        }

        if (bI.documentColisions)
        {
            bI.futureColisions.Add(collision);
            Vector3 hitPoint = collision.collider.transform.position;
            if (collision.contactCount > 0)
                hitPoint = collision.GetContact(0).point;
            bI.futureColisionPoints.Add(hitPoint);
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
