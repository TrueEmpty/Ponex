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
    bool launcherHold;
    bool holdPosSet;
    Vector3 holdPos;
    Vector3 velocityBeforePhysics;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        bI = GetComponent<BallInfo>();
        db = Database.instance;
    }

    /// <summary>When true, ball stays at zero velocity (Tic plunger bay after lifeline catch).</summary>
    public void SetLauncherHold(bool hold)
    {
        launcherHold = hold;
        if (hold && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public bool IsLauncherHeld => launcherHold;

    bool PreMatchHold => launcherHold || !Database.MatchPlayActive || bI == null || !bI.ballReady;

    void HoldStill()
    {
        if (rb == null)
            return;
        if (!holdPosSet)
        {
            holdPos = rb.position;
            holdPosSet = true;
        }
        if (db != null && db.FieldPlaySize > 0.01f)
            holdPos.z = db.FieldPlaySize;
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        rb.position = holdPos;
        transform.position = holdPos;
        velocityBeforePhysics = Vector3.zero;
    }

    void ReleaseHold()
    {
        if (rb != null && rb.isKinematic)
            rb.isKinematic = false;
    }

    void FixedUpdate()
    {
        if (PreMatchHold)
        {
            HoldStill();
            return;
        }

        ReleaseHold();
        velocityBeforePhysics = rb.linearVelocity;
    }

    void Update()
    {
        if (PreMatchHold)
        {
            HoldStill();
            return;
        }

        ReleaseHold();

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
        ReleaseHold();
        holdPosSet = false;
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

        // Clean planar bounce off walls / field obstacles
        if (tag == "Wall" || tag == "Walls" || tag == "Obstacle")
        {
            ReflectOffContact(collision);
            if (bI != null)
                bI.NoteWallBounce();
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
        if (collision == null || collision.gameObject == null || rb == null || bI == null || bI.ball == null)
            return;

        PlayerGrab pG = collision.gameObject.GetComponent<PlayerGrab>();
        if (pG == null)
            pG = collision.gameObject.GetComponentInParent<PlayerGrab>();
        string tag = collision.transform.tag;
        float speed = rb.linearVelocity.magnitude;

        switch (tag)
        {
            case "Player":
                SetSpeedPreservingDirection(speed * Mathf.Max(1f, bI.ball.speedIncrease * 1.2f));
                RecordHitIfLinked(pG, true, tag, false);
                break;

            case "Lifeline":
                // Mit catch/throw owns speed — don't double-boost stuck balls
                RecordHitIfLinked(pG, false, tag, false);
                break;

            case "Ball":
                break;

            case "Paddle":
                // Yuotay bat already applied directed hit force on Enter — keep that velocity
                if (collision.gameObject.GetComponentInParent<Yuotay>() != null)
                {
                    RecordHitIfLinked(pG, true, tag, false);
                    break;
                }

                // Bumps: raise speed toward 2x max ball speed (never slow the ball)
                {
                    float bumpCap = bI.ball.BumpSpeedCap;
                    float boostFactor = Mathf.Max(1.15f, bI.ball.speedIncrease);
                    float bumpedSpeed = Mathf.Min(Mathf.Max(speed, bI.ball.minSpeed) * boostFactor, bumpCap);
                    // Always move at least a bit closer to the bump cap
                    bumpedSpeed = Mathf.Max(bumpedSpeed, Mathf.MoveTowards(speed, bumpCap, bI.ball.maxSpeed * 0.15f));
                    bumpedSpeed = Mathf.Min(bumpedSpeed, bumpCap);
                    SetSpeedPreservingDirection(bumpedSpeed);

                    RecordHitIfLinked(pG, true, tag, true);
                }
                break;

            case "Wall":
            case "Walls":
            case "Obstacle":
                SetSpeedPreservingDirection(speed * Mathf.Max(1f, bI.ball.speedIncrease * 1.25f));
                break;
        }

        if (bI.documentColisions)
        {
            bI.futureColisions.Add(collision);
            Vector3 hitPoint = transform.position;
            if (collision.collider != null)
                hitPoint = collision.collider.transform.position;
            if (collision.contactCount > 0)
                hitPoint = collision.GetContact(0).point;
            bI.futureColisionPoints.Add(hitPoint);
        }
        else if (transform.childCount > 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).SendMessage("Ball_Hit", collision.gameObject, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    void RecordHitIfLinked(PlayerGrab pG, bool notifyAi, string tag, bool isBump)
    {
        if (pG == null || !pG.IsLinked() || db == null || db.players == null)
            return;
        if (pG.playerIndex < 0 || pG.playerIndex >= db.players.Count)
            return;

        Player p = db.players[pG.playerIndex];
        if (p == null)
            return;

        float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
        Vector3 toPaddle = p.spawnedPlayer != null
            ? p.spawnedPlayer.transform.position - transform.position
            : Vector3.zero;
        toPaddle.z = 0f;
        bool incoming = Vector3.Dot(rb != null ? rb.linearVelocity : Vector3.zero, -MatchStatTicker.IntoField(p.facing)) > 0.1f;
        bool isPaddle = tag == "Player" || tag == "Paddle";
        bool isLifeline = tag == "Lifeline";
        int ballId = gameObject.GetEntityId().GetHashCode();
        p.RecordBallHit(speed, incoming, isBump, isPaddle, isLifeline, ballId, toPaddle.magnitude);
        if (bI != null)
        {
            if (bI.ConsumeVolley())
                p.RecordVolley();
            bI.NotePlayerHit(pG.playerIndex);
        }
        if (notifyAi)
            ComputerAI.OnPaddleHitBall(p);
    }
}
