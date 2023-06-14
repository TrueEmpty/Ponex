using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarmenModeSwitch : MonoBehaviour
{
    PlayerGrab pG;
    ButtonManager bm;
    public GameObject hub;
    [SerializeField]
    bool driveMode = false;
    [SerializeField]
    float ready = 0;
    [SerializeField]
    float cooldown = 1.5f;

    [SerializeField]
    float height = 0;
    [SerializeField]
    float liftRate = 1.2f;

    public bool constraintExist = false;
    public bool constraintMExist = false;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
        bm = ButtonManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        Player p = pG.player;
        ready += Time.deltaTime;

        if (bm.KeyPressed(p.keys.super) && ready >= cooldown)
        {
            driveMode = !driveMode;
            ready = 0;
        }

        if(driveMode)
        {
            //Start ability to move
            if (constraintMExist)
            {
                p.RemoveConstraint(gameObject, PlayerConstraint.Move);
                constraintMExist = false;
            }

            p.player.superReadyPercent = 1;

            //Stop ability to shoot
            if(!constraintExist)
            {
                p.AddConstraint(gameObject, -1, PlayerConstraint.Bump);
                constraintExist = true;
            }

            //Raise Garmen
            if (height < .5f)
            {
                hub.transform.position += hub.transform.up * Time.deltaTime * (liftRate * 2);
                height += Time.deltaTime * liftRate;
            }
        }
        else
        {
            //Stop ability to move
            if (!constraintMExist)
            {
                p.AddConstraint(gameObject, -1, PlayerConstraint.Move);
                constraintMExist = true;
            }

            p.player.superReadyPercent = 0;

            //Continue Normal shoot ability
            if(constraintExist)
            {
                p.RemoveConstraint(gameObject, PlayerConstraint.Bump);
                constraintExist = false;
            }

            //Lower Garmen
            if (height > 0)
            {
                hub.transform.position -= hub.transform.up * Time.deltaTime * (liftRate * 2);
                height -= Time.deltaTime * liftRate;
            }
        }
    }
}
