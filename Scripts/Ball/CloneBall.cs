using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BallInfo))]
public class CloneBall : MonoBehaviour
{
    Database db;
    BallInfo bI;
    float popTimer = 0;
    public Vector3 popMainTimer; //Min, Max, Nextswitch
    public Vector3 sizeRange; //Min, MaxPop,Max
    public Vector2 growthRange; //Min, Max
    public int maxClones = 5;
    int maxCloneBalls = 25;

    public GameObject cloneBallChild;

    public List<GameObject> cloneChildren;

    [Range(0, 1)]
    public float cloneChance = .25f;
    float clonetimer = 0;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();

        if(bI.projectionOn)
        {
            GameObject[] allBalls = GameObject.FindGameObjectsWithTag("Ball");
            int cbC = 0;

            foreach (GameObject aB in allBalls)
            {
                if (aB != gameObject)
                {
                    BallInfo gbI = aB.GetComponent<BallInfo>();

                    if (gbI != null)
                    {
                        if (gbI.ball.name == "Clone Ball")
                        {
                            cbC++;

                            if (cbC >= maxCloneBalls)
                            {
                                Destroy(gameObject);
                            }
                        }
                    }
                }
            }

            popMainTimer.z = Random.Range(popMainTimer.x, popMainTimer.y);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (db.gameStart && bI.ballReady && bI.projectionOn)
        {
            //Pop clones off
            if (popTimer > popMainTimer.z)
            {
                if(cloneChildren.Count > 0)
                {
                    for(int i = cloneChildren.Count - 1; i >= 0; i--)
                    {
                        if(cloneChildren[i].transform.localScale.x > sizeRange.y)
                        {
                            GameObject ncb = Instantiate(bI.ball.prefab);
                            ncb.transform.localScale = cloneChildren[i].transform.localScale;

                            BallInfo nBI = ncb.GetComponent<BallInfo>();

                            if(nBI != null)
                            {
                                nBI.ball = new Ball(bI.ball);
                                nBI.ballReady = true;
                            }

                            Destroy(cloneChildren[i]);
                            cloneChildren.RemoveAt(i);
                        }
                    }
                }

                popMainTimer.z = Random.Range(popMainTimer.x, popMainTimer.y);
                popTimer = 0;
            }

            //Create Clone
            if(clonetimer > 1)
            {
                if(cloneChance >= Random.Range(0f,1f))
                {
                    if(cloneChildren.Count < maxClones)
                    {
                        GameObject cC = Instantiate(cloneBallChild);
                        cC.transform.parent = transform;
                        cC.transform.localScale = Vector3.one * sizeRange.x;
                        float randomX = Random.Range(-1f, 1f);
                        float randomY = Random.Range(-1f, 1f);

                        float totalSum = Mathf.Abs(randomX) + Mathf.Abs(randomY);

                        float percentX = Mathf.Abs(randomX) / totalSum;
                        float percentY = Mathf.Abs(randomY) / totalSum;

                        float trueX = transform.localScale.x * percentX;
                        float trueY = transform.localScale.y * percentY;

                        if(randomX < 0)
                        {
                            trueX *= -1;
                        }

                        if(randomY < 0)
                        {
                            trueY *= -1;
                        }

                        cC.transform.localPosition = new Vector3(trueX, trueY, 0);

                        SizeOverTime sot = cC.GetComponent<SizeOverTime>();

                        if(sot != null)
                        {
                            sot.growthRate = Random.Range(growthRange.x, growthRange.y);
                            sot.growthRange = new Vector2(sizeRange.x,sizeRange.z);
                        }

                        cloneChildren.Add(cC);
                    }
                }

                clonetimer = 0;
            }

            popTimer += Time.deltaTime;
            clonetimer += Time.deltaTime;
        }
    }
}
