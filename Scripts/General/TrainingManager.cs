using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// AI Training session controller. Auto-runs CPU matches with undertrained coverage
/// priority, supports stop-after / stop-now, and a join lobby to pick lineups.
/// Dev-only: runs in the Training scene (not shipped Gameplay).
/// </summary>
public class TrainingManager : MonoBehaviour
{
    public const string SceneName = "Training";
    public const string MenuTitle = "AI Training";

    public static TrainingManager instance;

    public enum SessionState
    {
        Idle,
        AutoRunning,
        StopAfterMatch,
        JoinLobby,
        InMatch,
    }

    Database db;
    MenuManager mm;

    public SessionState state = SessionState.Idle;
    public bool sessionActive;
    public int matchesCompleted;
    public string lastPlanSummary = "";
    public string statusLine = "Idle";

    string lastFieldName = "";
    bool waitingForRematch;
    Coroutine matchLoop;

    // UI
    GameObject panel;
    Text statusText;
    Text coverageText;
    Text btnAutoLabel;
    Text btnStopAfterLabel;

    public static bool IsActive => instance != null && instance.sessionActive;

    public static void EnsureExists()
    {
        // Never spawn training tooling in the player-facing Gameplay scene.
        if (SceneManager.GetActiveScene().name != SceneName)
            return;

        if (instance != null)
            return;

        GameObject go = new GameObject("TrainingManager");
        go.AddComponent<TrainingManager>();
    }

    void Awake()
    {
        if (SceneManager.GetActiveScene().name != SceneName)
        {
            Destroy(gameObject);
            return;
        }

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name != SceneName)
            return;

