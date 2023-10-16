using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MasterPong : MonoBehaviour
{
    Rigidbody rb;
    ButtonManager bm;
    Database db;
    PlayerGrab pg;

    public List<string> hitTags = new List<string>();

    public float dis = 1.38f;

    #region AI
    bool thinking = false;

    bool up = false;
    bool down = false;
    bool left = false;
    bool right = false;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        bm = ButtonManager.instance;
        db = Database.instance;

        rb.useGravity = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (db.gameStart && pg.player.currentHealth > 0)
        {
            if (pg.player.computer)
            {
                if (!thinking)
                {
                    thinking = true;
                    StartCoroutine(AI());
                }
            }

            if (pg.player.CanMove)
            {
                OnMove();
            }
            else
            {
                rb.velocity = Vector3.zero;
            }
        }
    }

    IEnumerator AI()
    {



        thinking = false;
        yield return null;
    }

    void OnMove()
    {
        int moveDir = 0;

        if (bm.KeyPressed(pg.player.buttons.Right(pg.player.facing)) && !WallInDirection(1))
        {
            moveDir += 1;
        }

        if (bm.KeyPressed(pg.player.buttons.Left(pg.player.facing)) && !WallInDirection(-1))
        {
            moveDir -= 1;
        }

        Vector3 rotDir = transform.right;
        rotDir.x = Mathf.Abs(rotDir.x);
        rotDir.y = Mathf.Abs(rotDir.y);
        rotDir.z = Mathf.Abs(rotDir.z);

        rb.velocity = rotDir * moveDir * pg.player.movementSpeed;
    }

    bool WallInDirection(int dir)
    {
        bool result = false;

        RaycastHit[] hits = Physics.RaycastAll(transform.position,dir * transform.right, (dis));

        if (hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                if (hitTags.Exists(x => x.ToLower().Trim() == hits[i].transform.tag.ToLower().Trim()))
                {
                    result = true;
                    break;
                }
            }
        }

        return result;
    }
}
