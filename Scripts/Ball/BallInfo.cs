using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallInfo : MonoBehaviour
{
    public Ball ball;

    public GameObject anchor;
    [SerializeField]
    public List<Collision> futureColisions = new List<Collision>();
    public List<Vector3> futureColisionPoints = new List<Vector3>();
    [HideInInspector()]
    public bool documentColisions = false;

    Rigidbody rb;

    Vector3 lPos = Vector3.zero;
    public float tolorance = .4f;
    float timeTillReset = 10;
    public float ttR = 0;

    public bool ballReady = false;
    public bool checkStuck = true;
    public bool speedCap = true;
    public bool projectionOn = true;
    public bool showProjection = false;

    int _maxPhysicsFrameIterations = 100;
    bool runProjection = false;
    float speedUp = 10;

    public bool showFutureCollisions = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if(anchor == null)
        {
            Destroy(gameObject);
        }

        if(ballReady)
        {
            if(checkStuck)
            {
                StuckInAxis();
            }

            if (!runProjection)
            {
                if(projectionOn == true)
                {
                    runProjection = true;
                    StartCoroutine(RunProjection());
                }
            }

            if(showFutureCollisions)
            {
                if(futureColisionPoints.Count > 0)
                {
                    for(int i = 0; i < futureColisionPoints.Count; i++)
                    {
                        Vector3 cP = futureColisionPoints[i];

                        GameObject cG = GameObject.CreatePrimitive(PrimitiveType.Cube);

                        GameObject oldHp = GameObject.Find("Hit Point: " + i);

                        if(oldHp != null)
                        {
                            Destroy(oldHp);
                        }

                        cG.name = "Hit Point: " + i;
                        cG.layer = 7;

                        cG.GetComponent<Renderer>().material = gameObject.GetComponent<Renderer>().material;
                        cG.GetComponent<Renderer>().material.color = Color.magenta;
                        cG.transform.localScale = Vector3.one * 5;
                        cG.transform.position = cP;
                        Destroy(cG, 1);
                    }
                }
            }
        }
    }

    void StuckInAxis()
    {
        bool within = false;

        if (transform.position.y >= lPos.y - tolorance && transform.position.y <= lPos.y + tolorance)
        {
            within = true;
        }
        else
        {
            lPos.y = transform.position.y;
        }

        if (transform.position.x >= lPos.x - tolorance && transform.position.x <= lPos.x + tolorance)
        {
            within = true;
        }
        else
        {
            lPos.x = transform.position.x;
        }

        if (within)
        {
            ttR += Time.deltaTime;
        }
        else
        {
            ttR = 0;
        }


        if (ttR > timeTillReset)
        {
            Destroy(gameObject);
        }
    }

    IEnumerator RunProjection()
    {
        if(projectionOn)
        {
            GameObject ghostObj = Instantiate(gameObject);
            ghostObj.name = "(Ghost) " + gameObject.name;
            ghostObj.tag = "Ghost";
            ghostObj.layer = 7;

            Renderer re = ghostObj.GetComponent<Renderer>();
            if (re != null)
            {
                re.enabled = showProjection;
            }

            BallInfo gBI = ghostObj.GetComponent<BallInfo>();
            gBI.anchor = gameObject;
            gBI.checkStuck = false;
            gBI.speedCap = false;
            gBI.futureColisions.Clear();
            gBI.futureColisionPoints.Clear();
            gBI.documentColisions = true;
            gBI.projectionOn = false;
            gBI.runProjection = true;

            if(gBI.transform.childCount > 0)
            {
                for(int i = 0; i < gBI.transform.childCount; i++)
                {
                    Transform cC = gBI.transform.GetChild(i);
                    cC.tag = "Ghost";
                    cC.gameObject.layer = 7;

                    Renderer cRe = cC.GetComponent<Renderer>();
                    if (cRe != null)
                    {
                        cRe.enabled = showProjection;
                    }
                }
            }
            yield return null;

            ghostObj.GetComponent<Rigidbody>().velocity = rb.velocity;
            yield return new WaitForSeconds(.001f);

            ghostObj.GetComponent<Rigidbody>().velocity *= speedUp;
            yield return null;

            yield return new WaitForSeconds(_maxPhysicsFrameIterations / 60);

            Destroy(ghostObj);
            futureColisions = ghostObj.GetComponent<BallInfo>().futureColisions;
            futureColisionPoints = ghostObj.GetComponent<BallInfo>().futureColisionPoints;
            yield return null;

            runProjection = false;
        }
        
        yield return null;
    }
}
