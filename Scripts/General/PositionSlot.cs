using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Clickable spawn-side slot for Position Select (facing + inner/outer).
/// </summary>
public class PositionSlot : MonoBehaviour
{
    public Facing facing = Facing.Up;
    public int slot = 0; // 0 = primary, 1 = secondary on that side

    Image bg;
    Text label;
    Outline outline;

    void Awake()
    {
        bg = GetComponent<Image>();
        outline = GetComponent<Outline>();
        label = GetComponentInChildren<Text>();
    }

    public void SetupVisual()
    {
        if (label != null)
        {
            string side = facing.ToString();
            label.text = side + (slot == 0 ? " A" : " B");
        }
    }

    public void OnClick(int player)
    {
        Database db = Database.instance;
        if (db == null) return;

        Player p = db.players.Find(x => x.index == player);
        if (p == null || p.characterSelected) return;

        // Swap with whoever already owns this slot
        Player occupant = db.players.Find(x => x != p && x.facing == facing && x.position == slot);
        Facing oldFacing = p.facing;
        int oldSlot = p.position;

        p.facing = facing;
        p.position = slot;

        if (occupant != null && !occupant.characterSelected)
        {
            occupant.facing = oldFacing;
            occupant.position = oldSlot;
        }

        PositionSelect ps = PositionSelect.instance;
        if (ps != null)
            ps.RefreshMarkers();
    }

    public void SetOccupiedLook(bool occupied, Color playerColor)
    {
        if (bg != null)
        {
            bg.color = occupied
                ? new Color(playerColor.r, playerColor.g, playerColor.b, 0.55f)
                : new Color(1f, 1f, 1f, 0.2f);
        }
        if (outline != null)
        {
            outline.effectColor = occupied ? playerColor : new Color(0.1f, 0.35f, 0.95f, 1f);
        }
    }
}
