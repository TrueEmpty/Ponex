using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarmenDrive : MonoBehaviour
{
    PlayerGrab pG;
    public LayerMask layers = new LayerMask();
    float serachLength = 2.4f;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
    }

    // Update is called once per frame
    void Update()
    {
        if (pG.player.CanMove && pG.player.player.superAmount >= pG.player.player.superCost)
        {
            int moveDir = 0;

            if (ButtonManager.KeyPressed(pG.player.keys.right) && !Physics.Raycast(transform.position,transform.right, serachLength, layers))
            {
                moveDir += 1;
            }

            if (ButtonManager.KeyPressed(pG.player.keys.left) && !Physics.Raycast(transform.position, transform.right*-1, serachLength, layers))
            {
                moveDir -= 1;
            }

            transform.position += transform.right * moveDir * pG.player.Speed * Time.deltaTime;
            pG.player.CostSuper(pG.player.player.superCost * Mathf.Abs(moveDir) * Time.deltaTime);
        }
    }
}
