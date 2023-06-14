using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicDash : MonoBehaviour
{
    int firstclick = 0;
    public bool notDashing = true;
    float doubleClickTime = .3f;
    float timeTillReset = 0;

    public float dashTime = .07f;
    float dashEnd = 0;
    float speedIncrease = 0;
    public float speedMultiplyer = 4;
    float canDash = 0;

    Player player;
    ButtonManager bm;

    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<Player>();
        bm = ButtonManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        float dC = player.player.dashCooldown;
        float dP = canDash / dC;

        if (dP > 1)
        {
            dP = 1;
        }
        else if (dP < 0)
        {
            dP = 0;
        }

        player.player.dashReadyPercent = dP;

        if (notDashing)
        {
            if (bm.KeyDown(player.keys.left) && canDash >= dC)
            {
                if (firstclick == 0)
                {
                    firstclick = 1;
                    timeTillReset = Time.time + doubleClickTime;
                }
                else if (timeTillReset >= Time.time && firstclick == 1)
                {
                    notDashing = false;
                    speedIncrease = player.Speed * speedMultiplyer;
                    player.Speed += speedIncrease;
                    dashEnd = Time.time + dashTime;
                    firstclick = 0;
                    timeTillReset = Time.time - 1;
                    canDash = 0;
                }
            }

            if (bm.KeyDown(player.keys.right) && canDash >= dC)
            {
                if (firstclick == 0)
                {
                    firstclick = -1;
                    timeTillReset = Time.time + doubleClickTime;
                }
                else if (timeTillReset >= Time.time && firstclick == -1)
                {
                    notDashing = false;
                    speedIncrease = player.Speed * speedMultiplyer;
                    player.Speed += speedIncrease;
                    dashEnd = Time.time + dashTime;
                    firstclick = 0;
                    timeTillReset = Time.time - 1;
                    canDash = 0;
                }
            }
        }

        if(Time.time > timeTillReset)
        {
            if(!notDashing && dashEnd < Time.time)
            {
                player.Speed -= speedIncrease;
                notDashing = true;
            }

            firstclick = 0;
            timeTillReset = Time.time - 1;
        }


        canDash += Time.deltaTime;
    }
}
