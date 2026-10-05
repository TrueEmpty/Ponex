using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Character Select lobby toggle: VS Mode vs Team Mode.
/// VS skips team/position select. 5+ players forces Team Mode.
/// Cursor SendMessage uses OnClick(int).
/// </summary>
public class TeamModeToggle : MonoBehaviour
{
    public const int ForceTeamAtPlayerCount = 5;

    Database db;

    [Header("UI")]
    public Text label;
    public Image background;
    public Outline outline;

    [Header("Colors")]
    public Color vsColor = new Color(0.29f, 0.89f, 1f, 1f);
    public Color teamColor = new Color(1f, 0.75f, 0.25f, 1f);
    public Color lockedTeamColor = new Color(1f, 0.55f, 0.15f, 1f);

    bool lastTeam;
    int lastPlayerCount = -1;
    float lastToggleUnscaled = -999f;
    const float ToggleDebounce = 0.08f;

    ButtonInteraction buttonInteraction;

    void Awake()
    {
        ResolveRefs();
        buttonInteraction = GetComponent<ButtonInteraction>();
    }

    void Start()
    {
        db = Database.instance;
        ApplyMode(db != null && db.teamSelect, force: false);
        RefreshVisuals();
    }

    void Update()
    {
        if (db == null)
            db = Database.instance;
        if (db == null)
            return;

        int count = db.players != null ? db.players.Count : 0;
        if (count >= ForceTeamAtPlayerCount && !db.teamSelect)
            ApplyMode(true, force: true);

        if (db.teamSelect != lastTeam || count != lastPlayerCount)
            RefreshVisuals();
    }

    void LateUpdate()
    {
        // Cursor SendMessage hits multiple OnClick handlers; always keep the label honest.
        ApplyLabelText();
    }

    /// <summary>Cursor / Selection click.</summary>
    public void OnClick(int player)
    {
        Toggle();
    }

    /// <summary>Unity Button / RunOnClicked.</summary>
    public void Toggle()
    {
        // Prefab historically wired Toggle on both TeamModeToggle.OnClick and RunOnClicked —
        // one confirm would flip twice and look stuck.
        if (Time.unscaledTime - lastToggleUnscaled < ToggleDebounce)
            return;
        lastToggleUnscaled = Time.unscaledTime;

        if (db == null)
            db = Database.instance;
        if (db == null)
            return;

        int count = db.players != null ? db.players.Count : 0;
        if (count >= ForceTeamAtPlayerCount)
        {
            ApplyMode(true, force: true);
            RefreshVisuals();
            return;
        }

        ApplyMode(!db.teamSelect, force: false);
        RefreshVisuals();
    }

    public void ApplyMode(bool teamMode, bool force)
    {
        if (db == null)
            db = Database.instance;
        if (db == null)
            return;

        db.teamSelect = teamMode;
        db.positionSelect = teamMode;

        if (db.gametype == Gametype.Arcade || db.gametype == Gametype.Story)
        {
            // leave alone
        }
        else
        {
            db.gametype = teamMode ? Gametype.Coop : Gametype.Vs;
        }

        if (db.gametype == Gametype.Vs || db.gametype == Gametype.Coop)
            db.maxPlayers = 8;
    }

    void RefreshVisuals()
    {
        if (db == null)
            return;

        ResolveRefs();

        bool team = db.teamSelect;
        int count = db.players != null ? db.players.Count : 0;
        bool locked = count >= ForceTeamAtPlayerCount;

        lastTeam = team;
        lastPlayerCount = count;

        ApplyLabelText();

        Color c = locked ? lockedTeamColor : (team ? teamColor : vsColor);
        if (background != null)
            background.color = c;
        if (outline != null)
            outline.effectColor = Color.Lerp(c, Color.black, 0.35f);

        if (buttonInteraction != null)
            buttonInteraction.SetBaseColors(c, Color.Lerp(c, Color.black, 0.35f));
    }

    void ApplyLabelText()
    {
        if (db == null)
            return;

        ResolveRefs();
        if (label == null)
            return;

        bool team = db.teamSelect;
        int count = db.players != null ? db.players.Count : 0;
        bool locked = count >= ForceTeamAtPlayerCount;

        string want = locked ? "Team Mode" : (team ? "Team Mode" : "VS Mode");
        if (locked)
            want = "Team Mode (Req)";

        if (label.text != want)
            label.text = want;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
    }

    void ResolveRefs()
    {
        if (label == null)
        {
            Text[] texts = GetComponentsInChildren<Text>(true);
            if (texts != null && texts.Length > 0)
                label = texts[0];
        }
        if (background == null)
            background = GetComponent<Image>();
        if (outline == null)
            outline = GetComponent<Outline>();
    }
}
