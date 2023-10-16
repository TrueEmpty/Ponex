using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BallInfo))]
public class SizeBall : MonoBehaviour
{
    Database db;
    BallInfo bI;
    SphereCollider sC;
    float timer = 0;
    public Vector3 sizeSwitch; //Min, Max, Nextswitch
    public Vector2 sizeRange;

    public Transform ball;
    public Transform adjusted;
    
    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();
        sC = GetComponent<SphereCollider>();

        if(bI.projectionOn)
        {
            SetSwitch();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(db.gameStart && bI.ballReady && bI.projectionOn)
        {
            bool iSR = InSwitchRange();
            ball.gameObject.SetActive(!iSR || (ball.localScale.x < adjusted.localScale.x));
            adjusted.gameObject.SetActive(iSR);

            if (timer > sizeSwitch.z)
            {
                ball.localScale = adjusted.localScale;
                sC.radius = ball.localScale.x / 2;
                SetSwitch();
                timer = 0;
            }

            timer += Time.deltaTime;
        }
    }

    bool InSwitchRange()
    {
        bool result = false;

        float timeLeft = sizeSwitch.z - timer;

        if(timeLeft <= 3 && timeLeft >= 2.55f)
        {
            result = true;
        }
        else if(timeLeft <= 2f && timeLeft >= 1.685f)
        {
            result = true;
        }
        else if (timeLeft <= 1.3f && timeLeft >= 1.0795f)
        {
            result = true;
        }
        else if (timeLeft <= 0.81f && timeLeft >= 0.65565f)
        {
            result = true;
        }
        else if (timeLeft <= 0.467f && timeLeft >= 0.358955f)
        {
            result = true;
        }
        else if (timeLeft <= 0.2269f && timeLeft >= 0.1512685f)
        {
            result = true;
        }
        else if (timeLeft <= 0.0588299999999999f && timeLeft >= 0.00588794999999986f)
        {
            result = true;
        }

        return result;
    }

    void SetSwitch()
    {
        sizeSwitch.z = Random.Range(sizeSwitch.x, sizeSwitch.y);
        adjusted.localScale = Vector3.one * Random.Range(sizeRange.x,sizeRange.y);
    }
}
