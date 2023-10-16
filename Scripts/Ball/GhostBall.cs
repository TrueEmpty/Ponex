using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostBall : MonoBehaviour
{
    Database db;
    BallInfo bI;
    Renderer ren;
    public GameObject smokePoof;

    float vanishTimer = 0;
    public Vector3 vanishRange; //Min, Max, Nextswitch
    Color baseColor = Color.gray;
    Color vanishColor = Color.gray;
    public float returnTime = 2;

    [Range(0,100)]
    public int vanishAlpha = 1;

    bool vanished = false;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();

        if (bI.projectionOn)
        {
            ren = GetComponent<Renderer>();
            baseColor = ren.material.color;
            vanishColor = baseColor;
            vanishColor.a = (float)vanishAlpha/100;
            SetSwitch();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (db.gameStart && bI.ballReady && bI.projectionOn)
        {
            if(vanished)
            {
                ren.material.color = vanishColor;
            }
            else
            {
                ren.material.color = baseColor;
            }

            if(vanishTimer >= (vanished ? returnTime : vanishRange.z))
            {
                vanished = !vanished;

                if(vanished)
                {
                    if(smokePoof != null)
                    {
                        Instantiate(smokePoof, transform.position, transform.rotation);
                    }
                    SetSwitch();
                }

                vanishTimer = 0;
            }

            vanishTimer += Time.deltaTime;
        }
    }

    void SetSwitch()
    {
        vanishRange.z = Random.Range(vanishRange.x, vanishRange.y);
    }
}
