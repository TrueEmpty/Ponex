using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger : MonoBehaviour
{
    Rigidbody rb;
    ButtonManager bm;
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
        bm = ButtonManager.instance;
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
                if (!thinking)
                {
                    thinking = true;
                    StartCoroutine(AI());
                }
            }

            if (pg.player.CanMove)
            {
                OnDash();
                OnMove();
            }
            else
            {
                rb.velocity = Vector3.zero;
            }

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

        if ((bm.KeyPressed(pg.player.buttons.Right(pg.player.facing)) || thought == Thought.MoveRight) && !WallInDirection(1))
        {
            moveDir += 1;
        }

        if ((bm.KeyPressed(pg.player.buttons.Left(pg.player.facing)) || thought == Thought.MoveLeft) && !WallInDirection(-1))
        {
            moveDir -= 1;
        }

        Vector3 rotDir = transform.right;
        rotDir.x = Mathf.Abs(rotDir.x);
        rotDir.y = Mathf.Abs(rotDir.y);
        rotDir.z = Mathf.Abs(rotDir.z);

        rb.velocity = rotDir * moveDir * speedIncrease * pg.player.movementSpeed;
    }

    void OnDash()
    {
        Skill d = pg.player.dash;

        if ((bm.KeyDown(pg.player.buttons.Right(pg.player.facing))) && d.amount >= d.cost)
        {
            if (Mathf.Abs(canDash) == 2)
            {

            }
            else if (canDash == 1 && dashEnd > 0)
            {
                speedIncrease = speedMultiplyer;
                dashEnd = dashTime;
                d.readyPercent = 0;
                d.Spend();
                canDash = 2;
            }
            else
            {
                canDash = 1;
                dashEnd = doubleClickTime;
            }
        }

        if ((bm.KeyDown(pg.player.buttons.Left(pg.player.facing))) && d.amount >= d.cost)
        {
            if (Mathf.Abs(canDash) == 2)
            {

            }
            else if (canDash == -1 && dashEnd > 0)
            {
                speedIncrease = speedMultiplyer;
                dashEnd = dashTime;
                d.Spend();
                canDash = -2;
            }
            else
            {
                canDash = -1;
                dashEnd = doubleClickTime;
            }
        }
    }

    void OnBump()
    {
        Skill b = pg.player.bump;

        if ((bm.KeyDown(pg.player.buttons.Up(pg.player.facing)) || thought == Thought.MoveUp) && b.Enough() && pg.player.super.amount >= b.cost)
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

    void OnSuper()
    {
        Skill b = pg.player.super;

        if (((bm.KeyDown(pg.player.buttons.Down(pg.player.facing)) || thought == Thought.MoveDown) && b.Enough()) || (superShotCount > 0 && superDelayAmount <= 0))
        {
            GameObject go = Instantiate(super, transform.position + (transform.up * bumpOffsetY), transform.rotation);

            PlayerGrab spG = go.GetComponent<PlayerGrab>();

            if (spG != null)
            {
                spG.playerIndex = pg.playerIndex;
            }

            if(superShotCount < 0)
            {
                b.Spend();
                thought = Thought.Nothing;
                superShotCount = 0;
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
        bool result = false;

        RaycastHit[] hits = Physics.RaycastAll(transform.position, dir * transform.right, (dis));

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
