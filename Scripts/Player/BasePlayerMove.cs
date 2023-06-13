using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody),typeof(Player))]
public class BasePlayerMove : MonoBehaviour
{
    Rigidbody rb;
    float canMove = 1;
    Player p;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        p = GetComponent<Player>();

        rb.useGravity = false;
    }

    // Update is called once per frame
    void Update()
    {
        OnMove();
    }

    void OnMove()
    {
        int moveDir = 0;

        if (ButtonManager.KeyPressed(p.keys.right))
        {
                moveDir += 1;
        }

        if (ButtonManager.KeyPressed(p.keys.left))
        {
                moveDir -= 1;
        }

        Vector3 rotDir = transform.right;
        rotDir.x = Mathf.Abs(rotDir.x);
        rotDir.y = Mathf.Abs(rotDir.y);
        rotDir.z = Mathf.Abs(rotDir.z);

        rb.velocity = rotDir * moveDir * p.Speed;
        //Debug.Log(p.position + "/ " + transform.right);
    }

    public float Movement
    {
        get { return canMove; }

        set { canMove = value; }
    }
}
