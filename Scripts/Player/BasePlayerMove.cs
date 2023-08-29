using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody),typeof(PlayerGrab))]
public class BasePlayerMove : MonoBehaviour
{
    Rigidbody rb;
    ButtonManager bm;
    float canMove = 1;
    PlayerGrab pg;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        bm = ButtonManager.instance;

        rb.useGravity = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(!pg.player.player.selection && pg.player.CanMove)
        {
            OnMove();
        }
    }

    void OnMove()
    {
        int moveDir = 0;

        if (bm.KeyPressed(pg.player.player.keys.right))
        {
                moveDir += 1;
        }

        if (bm.KeyPressed(pg.player.player.keys.left))
        {
                moveDir -= 1;
        }

        Vector3 rotDir = transform.right;
        rotDir.x = Mathf.Abs(rotDir.x);
        rotDir.y = Mathf.Abs(rotDir.y);
        rotDir.z = Mathf.Abs(rotDir.z);

        rb.velocity = rotDir * moveDir * pg.player.Speed;
        //Debug.Log(p.position + "/ " + transform.right);
    }

    public float Movement
    {
        get { return canMove; }

        set { canMove = value; }
    }
}
