using UnityEngine;

// Run before character scripts so tf_super / tf_dash edges are valid the same frame
[DefaultExecutionOrder(-100)]
public class PlayerGrab : MonoBehaviour
{
    Database db;
    public int playerIndex = -1;

    public Inputs inp = new Inputs();
    float deadzone = .7f;
    bool skinApplied = false;

    void Start()
    {
        db = Database.instance;
        TryApplySkin();
    }

    void LateUpdate()
    {
        // playerIndex is often set the same frame after Instantiate — apply once linked
        TryApplySkin();
    }

    void Update()
    {
        UpdateInputs();
    }

    void TryApplySkin()
    {
        if (skinApplied || playerIndex < 0)
            return;

        // Ball ownership changes — don't recolor the ball to the owner's skin
        if (CompareTag("Ball"))
        {
            skinApplied = true;
            return;
        }

        Player p = player;
        if (p == null)
            return;

        PlayerSkin.Apply(gameObject, p);
        skinApplied = true;
    }

    void UpdateInputs()
    {
        Player p = player;

        if (p == null)
            return;

        // Merge every device on this slot (Character Creation: keyboard + pads together)
        System.Collections.Generic.List<ControllerLink> links = CollectInputLinks(p);
        if (links.Count == 0)
            return;

        bool lf_r = inp.right;
        bool lf_l = inp.left;
        bool lf_u = inp.up;
        bool lf_d = inp.down;
        bool lf_dashL = inp.dashLeft;
        bool lf_dashR = inp.dashRight;
        bool lf_super = inp.superHeld;
        bool lf_bump = inp.bumpHeld;

        bool r = false, l = false, u = false, d = false;
        bool bumpBtn = false, superBtn = false, laneRightBtn = false, laneLeftBtn = false;
        bool dashLeftBtn = false, dashRightBtn = false;
        for (int i = 0; i < links.Count; i++)
        {
            ControllerLink cL = links[i];
            if (cL == null)
                continue;
            r |= PressedAxis(cL, "Move", 1, 0);
            l |= PressedAxis(cL, "Move", -1, 0);
            u |= PressedAxis(cL, "Move", 0, 1);
            d |= PressedAxis(cL, "Move", 0, -1);
            bumpBtn |= IsPressed(cL, "Jump");
            superBtn |= IsPressed(cL, "Interact");
            laneRightBtn |= IsPressed(cL, "Crouch");
            laneLeftBtn |= IsPressed(cL, "Attack");
            dashLeftBtn |= IsPressed(cL, "DashLeft");
            dashRightBtn |= IsPressed(cL, "DashRight");
        }

        // Directions are the stick, d-pad, and WASD only. Jump (south) and Interact (north) are not movement.
        if (p.ignoreFacing)
        {
            inp.up = u;
            inp.down = d;
            inp.right = r || laneRightBtn;
            inp.left = l || laneLeftBtn;
        }
        else
        {
            switch (p.facing)
            {
                case Facing.Up:
                    inp.up = u;
                    inp.down = d;
                    inp.right = r || laneRightBtn;
                    inp.left = l || laneLeftBtn;
                    break;
                case Facing.Down:
                    inp.up = d;
                    inp.down = u;
                    inp.right = r || laneRightBtn;
                    inp.left = l || laneLeftBtn;
                    break;
                case Facing.Left:
                    inp.up = r;
                    inp.down = l;
                    inp.right = d || laneRightBtn;
                    inp.left = u || laneLeftBtn;
                    break;
                case Facing.Right:
                    inp.up = l;
                    inp.down = r;
                    inp.right = u || laneRightBtn;
                    inp.left = d || laneLeftBtn;
                    break;
            }
        }

        inp.bumpHeld = bumpBtn;

        // North / Interact only. Pulling the stick back is not super.
        inp.superHeld = superBtn;
        inp.dashLeft = dashLeftBtn;
        inp.dashRight = dashRightBtn;

        inp.tf_right = !lf_r && inp.right;
        inp.tf_left = !lf_l && inp.left;
        inp.tf_up = !lf_u && inp.up;
        inp.tf_down = !lf_d && inp.down;
        inp.tf_dashLeft = !lf_dashL && inp.dashLeft;
        inp.tf_dashRight = !lf_dashR && inp.dashRight;
        inp.tf_bump = !lf_bump && inp.bumpHeld;

        // Super press edge — tracked on its own channel (not Enough(), not movement alone)
        inp.tf_super = !lf_super && inp.superHeld;
    }

    static System.Collections.Generic.List<ControllerLink> CollectInputLinks(Player p)
    {
        var links = new System.Collections.Generic.List<ControllerLink>(4);
        if (p.inputLinks != null)
        {
            for (int i = 0; i < p.inputLinks.Count; i++)
            {
                ControllerLink link = p.inputLinks[i];
                if (link != null && !links.Contains(link))
                    links.Add(link);
            }
        }
        if (p.cLink != null && !links.Contains(p.cLink))
            links.Add(p.cLink);
        return links;
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
            if (db == null || db.players == null || playerIndex < 0)
                return null;
            return db.players.Find(x => x != null && x.index == playerIndex);
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

    /// <summary>North / Interact held. Not a direction.</summary>
    public bool superHeld = false;

    /// <summary>Jump / south button. Not a direction.</summary>
    public bool bumpHeld = false;

    public bool bump => bumpHeld;
    public bool super => superHeld;

    public bool tf_right = false;
    public bool tf_left = false;
    public bool tf_up = false;
    public bool tf_down = false;
    public bool tf_dashLeft = false;
    public bool tf_dashRight = false;

    /// <summary>Super pressed this frame. Activation must use this + Enough(), never Enough() alone.</summary>
    public bool tf_super = false;

    /// <summary>Jump / south pressed this frame. Not the same as pushing down.</summary>
    public bool tf_bump = false;
}
