using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Celarus : MonoBehaviour
{
    Rigidbody rb;
    ButtonManager bm;
    Database db;
    PlayerGrab pg;

    Transform spinPoint;

    public bool day = false;
    public float dayCycle = 20;
    public float spinSpeed = 50;
    [SerializeField]
    float cycle = 0;   

    DamageOnTagHit sunHit;
    PullObjectIn sunGravity;

    DamageOnTagHit moonHit;
    PullObjectIn moonGravity;

    [SerializeField]
    bool moving = false;

    public Vector3 leavePoint = new Vector3(0,-1,-4.5f);
    public float leaveSpeed = 50;

    public Vector3 solarFlareSpeed;
    public GameObject solarFlare;
    public float sunRight = 0;
    public float sunMiddle = 0;
    public float sunLeft = 0;

    public Vector3 sFsideDir = new Vector3(3.5f,3.5f);
    public float sFResetCount = 2;
    public float sFoffset = 4;
    public float sFoffsetAngle = 35;
    public float moonGravityMultiplier = .5f;

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
            if(spinPoint != null && sunHit != null && sunGravity != null && moonHit != null && moonGravity != null)
            {
                if (pg.player.computer)
                {
                    if (!thinking)
                    {
                        thinking = true;
                        StartCoroutine(AI());
                    }
                }

                UpdateDayAndNight();
                PlanetsUpdate();
                CelarusMove();
                SunAttacks();
            }
            else
            {
                GetLifelineObjs();
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

    void CelarusMove()
    {
        if(!moving && !day)
        {
            Player p = pg.player;

            if (p.CanMove)
            {
                int moveDir = 0;

                if(bm.KeyPressed(p.buttons.Left(p.facing)) || thought == Thought.MoveRight)
                {
                    moveDir = -1;
                }

                if(bm.KeyPressed(p.buttons.Right(p.facing)) || thought == Thought.MoveLeft)
                {
                    moveDir = 1;
                }

                Vector3 curVe = rb.velocity;
                switch (p.facing)
                {
                    case Facing.Up:
                        rb.velocity = new Vector3(moveDir * p.movementSpeed * Time.deltaTime, curVe.y, curVe.z);
                        break;
                    case Facing.Down:
                        rb.velocity = new Vector3(-moveDir * p.movementSpeed * Time.deltaTime, curVe.y, curVe.z);
                        break;
                    case Facing.Left:
                        rb.velocity = new Vector3(curVe.x, moveDir * p.movementSpeed * Time.deltaTime, curVe.z);
                        break;
                    case Facing.Right:
                        rb.velocity = new Vector3(curVe.x, -moveDir * p.movementSpeed * Time.deltaTime, curVe.z);
                        break;
                }
            }

            Transform ch = transform.GetChild(0);
            float dA = (transform.position.x - moonGravity.transform.position.x) * (45/4);
            ch.localRotation = Quaternion.Euler(0, 180, dA);

            //Gravity
            Vector3 dif = moonGravity.transform.position - transform.position;
            rb.AddForce(dif * moonGravity.strength * moonGravityMultiplier * Time.deltaTime);
        }
    }

    void SunAttacks()
    {
        Player p = pg.player;

        if (p.CanBump && day)
        {
            Vector3 spPo = sunGravity.transform.position + (spinPoint.parent.transform.up * sFoffset);

            if (sunLeft >= 0)
            {
                //Spawn Ready Bubbles

                //Send Solar Flare
                if (bm.KeyPressed(p.buttons.Left(p.facing)) || thought == Thought.MoveLeft)
                {
                    GameObject ls = Instantiate(solarFlare, spPo + (-spinPoint.parent.transform.right * sFoffset), spinPoint.parent.rotation);

                    Vector3 ro = ls.transform.rotation.eulerAngles;
                    ro.z += sFoffsetAngle;
                    ls.transform.rotation = Quaternion.Euler(ro);

                    PlayerGrab lsPG = ls.GetComponent<PlayerGrab>();

                    if(lsPG != null)
                    {
                        lsPG.playerIndex = pg.playerIndex;
                    }

                    ConstantSpin lsCS = ls.transform.GetChild(0).GetComponent<ConstantSpin>();

                    if (lsCS != null)
                    {
                        lsCS.spinSpeed = Random.Range(solarFlareSpeed.x,solarFlareSpeed.y);
                    }

                    Rigidbody lsRB = ls.GetComponent<Rigidbody>();

                    if(lsRB != null)
                    {
                        lsRB.AddForce(new Vector3(sFsideDir.x, sFsideDir.y, sFsideDir.z) * solarFlareSpeed.z, ForceMode.Force);
                    }

                    /*ResetFlares lsRF = ls.transform.GetChild(0).GetComponent<ResetFlares>();

                    if (lsRF != null)
                    {
                        lsRF.script = this;
                    }*/

                    sunLeft = -sFResetCount;
                }
            }

            if (sunMiddle >= 0)
            {
                //Spawn Ready Bubbles

                //Send Solar Flare
                if (bm.KeyPressed(p.buttons.Up(p.facing)) || thought == Thought.MoveUp)
                {
                    GameObject ls = Instantiate(solarFlare, spPo, spinPoint.parent.rotation);

                    PlayerGrab lsPG = ls.GetComponent<PlayerGrab>();

                    if (lsPG != null)
                    {
                        lsPG.playerIndex = pg.playerIndex;
                    }

                    ConstantSpin lsCS = ls.GetComponent<ConstantSpin>();

                    if (lsCS != null)
                    {
                        lsCS.spinSpeed = Random.Range(solarFlareSpeed.x, solarFlareSpeed.y);
                    }

                    /*Rigidbody lsRB = ls.GetComponent<Rigidbody>();

                    if (lsRB != null)
                    {
                        lsRB.AddForce(new Vector3(sFsideDir.x, sFsideDir.y, sFsideDir.z) * solarFlareSpeed.z, ForceMode.Force);
                    }*/

                    sunMiddle = -sFResetCount;
                }
            }

            if (sunRight >= 0)
            {
                //Spawn Ready Bubbles

                //Send Solar Flare
                if (bm.KeyPressed(p.buttons.Right(p.facing)) || thought == Thought.MoveRight)
                {
                    GameObject ls = Instantiate(solarFlare, spPo + (spinPoint.parent.transform.right * sFoffset), spinPoint.parent.rotation);

                    Vector3 ro = ls.transform.rotation.eulerAngles;
                    ro.z -= sFoffsetAngle;
                    ls.transform.rotation = Quaternion.Euler(ro);

                    PlayerGrab lsPG = ls.GetComponent<PlayerGrab>();

                    if (lsPG != null)
                    {
                        lsPG.playerIndex = pg.playerIndex;
                    }

                    ConstantSpin lsCS = ls.GetComponent<ConstantSpin>();

                    if (lsCS != null)
                    {
                        lsCS.spinSpeed = Random.Range(solarFlareSpeed.x, solarFlareSpeed.y);
                    }

                    /*Rigidbody lsRB = ls.GetComponent<Rigidbody>();

                    if (lsRB != null)
                    {
                        lsRB.AddForce(new Vector3(sFsideDir.x, sFsideDir.y, sFsideDir.z) * solarFlareSpeed.z,ForceMode.Force);
                    }*/

                    sunRight = -sFResetCount;
                }
            }

            sunLeft += Time.deltaTime;
            sunMiddle += Time.deltaTime;
            sunRight += Time.deltaTime;
        }
    }

    void PlanetsUpdate()
    {
        sunHit.enabled = (!moving && day);
        sunGravity.active = (!moving && day);

        moonHit.enabled = (!moving && !day);
        moonGravity.active = (!moving && !day);
    }

    void UpdateDayAndNight()
    {
        if(!moving)
        {
            if (cycle >= dayCycle)
            {
                day = !day;
                moving = true;
                StartCoroutine(SwapCycle());
            }

            cycle += Time.deltaTime;
        }
    }

    IEnumerator SwapCycle()
    {
        float rot = 0;
        int dir = 1;

        //Start Celarus Leaving
        if (day)
        {
            rot = 180;
            dir = -1;

            //Raise Celarus
            float leaveTime = 0;
            float timeframe = .75f;

            while(leaveTime < 1)
            {
                transform.position += transform.up * Time.deltaTime;

                leaveTime += (1/ timeframe) * Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            //Make Celarus Spin
        }

        Vector3 curRot = spinPoint.transform.localRotation.eulerAngles;

        while(Mathf.Abs(curRot.z - rot) > 1)
        {
            spinPoint.transform.localRotation = Quaternion.Euler(0, 0, curRot.z + (dir * spinSpeed * Time.deltaTime));

            //Make Celarus Leave/Return
            transform.position = (moonGravity.transform.position + leavePoint);
            yield return new WaitForEndOfFrame();
            curRot = spinPoint.transform.localRotation.eulerAngles;
        }

        spinPoint.transform.localRotation = Quaternion.Euler(0, 0, rot);
        rb.velocity = Vector3.zero;
        yield return null;

        //Finish Celarus Return
        if (!day)
        {
            transform.position = (moonGravity.transform.position + (moonGravity.transform.up * 3));
            yield return null;
        }

        cycle = 0;
        moving = false;
        yield return null;
    }

    void GetLifelineObjs()
    {
        Player p = pg.player;

        if (p != null)
        {
            GameObject sll = p.spawnedLifeline;

            if (sll != null)
            {
                if(sll.transform.childCount >= 1)
                {
                    spinPoint = sll.transform.GetChild(0);

                    if(spinPoint != null)
                    {
                        if(spinPoint.childCount > 0)
                        {
                            for(int i = 0; i < spinPoint.childCount; i++)
                            {
                                Transform spc = spinPoint.GetChild(i);

                                if(spc != null)
                                {
                                    if(spc.name.ToLower().Trim() == "moon")
                                    {
                                        moonHit = spc.GetComponent<DamageOnTagHit>();
                                        moonGravity = spc.GetComponent<PullObjectIn>();
                                    }
                                    else if(spc.name.ToLower().Trim() == "sun")
                                    {
                                        sunHit = spc.GetComponent<DamageOnTagHit>();
                                        sunGravity = spc.GetComponent<PullObjectIn>();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