        db = Database.instance;
        mm = MenuManager.instance;
        BuildPanel();
        EnterSession(autoStart: true);
    }

    void Update()
    {
        if (!sessionActive || panel == null)
            return;

        RefreshLabels();

        // After a match ends in auto mode, kick the next one
        if ((state == SessionState.AutoRunning || state == SessionState.StopAfterMatch)
            && db != null && db.winnerScreen && !waitingForRematch && !db.startingGame)
        {
            waitingForRematch = true;
            StartCoroutine(ContinueAfterWin());
        }
    }

    // ─── Session control ───────────────────────────────────────────────

    public void EnterSession(bool autoStart)
    {
        if (db == null)
            db = Database.instance;
        if (mm == null)
            mm = MenuManager.instance;

        sessionActive = true;
        db.aiTrainingSession = true;
        SetPanelVisible(true);
        statusLine = "Training session ready";

        if (autoStart)
            StartAuto();
    }

    public void ExitSession()
    {
        StopAuto();
        AbortMatchNow(openMainMenu: false);
        sessionActive = false;
        if (db != null)
            db.aiTrainingSession = false;
        SetPanelVisible(false);

        if (mm != null)
        {
            mm.openMenu.Clear();
            mm.OpenMenu("Main Menu");
        }
    }

    public void StartAuto()
    {
        if (db == null)
            return;

        state = SessionState.AutoRunning;
        statusLine = "Auto training…";
        waitingForRematch = false;

        if (!db.gameStart && !db.startingGame && !db.winnerScreen)
            BeginNextMatch();
    }

    public void StopAuto()
    {
        waitingForRematch = false;
        if (state == SessionState.AutoRunning || state == SessionState.StopAfterMatch || state == SessionState.InMatch)
        {
            state = SessionState.Idle;
            statusLine = "Auto stopped";
        }
    }

    public void RequestStopAfterMatch()
    {
        if (state == SessionState.AutoRunning || state == SessionState.InMatch)
        {
            state = SessionState.StopAfterMatch;
            statusLine = "Will stop after this match";
        }
        else
        {
            state = SessionState.Idle;
            statusLine = "Stopped";
        }
    }

    public void AbortMatchNow(bool openMainMenu)
    {
        waitingForRematch = false;
        if (matchLoop != null)
        {
            StopCoroutine(matchLoop);
            matchLoop = null;
        }

        if (db == null)
            db = Database.instance;
        if (db == null)
            return;

        db.AbortCurrentMatch();
        state = SessionState.Idle;
        statusLine = "Match aborted";

        if (openMainMenu && mm != null)
        {
            mm.openMenu.Clear();
            mm.OpenMenu("Main Menu");
        }
    }

    /// <summary>
    /// Open Character Select so you can join, pick characters / opponents, then start.
    /// </summary>
    public void OpenJoinLobby()
    {
        if (db == null || mm == null)
            return;

        waitingForRematch = false;
        if (db.gameStart || db.startingGame || db.winnerScreen)
            db.AbortCurrentMatch();

        state = SessionState.JoinLobby;
        statusLine = "Join lobby — pick fighters, then Confirm";
        sessionActive = true;
        db.aiTrainingSession = true;

        PrepareLobbyDefaults();
        EnsureMinCpuRoster(2);

        // Put every slot on Training difficulty for authoring
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p != null && p.computer)
                ComputerAI.SetDifficulty(p, ComputerAI.CpuDifficulty.Training);
        }

        mm.openMenu.Clear();
        mm.OpenMenu("Character Select");
        for (int i = 0; i < db.players.Count; i++)
            db.players[i].state = "Character Select";

        SetPanelVisible(true);
    }

    public void StartManualFromLobby()
    {
        if (db == null || db.players == null || db.players.Count < 2)
        {
            statusLine = "Need at least 2 players";
            return;
        }

        state = SessionState.InMatch;
        statusLine = "Manual training match…";
        sessionActive = true;
        db.aiTrainingSession = true;

        db.gametype = Gametype.Vs;
        db.levelSelect = false;
        db.ballSelect = false;
        db.positionSelect = false;
        // Keep teamSelect as configured in lobby (5+ forces team)
        db.selectedField = -1;
        db.selectedBall = -1;
        db.maxPlayers = Mathf.Max(db.players.Count, db.maxPlayers);
        db.minPlayers = Mathf.Min(2, db.players.Count);

        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null)
                continue;
            if (p.computer)
                ComputerAI.SetDifficulty(p, ComputerAI.CpuDifficulty.Training);
            p.characterSelected = true;
        }

        lastFieldName = "";
        db.CharactersPicked("balls");
    }

    // ─── Match pipeline ────────────────────────────────────────────────

    void BeginNextMatch()
    {
        if (db == null)
            return;

        ComputerAI.TrainingMatchPlan plan = ComputerAI.PickUndertrainedMatch(
            db.characters, db.fields, 2, 8);

        if (plan == null || plan.characterNames.Count < 2)
        {
            statusLine = "No roster/fields available for training";
            state = SessionState.Idle;
            return;
        }

        ApplyPlan(plan);
        lastPlanSummary = DescribePlan(plan);
        statusLine = "Starting: " + lastPlanSummary;
        if (state != SessionState.AutoRunning && state != SessionState.StopAfterMatch)
            state = SessionState.InMatch;
        lastFieldName = plan.fieldName;

        db.CharactersPicked("balls");
    }

    void ApplyPlan(ComputerAI.TrainingMatchPlan plan)
    {
        PrepareLobbyDefaults();

        // Clear existing players (keep human controllers for re-add later)
        db.players.Clear();

        db.minPlayers = 2;
        db.maxPlayers = plan.playerCount;
        db.selectedField = plan.fieldIndex;
        db.selectedBall = -1;
        db.levelSelect = false;
        db.ballSelect = false;
        db.positionSelect = false;
        db.teamSelect = plan.playerCount >= TeamModeToggle.ForceTeamAtPlayerCount;
        db.gametype = db.teamSelect ? Gametype.Coop : Gametype.Vs;

        for (int i = 0; i < plan.playerCount; i++)
        {
            db.PlayerAdd(null);
            Player p = db.players[db.players.Count - 1];
            string charName = plan.characterNames[i];
            Characters c = db.characters.Find(x => x != null && x.name == charName);
            if (c == null)
                c = db.RandomCharacter();
            if (c != null)
                p.SetUpCharacter(c);

            ComputerAI.SetDifficulty(p, ComputerAI.CpuDifficulty.Training);
            int variant = i < plan.variants.Count ? plan.variants[i] : 0;
            ComputerAI.AssignMatchBrain(p, variant);
            p.characterSelected = true;
            p.wantRandomCharacter = false;
        }
    }

    void PrepareLobbyDefaults()
    {
        db.gametype = Gametype.Vs;
        db.levelSelect = false;
        db.ballSelect = false;
        db.positionSelect = false;
        db.someoneWon = false;
        db.winnerScreen = false;
        db.gameStart = false;
        db.startingGame = false;
        Time.timeScale = 1f;
    }

    void EnsureMinCpuRoster(int count)
    {
        while (db.players.Count < count)
            db.PlayerAdd(null);

        // Trim excess CPUs if somehow over 8
        while (db.players.Count > 8)
            db.players.RemoveAt(db.players.Count - 1);
    }

    IEnumerator ContinueAfterWin()
    {
        // Let SomeoneWon finish clearing the field
        yield return new WaitForSecondsRealtime(2.0f);

        string fieldName = lastFieldName;
        if (string.IsNullOrEmpty(fieldName) && db.selectedField >= 0 && db.selectedField < db.fields.Count
            && db.fields[db.selectedField] != null)
            fieldName = db.fields[db.selectedField].name;

        ComputerAI.RecordTrainingMatchCoverage(db.players, fieldName);
        matchesCompleted++;

        if (state == SessionState.StopAfterMatch)
        {
            db.ResetPlayersForNewMatch();
            db.AbortCurrentMatch();
            if (mm != null)
            {
                mm.RemoveMenu("Playing");
                mm.OpenMenu("Character Select");
            }
            state = SessionState.Idle;
            statusLine = "Stopped after match #" + matchesCompleted;
            waitingForRematch = false;
            yield break;
        }

        if (state != SessionState.AutoRunning)
        {
            waitingForRematch = false;
            yield break;
        }

        db.ResetPlayersForNewMatch();
        db.AbortCurrentMatch();
        yield return null;

        waitingForRematch = false;
        BeginNextMatch();
    }

    /// <summary>Called from Database.SomeoneWon when training wants to skip normal win UI delay handling.</summary>
    public bool ShouldAutoContinue()
    {
        return sessionActive && (state == SessionState.AutoRunning || state == SessionState.StopAfterMatch);
    }

    string DescribePlan(ComputerAI.TrainingMatchPlan plan)
    {
        if (plan == null)
            return "";
        return plan.playerCount + "P | " + plan.fieldName + " | " + string.Join(", ", plan.characterNames);
    }

    // ─── UI ────────────────────────────────────────────────────────────

    void BuildPanel()
    {
        if (panel != null)
            return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cGo = new GameObject("TrainingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
        }

        panel = new GameObject("Training Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 1f);
        prt.anchorMax = new Vector2(0f, 1f);
        prt.pivot = new Vector2(0f, 1f);
        prt.anchoredPosition = new Vector2(12f, -12f);
        prt.sizeDelta = new Vector2(420f, 210f);
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.14f, 0.88f);

        statusText = MakeLabel(panel.transform, "Status", new Vector2(10, -8), new Vector2(400, 40), 13);
        coverageText = MakeLabel(panel.transform, "Coverage", new Vector2(10, -48), new Vector2(400, 50), 11);

        float y = -105f;
        btnAutoLabel = MakeButton(panel.transform, "BtnAuto", "Start Auto", new Vector2(10, y), new Vector2(130, 36), OnClickAuto);
        btnStopAfterLabel = MakeButton(panel.transform, "BtnStopAfter", "Stop After", new Vector2(150, y), new Vector2(120, 36), OnClickStopAfter);
        MakeButton(panel.transform, "BtnStopNow", "Stop Now", new Vector2(280, y), new Vector2(120, 36), OnClickStopNow);

        y = -150f;
        MakeButton(panel.transform, "BtnJoin", "Join / Pick", new Vector2(10, y), new Vector2(130, 36), OnClickJoin);
        MakeButton(panel.transform, "BtnStartLobby", "Start Lobby", new Vector2(150, y), new Vector2(120, 36), OnClickStartLobby);
        MakeButton(panel.transform, "BtnExit", "Exit", new Vector2(280, y), new Vector2(120, 36), OnClickExit);
    }

    Text MakeLabel(Transform parent, string name, Vector2 pos, Vector2 size, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Text t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null)
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = TextAnchor.UpperLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    Text MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.color = new Color(0.2f, 0.45f, 0.7f, 1f);
        Button btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        Text t = textGo.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null)
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = 13;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;
        t.text = label;
        return t;
    }

    void SetPanelVisible(bool on)
    {
        if (panel != null)
            panel.SetActive(on);
    }

    void RefreshLabels()
    {
        if (statusText != null)
        {
            statusText.text = "[" + state + "]  Matches: " + matchesCompleted + "\n" + statusLine
                + (string.IsNullOrEmpty(lastPlanSummary) ? "" : "\n" + lastPlanSummary);
        }

        if (coverageText != null && db != null)
            coverageText.text = "Gaps: " + ComputerAI.DescribeCoverageGaps(db.characters, 5);

        if (btnAutoLabel != null)
            btnAutoLabel.text = (state == SessionState.AutoRunning) ? "Running…" : "Start Auto";

        if (btnStopAfterLabel != null)
            btnStopAfterLabel.text = (state == SessionState.StopAfterMatch) ? "Stopping…" : "Stop After";
    }

    void OnClickAuto()
    {
        if (!sessionActive)
            EnterSession(autoStart: true);
        else
            StartAuto();
    }

    void OnClickStopAfter() => RequestStopAfterMatch();
    void OnClickStopNow() => AbortMatchNow(openMainMenu: false);
    void OnClickJoin() => OpenJoinLobby();
    void OnClickStartLobby() => StartManualFromLobby();
    void OnClickExit() => ExitSession();
}
