using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a lightweight Options overlay (hazards toggle) when the Options menu opens.
/// Options was previously only a Main Menu button with no panel content.
/// </summary>
public class OptionsMenuBootstrap : MonoBehaviour
{
    static OptionsMenuBootstrap instance;
    GameObject panel;
    Toggle hazardsToggle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (instance != null)
            return;
        GameObject go = new GameObject("OptionsMenuBootstrap");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<OptionsMenuBootstrap>();
    }

    void Update()
    {
        MenuManager mm = MenuManager.instance;
        if (mm == null)
            return;

        // Options is an overlay — check the raw open stack, not non-overlay GetOpenMenu(true)
        bool optionsOpen = false;
        if (mm.openMenu != null)
        {
            for (int i = 0; i < mm.openMenu.Count; i++)
            {
                if (mm.openMenu[i] != null
                    && mm.openMenu[i].Equals("Options", System.StringComparison.OrdinalIgnoreCase))
                {
                    optionsOpen = true;
                    break;
                }
            }
        }

        if (optionsOpen)
        {
            if (panel == null)
                BuildPanel();
            if (panel != null && !panel.activeSelf)
            {
                panel.SetActive(true);
                SyncToggles();
            }
        }
        else if (panel != null && panel.activeSelf)
        {
            panel.SetActive(false);
        }
    }

    void SyncToggles()
    {
        GameSettings.EnsureLoaded();
        if (hazardsToggle != null)
            hazardsToggle.SetIsOnWithoutNotify(GameSettings.HazardsEnabled);
    }

    void BuildPanel()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        panel = new GameObject("Options Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(420f, 220f);
        rt.anchoredPosition = Vector2.zero;

        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.14f, 0.94f);

        Text title = MakeText(panel.transform, "Options", 28, new Vector2(0f, 70f), new Vector2(380f, 40f));
        title.alignment = TextAnchor.MiddleCenter;

        GameObject row = new GameObject("Hazards Row", typeof(RectTransform));
        row.transform.SetParent(panel.transform, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.sizeDelta = new Vector2(360f, 40f);
        rowRt.anchoredPosition = new Vector2(0f, 10f);

        Text label = MakeText(row.transform, "Hazards", 22, new Vector2(-60f, 0f), new Vector2(200f, 36f));
        label.alignment = TextAnchor.MiddleLeft;

        GameObject toggleGo = new GameObject("Hazards Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
        toggleGo.transform.SetParent(row.transform, false);
        RectTransform tRt = toggleGo.GetComponent<RectTransform>();
        tRt.anchorMin = tRt.anchorMax = new Vector2(0.5f, 0.5f);
        tRt.sizeDelta = new Vector2(36f, 36f);
        tRt.anchoredPosition = new Vector2(140f, 0f);
        Image tBg = toggleGo.GetComponent<Image>();
        tBg.color = new Color(0.25f, 0.28f, 0.35f, 1f);

        GameObject check = new GameObject("Check", typeof(RectTransform), typeof(Image));
        check.transform.SetParent(toggleGo.transform, false);
        RectTransform cRt = check.GetComponent<RectTransform>();
        cRt.anchorMin = Vector2.zero;
        cRt.anchorMax = Vector2.one;
        cRt.offsetMin = new Vector2(6f, 6f);
        cRt.offsetMax = new Vector2(-6f, -6f);
        Image cImg = check.GetComponent<Image>();
        cImg.color = new Color(0.35f, 0.85f, 0.45f, 1f);

        hazardsToggle = toggleGo.GetComponent<Toggle>();
        hazardsToggle.targetGraphic = tBg;
        hazardsToggle.graphic = cImg;
        hazardsToggle.isOn = GameSettings.HazardsEnabled;
        hazardsToggle.onValueChanged.AddListener(v => GameSettings.HazardsEnabled = v);

        Text hint = MakeText(panel.transform,
            "When off, only walls and borders spawn.\nInteractive level objects are skipped.",
            14, new Vector2(0f, -50f), new Vector2(380f, 50f));
        hint.alignment = TextAnchor.MiddleCenter;
        hint.color = new Color(0.75f, 0.78f, 0.85f, 1f);

        // Close affordance — Cancel / Esc still handled by menu stack; also a button
        GameObject close = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
        close.transform.SetParent(panel.transform, false);
        RectTransform clRt = close.GetComponent<RectTransform>();
        clRt.anchorMin = clRt.anchorMax = new Vector2(0.5f, 0.5f);
        clRt.sizeDelta = new Vector2(120f, 36f);
        clRt.anchoredPosition = new Vector2(0f, -90f);
        close.GetComponent<Image>().color = new Color(0.35f, 0.4f, 0.5f, 1f);
        Text closeTxt = MakeText(close.transform, "Back", 18, Vector2.zero, new Vector2(120f, 36f));
        closeTxt.alignment = TextAnchor.MiddleCenter;
        close.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (MenuManager.instance != null)
                MenuManager.instance.RemoveMenu("Options");
            if (panel != null)
                panel.SetActive(false);
        });

        // Ensure Options exists in MenuManager so OpenMenu("Options") works
        EnsureOptionsMenuRegistered();
        panel.SetActive(false);
    }

    void EnsureOptionsMenuRegistered()
    {
        MenuManager mm = MenuManager.instance;
        if (mm == null || mm.menus == null)
            return;
        if (mm.FindMenu("Options") != null)
            return;
        mm.menus.Add(new MenuClass("Options", true, panel));
    }

    static Text MakeText(Transform parent, string value, int size, Vector2 pos, Vector2 delta)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = delta;
        rt.anchoredPosition = pos;
        Text t = go.GetComponent<Text>();
        t.text = value;
        t.fontSize = size;
        t.color = Color.white;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null)
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return t;
    }
}
