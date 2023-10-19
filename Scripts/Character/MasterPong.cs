using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MasterPong : MonoBehaviour
{
    Rigidbody rb;
    ButtonManager bm;
    Database db;
    PlayerGrab pg;

    public List<string> hitTags = new List<string>();

    public float dis = 1.38f;

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
                OnMove();
            }
            else
            {
                rb.velocity = Vector3.zero;
            }
        }
    }

    IEnumerator AI()
    {
        //Inital Info
        Facing f = pg.player.facing;
        string thoughtString = "";

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
                        disRight = v3.x - curPos.x;
                        disRight *= -1;
                        break;
                    case Facing.Left:
                        disRight = v3.y - curPos.y;
                        break;
                    case Facing.Right:
                        disRight = v3.y - curPos.y;
                        disRight *= -1;
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

        thoughtString += "Ball Count: " + allBalls.Length + "/" + importantCollisions.Count + "\n";

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
            thoughtString += "Choice: Do Nothing";
        }
        else if (rN <= rMR)
        {
            thought = Thought.MoveRight;
            thoughtString += "Choice: Move Right";
        }
        else if (rN <= rML)
        {
            thought = Thought.MoveLeft;
            thoughtString += "Choice: Move Left";
        }
        else if (rN <= rMB)
        {
            thought = Thought.MoveUp;
            thoughtString += "Choice: Bump";
        }
        else if (rN <= rMS)
        {
            thought = Thought.MoveDown;
            thoughtString += "Choice: Super";
        }

        thoughtString += "\n";
        thoughtString += "Random Number: " + rN + "\n";
        thoughtString += "Nothing: " + rNull + "\n";
        thoughtString += "Move Right: " + rMR + "\n";
        thoughtString += "Move Left: " + rML + "\n";
        thoughtString += "Use Bump: " + rMB + "\n";
        thoughtString += "Use Super: " + rMS;

        //Debug.Log(thoughtString);
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

        rb.velocity = rotDir * moveDir * pg.player.movementSpeed;
    }

    bool WallInDirection(int dir)
    {
        bool result = false;

        RaycastHit[] hits = Physics.RaycastAll(transform.position,dir * transform.right, (dis));

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
