using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallInfo : MonoBehaviour
{
    public Ball ball;

    Database db;
    public GameObject anchor;
    [SerializeField]
    public List<Collision> futureColisions = new List<Collision>();
    public List<Vector3> futureColisionPoints = new List<Vector3>();
    [HideInInspector()]
    public bool documentColisions = false;

    Rigidbody rb;

    Vector3 lPos = Vector3.zero;
    [Tooltip("How little X or Y can change before that axis counts as stuck.")]
    public float tolorance = 0.5f;
    [Tooltip("Seconds stuck on the same X or same Y before the ball is destroyed.")]
    public float timeTillReset = 8f;
    public float ttR = 0;

    float stuckTimerX = 0f;
    float stuckTimerY = 0f;

    public bool ballReady = false;
    public bool checkStuck = true;
    public bool speedCap = true;
    public bool projectionOn = true;
    public bool showProjection = false;

    int _maxPhysicsFrameIterations = 100;
    bool runProjection = false;
    float speedUp = 10;

    public bool showFutureCollisions = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        db = Database.instance;
        lPos = transform.position;
    }

    void Update()
    {
        if (db.gameStart)
        {
            if (anchor == null)
            {
                Destroy(gameObject);
            }

            if (ballReady)
            {
                if (checkStuck)
                {
                    StuckInAxis();
                }

                if (!runProjection)
                {
                    if (projectionOn == true)
                    {
                        runProjection = true;
                        StartCoroutine(RunProjection());
                    }
                }

                if (showFutureCollisions)
                {
                    if (futureColisionPoints.Count > 0)
                    {
                        for (int i = 0; i < futureColisionPoints.Count; i++)
                        {
                            Vector3 cP = futureColisionPoints[i];

                            GameObject cG = GameObject.CreatePrimitive(PrimitiveType.Cube);

                            GameObject oldHp = GameObject.Find("Hit Point: " + i);

                            if (oldHp != null)
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
    }

    void StuckInAxis()
    {
        // Prefabs previously used 0.05 which never stayed "stuck" due to physics jitter
        float tol = Mathf.Max(tolorance, 0.35f);
        Vector3 pos = transform.position;

        if (Mathf.Abs(pos.x - lPos.x) <= tol)
        {
            stuckTimerX += Time.deltaTime;
        }
        else
        {
            stuckTimerX = 0f;
            lPos.x = pos.x;
        }

        if (Mathf.Abs(pos.y - lPos.y) <= tol)
        {
            stuckTimerY += Time.deltaTime;
        }
        else
        {
            stuckTimerY = 0f;
            lPos.y = pos.y;
        }

        ttR = Mathf.Max(stuckTimerX, stuckTimerY);

        if (stuckTimerX > timeTillReset || stuckTimerY > timeTillReset)
        {
            Destroy(gameObject);
        }
    }

    IEnumerator RunProjection()
    {
        if (projectionOn)
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

            if (gBI.transform.childCount > 0)
            {
                for (int i = 0; i < gBI.transform.childCount; i++)
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

            ghostObj.GetComponent<Rigidbody>().linearVelocity = rb.linearVelocity;
            yield return new WaitForSeconds(.001f);

            ghostObj.GetComponent<Rigidbody>().linearVelocity *= speedUp;
            yield return null;

            yield return new WaitForSeconds(_maxPhysicsFrameIterations / 60);

            // Copy data before destroy — previously read after Destroy (always failed)
            List<Collision> cols = gBI.futureColisions;
            List<Vector3> points = gBI.futureColisionPoints;
            Destroy(ghostObj);
            futureColisions = cols;
            futureColisionPoints = points;
            yield return null;

            runProjection = false;
        }

        yield return null;
    }
}
