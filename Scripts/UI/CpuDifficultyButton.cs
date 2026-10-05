using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Per-portrait CPU difficulty cycle control (Character Select).
/// Plain outlined text beside the "CPUn" name — glows on highlight / click.
/// </summary>
public class CpuDifficultyButton : MonoBehaviour
{
    public const string ButtonName = "CPU Level";

    public int attachedIndex = -1;

    Text label;
    Outline outline;
    Shadow glow;

    Color baseTextColor = Color.white;
    Color baseOutlineColor = new Color(0.05f, 0.05f, 0.08f, 0.95f);

    int highlightCount;
    bool pressed;

    // Player-facing levels only — Training is reserved for AI authoring, not lobby play.
    static readonly ComputerAI.CpuDifficulty[] PlayableLevels =
    {
        ComputerAI.CpuDifficulty.Easy,
        ComputerAI.CpuDifficulty.Normal,
        ComputerAI.CpuDifficulty.Hard,
        ComputerAI.CpuDifficulty.Expert,
        ComputerAI.CpuDifficulty.Impossible,
    };

    static readonly string[] ShortNames =
    {
        "Easy",
        "Norm",
        "Hard",
        "Expert",
        "Max",
    };

    static readonly Color[] LevelColors =
    {
        new Color(0.45f, 0.85f, 0.95f, 1f), // Easy
        new Color(0.95f, 0.85f, 0.35f, 1f), // Normal
        new Color(1f, 0.55f, 0.25f, 1f),    // Hard
        new Color(0.95f, 0.35f, 0.45f, 1f), // Expert
        new Color(0.85f, 0.25f, 0.95f, 1f), // Impossible
    };

