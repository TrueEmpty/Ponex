using System.Collections.Generic;
using UnityEngine;

public class Trigger : MonoBehaviour
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
    ComputerBrain brain;

    public GameObject bump;
    public float bumpOffsetY = .25f;

    public GameObject super;
    public float superDelay = .125f;
    float superDelayAmount = 0;
    public int superShots = 20;
    int superShotCount = -1;

    PaddleWall.LaneLock lane;

    [SerializeField]
    Thought thought = Thought.Nothing;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;
        brain = ComputerBrain.Ensure(gameObject, ComputerBrain.Mode.LanePaddle);
        if (brain != null)
        {
            brain.hitTags = hitTags;
            brain.wallStopDistance = WallStopDistance();
        }

        rb.useGravity = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (db.gameStart && pg.player.currentHealth > 0)
        {
            if (pg.player.CanMove)
            {
                OnMove(); // evaluate AI first so dash can read the decision
                if (pg.player.CanDash)
                    OnDash();
            }
            else
            {
                rb.linearVelocity = Vector3.zero;
            }

            if (pg.player != null)
                lane.Ensure(rb, pg.player.facing);
            // Dash can overshoot soft wall stop — push out so leave-input always works
            PaddleWall.Unstick(rb, transform, hitTags);
            lane.Clamp(rb);

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

            if(superDelayAmount > 0)
            {
                superDelayAmount -= Time.deltaTime;
            }

            // readyPercent here means "meter full enough to cast" (NOT an active mode).
            // Actual cast still requires tf_super in OnSuper — Enough() alone must never fire.
            Skill superSkill = pg.player.super;
            if (superSkill != null && superShotCount < 0)
            {
                superSkill.readyPercent = superSkill.Enough() ? 1f : 0f;
            }
        }
    }

    void FixedUpdate()
    {
        lane.Clamp(rb);
    }

    void OnMove()
    {
        int moveDir = 0;

        if (pg.player.computer)
        {
            ComputerAI.Decision d = brain != null
                ? brain.Lane
                : ComputerAI.Evaluate(transform, pg.player, hitTags, WallStopDistance());
            moveDir = d.moveDir;
            thought = brain != null
                ? brain.Thought
                : (d.wantBump ? Thought.MoveUp : (d.wantSuper ? Thought.MoveDown : Thought.Nothing));
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
        rb.linearVelocity = rotDir * moveDir * speedIncrease * pg.player.EffectiveMovementSpeed;
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

        if ((pg.inp.bump || thought == Thought.MoveUp) && b.Enough() && pg.player.super.amount >= b.cost)
        {
            GameObject go = Instantiate(bump, transform.position + (transform.up * bumpOffsetY), transform.rotation);

            PlayerGrab bpG = go.GetComponent<PlayerGrab>();

            if (bpG != null)
            {
                bpG.playerIndex = pg.playerIndex;
            }

            b.Spend();
            pg.player.super.Spend(b.cost);
            thought = Thought.Nothing;
        }
    }

    bool SuperReady()
    {
        Skill b = pg.player != null ? pg.player.super : null;
        return b != null && b.Enough() && b.readyPercent >= 1f;
    }

    void OnSuper()
    {
        Skill b = pg.player.super;

        // Humans: only the super input edge. AI: MoveDown thought from ComputerAI.
        bool wantStart = SuperReady() && (
            pg.player.computer
                ? thought == Thought.MoveDown
                : pg.inp.tf_super);

        bool volleyContinue = superShotCount > 0 && superDelayAmount <= 0;

        if (wantStart || volleyContinue)
        {
            GameObject go = Instantiate(super, transform.position + (transform.up * bumpOffsetY), transform.rotation);

            PlayerGrab spG = go.GetComponent<PlayerGrab>();

            if (spG != null)
            {
                spG.playerIndex = pg.playerIndex;
            }

            if (superShotCount < 0)
            {
                b.Spend();
                b.readyPercent = 0f;
                thought = Thought.Nothing;
                superShotCount = 0;
                pg.player.RecordUltUsed();
            }

            superShotCount++;
            superDelayAmount = superDelay;

            if (superShotCount >= superShots)
            {
                superDelayAmount = 0;
                superShotCount = -1;
            }
        }
    }

    bool WallInDirection(int dir)
    {
        return PaddleWall.WallInDirection(transform, dir, hitTags, WallStopDistance());
    }

    float WallStopDistance()
    {
        return PaddleWall.LateralStopDistance(transform);
    }
}

