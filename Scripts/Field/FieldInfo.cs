using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldInfo : MonoBehaviour
{
    Database db;
    public Field field = new Field();
    public bool spawning = false;
    public bool createMode = false;
    public bool started = false;

    public Ball ball;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
    }

    private void Update()
    {
        if(started)
        {
            BallExistCheck();
        }
    }

    void BallExistCheck()
    {
        GameObject[] allBalls = GameObject.FindGameObjectsWithTag("Ball");

        //If no balls present spawn the ball
        if (allBalls.Length == 0)
        {
            SpawnBall();
        }
    }

    public void SpawnBall()
    {
        GameObject bSpawn = GetSpawn("Ball");

        if (bSpawn != null)
        {
            GameObject b = Instantiate(ball.prefab, bSpawn.transform.position, Quaternion.identity);

            if (b != null)
            {
                BallInfo bI = b.GetComponent<BallInfo>();

                if (bI != null)
                {
                    bI.ball = new Ball(ball);
                    bI.ballReady = true;
                }
            }
        }
    }

    public void StartFieldSpawn()
    {
        if(!spawning && field.parts.Count > 0)
        {
            spawning = true;
            StartCoroutine(SpawnField());
        }
    }


    public IEnumerator BeginGame(Ball ballstartingWith)
    {
        //Spawn Ball
        GameObject bSpawn = GetSpawn("Ball");
        ball = new Ball(ballstartingWith);

        if (bSpawn != null)
        {
            GameObject b = Instantiate(ball.prefab, bSpawn.transform.position, Quaternion.identity);

            if(b != null)
            {
                BallInfo bI = b.GetComponent <BallInfo>();

                if(bI != null)
                {
                    bI.ball = ball;
                    yield return null;

                    //Begin Countdown
                    db.mD.Write("3");
                    yield return new WaitForSeconds(1);

                    db.mD.Write("2");
                    yield return new WaitForSeconds(1);

                    db.mD.Write("1");
                    yield return new WaitForSeconds(1);

                    db.mD.Clear();
                    yield return new WaitForSeconds(1);

                    bI.ballReady = true;
                    yield return null;
                }
            }
        }

        //Set to Playing
        db.gamestate = GameState.Playing;
        started = true;
        yield return null;
    }

    IEnumerator SpawnField()
    {
        transform.position = new Vector3(0, 0, field.size);

        for(int i = 0; i < field.parts.Count; i++)
        {
            Part p = field.parts[i];

            if(p.prefab != null)
            {
                //Create Part
                GameObject go = Instantiate(p.prefab, p.position, Quaternion.Euler(p.rotation),transform) as GameObject;

                if(go != null)
                {
                    //Set Scale
                    go.transform.localScale = p.size;
                    go.transform.localPosition = p.position;
                    go.transform.localEulerAngles = p.rotation;

                    //Add Scripts

                    if (!createMode && p.type.ToLower().Trim() == "spawn")
                    {
                        go.SetActive(false);
                    }

                    PartInfo pI = go.GetComponent<PartInfo>();

                    if(pI != null)
                    {
                        pI.part = p;
                    }
                }
            }
        }

        spawning = false;
        yield return null;
    }

    public GameObject GetSpawn(string id)
    {
        GameObject result = null;

        if(transform.childCount > 0)
        {
            for(int i = 0; i < transform.childCount; i++)
            {
                PartInfo pI = transform.GetChild(i).GetComponent<PartInfo>();

                if(pI != null)
                {
                    if(pI.part.name.ToLower().Trim() == id.ToLower().Trim())
                    {
                        result = transform.GetChild(i).gameObject;
                        break;
                    }
                }
            }
        }

        return result;
    }

    public void DestroyField()
    {
        Destroy(gameObject);
    }
}
