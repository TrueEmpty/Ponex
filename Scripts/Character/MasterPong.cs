using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MasterPong : MonoBehaviour
{
    Rigidbody rb;
    Database db;
    PlayerGrab pg;

    public List<string> hitTags = new List<string>();

    public float dis = 1.38f;

    [SerializeField]
#pragma warning disable CS0414 // Inspector AI-thought debug
    Thought thought = Thought.Nothing;
#pragma warning restore CS0414
    ComputerBrain brain;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;
        brain = ComputerBrain.Ensure(gameObject, ComputerBrain.Mode.LanePaddle);
        if (brain != null)
        {
            brain.hitTags = hitTags;
            brain.wallStopDistance = dis;
        }

        rb.useGravity = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (db.gameStart && pg.player.currentHealth > 0)
        {
            if (pg.player.CanMove)
            {
                OnMove();
            }
            else
            {
                rb.linearVelocity = Vector3.zero;
            }

            // Fast wall contact can overshoot soft stop — push out so leave-input always works
            PaddleWall.Unstick(rb, transform, hitTags);
        }
    }

    void OnMove()
    {
        int moveDir = 0;

        if (pg.player.computer)
        {
            ComputerAI.Decision d = brain != null
                ? brain.Lane
                : ComputerAI.Evaluate(transform, pg.player, hitTags, dis);
            moveDir = d.moveDir;
            thought = brain != null
                ? brain.Thought
                : (d.wantBump ? Thought.MoveUp : (d.wantSuper ? Thought.MoveDown : Thought.Nothing));
        }
        else
        {
            if (pg.inp.right)
                moveDir += 1;
            if (pg.inp.left)
                moveDir -= 1;
        }

        // Block only into-wall travel; moving away from a wall is always allowed
        if (moveDir != 0 && WallInDirection(moveDir))
            moveDir = 0;

        Vector3 rotDir = PaddleWall.AbsAxes(transform.right);
        rb.linearVelocity = rotDir * moveDir * pg.player.EffectiveMovementSpeed;
    }

    bool WallInDirection(int dir)
    {
        return PaddleWall.WallInDirection(transform, dir, hitTags, dis);
    }
}
