using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bahrue : MonoBehaviour
{
    Rigidbody rb;
    ButtonManager bm;
    Database db;
    PlayerGrab pg;
    public List<string> hitTags = new List<string>();

    [SerializeField]
    List<FollowPlayer> bahrueObjs = new List<FollowPlayer>();
    int lastHealth = -1;

    public GameObject bahrueObj;
    public GameObject bahrueBump;

    [SerializeField]
    public List<Vector2Int> amountPerRow = new List<Vector2Int>();

    public Vector3 offsetAmounts = Vector3.zero;
    public Transform holder;

    public Transform token;
    public Renderer ring;
    public Renderer emblem;

    public float bumpCooldown = 2;
    float timeTillReset = 0;
    public int healthGain = 3;

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
            BahrueUpkeep();

            if (pg.player.computer)
            {
                if(!thinking)
                {
                    thinking = true;
                    StartCoroutine(AI());
                }
            }

            if(pg.player.CanMove)
            {
                OnMove();
            }

            if (pg.player.CanSuper)
            {
                ActivateSuper();
            }

            if (pg.player.CanBump)
            {
                ActivateBump();
            }
        }
    }

    IEnumerator AI()
    {
        //Inital Info
        Facing f = pg.player.facing;
        string thoughtString = "";
        float extendTime = 0;

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
                    dMS += 10 / iCo;

                    if (thought == Thought.MoveDown)
                    {
                        dMS += 5;
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
            extendTime += 10;
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
        yield return new WaitForSeconds(thinkTime + extendTime);

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
        float dis = 1;

        if (amountPerRow.Count >= 1)
        {
            float halfDis = (((float)amountPerRow[0].x / 2) * (offsetAmounts.x * transform.localScale.x));
            dis = halfDis;
        }

        RaycastHit[] hits = Physics.RaycastAll(holder.position, dir * holder.right, (dis));

        if (hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                if (hitTags.Exists(x=> x.ToLower().Trim() == hits[i].transform.tag.ToLower().Trim()))
                {
                    result = true;
                    break;
                }
            }
        }

        return result;
    }

    void BahrueUpkeep()
    {
        if (pg.player.currentHealth > 0)
        {
            if (lastHealth != pg.player.currentHealth)
            {
                UpdateBahrue();
            }

            pg.player.movementSpeed = (1.5f * (50 / (float)pg.player.currentHealth))/4;
        }
    }

    void UpdateBahrue()
    {
        //Remove Extras
        if (bahrueObjs.Count > pg.player.currentHealth)
        {
            for (int i = bahrueObjs.Count - 1; i >= pg.player.currentHealth; i--)
            {
                //Run Animation at position

                //Remove
                Destroy(bahrueObjs[i].gameObject);
                bahrueObjs.RemoveAt(i);
            }
        }

        //Add Missing
        if (bahrueObjs.Count < pg.player.currentHealth)
        {
            for (int i = bahrueObjs.Count; i < pg.player.currentHealth; i++)
            {
                //Run Animation at position

                //Add
                GameObject go = Instantiate(bahrueObj, holder.position + transform.up * -2.5f,transform.rotation);

                go.transform.parent = holder;

                PlayerGrab cpg = go.GetComponent<PlayerGrab>();
                FollowPlayer fp = go.GetComponent<FollowPlayer>();

                if (cpg != null)
                {
                    cpg.playerIndex = pg.playerIndex;
                }

                if (fp != null)
                {
                    fp.playerTransform = holder;
                    fp.offset = new Vector3(0, -3, 0);
                    bahrueObjs.Add(fp);
                }
            }
        }

        //Update offsets
        if (bahrueObjs.Count > 0)
        {
            amountPerRow = AmountPerRow();
            for (int i = 0; i < bahrueObjs.Count; i++)
            {
                Vector3 getPosOff = GetPosition(i);

                if (getPosOff != bahrueObjs[i].offset)
                {
                    bahrueObjs[i].LerpOffset(getPosOff, 5.5f);
                }
            }
        }

        lastHealth = pg.player.currentHealth;
    }

    void ActivateSuper()
    {
        Vector3 pP = Vector3.zero;
        pP.y = GetTopPoint() + 1;
        token.localPosition = pP;

        if (bm.KeyPressed(pg.player.buttons.Down(pg.player.facing)) || thought == Thought.MoveDown)
        {
            pg.player.AddConstraint(gameObject, -1, PlayerConstraint.Move);
            pg.player.AddConstraint(gameObject, -1, PlayerConstraint.Bump);

            float superGainSpeed = .9f * (50 / (float)pg.player.currentHealth);
            pg.player.super.amount += ((Time.deltaTime * superGainSpeed) / 100);

            if (pg.player.super.amount >= pg.player.super.cost)
            {
                pg.player.maxHealth += healthGain;
                pg.player.Heal(healthGain);
                pg.player.super.amount = 0;
                //Play Shine Animation/Sound/Particle
            }
        }
        else
        {
            pg.player.RemoveConstraint(gameObject, PlayerConstraint.Move);
            pg.player.RemoveConstraint(gameObject, PlayerConstraint.Bump);
            pg.player.super.amount = 0;
        }

        ring.gameObject.SetActive(pg.player.super.amount != 0);
        emblem.gameObject.SetActive(pg.player.super.amount != 0);

        Color rC = ring.material.color;
        Color eC = emblem.material.color;

        rC.a = pg.player.super.amount;
        eC.a = pg.player.super.amount;

        ring.material.color = rC;
        emblem.material.color = eC;

        pg.player.maxHealth = pg.player.currentHealth;
    }

    void ActivateBump()
    {
        float bP = timeTillReset / bumpCooldown;

        if (bP > 1)
        {
            bP = 1;
        }
        else if (bP < 0)
        {
            bP = 0;
        }

        pg.player.bump.readyPercent = bP;

        if (bm.KeyPressed(pg.player.buttons.Up(pg.player.facing)) || thought == Thought.MoveUp)
        {
            if (timeTillReset >= bumpCooldown && pg.player.CanBump)
            {
                if (bahrueObjs.Count > 1)
                {
                    if (amountPerRow.Count > 1)
                    {
                        int cILR = amountPerRow[^1].x;

                        if (bahrueObjs.Count > cILR)
                        {
                            for (int i = 0; i < cILR; i++)
                            {
                                GameObject bo = bahrueObjs[^1].gameObject;

                                Vector3 spawnPos = bo.transform.position;
                                GameObject go = Instantiate(bahrueBump, spawnPos, transform.rotation) as GameObject;

                                PlayerGrab bpg = go.GetComponent<PlayerGrab>();

                                if (pg != null)
                                {
                                    bpg.playerIndex = pg.playerIndex;
                                }

                                pg.player.currentHealth--;
                                pg.player.maxHealth = pg.player.currentHealth;
                                Destroy(bo);
                                bahrueObjs.RemoveAt(bahrueObjs.Count - 1);
                            }

                            timeTillReset = 0;
                        }
                    }
                }
            }
        }

        timeTillReset += Time.deltaTime;
    }

    Vector3 GetPosition(int index)
    {
        Vector3 result = Vector3.zero;
        int totalCount = 0;
        int row = -1;
        int col = -1;

        if (amountPerRow.Count > 0)
        {
            for (int i = 0; i < amountPerRow.Count; i++)
            {
                Vector2Int apr = amountPerRow[i];
                if (index < totalCount + apr.x)
                {
                    row = i;
                    col = totalCount - index;
                    break;
                }

                totalCount += apr.x;
            }

            if (row >= 0 && row < amountPerRow.Count)
            {
                Vector2Int apr = amountPerRow[row];
                float maxDis = (apr.x * offsetAmounts.x) * .5f;
                result.x = (maxDis + (col * offsetAmounts.x)) - (offsetAmounts.x / 2);
                result.y = row * offsetAmounts.y;
            }
        }

        return result;
    }

    List<Vector2Int> AmountPerRow()
    {
        List<Vector2Int> result = new List<Vector2Int>();
        //How Many in that row,Position Until Up = > 2

        if (bahrueObjs.Count > 0)
        {
            result.Add(new Vector2Int(0, 0));

            for (int i = 0; i < bahrueObjs.Count; i++)
            {
                for (int r = 0; r < result.Count; r++)
                {
                    Vector2Int v2I = result[r];
                    if (v2I.y < 2)
                    {
                        v2I.x++;
                        v2I.y++;
                        result[r] = v2I;
                        break;
                    }
                    else
                    {
                        if (result.Count <= r + 1)
                        {
                            for (int n = r; n >= 0; n--)
                            {
                                Vector2Int n2I = result[n];
                                n2I.y = 1;
                                result[n] = n2I;
                            }

                            result.Add(new Vector2Int(1, 1));
                            break;
                        }
                    }
                }
            }
        }

        return result;
    }

    float GetTopPoint()
    {
        return amountPerRow.Count * offsetAmounts.y;
    }
}
