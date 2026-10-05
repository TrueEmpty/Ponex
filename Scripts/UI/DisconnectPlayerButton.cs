using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Long-press: drop the clicking controller + selector.
/// In Character Select (or Main Menu) removes that player; past Character Select converts them to CPU.
/// </summary>
public class DisconnectPlayerButton : MonoBehaviour
{
    const string ButtonName = "Leave";

    Text label;

    public static void EnsureExists(Transform parent)
    {
        if (parent == null)
            return;

        Transform existing = parent.Find(ButtonName);
        if (existing != null)
        {
            if (existing.GetComponent<DisconnectPlayerButton>() == null)
                existing.gameObject.AddComponent<DisconnectPlayerButton>();
            SelectorClickable.Ensure(existing.gameObject, 170f);
            return;
        }

        GameObject go = new GameObject(ButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5;
        go.tag = "Selection";
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(36f, 36f);
        rt.sizeDelta = new Vector2(170f, 55f);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.85f, 0.22f, 0.18f, 1f);
        img.raycastTarget = true;

        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(0.4f, 0.05f, 0.05f, 1f);
        ol.effectDistance = new Vector2(3.5f, 3.5f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8f, 4f);
        trt.offsetMax = new Vector2(-8f, -4f);

        Text text = textGo.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 18;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = 22;
        text.raycastTarget = false;
        text.text = "Leave\n(Hold)";

        ButtonInteraction bi = go.AddComponent<ButtonInteraction>();
        bi.highlightColor = new Color(1f, 0.4f, 0.35f, 1f);
        bi.pressedColor = new Color(0.5f, 0.08f, 0.05f, 1f);

        DisconnectPlayerButton btn = go.AddComponent<DisconnectPlayerButton>();
        btn.label = text;
        SelectorClickable.Ensure(go, 170f);
        go.transform.SetAsLastSibling();
    }

    void Awake()
    {
        if (label == null)
        {
            Text[] texts = GetComponentsInChildren<Text>(true);
            if (texts != null && texts.Length > 0)
                label = texts[0];
        }
        SelectorClickable.Ensure(gameObject, 170f);
    }

    void Update()
    {
        if (label == null)
            return;

        Database db = Database.instance;
        if (db == null)
            return;

        label.text = db.IsPastCharacterSelect()
            ? "Go CPU\n(Hold)"
            : "Leave\n(Hold)";
    }

    public void OnLongClick(int player)
    {
        Database db = Database.instance;
        if (db == null)
            return;

        db.DisconnectPlayer(ResolveHumanOwner(db, player));
    }

    /// <summary>
    /// ClickPlayerIndex can be a CPU when a human is piloting one — always drop the human owner.
    /// </summary>
    static int ResolveHumanOwner(Database db, int clickPlayer)
    {
        if (db.players == null)
            return clickPlayer;

        Player direct = db.players.Find(x => x.index == clickPlayer);
        if (direct != null && !direct.computer)
            return clickPlayer;

        Player owner = db.players.Find(x =>
            x != null &&
            !x.computer &&
            x.pso != null &&
            x.pso.cpuControl == clickPlayer);
        if (owner != null)
            return owner.index;

        return clickPlayer;
    }
}