    public static CpuDifficultyButton Ensure(Transform portraitPlayerHolder, int playerIndex, Font font)
    {
        if (portraitPlayerHolder == null)
            return null;

        Transform existing = portraitPlayerHolder.Find(ButtonName);
        CpuDifficultyButton btn;
        if (existing != null)
        {
            // Rebuild older Image-based chips as text-only
            if (existing.GetComponent<Image>() != null)
            {
                Object.Destroy(existing.gameObject);
                existing = null;
            }
        }

        if (existing != null)
        {
            btn = existing.GetComponent<CpuDifficultyButton>();
            if (btn == null)
                btn = existing.gameObject.AddComponent<CpuDifficultyButton>();
            btn.attachedIndex = playerIndex;
            btn.CacheRefs();
            EnsureClickTarget(existing.gameObject);
            return btn;
        }

        GameObject go = new GameObject(ButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.layer = 5;
        go.tag = "Selection";
        go.transform.SetParent(portraitPlayerHolder, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        // Further right of "CPUn" so it doesn't overlap
        rt.anchoredPosition = new Vector2(78f, 101f);
        rt.sizeDelta = new Vector2(70f, 28f);

        Text text = go.GetComponent<Text>();
        text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 14;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = LevelColors[0];
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = ShortNames[0];

        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(0.05f, 0.05f, 0.08f, 0.95f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow sh = go.AddComponent<Shadow>();
        sh.effectColor = new Color(LevelColors[0].r, LevelColors[0].g, LevelColors[0].b, 0f);
        sh.effectDistance = new Vector2(0f, 0f);

        btn = go.AddComponent<CpuDifficultyButton>();
        btn.attachedIndex = playerIndex;
        btn.label = text;
        btn.outline = ol;
        btn.glow = sh;
        btn.baseTextColor = LevelColors[0];
        EnsureClickTarget(go);
        return btn;
    }

    static void EnsureClickTarget(GameObject go)
    {
        SelectorClickable.Ensure(go, 70f);
        // Handle glow ourselves — ButtonInteraction fights Text outline colors
        ButtonInteraction bi = go.GetComponent<ButtonInteraction>();
        if (bi != null)
            Object.Destroy(bi);
    }

    void Awake()
    {
        CacheRefs();
        EnsureClickTarget(gameObject);
    }

    void CacheRefs()
    {
        if (label == null)
            label = GetComponent<Text>();
        if (outline == null)
            outline = GetComponent<Outline>();
        if (glow == null)
            glow = GetComponent<Shadow>();
    }

    void Update()
    {
        Refresh();
    }

    void LateUpdate()
    {
        ApplyGlowVisuals();
        pressed = false;
    }

    public void Refresh()
    {
        Database db = Database.instance;
        if (db == null || db.players == null)
        {
            SetVisible(false);
            return;
        }

        Player p = db.players.Find(x => x != null && x.index == attachedIndex);
        if (p == null || !p.computer)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        ComputerAI.CpuDifficulty diff = ComputerAI.GetDifficulty(p);

        if (diff == ComputerAI.CpuDifficulty.Training && !TrainingManager.IsActive)
        {
            ComputerAI.SetDifficulty(p, ComputerAI.CpuDifficulty.Easy);
            diff = ComputerAI.CpuDifficulty.Easy;
        }

        Color c;
        string name;
        if (diff == ComputerAI.CpuDifficulty.Training)
        {
            name = "Train";
            c = new Color(0.55f, 0.75f, 0.55f, 1f);
        }
        else
        {
            int i = IndexOfPlayable(diff);
            name = ShortNames[i];
            c = LevelColors[i];
        }

        if (label != null)
        {
            label.text = name;
            baseTextColor = c;
            if (highlightCount <= 0 && !pressed)
                label.color = c;
        }
    }

    void SetVisible(bool on)
    {
        if (gameObject.activeSelf != on)
            gameObject.SetActive(on);
    }

    void ApplyGlowVisuals()
    {
        if (label == null)
            return;

        bool lit = highlightCount > 0 || pressed;
        Color c = baseTextColor;

        if (pressed)
        {
            label.color = Color.Lerp(c, Color.white, 0.55f);
            if (outline != null)
            {
                outline.effectColor = Color.Lerp(c, Color.white, 0.8f);
                outline.effectDistance = new Vector2(3f, -3f);
            }
            if (glow != null)
            {
                glow.effectColor = new Color(c.r, c.g, c.b, 0.85f);
                glow.effectDistance = new Vector2(2f, -2f);
            }
        }
        else if (lit)
        {
            label.color = Color.Lerp(c, Color.white, 0.35f);
            if (outline != null)
            {
                outline.effectColor = new Color(c.r, c.g, c.b, 1f);
                outline.effectDistance = new Vector2(2.5f, -2.5f);
            }
            if (glow != null)
            {
                glow.effectColor = new Color(c.r, c.g, c.b, 0.65f);
                glow.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }
        else
        {
            label.color = c;
            if (outline != null)
            {
                outline.effectColor = baseOutlineColor;
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }
            if (glow != null)
            {
                glow.effectColor = new Color(c.r, c.g, c.b, 0f);
                glow.effectDistance = Vector2.zero;
            }
        }
    }

    public void OnHighlighted(int player)
    {
        highlightCount++;
    }

    public void OnUnHighlighted(int player)
    {
        highlightCount = Mathf.Max(0, highlightCount - 1);
    }

    public void Pressed()
    {
        pressed = true;
    }

    /// <summary>Cursor / Selection click — cycle Easy → … → Impossible → Easy (Training only in AI Training).</summary>
    public void OnClick(int player)
    {
        Database db = Database.instance;
        if (db == null || db.players == null)
            return;

        Player p = db.players.Find(x => x != null && x.index == attachedIndex);
        if (p == null || !p.computer)
            return;

        ComputerAI.CpuDifficulty cur = ComputerAI.GetDifficulty(p);

        if (TrainingManager.IsActive)
        {
            if (cur == ComputerAI.CpuDifficulty.Training)
            {
                ComputerAI.SetDifficulty(p, ComputerAI.CpuDifficulty.Easy);
            }
            else
            {
                int i = IndexOfPlayable(cur);
                if (i >= PlayableLevels.Length - 1)
                    ComputerAI.SetDifficulty(p, ComputerAI.CpuDifficulty.Training);
                else
                    ComputerAI.SetDifficulty(p, PlayableLevels[i + 1]);
            }
        }
        else
        {
            int i = IndexOfPlayable(cur);
            int next = (i + 1) % PlayableLevels.Length;
            ComputerAI.SetDifficulty(p, PlayableLevels[next]);
        }

        pressed = true;
        Refresh();
    }

    void OnDisable()
    {
        highlightCount = 0;
        pressed = false;
    }

    static int IndexOfPlayable(ComputerAI.CpuDifficulty diff)
    {
        for (int i = 0; i < PlayableLevels.Length; i++)
        {
            if (PlayableLevels[i] == diff)
                return i;
        }
        return 0;
    }
}
