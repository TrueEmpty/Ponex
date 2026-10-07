using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BallSelect : MonoBehaviour
{
    public static BallSelect instance;
    Database db;
    MenuManager mm;

    int lastShownBall = -10;
    int focusSlot;
    float nextShow = 0;
    float showSwap = .25f;

    GameObject shownBall = null;
    public Text ballName;

    public Transform controlsHolder;
    bool setup;

    Text countLabel;
    readonly List<Text> slotLabels = new List<Text>();
    Transform loadoutRoot;
    bool loadoutDirty = true;
    int lastLabelFocus = -99;
    int lastLabelCount = -99;

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

    void OnEnable()
    {
        if (db == null)
            db = Database.instance;
        RefreshLoadoutUI();
    }

    void Update()
    {
        if (!setup)
            SetupControls();

        if (db == null)
            return;

        db.EnsureBallSlotsPublic();
        focusSlot = Mathf.Clamp(focusSlot, 0, Mathf.Max(0, db.ballCount - 1));
        if (focusSlot != lastLabelFocus || db.ballCount != lastLabelCount)
        {
            lastLabelFocus = focusSlot;
            lastLabelCount = db.ballCount;
            loadoutDirty = true;
        }
        int slotType = db.GetBallSlot(focusSlot);

        if (slotType >= 0 && slotType < db.balls.Count)
        {
            Ball b = db.balls[slotType];
            if (ballName != null)
            {
                ballName.text = b.name;
                ballName.color = b.color;
            }
        }
        else if (ballName != null)
        {
            ballName.text = "Random";
            ballName.color = Color.black;
        }

        if (loadoutDirty)
        {
            RefreshLoadoutLabels();
            loadoutDirty = false;
        }

        if (lastShownBall != slotType)
        {
            if (slotType >= 0 && slotType < db.balls.Count)
            {
                UpdateShownBall(slotType);
                lastShownBall = slotType;
            }
            else if (nextShow < Time.time)
            {
                UpdateShownBall(Random.Range(0, db.balls.Count));
                nextShow = Time.time + showSwap;
                lastShownBall = -1;
            }
        }
    }

    public void RefreshLoadoutUI()
    {
        if (db == null)
            db = Database.instance;
        if (db == null)
            return;

        db.EnsureBallSlotsPublic();
        if (loadoutRoot != null)
            BuildSlotRows();
        RefreshLoadoutLabels();

        // Keep existing Prev/Next/Random wired to focused slot
        if (loadoutRoot == null)
            return;

        BallStepButton[] steps = GetComponentsInChildren<BallStepButton>(true);
        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i] == null || steps[i].editCount)
                continue;
            // Main controls (not slot-row buttons) track focus slot
            if (steps[i].transform.IsChildOf(loadoutRoot))
                continue;
            steps[i].slotIndex = focusSlot;
        }
    }

    void RefreshLoadoutLabels()
    {
        if (db == null)
            return;

        if (countLabel != null)
            countLabel.text = "Balls: " + db.ballCount;

        for (int i = 0; i < slotLabels.Count; i++)
        {
            if (slotLabels[i] == null)
                continue;

            bool active = i < db.ballCount;
            slotLabels[i].gameObject.SetActive(active);
            if (!active)
                continue;

            int type = db.GetBallSlot(i);
            string label = type >= 0 && type < db.balls.Count ? db.balls[type].name : "Random";
            string focus = i == focusSlot ? "> " : "  ";
            slotLabels[i].text = focus + "Ball " + (i + 1) + ": " + label;
            slotLabels[i].color = type >= 0 && type < db.balls.Count ? db.balls[type].color : Color.black;
        }
    }

    void SetupControls()
    {
        if (setup)
            return;

        BallStepButton[] steps = GetComponentsInChildren<BallStepButton>(true);
        if (steps == null || steps.Length == 0)
            Debug.LogWarning("BallSelect: no BallStepButton components found. Run Tools/Ponex/Bake Selector UI.");

        EnsureLoadoutPanel();
        setup = true;
        RefreshLoadoutUI();
    }

    void EnsureLoadoutPanel()
    {
        if (loadoutRoot != null)
            return;

        Transform parent = controlsHolder != null ? controlsHolder : transform;
        GameObject root = new GameObject("Loadout", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 110f);
        rt.sizeDelta = new Vector2(720f, 280f);
        loadoutRoot = root.transform;

        // Count row
        CreateLabelButton(loadoutRoot, "CountLabel", new Vector2(0f, 110f), new Vector2(220f, 36f), out countLabel);
        CreateStepButton(loadoutRoot, "CountPrev", new Vector2(-160f, 110f), -1, true, 0);
        CreateStepButton(loadoutRoot, "CountNext", new Vector2(160f, 110f), 1, true, 0);

        BuildSlotRows();
    }

    void BuildSlotRows()
    {
        if (loadoutRoot == null || db == null)
            return;

        // Clear old slot rows (immediate so we can rebuild this frame)
        for (int i = loadoutRoot.childCount - 1; i >= 0; i--)
        {
            Transform c = loadoutRoot.GetChild(i);
            if (c.name.StartsWith("Slot"))
                DestroyImmediate(c.gameObject);
        }
        slotLabels.Clear();

        for (int i = 0; i < Database.MaxMatchBalls; i++)
        {
            float y = 60f - i * 42f;
            GameObject row = new GameObject("Slot" + i, typeof(RectTransform));
            row.transform.SetParent(loadoutRoot, false);
            RectTransform rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 0.5f);
            rowRt.anchorMax = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = new Vector2(0f, y);
            rowRt.sizeDelta = new Vector2(700f, 40f);

            CreateLabelButton(row.transform, "Label", new Vector2(0f, 0f), new Vector2(320f, 34f), out Text label);
            int slot = i;
            // Clicking the label focuses that slot and steps type
            BallStepButton labelStep = label.gameObject.GetComponent<BallStepButton>();
            if (labelStep == null)
                labelStep = label.gameObject.AddComponent<BallStepButton>();
            labelStep.slotIndex = slot;
            labelStep.step = 1;
            labelStep.setRandom = false;
            labelStep.editCount = false;

            // Focus helper
            FocusBallSlot focus = label.gameObject.GetComponent<FocusBallSlot>();
            if (focus == null)
                focus = label.gameObject.AddComponent<FocusBallSlot>();
            focus.slotIndex = slot;

            slotLabels.Add(label);

            CreateStepButton(row.transform, "Prev", new Vector2(-220f, 0f), -1, false, slot);
            CreateStepButton(row.transform, "Next", new Vector2(220f, 0f), 1, false, slot);
            CreateStepButton(row.transform, "Rand", new Vector2(300f, 0f), 0, false, slot, true);
        }
    }

    void CreateLabelButton(Transform parent, string name, Vector2 pos, Vector2 size, out Text text)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(BoxCollider));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        text = go.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 22;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.font = font;

        BoxCollider box = go.GetComponent<BoxCollider>();
        box.size = new Vector3(size.x, size.y, 1f);

        if (go.GetComponent<ButtonInteraction>() == null)
            go.AddComponent<ButtonInteraction>();
    }

    void CreateStepButton(Transform parent, string name, Vector2 pos, int step, bool editCount, int slot, bool random = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(BoxCollider));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(70f, 34f);

        Text text = go.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 20;
        text.color = Color.white;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.font = font;
        if (random)
            text.text = "Rnd";
        else if (step < 0)
            text.text = "<";
        else
            text.text = ">";

        BoxCollider box = go.GetComponent<BoxCollider>();
        box.size = new Vector3(70f, 34f, 1f);

        if (go.GetComponent<ButtonInteraction>() == null)
            go.AddComponent<ButtonInteraction>();

        BallStepButton stepBtn = go.GetComponent<BallStepButton>();
        if (stepBtn == null)
            stepBtn = go.AddComponent<BallStepButton>();
        stepBtn.step = step;
        stepBtn.setRandom = random;
        stepBtn.editCount = editCount;
        stepBtn.slotIndex = slot;

        if (!editCount)
        {
            FocusBallSlot focus = go.GetComponent<FocusBallSlot>();
            if (focus == null)
                focus = go.AddComponent<FocusBallSlot>();
            focus.slotIndex = slot;
        }
    }

    public void SetFocusSlot(int slot)
    {
        if (db == null)
            db = Database.instance;
        if (db == null)
            return;
        focusSlot = Mathf.Clamp(slot, 0, Mathf.Max(0, db.ballCount - 1));
        lastShownBall = -10;
        loadoutDirty = true;
        RefreshLoadoutUI();
    }

    void UpdateShownBall(int ballToShow)
    {
        if (shownBall != null)
            Destroy(shownBall);

        if (db.balls == null || ballToShow < 0 || ballToShow >= db.balls.Count)
            return;

        Ball b = db.balls[ballToShow];
        if (b.selection == null)
            return;

        shownBall = Instantiate(b.selection);
        shownBall.transform.position = new Vector3(0, 0, 5);
    }

    private void OnDisable()
    {
        if (shownBall != null)
            Destroy(shownBall);
    }
}

/// <summary>Focuses a ball loadout slot when the control is clicked.</summary>
public class FocusBallSlot : MonoBehaviour
{
    public int slotIndex;

    public void OnClick(int player)
    {
        if (BallSelect.instance != null)
            BallSelect.instance.SetFocusSlot(slotIndex);
    }
}
