using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Garmen : MonoBehaviour
{
    PlayerGrab pg;
    Database db;

    GameObject ll;
    FollowPlayer fp;

    public Transform shootPoint;
    public GameObject projectile;
    float gbC = 0;

    public float cannonOffset = .07f;

    bool lastStickBack = false;
    bool lastAiWantSuper = false;

    #region AI
    bool thinking = false;
    public float thinkTime = .5f;

    [SerializeField]
    public Thought thought = Thought.Nothing;

    //Starting Chance Weight
    public float chanceToDoNothing = 50; //Do Nothing
    public float chanceToMove = 100; //Move Left
    public float chanceToBump = 0; //Move Bump
    public float chanceToSuper = 0; //Move Super
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;
        fp = GetComponent<FollowPlayer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (ll != null)
        {
            Player p = pg.player;

            if (pg.player.computer)
            {
                thinking = false;
                ComputerAI.Decision d = ComputerAI.Evaluate(transform, pg.player, null, 2.4f);
                if (d.wantBump) thought = Thought.MoveUp;
                else if (d.wantSuper) thought = Thought.MoveDown;
                else if (d.moveDir > 0) thought = Thought.MoveRight;
                else if (d.moveDir < 0) thought = Thought.MoveLeft;
                else thought = Thought.Nothing;
            }

            //Position Cannon
            transform.position = ll.transform.position + (ll.transform.up * cannonOffset);

            if (db.gameStart && p.currentHealth > 0)
            {
                if (pg.player.CanBump)
                {
                    if ((pg.inp.up || thought == Thought.MoveUp) && p.bump.amount >= p.bump.cost && gbC >= .25f)
                    {
                        GameObject pro = Instantiate(projectile, shootPoint.position, shootPoint.rotation);
                        
                        PlayerGrab pPg = pro.GetComponent<PlayerGrab>();
                        
                        if(pPg != null)
                        {
                            pPg.playerIndex = pg.playerIndex;
                        }

                        p.bump.Spend();
                        gbC = 0;
                    }
                }

                gbC += Time.deltaTime;

                if(p.bump.amount < p.bump.max)
                {
                    p.bump.readyPercent += Time.deltaTime/3;

                    if(p.bump.readyPercent >= 1)
                    {
                        p.bump.Gain(1);
                        p.bump.readyPercent = 0;
                    }
                }
            }
        }
        else
        {
            ll = pg.player.spawnedLifeline;
        }
    }

    // LateUpdate: ControllerLink + PlayerGrab have already updated this frame
    void LateUpdate()
    {
        if (ll == null || pg == null || pg.player == null || db == null || !db.gameStart)
            return;

        Player p = pg.player;
        if (p.currentHealth <= 0 || p.super == null)
            return;

        bool pressedSuper = ReadSuperPress(p);
        bool hasGas = p.super.Enough();

        // Exit free on press. Enter = press AND Enough()
        if (pressedSuper && p.super.readyPercent >= 1f)
        {
            p.super.readyPercent = 0f;
            thought = Thought.Nothing;
        }
        else if (pressedSuper && hasGas)
        {
            p.super.readyPercent = 1f;
            thought = Thought.Nothing;
            p.RecordUltUsed();
        }
    }

    bool ReadSuperPress(Player p)
    {
        if (p.computer)
        {
            bool want = thought == Thought.MoveDown;
            bool edge = want && !lastAiWantSuper;
            lastAiWantSuper = want;
            return edge;
        }

        // Prefer ControllerLink edges (valid in LateUpdate even if PlayerGrab ran early)
        ControllerLink cL = p.cLink;
        bool interactEdge = false;
        bool stickBack = false;
        if (cL != null)
        {
            ControllerButtons interact = cL["Interact"];
            interactEdge = interact != null && interact.wasPressedThisFrame;

            bool r = PressedAxis(cL, "Move", 1, 0);
            bool l = PressedAxis(cL, "Move", -1, 0);
            bool u = PressedAxis(cL, "Move", 0, 1);
            bool d = PressedAxis(cL, "Move", 0, -1);

            if (p.ignoreFacing)
                stickBack = d;
            else
            {
                switch (p.facing)
                {
                    case Facing.Down: stickBack = u; break;
                    case Facing.Left: stickBack = l; break;
                    case Facing.Right: stickBack = r; break;
                    default: stickBack = d; break;
                }
            }
        }

        bool stickEdge = stickBack && !lastStickBack;
        lastStickBack = stickBack;

        return interactEdge || stickEdge || pg.inp.tf_super;
    }

    static bool PressedAxis(ControllerLink cL, string action, int xSign, int ySign)
    {
        ControllerButtons b = cL[action];
        if (b == null)
            return false;

        const float deadzone = .5f;
        if (xSign > 0) return b.value.x > deadzone;
        if (xSign < 0) return b.value.x < -deadzone;
        if (ySign > 0) return b.value.y > deadzone;
        if (ySign < 0) return b.value.y < -deadzone;
        return false;
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

                dMR -= disRight;
                dML += disRight;

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
}
