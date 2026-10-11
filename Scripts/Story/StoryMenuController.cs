using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoryMenuController : MonoBehaviour
{
    enum Page
    {
        Hidden,
        Characters,
        SaveChoice,
        Chapters
    }

    Database db;
    RectTransform root;
    Page page = Page.Hidden;
    string picked;
    bool builtForOpen;

    void Update()
    {
        if (db == null)
            db = Database.instance;
        MenuManager mm = MenuManager.instance;
        bool open = mm != null && mm.IsMenuVisible("Story");
        if (!open)
        {
            if (page != Page.Hidden)
                Clear();
            builtForOpen = false;
            return;
        }

        if (builtForOpen)
            return;
        builtForOpen = true;

        if (StoryProgress.ShouldSkipMenus(db))
        {
            bool loadPos = StoryProgress.HasSave("Test");
            StorySession.Begin("Test", 0, loadPos);
            return;
        }

        ShowCharacters();
    }

    void ShowCharacters()
    {
        page = Page.Characters;
        RectTransform panel = Prepare("Whose story?");
        List<string> names = StoryProgress.PlayableCharacters(db);
        float y = 180f;
        for (int i = 0; i < names.Count; i++)
        {
            string name = names[i];
            MakeButton(panel, name, new Vector2(0f, y), new Color(0.2f, 0.45f, 0.75f), _ => PickCharacter(name));
            y -= 80f;
        }

        string skipLabel = db != null && db.skipStoryCutscenes ? "Skip Cutscenes: On" : "Skip Cutscenes: Off";
        MakeButton(panel, skipLabel, new Vector2(0f, y - 20f), new Color(0.35f, 0.35f, 0.4f), _ =>
        {
            StoryProgress.SetSkipCutscenes(!(db != null && db.skipStoryCutscenes));
            ShowCharacters();
        });
        MakeButton(panel, "Back", new Vector2(0f, -280f), new Color(0.45f, 0.2f, 0.2f), _ =>
        {
            Clear();
            if (MenuManager.instance != null)
                MenuManager.instance.BackMenu();
        });
    }

    void PickCharacter(string name)
    {
        picked = name;
        if (StoryProgress.HasSave(name))
            ShowSaveChoice();
        else
            ShowChapters();
    }

    void ShowSaveChoice()
    {
        page = Page.SaveChoice;
        RectTransform panel = Prepare(picked + " has a save");
        MakeButton(panel, "Load Save", new Vector2(0f, 60f), new Color(0.2f, 0.55f, 0.3f), _ =>
        {
            StorySession.Begin(picked, StoryProgress.SavedChapter(picked), true);
        });
        MakeButton(panel, "Select Chapter", new Vector2(0f, -40f), new Color(0.2f, 0.45f, 0.75f), _ => ShowChapters());
        MakeButton(panel, "Back", new Vector2(0f, -180f), new Color(0.45f, 0.2f, 0.2f), _ => ShowCharacters());
    }

    void ShowChapters()
    {
        page = Page.Chapters;
        RectTransform panel = Prepare(picked + " chapters");
        List<StoryChapter> chapters = StoryProgress.ChaptersFor(picked);
        float y = 160f;
        for (int i = 0; i < chapters.Count; i++)
        {
            StoryChapter chapter = chapters[i];
            bool open = StoryProgress.ChapterUnlocked(chapter.character, chapter.index);
            string label = open ? chapter.title : chapter.title + " (Locked)";
            Color color = open ? new Color(0.25f, 0.4f, 0.7f) : new Color(0.3f, 0.3f, 0.3f);
            MakeButton(panel, label, new Vector2(0f, y), color, _ =>
            {
                if (!open)
                    return;
                StoryProgress.WriteChapterSave(picked, chapter.index);
                StorySession.Begin(picked, chapter.index, false);
            });
            y -= 80f;
        }
        MakeButton(panel, "Back", new Vector2(0f, -280f), new Color(0.45f, 0.2f, 0.2f), _ =>
        {
            if (StoryProgress.HasSave(picked))
                ShowSaveChoice();
            else
                ShowCharacters();
        });
    }

    RectTransform Prepare(string title)
    {
        Clear();
        page = page == Page.Hidden ? Page.Characters : page;
        root = FindStoryHolder();
        if (root == null)
            return null;

        Text heading = MakeText(root, title, 42, FontStyle.Bold);
        RectTransform headingRt = heading.rectTransform;
        headingRt.anchorMin = headingRt.anchorMax = new Vector2(0.5f, 0.5f);
        headingRt.anchoredPosition = new Vector2(0f, 300f);
        headingRt.sizeDelta = new Vector2(900f, 80f);
        return root;
    }

    void Clear()
    {
        if (root == null)
            root = FindStoryHolder();
        if (root == null)
            return;
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    RectTransform FindStoryHolder()
    {
        MenuManager mm = MenuManager.instance;
        if (mm == null)
            return null;
        MenuClass menu = mm.FindMenu("Story");
        if (menu == null || menu.holder == null)
            return null;
        return menu.holder.GetComponent<RectTransform>();
    }

    StoryButton MakeButton(RectTransform parent, string label, Vector2 pos, Color color, System.Action<int> click)
    {
        GameObject go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(StoryButton));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520f, 64f);
        rt.anchoredPosition = pos;
        Image image = go.GetComponent<Image>();
        image.color = color;
        StoryButton button = go.GetComponent<StoryButton>();
        button.SetBaseColor(color);
        button.clicked = click;
        Text text = MakeText(rt, label, 28, FontStyle.Normal);
        text.color = Color.white;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    static Text MakeText(Transform parent, string value, int size, FontStyle style)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.95f, 0.95f, 0.95f);
        text.raycastTarget = false;
        return text;
    }
}
