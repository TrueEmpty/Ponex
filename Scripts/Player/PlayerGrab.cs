using UnityEngine;

// Run before character scripts so tf_super / tf_dash edges are valid the same frame
[DefaultExecutionOrder(-100)]
public class PlayerGrab : MonoBehaviour
{
    Database db;
    public int playerIndex = -1;

    public Inputs inp = new Inputs();
    float deadzone = .7f;

    void Start()
    {
        db = Database.instance;
    }

    void Update()
    {
        UpdateInputs();
    }

    void UpdateInputs()
    {
        Player p = player;

        if (p == null)
            return;

        ControllerLink cL = p.cLink;
        if (cL == null)
            return;

        bool lf_r = inp.right;
        bool lf_l = inp.left;
        bool lf_u = inp.up;
        bool lf_d = inp.down;
        bool lf_dashL = inp.dashLeft;
        bool lf_dashR = inp.dashRight;
        bool lf_super = inp.superHeld;

        // Move stick / D-pad
        bool r = PressedAxis(cL, "Move", 1, 0);
        bool l = PressedAxis(cL, "Move", -1, 0);
        bool u = PressedAxis(cL, "Move", 0, 1);
        bool d = PressedAxis(cL, "Move", 0, -1);

        bool bumpBtn = IsPressed(cL, "Jump");
        bool superBtn = IsPressed(cL, "Interact");
        bool laneRightBtn = IsPressed(cL, "Crouch");
        bool laneLeftBtn = IsPressed(cL, "Attack");
        bool dashLeftBtn = IsPressed(cL, "DashLeft");
        bool dashRightBtn = IsPressed(cL, "DashRight");

        // Facing-relative stick "back" (toward your wall) — separate from Interact
        bool stickBack;
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

        // Movement axes (bump/super buttons do NOT get remapped into lane left/right)
        if (p.ignoreFacing)
        {
            inp.up = u || bumpBtn;
            inp.down = d || superBtn;
            inp.right = r || laneRightBtn;
            inp.left = l || laneLeftBtn;
        }
        else
        {
            switch (p.facing)
            {
                case Facing.Up:
                    inp.up = u || bumpBtn;
                    inp.down = d || superBtn;
                    inp.right = r || laneRightBtn;
                    inp.left = l || laneLeftBtn;
                    break;
                case Facing.Down:
                    inp.up = d || bumpBtn;
                    inp.down = u || superBtn;
                    inp.right = r || laneRightBtn;
                    inp.left = l || laneLeftBtn;
                    break;
                case Facing.Left:
                    inp.up = r || bumpBtn;
                    inp.down = l || superBtn;
                    inp.right = d || laneRightBtn;
                    inp.left = u || laneLeftBtn;
                    break;
                case Facing.Right:
                    inp.up = l || bumpBtn;
                    inp.down = r || superBtn;
                    inp.right = u || laneRightBtn;
                    inp.left = d || laneLeftBtn;
                    break;
            }
        }

        // Dedicated super channel: Interact and/or stick-back (for hold-charge chars like Bahrue)
        inp.superHeld = stickBack || superBtn;
        inp.dashLeft = dashLeftBtn;
        inp.dashRight = dashRightBtn;

        inp.tf_right = !lf_r && inp.right;
        inp.tf_left = !lf_l && inp.left;
        inp.tf_up = !lf_u && inp.up;
        inp.tf_down = !lf_d && inp.down;
        inp.tf_dashLeft = !lf_dashL && inp.dashLeft;
        inp.tf_dashRight = !lf_dashR && inp.dashRight;

        // Super press edge — tracked on its own channel (not Enough(), not movement alone)
        inp.tf_super = !lf_super && inp.superHeld;
    }

    bool IsPressed(ControllerLink cL, string action)
    {
        ControllerButtons b = cL[action];
        return b != null && b.isPressed;
    }

    bool PressedAxis(ControllerLink cL, string action, int xSign, int ySign)
    {
        ControllerButtons b = cL[action];
        if (b == null)
            return false;

        if (xSign > 0) return b.value.x > deadzone;
        if (xSign < 0) return b.value.x < -deadzone;
        if (ySign > 0) return b.value.y > deadzone;
        if (ySign < 0) return b.value.y < -deadzone;
        return false;
    }

    public Player player
    {
        get
        {
            if (db == null)
                db = Database.instance;
            if (db == null || playerIndex < 0 || playerIndex >= db.players.Count)
                return null;
            return db.players[playerIndex];
        }
    }

    public bool IsLinked()
    {
        return player != null;
    }
}

public class Inputs
{
    public bool right = false;
    public bool left = false;
    public bool up = false;
    public bool down = false;
    public bool dashLeft = false;
    public bool dashRight = false;

    /// <summary>Super held (Interact and/or stick-back). Use for hold-to-charge (Bahrue).</summary>
    public bool superHeld = false;

    public bool bump => up;
    public bool super => superHeld;

    public bool tf_right = false;
    public bool tf_left = false;
    public bool tf_up = false;
    public bool tf_down = false;
    public bool tf_dashLeft = false;
    public bool tf_dashRight = false;

    /// <summary>Super pressed this frame. Activation must use this + Enough(), never Enough() alone.</summary>
    public bool tf_super = false;

    public bool tf_bump => tf_up;
}
