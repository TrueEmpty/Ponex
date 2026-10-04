using System.Collections;
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

    public GameObject bump;
    public float bumpOffsetY = .25f;

    public GameObject super;
    public float superDelay = .25f;
    float superDelayAmount = 0;
    public int superShots = 10;
    int superShotCount = -1;

    #region AI
    bool thinking = false;
    public float thinkTime = .5f;

    [SerializeField]
    Thought thought = Thought.Nothing;

    //Starting Chance Weight
    public float chanceToDoNothing = 50; //Do Nothing
    public float chanceToMove = 100; //Move Left
    public float chanceToBump = 0; //Move Bump
    public float chanceToSuper = 0; //Move Super

    enum Thought
    {
        Nothing,
        MoveLeft,
        MoveRight,
        MoveUp,
        MoveDown
    }
    #endregion

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
            if (pg.player.computer)
            {
                thinking = false;
            }

            if (pg.player.CanMove)
            {
                OnMove(); // evaluate AI first so dash can read the decision
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

    IEnumerator AI()
    {
        //Inital Info
        Facing f = pg.player.facing;

        //Decisions
        float dNull = chanceToDoNothing; //Do Nothing
        float dML = chanceToMove; //Move Left
        float dMR = chanceToMove; //Move Right
        float dMB = chanceToBump; //Move Bump
        float dMS = chanceToSuper; //Move Super

        //Get Ball Hit locations that Will are set to hit the wall behind him
        GameObject[] allBalls = GameObject.FindGameObjectsWithTag("Ball");
        List<Vector3> importantCollisions = new List<Vector3>();
        Vector3 curPos = transform.position;

        if (allBalls.Length > 0)
        {
            for (int i = 0; i < allBalls.Length; i++)
            {
                BallInfo bI = allBalls[i].GetComponent<BallInfo>();

                if (bI != null)
                {
                    int fcpc = bI.futureColisionPoints.Count;

                    if (fcpc > 0)
                    {
                        for (int c = 0; c < fcpc; c++)
                        {
                            Vector3 cp = bI.futureColisionPoints[c];

                            switch (f)
                            {
                                case Facing.Up:
                                    if (cp.y <= curPos.y)
                                    {
                                        importantCollisions.Add(cp);
                                    }
                                    break;
                                case Facing.Down:
                                    if (cp.y >= curPos.y)
                                    {
                                        importantCollisions.Add(cp);
                                    }
                                    break;
                                case Facing.Left:
                                    if (cp.x >= curPos.x)
                                    {
                                        importantCollisions.Add(cp);
                                    }
                                    break;
                                case Facing.Right:
                                    if (cp.x <= curPos.x)
                                    {
                                        importantCollisions.Add(cp);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        //Go through important collisions and use that to help determine the next action
        if (importantCollisions.Count > 0)
        {
            float iCo = importantCollisions.Count;

            for (int c = 0; c < iCo; c++)
            {
                Vector3 v3 = importantCollisions[c];
                float disRight = 0;

                switch (f)
                {
                    case Facing.Up:
                        disRight = v3.x - curPos.x;
                        break;
                    case Facing.Down:
                        disRight = curPos.x - v3.x;
                        break;
                    case Facing.Left:
                        disRight = v3.y - curPos.y;
                        break;
                    case Facing.Right:
                        disRight = curPos.y - v3.y;
                        break;
                }

                if (Mathf.Abs(disRight) <= 1) //In line with Collision
                {
                    disRight = 0;
                    dNull += 10;

                    if (pg.player.bump.Enough())
                    {
                        dMB += 5 / iCo;
                    }

                    if (pg.player.super.Enough())
                    {
                        dMS += 5 / iCo;
                    }
                }

                dMR += disRight;
                dML -= disRight;

                if (Mathf.Abs(disRight) >= 3) //At least 2 lengths away
                {
                    if (pg.player.super.Enough())
                    {
                        dMS += 10 / iCo;
                    }
                }
            }
        }

        //Choose an action
        if (dNull < 0)
        {
            dNull = 0;
        }

        if (dMR < 0)
        {
            dMR = 0;
        }

        if (dML < 0)
        {
            dML = 0;
        }

        if (dMB < 0)
        {
            dMB = 0;
        }

        if (dMS < 0)
        {
            dMS = 0;
        }

        float rNull = dNull;
        float rMR = dNull + dMR;
        float rML = rMR + dML;
        float rMB = rML + dMB;
        float rMS = rMB + dMS;

        float rN = Random.Range(0f, rMS);

        if (rN <= rNull)
        {
            thought = Thought.Nothing;
        }
        else if (rN <= rMR)
        {
            thought = Thought.MoveRight;
        }
        else if (rN <= rML)
        {
            thought = Thought.MoveLeft;
        }
        else if (rN <= rMB)
        {
            thought = Thought.MoveUp;
        }
        else if (rN <= rMS)
        {
            thought = Thought.MoveDown;
        }

        yield return null;

        //Wait until thinking again
        yield return new WaitForSeconds(thinkTime);
        thinking = false;
        yield return null;
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
        return PaddleWall.WallInDirection(transform, dir, hitTags, dis);
    }
}
