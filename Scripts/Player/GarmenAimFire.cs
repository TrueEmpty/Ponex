using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarmenAimFire : MonoBehaviour
{
    PlayerGrab pG;
    public Transform hub;
    public float speed = 1;
    public float rotAmount = 0;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
    }

    // Update is called once per frame
    void Update()
    {
        if(pG.player.CanBump)
        {
            int rotDir = 0;

            if (ButtonManager.KeyPressed(pG.player.keys.right) && rotAmount > -90)
            {
                rotDir -= 1;
            }

            if (ButtonManager.KeyPressed(pG.player.keys.left) && rotAmount < 90)
            {
                rotDir += 1;
            }

            if (pG.player.keys.dirShown == "D" || pG.player.keys.dirShown == "R")
            {
                rotDir *= -1;
            }

            hub.Rotate(Vector3.forward * rotDir * Time.deltaTime * speed, Space.Self);
            rotAmount += Time.deltaTime * speed * rotDir;
        }
    }
}
