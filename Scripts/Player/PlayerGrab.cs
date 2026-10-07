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
