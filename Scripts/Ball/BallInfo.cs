using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallInfo : MonoBehaviour
{
    public Ball ball;

    Vector3 lPos = Vector3.zero;
    float tolorance = .4f;
    float timeTillReset = 10;
    float ttR = 0;

    public bool ballReady = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        StuckInAxis();
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
}
