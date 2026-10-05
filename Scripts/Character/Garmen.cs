using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Garmen : MonoBehaviour
{
    PlayerGrab pg;
    Database db;

    GameObject ll;
    FollowPlayer fp;

    public Transform shootPoint;
    public GameObject projectile;
    float gbC = 0;

    public float cannonOffset = .07f;

    bool lastStickBack = false;
    bool lastAiWantSuper = false;

    [SerializeField]
    public Thought thought = Thought.Nothing;

    // Start is called before the first frame update
    void Start()
    {
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;
        fp = GetComponent<FollowPlayer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (ll == null && pg != null && pg.player != null)
            ll = pg.player.spawnedLifeline;

        if (ll == null || pg == null || pg.player == null)
            return;

        Player p = pg.player;

        if (pg.player.computer)
        {
            ComputerAI.Decision d = ComputerAI.Evaluate(transform, pg.player, null, 2.4f);
            if (d.wantBump) thought = Thought.MoveUp;
            else if (d.wantSuper) thought = Thought.MoveDown;
            else if (d.moveDir > 0) thought = Thought.MoveRight;
            else if (d.moveDir < 0) thought = Thought.MoveLeft;
            else thought = Thought.Nothing;
        }

        if (db != null && db.gameStart && p.currentHealth > 0)
        {
            if (pg.player.CanBump)
            {
                if ((pg.inp.up || thought == Thought.MoveUp) && p.bump.amount >= p.bump.cost && gbC >= .25f)
                {
                    GameObject pro = Instantiate(projectile, shootPoint.position, shootPoint.rotation);

                    PlayerGrab pPg = pro.GetComponent<PlayerGrab>();

                    if (pPg != null)
                    {
                        pPg.playerIndex = pg.playerIndex;
                    }

                    p.bump.Spend();
                    gbC = 0;
                }
            }

            gbC += Time.deltaTime;

            if (p.bump.amount < p.bump.max)
            {
                p.bump.readyPercent += Time.deltaTime / 3;

                if (p.bump.readyPercent >= 1)
                {
                    p.bump.Gain(1);
                    p.bump.readyPercent = 0;
                }
            }
        }
    }

    // LateUpdate: stay glued to lifeline (wall), then handle Drive toggle after inputs
    void LateUpdate()
    {
        if (ll == null && pg != null && pg.player != null)
            ll = pg.player.spawnedLifeline;

        if (ll != null)
            transform.position = ll.transform.position + (ll.transform.up * cannonOffset);

        if (ll == null || pg == null || pg.player == null || db == null || !db.gameStart)
            return;

        Player p = pg.player;
        if (p.currentHealth <= 0 || p.super == null)
            return;

        bool pressedSuper = ReadSuperPress(p);
        bool hasGas = p.super.Enough();

        // Exit free on press. Enter = press AND Enough()
        if (pressedSuper && p.super.readyPercent >= 1f)
        {
            p.super.readyPercent = 0f;
            thought = Thought.Nothing;
        }
        else if (pressedSuper && hasGas)
        {
            p.super.readyPercent = 1f;
            thought = Thought.Nothing;
            p.RecordUltUsed();
        }
    }

    bool ReadSuperPress(Player p)
    {
        if (p.computer)
        {
            bool want = thought == Thought.MoveDown;
            bool edge = want && !lastAiWantSuper;
            lastAiWantSuper = want;
            return edge;
        }

        // Prefer ControllerLink edges (valid in LateUpdate even if PlayerGrab ran early)
        ControllerLink cL = p.cLink;
        bool interactEdge = false;
        bool stickBack = false;
        if (cL != null)
        {
            ControllerButtons interact = cL["Interact"];
            interactEdge = interact != null && interact.wasPressedThisFrame;

            bool r = PressedAxis(cL, "Move", 1, 0);
            bool l = PressedAxis(cL, "Move", -1, 0);
            bool u = PressedAxis(cL, "Move", 0, 1);
            bool d = PressedAxis(cL, "Move", 0, -1);

            if (p.ignoreFacing)
                stickBack = d;
            else
            {
                switch (p.facing)
                {
                    case Facing.Down: stickBack = u; break;
                    case Facing.Left: stickBack = l; break;
                    case Facing.Right: stickBack = r; break;
                    default: stickBack = d; break;
                }
            }
        }

        bool stickEdge = stickBack && !lastStickBack;
        lastStickBack = stickBack;

        return interactEdge || stickEdge || pg.inp.tf_super;
    }

    static bool PressedAxis(ControllerLink cL, string action, int xSign, int ySign)
    {
        ControllerButtons b = cL[action];
        if (b == null)
            return false;

        const float deadzone = .5f;
        if (xSign > 0) return b.value.x > deadzone;
        if (xSign < 0) return b.value.x < -deadzone;
        if (ySign > 0) return b.value.y > deadzone;
        if (ySign < 0) return b.value.y < -deadzone;
        return false;
    }
}
