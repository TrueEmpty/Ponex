using UnityEngine;
using UnityEngine.UI;

public class StoryPauseMenu : MonoBehaviour
{
    static StoryPauseMenu current;

    public static bool IsOpen => current != null;

    public static void Open(Transform canvas)
    {
        if (current != null || canvas == null)
            return;
        GameObject go = new GameObject("Story Pause", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(canvas, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        current = go.AddComponent<StoryPauseMenu>();
        current.Build(rt);

        Database db = Database.instance;
        if (db != null)
        {
            db.storyPauseOpen = true;
            db.storySharedCursor = true;
        }
    }

    public static void Close()
    {
        Database db = Database.instance;
        if (db != null)
        {
            db.storyPauseOpen = false;
            db.storySharedCursor = false;
        }
        if (current != null)
            Destroy(current.gameObject);
        current = null;
    }

    void Build(RectTransform panel)
    {
        Image dim = gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.45f);
        dim.raycastTarget = false;

        Database db = Database.instance;
        bool skip = db != null && db.skipStoryCutscenes;
        Make("Resume", new Vector2(0f, 80f), new Color(0.2f, 0.5f, 0.3f), _ => Close());
        Make(skip ? "Skip Cutscenes: On" : "Skip Cutscenes: Off", new Vector2(0f, 0f), new Color(0.3f, 0.35f, 0.45f), _ =>
        {
            StoryProgress.SetSkipCutscenes(!(Database.instance != null && Database.instance.skipStoryCutscenes));
            Close();
            Transform canvas = panel.parent;
            Open(canvas);
        });
        Make("Exit Story Mode", new Vector2(0f, -80f), new Color(0.55f, 0.22f, 0.22f), _ => StorySession.ExitToMenu());
    }

    void Make(string label, Vector2 pos, Color color, System.Action<int> click)
    {
        GameObject go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(StoryButton));
        go.layer = 5;
        go.transform.SetParent(transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(460f, 64f);
        rt.anchoredPosition = pos;
        Image image = go.GetComponent<Image>();
        image.color = color;
        StoryButton button = go.GetComponent<StoryButton>();
        button.SetBaseColor(color);
        button.clicked = click;

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.layer = 5;
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        Text text = textGo.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 28;
        text.color = Color.white;
        text.raycastTarget = false;
    }
}
