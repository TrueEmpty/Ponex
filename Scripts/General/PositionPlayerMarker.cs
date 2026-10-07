using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// P1 / CPU2 chip on Position Select. Works with Ponex cursor (OnClick pick-up)
/// and optional mouse EventSystem drag/drop onto a <see cref="PositionSlot"/>.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PositionPlayerMarker : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int playerIndex = -1;

    RectTransform rect;
    CanvasGroup canvasGroup;
    Transform homeParent;
    Vector2 homeAnchored;
    int homeSibling;
    bool dragging;
    bool cursorHeld;

    public bool IsDragging => dragging || cursorHeld;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Text t = GetComponent<Text>();
        if (t != null)
            t.raycastTarget = true;

        Image img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.01f);
        }
        img.raycastTarget = true;

        if (GetComponent<BoxCollider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(80f, 40f, 1f);
        }
    }

    void LateUpdate()
    {
        if (!cursorHeld || rect == null)
            return;

        PositionSelect ps = PositionSelect.instance;
        if (ps == null || ps.heldPlayerIndex != playerIndex)
        {
            cursorHeld = false;
            return;
        }

        PlayerSelectorObj pso = FindCarrier();
        if (pso != null)
            rect.position = pso.transform.position;
    }

    PlayerSelectorObj FindCarrier()
    {
        Database db = Database.instance;
        if (db == null || playerIndex < 0)
            return null;
        Player p = db.players.Find(x => x.index == playerIndex);
        if (p != null && p.pso != null)
            return p.pso;
        // Fallback: any active selector
        return FindAnyObjectByType<PlayerSelectorObj>();
    }

    /// <summary>Ponex cursor short-press — pick up / put down this chip.</summary>
    public void OnClick(int player)
    {
        Database db = Database.instance;
        if (db == null || playerIndex < 0)
            return;
        Player p = db.players.Find(x => x.index == playerIndex);
        if (p == null || p.characterSelected)
            return;

        PositionSelect ps = PositionSelect.instance;
        if (ps == null)
            return;

        if (ps.heldPlayerIndex == playerIndex)
        {
            // Put down — snap home via refresh
            ps.heldPlayerIndex = -1;
            cursorHeld = false;
            ps.RefreshMarkers();
            return;
        }

        ps.heldPlayerIndex = playerIndex;
        cursorHeld = true;
        homeParent = rect.parent;
        homeAnchored = rect.anchoredPosition;
        homeSibling = rect.GetSiblingIndex();
    }

    public void ClearCursorHold()
    {
        cursorHeld = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Database db = Database.instance;
        if (db == null || playerIndex < 0)
            return;
        Player p = db.players.Find(x => x.index == playerIndex);
        if (p == null || p.characterSelected)
            return;

        dragging = true;
        homeParent = rect.parent;
        homeAnchored = rect.anchoredPosition;
        homeSibling = rect.GetSiblingIndex();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            rect.SetParent(canvas.transform, true);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.85f;

        PositionSelect ps = PositionSelect.instance;
        if (ps != null)
            ps.heldPlayerIndex = playerIndex;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || rect == null)
            return;
        RectTransform parent = rect.parent as RectTransform;
        if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, eventData.position, eventData.pressEventCamera, out Vector2 local))
        {
            rect.localPosition = local;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;
        dragging = false;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        if (homeParent != null)
        {
            rect.SetParent(homeParent, false);
            rect.anchoredPosition = homeAnchored;
            rect.SetSiblingIndex(homeSibling);
        }

        PositionSelect ps = PositionSelect.instance;
        if (ps != null)
        {
            // If drop didn't assign, clear hold; AssignPlayer clears heldPlayerIndex
            if (ps.heldPlayerIndex == playerIndex)
                ps.heldPlayerIndex = -1;
            ps.RefreshMarkers();
        }
    }
}
