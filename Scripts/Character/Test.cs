using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    Rigidbody rb;
    Database db;
    PlayerGrab pg;

    public List<string> hitTags = new List<string>();

    public float dis = 1.38f;

    int canDash = 0;
    float dashEnd = 0;
    float speedIncrease = 1;
    public float speedMultiplyer = 4;
    float doubleClickTime = .3f;
    public float dashTime = .3f;

    public GameObject bump;
    public GameObject super;
    public GameObject superEffect;

    [SerializeField]
    Thought thought = Thought.Nothing;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;

        rb.useGravity = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (db.gameStart && pg.player.currentHealth > 0)
        {
            if (pg.player.CanMove)
            {
                OnMove();
                OnDash();
            }
            else
            {
                rb.linearVelocity = Vector3.zero;
            }

            // Dash can overshoot soft wall stop — push out so leave-input always works
            PaddleWall.Unstick(rb, transform, hitTags);

            if (pg.player.CanBump)
            {
                OnBump();
            }

            if (pg.player.CanSuper)
            {
                OnSuper();
            }

            //Dash
            if (dashEnd > 0)
            {
                dashEnd -= Time.deltaTime;
            }
            else
            {
                canDash = 0;
                speedIncrease = 1;
            }

           pg.player.dash.Charge();
           pg.player.bump.Charge();
        }
    }

    void OnMove()
    {
        int moveDir = 0;

        if (pg.player.computer)
        {
            ComputerAI.Decision d = ComputerAI.Evaluate(transform, pg.player, hitTags, dis);
            moveDir = d.moveDir;
            if (d.wantBump) thought = Thought.MoveUp;
            else if (d.wantSuper) thought = Thought.MoveDown;
            else thought = Thought.Nothing;
        }
        else
        {
            thought = Thought.Nothing;
            if (pg.inp.right)
                moveDir += 1;
            if (pg.inp.left)
                moveDir -= 1;
        }

        // Block only into-wall travel; moving away from a wall is always allowed
        if (moveDir != 0 && WallInDirection(moveDir))
            moveDir = 0;

        Vector3 rotDir = PaddleWall.AbsAxes(transform.right);
        rb.linearVelocity = rotDir * moveDir * speedIncrease * pg.player.movementSpeed;
    }

    void OnDash()
    {
        Skill d = pg.player.dash;
        if (d == null)
            return;

        if (pg.player.computer)
        {
            ComputerAI.Decision ai = ComputerAI.GetLastDecision(pg.playerIndex);
            if (ai.wantDash && ai.moveDir != 0 && d.amount >= d.cost && Mathf.Abs(canDash) != 2)
            {
                StartDash(ai.moveDir > 0 ? 1 : -1, d);
            }
            return;
        }

        // Bumper / Q-R: instant dash (no double-tap)
        if (pg.inp.tf_dashRight && d.amount >= d.cost && Mathf.Abs(canDash) != 2)
        {
            StartDash(1, d);
            return;
        }
        if (pg.inp.tf_dashLeft && d.amount >= d.cost && Mathf.Abs(canDash) != 2)
        {
            StartDash(-1, d);
            return;
        }

        // Stick / D-pad: double-tap still works
        if (pg.inp.tf_right && d.amount >= d.cost)
        {
            if (Mathf.Abs(canDash) == 2) { }
            else if (canDash == 1 && dashEnd > 0)
                StartDash(1, d);
            else
            {
                canDash = 1;
                dashEnd = doubleClickTime;
            }
        }

        if (pg.inp.tf_left && d.amount >= d.cost)
        {
            if (Mathf.Abs(canDash) == 2) { }
            else if (canDash == -1 && dashEnd > 0)
                StartDash(-1, d);
            else
            {
                canDash = -1;
                dashEnd = doubleClickTime;
            }
        }
    }

    void StartDash(int dir, Skill d)
    {
        speedIncrease = speedMultiplyer;
        dashEnd = dashTime;
        d.readyPercent = 0;
        d.Spend();
        canDash = dir > 0 ? 2 : -2;
        pg.player.RecordDash();
    }

    void OnBump()
    {
        Skill b = pg.player.bump;

        if ((pg.inp.bump || thought == Thought.MoveUp) && b.Enough())
        {
            GameObject go = Instantiate(bump, transform.position, transform.rotation);
            go.transform.parent = transform;

            PlayerGrab bpG = go.GetComponent<PlayerGrab>();

            if(bpG != null)
            {
                bpG.playerIndex = pg.playerIndex;
            }

            b.Spend();
            thought = Thought.Nothing;
        }
    }

    void OnSuper()
    {
        Skill b = pg.player.super;
        if (b == null)
            return;

        // Press edge only — held inp.super was re-casting every frame while ready
        bool wantSuper = pg.player.computer
            ? thought == Thought.MoveDown
            : pg.inp.tf_super;

        if (wantSuper && b.Enough())
        {
            GameObject go = Instantiate(super, transform.position, transform.rotation);

            PlayerGrab spG = go.GetComponent<PlayerGrab>();

            if(spG != null)
            {
                spG.playerIndex = pg.playerIndex;
            }

            b.Spend();
            pg.player.RecordUltUsed();

            GameObject goE = Instantiate(superEffect, transform.position, transform.rotation);
            goE.transform.parent = transform;
            thought = Thought.Nothing;
        }
    }

    bool WallInDirection(int dir)
    {
        return PaddleWall.WallInDirection(transform, dir, hitTags, dis);
    }
}
