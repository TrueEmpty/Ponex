using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TicPlayerMove : MonoBehaviour
{
    /*float canMove = 1;
    PlayerInfo p;
    ButtonManager bm;

    public int lMove = 0;
    public int rMove = 0;
    public int cMove = 0;

    public GameObject rightObj;
    public GameObject leftObj;
    public GameObject centerObj;

    float ltime = 0;
    float rtime = 0;
    float ctime = 0;

    float bumperDistance = 0;
    Vector3 cStart;

    public float rotAmountL = 0;
    public float rotAmountR = 0;

    // Start is called before the first frame update
    void Start()
    {
        p = GetComponent<PlayerInfo>();
        cStart = centerObj.transform.position;
        bm = ButtonManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if(!p.player.selection)
        {
            OnMove();
        }
    }

    void OnMove()
    {
        if (p.player.keys.dirShown == "U" || p.player.keys.dirShown == "L")
        {
            if (bm.KeyPressed(p.player.keys.right) && rMove == 0)
            {
                rMove = 1;
            }

            if (bm.KeyPressed(p.player.keys.left) && lMove == 0)
            {
                lMove = 1;
            }

            if (bm.KeyPressed(p.player.keys.bump) && cMove == 0)
            {
                cMove = 1;
            }

            if (bm.KeyPressed(p.player.keys.super) && cMove == 0)
            {
                cMove = -1;
            }
        }
        else
        {
            if (bm.KeyPressed(p.player.keys.right) && lMove == 0)
            {
                lMove = 1;
            }

            if (bm.KeyPressed(p.player.keys.left) && rMove == 0)
            {
                rMove = 1;
            }

            if (bm.KeyPressed(p.player.keys.bump) && cMove == 0)
            {
                cMove = 1;
            }

            if (bm.KeyPressed(p.player.keys.super) && cMove == 0)
            {
                cMove = -1;
            }

        }

        switch (rMove)
        {
            case 1:
                if (rotAmountR < 45)
                {
                    rightObj.transform.Rotate(Vector3.right * p.BumpSpeed * Time.deltaTime, Space.Self);
                    rotAmountR += Time.deltaTime * p.BumpSpeed;
                }
                else
                {
                    rMove = 2;
                }
            break;
            case 2:
                if (rotAmountR > 0)
                {
                    rightObj.transform.Rotate(Vector3.right * -p.BumpSpeed * Time.deltaTime, Space.Self);
                    rotAmountR += Time.deltaTime * -p.BumpSpeed;
                }
                else
                {
                    rMove = 3;
                }
            break;
            case 3:
                rtime += Time.deltaTime;

                if(rtime > .15f)
                {
                    rMove = 0;
                    rtime = 0;
                }
            break;
        }

        switch (lMove)
        {
            case 1:
                if (rotAmountL < 45)
                {
                    leftObj.transform.Rotate(Vector3.right * p.BumpSpeed * Time.deltaTime, Space.Self);
                    rotAmountL += Time.deltaTime * p.BumpSpeed;
                }
                else
                {
                    lMove = 2;
                }
                break;
            case 2:
                if (rotAmountL > 0)
                {
                    leftObj.transform.Rotate(Vector3.right * -p.BumpSpeed * Time.deltaTime, Space.Self);
                    rotAmountL += Time.deltaTime * -p.BumpSpeed;
                }
                else
                {
                    lMove = 3;
                }
                break;
            case 3:
                ltime += Time.deltaTime;

                if (ltime > .15f)
                {
                    lMove = 0;
                    ltime = 0;
                }
                break;
        }

        bumperDistance = Vector3.Distance(leftObj.transform.position, rightObj.transform.position) - 20;
        float selfDis = Vector3.Distance(cStart, centerObj.transform.position);

        switch (Mathf.Abs(cMove))
        {
            case 1:
                if (selfDis < (bumperDistance/2))
                {
                    centerObj.transform.position += centerObj.transform.right * cMove * Time.deltaTime * p.Speed;
                }
                else
                {
                    cMove *= 2;
                }
                break;
            case 2:
                if (selfDis > .5f)
                {
                    centerObj.transform.position += centerObj.transform.right * -cMove * Time.deltaTime * p.Speed;
                }
                else
                {
                    centerObj.transform.position = cStart;
                    cMove = 3;
                }
                break;
            case 3:
                ctime += Time.deltaTime;

                if (ctime > .15f)
                {
                    cMove = 0;
                    ctime = 0;
                }
                break;
        }
    }*/
}
