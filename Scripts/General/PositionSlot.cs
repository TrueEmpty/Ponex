using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Clickable / droppable spawn-side slot for Position Select (facing + inner/outer).
/// One player per spot; drop or click takes the slot (swaps if occupied and not ready).
/// </summary>
public class PositionSlot : MonoBehaviour, IDropHandler
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
        if (bg != null)
            bg.raycastTarget = true;
    }

    public void SetupVisual()
    {
        if (label != null)
        {
            string side = facing.ToString();
            label.text = side + (slot == 0 ? " A" : " B");
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        PositionPlayerMarker marker = eventData.pointerDrag.GetComponent<PositionPlayerMarker>();
        if (marker == null || marker.playerIndex < 0)
            return;

        AssignPlayer(marker.playerIndex);
    }

    public void OnClick(int player)
    {
        PositionSelect ps = PositionSelect.instance;
        int target = (ps != null && ps.heldPlayerIndex >= 0) ? ps.heldPlayerIndex : player;
        AssignPlayer(target);
    }

    public void AssignPlayer(int player)
    {
        Database db = Database.instance;
        if (db == null) return;

        Player p = db.players.Find(x => x.index == player);
        if (p == null || p.characterSelected) return;

        // One player per spot: swap with occupant (if not locked), else take empty
        Player occupant = db.players.Find(x => x != p && x.facing == facing && x.position == slot);
        if (occupant != null && occupant.characterSelected)
            return; // locked occupant keeps the spot

        Facing oldFacing = p.facing;
        int oldSlot = p.position;

        p.facing = facing;
        p.position = slot;

        if (occupant != null)
        {
            occupant.facing = oldFacing;
            occupant.position = oldSlot;
        }

        // Enforce uniqueness in case of stale duplicates
        for (int i = 0; i < db.players.Count; i++)
        {
            Player other = db.players[i];
            if (other == null || other == p) continue;
            if (other.facing == facing && other.position == slot && other != occupant)
            {
                // Kick stray duplicate to opposite secondary if needed
                other.position = slot == 0 ? 1 : 0;
            }
        }

        PositionSelect ps = PositionSelect.instance;
        if (ps != null)
        {
            ps.heldPlayerIndex = -1;
            PositionPlayerMarker[] all = ps.GetComponentsInChildren<PositionPlayerMarker>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null)
                    all[i].ClearCursorHold();
            }
            ps.RefreshMarkers();
        }

        if (db != null)
            db.RememberAllLobbySeats();
    }

    public void SetOccupiedLook(bool occupied, Color playerColor, string occupantLabel = null)
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
        if (label != null)
        {
            if (occupied && !string.IsNullOrEmpty(occupantLabel))
                label.text = occupantLabel;
            else
                label.text = facing + (slot == 0 ? " A" : " B");
        }
    }
}
