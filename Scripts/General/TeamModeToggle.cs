using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Character Select lobby toggle: VS Mode vs Team Mode.
/// VS skips team/position select and auto-seats by player index.
/// Team Mode remembers position/team picks across toggles. 5+ players forces Team Mode.
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
        ApplyLabelText();
    }

    public void OnClick(int player)
    {
        Toggle();
    }

    public void Toggle()
    {
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

        bool wasTeam = db.teamSelect;

        // Leaving Team Mode — snapshot seats before VS overwrites them
        if (wasTeam && !teamMode)
            db.RememberAllLobbySeats();

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

        if (teamMode)
        {
            // Entering / staying in Team Mode — restore remembered walls/teams
            if (!wasTeam || force)
                db.RestoreTeamModeSeats();
        }
        else
        {
            // VS: no position/team menus — unique teams + index walls
            db.ApplyVersusSeatLayout();
        }
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

        string want = locked ? "Team Mode (Req)" : (team ? "Team Mode" : "VS Mode");

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
