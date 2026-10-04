using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PositionSelect : MonoBehaviour
{
    public static PositionSelect instance;
    Database db;
    MenuManager mm;

    public Color outlineColor = Color.blue;
    public Color readyColor = Color.green;

    public Vector3 setPos = new Vector3(225, 175, 25);

    public List<PositionSlot> slots = new List<PositionSlot>();
    readonly List<Text> markers = new List<Text>();

    Transform markerHolder;
    bool setup;

    private void Awake()
    {
        if (instance != null)
            Destroy(this);
        else
            instance = this;
    }

    void Start()
    {
        db = Database.instance;
        mm = MenuManager.instance;
    }

    void Update()
    {
        if (!setup)
            Setup();
        else
            RefreshMarkers();
    }

    void Setup()
    {
        if (setup) return;

        // Authored PositionSlot components under Zones / menu (baked into the UI)
        if (slots == null || slots.Count == 0)
        {
            slots = new List<PositionSlot>();
            PositionSlot[] found = GetComponentsInChildren<PositionSlot>(true);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null)
                    slots.Add(found[i]);
            }
        }

        if (slots.Count == 0)
            Debug.LogWarning("PositionSelect: no PositionSlot components found. Run Tools/Ponex/Bake Selector UI.");

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
                slots[i].SetupVisual();
        }

        markerHolder = FindChildNamed(transform, "Markers");
        if (markerHolder == null && transform.childCount > 2)
            markerHolder = transform.GetChild(2);

        markers.Clear();
        if (markerHolder != null)
        {
            for (int i = 0; i < markerHolder.childCount; i++)
            {
                Text t = markerHolder.GetChild(i).GetComponent<Text>();
                if (t != null)
                    markers.Add(t);
            }
        }

        setup = true;
        RefreshMarkers();
    }

    Transform FindChildNamed(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform f = FindChildNamed(root.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }

    public void RefreshMarkers()
    {
        if (db == null) return;

        for (int i = 0; i < markers.Count; i++)
        {
            Text marker = markers[i];
            if (i < db.players.Count)
            {
                Player p = db.players[i];
                marker.gameObject.SetActive(true);

                Color c = i < db.playerColors.Count ? db.playerColors[i].color : Color.white;
                if (p.computer)
                {
                    marker.text = "CPU" + (i + 1);
                    marker.color = Color.gray;
                }
                else
                {
                    marker.text = string.IsNullOrEmpty(p.nickName) ? "P" + (i + 1) : p.nickName;
                    marker.color = c;
                }

                Outline ol = marker.GetComponent<Outline>();
                if (ol != null)
                    ol.effectColor = p.characterSelected ? readyColor : outlineColor;

                Vector2 cP = new Vector2(p.position == 0 ? setPos.z : -setPos.z, p.position == 0 ? setPos.x : setPos.y);
                switch (p.facing)
                {
                    case Facing.Up:
                        marker.rectTransform.anchoredPosition = new Vector2(cP.x, -cP.y);
                        break;
                    case Facing.Down:
                        marker.rectTransform.anchoredPosition = new Vector2(cP.x, cP.y);
                        break;
                    case Facing.Right:
                        marker.rectTransform.anchoredPosition = new Vector2(-cP.y, cP.x);
                        break;
                    case Facing.Left:
                        marker.rectTransform.anchoredPosition = new Vector2(cP.y, cP.x);
                        break;
                }
            }
            else
            {
                marker.gameObject.SetActive(false);
            }
        }

        for (int s = 0; s < slots.Count; s++)
        {
            PositionSlot slot = slots[s];
            if (slot == null) continue;
            Player owner = db.players.Find(x => x.facing == slot.facing && x.position == slot.slot);
            if (owner != null)
            {
                Color c = owner.index < db.playerColors.Count ? db.playerColors[owner.index].color : Color.white;
                slot.SetOccupiedLook(true, c);
            }
            else
            {
                slot.SetOccupiedLook(false, Color.white);
            }
        }
    }

    public void PlayerConfirm(int player)
    {
        Player p = db.players.Find(x => x.index == player);
        if (p == null) return;

        p.characterSelected = true;
        RefreshMarkers();

        if (!db.players.Exists(x => !x.characterSelected))
            db.CharactersPicked("positions");
    }
}
