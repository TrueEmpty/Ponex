using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarmenDrive : MonoBehaviour
{
    PlayerGrab pG;
    ButtonManager bm;
    public LayerMask layers = new LayerMask();
    float serachLength = 2.4f;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
        bm = ButtonManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if(!pG.player.player.selection)
        {
            Action();
        }
    }

    void Action()
    {
        if (pG.player.CanMove && pG.player.player.player.superAmount >= pG.player.player.player.superCost)
        {
            int moveDir = 0;

            if (bm.KeyPressed(pG.player.player.keys.right) && !Physics.Raycast(transform.position, transform.right, serachLength, layers))
            {
                moveDir += 1;
            }

            if (bm.KeyPressed(pG.player.player.keys.left) && !Physics.Raycast(transform.position, transform.right * -1, serachLength, layers))
            {
                moveDir -= 1;
            }

            transform.position += transform.right * moveDir * pG.player.Speed * Time.deltaTime;
            pG.player.CostSuper(pG.player.player.player.superCost * Mathf.Abs(moveDir) * Time.deltaTime);
        }
    }
}
