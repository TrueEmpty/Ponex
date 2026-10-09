using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class Database : MonoBehaviour
{
    public static Database instance;
    MenuManager mm;
    CharacterSelect cS;

    #region Setting
    public float sensitivity = .25f;
    #endregion

    #region Fields
    public List<Field> fields;
    public List<Part> parts;
    public List<Ball> balls;

    public int selectedField = 0;
    public int selectedBall = -1;

    public const int MaxMatchBalls = 5;
    [Tooltip("How many match balls to keep in play (1-5).")]
    public int ballCount = 1;
    [Tooltip("Ball type index per slot; -1 = Random.")]
    public int[] ballSlots = new int[] { -1, -1, -1, -1, -1 };

    int fieldSize = 0;
    /// <summary>Current match playfield depth/plane (framed Z). 0 before a match starts.</summary>
    public float FieldPlaySize => fieldSize;

    public void SetFieldPlaySize(float size)
    {
        fieldSize = Mathf.Max(1, Mathf.RoundToInt(size));
    }

    public GameObject outofBounds;
    public GameObject background;
    [Tooltip("Optional Tic coop wing-bumper form. Falls back to runtime build / Resources/TicWing.")]
    public GameObject ticWingBumperPrefab;
    public GameObject fieldObj;
    public Text gameplayinfo;
    #endregion

    #region Players
    public List<ControllerLink> controllers = new List<ControllerLink>();
    [Tooltip("Filled at runtime from Resources/Characters ScriptableObjects.")]
    public List<Characters> characters = new List<Characters>();
    public List<Player> players;
    public Player allplay;
    public Color apColor = Color.yellow;
    public List<PlayerColors> playerColors;

    public Transform playerSelectors;
    public GameObject playerSelectorGobj;

    public List<Color> pawnColors;
    public List<Effects> effects;
    public GameObject playerInfo;
    public Transform playerInfoHolderLS;
    public Transform playerInfoHolderRS;
    public List<Color> teamColors;//0 = Neutral, 5 = Dead

    public Transform showWinner;
    public GameObject wonBox;
    #endregion

    #region Setup
    public int minPlayers = 1;
    public int maxPlayers = 8;

    public bool levelSelect = false;
    public bool positionSelect = false;
    public bool teamSelect = false;
    public bool ballSelect = false;

    public Gametype gametype = Gametype.Vs;
    [Tooltip("Online matches do not freeze time. The pause menu shows Forfeit instead of Reset Match.")]
    public bool onlineMatch = false;
    public bool pauseMenuOpen = false;
    public bool LocalMatchPaused => pauseMenuOpen && !onlineMatch;
    public bool startingGame = false;
    public bool someoneWon = false;
    public bool winnerScreen = false;
    public GridControl mmOp;
    public bool gameStart = false;
    /// <summary>True while AI Training session is driving matches (Training scene / panel).</summary>
    public bool aiTrainingSession = false;
    #endregion

    #region Sounds
    public List<Sounds> extraSounds = new List<Sounds>();
    public AudioSource effectAudio;
    #endregion

    const string SelectedCharacterPref = "Ponex.SelectedCharacter.";
    const string PrefSelectedField = "Ponex.SelectedField";
    const string PrefBallCount = "Ponex.BallCount";
    const string PrefBallSlot = "Ponex.BallSlot.";
    const string PrefSelectedBall = "Ponex.SelectedBall";
    const float CharacterPrefSaveDelay = 0.75f;
    bool characterPrefsDirty;
    float characterPrefsSaveAt;

    /// <summary>Team-mode seat memory (facing / lane / team) restored when leaving VS.</summary>
    struct LobbySeatMemory
    {
        public bool valid;
        public Facing facing;
        public int position;
        public int team;
    }

    readonly LobbySeatMemory[] lobbySeatMemory = new LobbySeatMemory[8];
    public const string RandomCharacterSentinel = "__RANDOM__";

    private void Awake()
    {
        if(instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
            EnsureBallSlots();
            LoadCharactersFromAssets();
            LoadMatchPrefs();
        GameSettings.EnsureLoaded();
        FieldCatalog.EnsureRegistered(this);
        // Before ControllerLink.Start (-200) so Character Creation can claim pads
            CharacterCreationManager.EnsureExists();
            TrainingManager.EnsureExists();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        mm = MenuManager.instance;
        cS = CharacterSelect.instance;
        ComputerAI.EnsureLoaded();
        if (characters == null || characters.Count == 0)
            LoadCharactersFromAssets();

        EnsureBallSlots();
        LoadMatchPrefs();

        // Remove leftover Leave UI button — disconnect is a hold input (Select / Backspace), not UI
        if (playerSelectors != null)
        {
            Transform leave = playerSelectors.Find("Leave");
            if (leave != null)
                Destroy(leave.gameObject);
            if (playerSelectors.parent != null)
            {
                Transform leave2 = playerSelectors.parent.Find("Leave");
                if (leave2 != null)
                    Destroy(leave2.gameObject);
            }
        }

        BackButtonClick.EnsureAll();
        TrainingManager.EnsureExists();
        CharacterCreationManager.EnsureExists();
        GameSettings.EnsureLoaded();
        // Purge generic levels again in case scene serialization re-added them
        FieldCatalog.EnsureRegistered(this);
    }

    public void EnsureBallSlotsPublic() => EnsureBallSlots();

    void EnsureBallSlots()
    {
        if (ballSlots == null || ballSlots.Length != MaxMatchBalls)
        {
            int[] next = new int[MaxMatchBalls];
            for (int i = 0; i < MaxMatchBalls; i++)
                next[i] = -1;
            if (ballSlots != null)
            {
                for (int i = 0; i < Mathf.Min(ballSlots.Length, MaxMatchBalls); i++)
                    next[i] = ballSlots[i];
            }
            ballSlots = next;
        }

        ballCount = Mathf.Clamp(ballCount, 1, MaxMatchBalls);
        selectedBall = ballSlots[0];
    }

    public void LoadMatchPrefs()
    {
        EnsureBallSlots();

        if (PlayerPrefs.HasKey(PrefSelectedField))
            selectedField = PlayerPrefs.GetInt(PrefSelectedField, selectedField);

        if (PlayerPrefs.HasKey(PrefBallCount))
            ballCount = Mathf.Clamp(PlayerPrefs.GetInt(PrefBallCount, ballCount), 1, MaxMatchBalls);

        for (int i = 0; i < MaxMatchBalls; i++)
        {
            string key = PrefBallSlot + i;
            if (PlayerPrefs.HasKey(key))
                ballSlots[i] = PlayerPrefs.GetInt(key, -1);
        }

        // Legacy single-ball key
        if (PlayerPrefs.HasKey(PrefSelectedBall) && !PlayerPrefs.HasKey(PrefBallSlot + "0"))
            ballSlots[0] = PlayerPrefs.GetInt(PrefSelectedBall, -1);

        selectedBall = ballSlots[0];
    }

    public void SaveMatchPrefs()
    {
        EnsureBallSlots();
        PlayerPrefs.SetInt(PrefSelectedField, selectedField);
        PlayerPrefs.SetInt(PrefBallCount, ballCount);
        PlayerPrefs.SetInt(PrefSelectedBall, ballSlots[0]);
        for (int i = 0; i < MaxMatchBalls; i++)
            PlayerPrefs.SetInt(PrefBallSlot + i, ballSlots[i]);
        PlayerPrefs.Save();
    }

    public int GetBallSlot(int slot)
    {
        EnsureBallSlots();
        if (slot < 0 || slot >= MaxMatchBalls)
            return -1;
        return ballSlots[slot];
    }

    public void SetBallSlot(int slot, int ballIndex)
    {
        EnsureBallSlots();
        if (slot < 0 || slot >= MaxMatchBalls)
            return;

        if (ballIndex < -1)
            ballIndex = -1;
        if (balls != null && ballIndex >= balls.Count)
            ballIndex = -1;

        ballSlots[slot] = ballIndex;
        if (slot == 0)
            selectedBall = ballIndex;
        SaveMatchPrefs();
    }

    public void CycleBallSlot(int slot, int step, bool setRandom)
    {
        EnsureBallSlots();
        if (slot < 0 || slot >= MaxMatchBalls || balls == null || balls.Count == 0)
            return;

        if (setRandom)
        {
            SetBallSlot(slot, -1);
            return;
        }

        int cur = ballSlots[slot];
        cur += step;
        if (cur >= balls.Count)
            cur = -1;
        else if (cur < -1)
            cur = balls.Count - 1;
        SetBallSlot(slot, cur);
    }

    public void CycleBallCount(int step)
    {
        EnsureBallSlots();
        int next = Mathf.Clamp(ballCount + step, 1, MaxMatchBalls);
        if (next == ballCount)
            return;
        ballCount = next;
        SaveMatchPrefs();
    }

    public void SetSelectedFieldPersistent(int fieldIndex)
    {
        selectedField = fieldIndex;
        SaveMatchPrefs();
    }

    int ResolveBallTypeIndex(int slotOrSelected)
    {
        int sB = slotOrSelected;
        if (sB < 0 || balls == null || sB >= balls.Count)
            sB = RandomBallIndex();
        return sB;
    }

    readonly bool[] matchSlotFilled = new bool[MaxMatchBalls];
    float nextBallCheckAt;

    GameObject SpawnMatchBall(int slot, bool ballReady)
    {
        EnsureBallSlots();
        if (balls == null || balls.Count == 0 || slot < 0 || slot >= ballCount)
            return null;

        int type = ResolveBallTypeIndex(ballSlots[slot]);
        if (type < 0 || type >= balls.Count)
            return null;

        Ball ball = balls[type];
        if (ball == null || ball.prefab == null)
            return null;

        GameObject bSpawned = Instantiate(ball.prefab);
        float spread = Mathf.Max(0, ballCount - 1) * 0.55f;
        float x = ballCount <= 1 ? 0f : Mathf.Lerp(-spread, spread, slot / Mathf.Max(1f, ballCount - 1f));
        bSpawned.transform.position = new Vector3(x, 0f, fieldSize);

        BallInfo bI = bSpawned.GetComponent<BallInfo>();
        if (bI != null)
        {
            bI.ballReady = ballReady;
            bI.matchSlot = slot;
        }

        return bSpawned;
    }

    void EnsureMatchBalls(bool ballReady)
    {
        EnsureBallSlots();
        for (int i = 0; i < matchSlotFilled.Length; i++)
            matchSlotFilled[i] = false;

        // One pass over the live registry instead of FindObjects × ballCount
        for (int i = 0; i < LiveBallRegistry.Count; i++)
        {
            BallInfo info = LiveBallRegistry.GetAt(i);
            if (info == null || info.gameObject == null || !info.gameObject.activeInHierarchy)
                continue;
            int s = info.matchSlot;
            if (s >= 0 && s < ballCount && s < matchSlotFilled.Length)
                matchSlotFilled[s] = true;
        }

        for (int slot = 0; slot < ballCount; slot++)
        {
            if (!matchSlotFilled[slot])
                SpawnMatchBall(slot, ballReady);
        }
    }

    /// <summary>
    /// True once the lobby has left Character Select (Field/Team/Playing/etc.).
    /// Main Menu and Character Select are not "past".
    /// </summary>
    public bool IsPastCharacterSelect()
    {
        if (gameStart || startingGame || winnerScreen)
            return true;

        if (mm == null)
            mm = MenuManager.instance;
        if (mm == null)
            return false;

        MenuClass open = mm.GetOpenMenu(true);
        if (open == null || string.IsNullOrEmpty(open.title))
            return false;

        string title = open.title.Trim();
        if (title.Equals("Character Select", StringComparison.OrdinalIgnoreCase))
            return false;
        if (title.Equals("Main Menu", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    /// <summary>
    /// Unlinks a human controller + selector.
    /// Main Menu: always removes the player (never converts to CPU).
    /// Character Select: removes when above minPlayers, otherwise converts to CPU.
    /// Past Character Select / in match: converts to CPU.
    /// </summary>
    public bool DisconnectPlayer(int playerIndex)
    {
        Player p = players != null ? players.Find(x => x.index == playerIndex) : null;
        if (p == null || p.computer)
            return false;

        ControllerLink link = p.cLink;
        p.cLink = null;
        if (p.inputLinks != null)
            p.inputLinks.RemoveAll(x => x == null || x == link);

        if (p.pso != null)
        {
            Destroy(p.pso.gameObject);
            p.pso = null;
        }

        controllers.RemoveAll(x => x == null || x == link || x.index == playerIndex);
        if (link != null)
            Destroy(link.gameObject);

        // Character Creation draft must stay human-controlled (never flip to AI)
        if (CharacterCreationManager.IsActive && p.index == 0)
        {
            p.computer = false;
            if (p.inputLinks != null && p.inputLinks.Count > 0)
                p.cLink = p.inputLinks[p.inputLinks.Count - 1];
            return true;
        }

        bool onMainMenu = IsMainMenu();
        bool removeSlot = onMainMenu || (!IsPastCharacterSelect() && players.Count > minPlayers);
        if (removeSlot)
        {
            players.Remove(p);
            if (cS == null)
                cS = CharacterSelect.instance;
            cS?.EnforceTeamModeForPlayerCount();
            return true;
        }

        p.computer = true;
        p.cpuDifficulty = ComputerAI.CpuDifficulty.Easy;
        return true;
    }

    public bool IsMainMenu()
    {
        if (gameStart || startingGame || winnerScreen)
            return false;

        if (mm == null)
            mm = MenuManager.instance;
        if (mm == null)
            return false;

        MenuClass open = mm.GetOpenMenu(true);
        if (open == null || string.IsNullOrEmpty(open.title))
            return false;

        return open.title.Trim().Equals("Main Menu", StringComparison.OrdinalIgnoreCase);
    }

    void LoadCharactersFromAssets()
    {
        CharacterData[] assets = Resources.LoadAll<CharacterData>("Characters");
        if (assets == null || assets.Length == 0)
        {
            Debug.LogWarning("Database: no CharacterData found in Resources/Characters — keeping inspector list.");
            if (characters == null)
                characters = new List<Characters>();
            return;
        }

        System.Array.Sort(assets, (a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            int byOrder = a.rosterOrder.CompareTo(b.rosterOrder);
            if (byOrder != 0) return byOrder;
            return string.CompareOrdinal(a.characterName, b.characterName);
        });

        characters = new List<Characters>(assets.Length);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] == null)
                continue;
            characters.Add(assets[i].ToCharacters());
        }

        Debug.Log($"Database: loaded {characters.Count} characters from ScriptableObjects.");
    }

    public void RememberSelectedCharacter(int playerIndex, string characterName)
    {
        if (playerIndex < 0 || string.IsNullOrEmpty(characterName))
            return;

        PlayerPrefs.SetString(SelectedCharacterPref + playerIndex, characterName);
        characterPrefsDirty = true;
        characterPrefsSaveAt = Time.unscaledTime + CharacterPrefSaveDelay;
    }

    void FlushCharacterPrefs()
    {
        if (!characterPrefsDirty)
            return;
        PlayerPrefs.Save();
        characterPrefsDirty = false;
    }

    void OnApplicationQuit()
    {
        FlushCharacterPrefs();
    }

    /// <summary>
    /// Standard VS layout by player index: unique wall/team cycle, A then B lane.
    /// P0 Up/T1, P1 Down/T2, P2 Right/T3, P3 Left/T4, P4–P7 same walls lane B.
    /// </summary>
    public static void ApplyIndexSeatDefaults(Player p)
    {
        if (p == null || p.index < 0)
            return;

        int i = p.index % 8;
        p.position = i >= 4 ? 1 : 0;
        switch (i % 4)
        {
            case 0: p.facing = Facing.Up; p.team = 1; break;
            case 1: p.facing = Facing.Down; p.team = 2; break;
            case 2: p.facing = Facing.Right; p.team = 3; break;
            default: p.facing = Facing.Left; p.team = 4; break;
        }
    }

    /// <summary>VS mode: every lobby seat gets index-based facing/team (skips position/team menus).</summary>
    public void ApplyVersusSeatLayout()
    {
        if (players == null)
            return;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null)
                ApplyIndexSeatDefaults(players[i]);
        }
    }

    public void RememberLobbySeat(Player p)
    {
        if (p == null || p.index < 0 || p.index >= lobbySeatMemory.Length)
            return;
        lobbySeatMemory[p.index] = new LobbySeatMemory
        {
            valid = true,
            facing = p.facing,
            position = p.position,
            team = p.team
        };
    }

    public void RememberAllLobbySeats()
    {
        if (players == null)
            return;
        for (int i = 0; i < players.Count; i++)
            RememberLobbySeat(players[i]);
    }

    public bool TryRestoreLobbySeat(Player p)
    {
        if (p == null || p.index < 0 || p.index >= lobbySeatMemory.Length)
            return false;
        LobbySeatMemory m = lobbySeatMemory[p.index];
        if (!m.valid)
            return false;
        p.facing = m.facing;
        p.position = m.position;
        p.team = m.team;
        return true;
    }

    /// <summary>Team Mode on: restore remembered seats, or keep current / index defaults.</summary>
    public void RestoreTeamModeSeats()
    {
        if (players == null)
            return;
        for (int i = 0; i < players.Count; i++)
        {
            Player p = players[i];
            if (p == null)
                continue;
            if (!TryRestoreLobbySeat(p))
                ApplyIndexSeatDefaults(p);
        }
    }

    /// <summary>
    /// Call when leaving Team Mode menus or confirming seats so VS↔Team toggles keep picks.
    /// </summary>
    public void EnsureLobbySeatsForStart()
    {
        if (positionSelect || teamSelect)
        {
            RememberAllLobbySeats();
            return;
        }

        // VS (no position/team menus): force index layout — everyone own team + wall by index
        ApplyVersusSeatLayout();
    }

    public bool RemembersRandomCharacter(int playerIndex)
    {
        if (playerIndex < 0)
            return false;
        return PlayerPrefs.GetString(SelectedCharacterPref + playerIndex, "") == RandomCharacterSentinel;
    }

    public Characters GetRememberedCharacter(int playerIndex)
    {
        if (playerIndex < 0 || characters == null)
            return null;

        string saved = PlayerPrefs.GetString(SelectedCharacterPref + playerIndex, "");
        if (string.IsNullOrEmpty(saved) || saved == RandomCharacterSentinel)
            return null;

        return characters.Find(x =>
            x != null
            && x.name == saved
            && x.active
            && x.character != null
            && x.character.prefabs != null);
    }

    public void ApplyRememberedOrRandomCharacter(Player p)
    {
        if (p == null)
            return;

        if (p.wantRandomCharacter || RemembersRandomCharacter(p.index))
        {
            p.SetRandomCharacterPending();
            return;
        }

        Characters remembered = GetRememberedCharacter(p.index);
        if (remembered != null)
            p.SetUpCharacter(remembered);
        else
            p.SetUpCharacter(RandomCharacter());

        PlayerSkin.AssignUniqueForCharacter(p, this);
    }

    public int RandomActiveFieldIndex()
    {
        if (fields == null || fields.Count == 0)
            return -1;

        List<int> active = new List<int>();
        for (int i = 0; i < fields.Count; i++)
        {
            if (fields[i] != null && fields[i].active)
                active.Add(i);
        }

        if (active.Count == 0)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i] != null)
                    active.Add(i);
            }
        }

        if (active.Count == 0)
            return -1;

        return active[UnityEngine.Random.Range(0, active.Count)];
    }

    public int RandomBallIndex()
    {
        if (balls == null || balls.Count == 0)
            return -1;
        return UnityEngine.Random.Range(0, balls.Count);
    }

    // Update is called once per frame
    void Update()
    {
        if (characterPrefsDirty && Time.unscaledTime >= characterPrefsSaveAt)
            FlushCharacterPrefs();

        ShowAndHidePlayerSelectors();
        HandlePauseInput();

        if (gameStart)
        {
            if (!winnerScreen && !LocalMatchPaused)
            {
                CheckPlayerConstrants();
                BallCheck();
                CheckForWinner();
            }
        }
    }

    int pauseActionFrame = -1;
    int winActionFrame = -1;
    string lastWinAction = "";
    Coroutine startRoutine;

    bool MenuPressedThisFrame()
    {
        if (controllers == null)
            return false;

        for (int i = 0; i < controllers.Count; i++)
        {
            ControllerLink link = controllers[i];
            if (link == null)
                continue;
            ControllerButtons menu = link["Menu"];
            if (menu != null && menu.wasPressedThisFrame)
                return true;
        }

        return false;
    }

    bool PlayingMenuOpen()
    {
        if (mm == null)
            mm = MenuManager.instance;
        if (mm == null)
            return false;

        MenuClass open = mm.GetOpenMenu(true);
        return open != null
            && !string.IsNullOrEmpty(open.title)
            && open.title.Trim().Equals("Playing", StringComparison.OrdinalIgnoreCase);
    }

    void HandlePauseInput()
    {
        if (!MenuPressedThisFrame())
            return;
        if (winnerScreen || someoneWon)
            return;
        if (!pauseMenuOpen && !PlayingMenuOpen())
            return;

        if (pauseMenuOpen)
            ClosePauseMenu();
        else
            OpenPauseMenu();
    }

    void OpenPauseMenu()
    {
        if (mm == null)
            mm = MenuManager.instance;
        if (mm == null)
            return;

        pauseMenuOpen = true;
        if (!onlineMatch)
            Time.timeScale = 0f;

        mm.OpenMenu("Pause");
        lastPlayerSelectorsShown = null;

        if (players == null)
            return;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null)
                players[i].state = "Pause";
        }
    }

    void ClosePauseMenu()
    {
        pauseMenuOpen = false;
        Time.timeScale = 1f;

        if (mm == null)
            mm = MenuManager.instance;
        if (mm != null)
            mm.RemoveMenu("Pause");

        lastPlayerSelectorsShown = null;

        if (players == null)
            return;

        for (int i = 0; i < players.Count; i++)
        {
            Player p = players[i];
            if (p != null && p.state == "Pause")
                p.state = "";
        }
    }

    public void PauseButtonPressed(string action)
    {
        if (pauseActionFrame == Time.frameCount)
            return;
        pauseActionFrame = Time.frameCount;

        if (string.IsNullOrEmpty(action))
            action = "Resume";

        switch (action)
        {
            case "Resume":
                ClosePauseMenu();
                break;
            case "Reset Match":
                ClosePauseMenu();
                RestartCurrentMatch();
                break;
            case "Reset Game":
                ClosePauseMenu();
                ReturnToCharacterSelect();
                break;
            case "Forfeit":
            case "Main Menu":
                ClosePauseMenu();
                LeaveMatchToMainMenu();
                break;
            default:
                ClosePauseMenu();
                break;
        }
    }

    void HaltStartRoutine()
    {
        if (startRoutine != null)
        {
            StopCoroutine(startRoutine);
            startRoutine = null;
        }
        startingGame = false;
        gameStart = false;
        Time.timeScale = 1f;
    }

    void DestroyLiveMatch()
    {
        if (players != null)
        {
            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];
                if (p == null)
                    continue;
                if (p.spawnedPlayer != null)
                    Destroy(p.spawnedPlayer);
                if (p.spawnedLifeline != null)
                    Destroy(p.spawnedLifeline);
                p.spawnedPlayer = null;
                p.spawnedLifeline = null;
            }
        }

        BallInfo[] ballsLive = FindObjectsByType<BallInfo>(FindObjectsInactive.Include);
        for (int i = ballsLive.Length - 1; i >= 0; i--)
        {
            if (ballsLive[i] != null)
                Destroy(ballsLive[i].gameObject);
        }

        Field_Info[] fieldsLive = FindObjectsByType<Field_Info>(FindObjectsInactive.Include);
        for (int i = fieldsLive.Length - 1; i >= 0; i--)
        {
            if (fieldsLive[i] != null)
                Destroy(fieldsLive[i].gameObject);
        }

        ClearAfterTheGame[] toClear = FindObjectsByType<ClearAfterTheGame>(FindObjectsInactive.Include);
        for (int i = toClear.Length - 1; i >= 0; i--)
        {
            if (toClear[i] != null)
                Destroy(toClear[i].gameObject);
        }

        ClearHolderChildren(playerInfoHolderLS);
        ClearHolderChildren(playerInfoHolderRS);
    }

    static void ClearHolderChildren(Transform holder)
    {
        if (holder == null)
            return;
        for (int i = holder.childCount - 1; i >= 0; i--)
            Destroy(holder.GetChild(i).gameObject);
    }

    void RestartCurrentMatch()
    {
        HaltStartRoutine();
        DestroyLiveMatch();
        ResetPlayersForNewMatch();
        CharactersPicked("balls");
    }

    void ReturnToCharacterSelect()
    {
        HaltStartRoutine();
        DestroyLiveMatch();
        AbortCurrentMatch();

        if (mm == null)
            mm = MenuManager.instance;
        if (mm != null)
        {
            if (mm.openMenu != null && mm.openMenu.Count > 1)
                mm.BackUnitl("Character Select");
            MenuClass open = mm.GetOpenMenu(true);
            if (open == null || string.IsNullOrEmpty(open.title)
                || !open.title.Trim().Equals("Character Select", StringComparison.OrdinalIgnoreCase))
            {
                mm.OpenMenu("Character Select");
            }
        }

        if (players == null)
            return;

        for (int i = 0; i < players.Count; i++)
        {
            Player p = players[i];
            if (p == null)
                continue;
            p.characterSelected = false;
            p.state = "Character Select";
            p.gridLock = false;
        }
    }

    void LeaveMatchToMainMenu()
    {
        HaltStartRoutine();
        DestroyLiveMatch();
        AbortCurrentMatch();

        if (mm == null)
            mm = MenuManager.instance;
        if (mm == null)
            return;

        mm.openMenu.Clear();
        mm.OpenMenu("Main Menu");
    }

    IEnumerator WaitMatchSecond()
    {
        float t = 0f;
        while (t < 1f)
        {
            if (!LocalMatchPaused)
                t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    bool? lastPlayerSelectorsShown;

    void ShowAndHidePlayerSelectors()
    {
        // Hide during active match; show on the win screen and the pause menu
        bool show = winnerScreen || pauseMenuOpen || !(gameStart || startingGame);
        if (lastPlayerSelectorsShown.HasValue && lastPlayerSelectorsShown.Value == show)
            return;
        lastPlayerSelectorsShown = show;
        if (playerSelectors != null)
            playerSelectors.gameObject.SetActive(show);
    }

    public int PlayerAdd(ControllerLink cL)
    {
        int result = -1;
        bool computerMode = false;

        // Character Creation: every device drives the draft slot (index 0)
        if (cL != null && CharacterCreationManager.IsActive)
        {
            int bound = CharacterCreationManager.instance.BindControllerToDraft(cL);
            if (bound >= 0)
                return bound;
        }

        // Human rejoining after Leave: take over an unbound CPU instead of spawning a duplicate
        if (cL != null && players != null)
        {
            Player reclaim = FindReclaimableCpuSlot();
            if (reclaim != null)
            {
                return BindControllerToExistingPlayer(reclaim, cL);
            }
        }

        if (players.Count < 8)
        {
            if(cL == null)
            {
                if (players.Count >= maxPlayers)
                {
                    return -1;
                }

                computerMode = true;
            }
            else if (players.Count >= maxPlayers)
            {
                return -1;
            }

            players.Add(new Player());

            int pc = players.Count - 1;
            Player p = players[pc];

            p.computer = computerMode;
            p.cLink = cL;
            if (cL != null)
            {
                if (p.inputLinks == null)
                    p.inputLinks = new System.Collections.Generic.List<ControllerLink>();
                if (!p.inputLinks.Contains(cL))
                    p.inputLinks.Add(cL);
            }
            // Lobby CPUs default to Easy. Training is authoring-only (not in the level cycle).
            if (computerMode)
                p.cpuDifficulty = ComputerAI.CpuDifficulty.Easy;

            int index = -1;
            for (int i = 0; i < 8; i++)
            {
                if (!players.Exists(x => x.index == i))
                {
                    index = i;
                    break;
                }
            }

            p.index = index;

            // VS / index defaults; Team Mode restores remembered seats when toggled on
            if (teamSelect && TryRestoreLobbySeat(p))
            { }
            else
                ApplyIndexSeatDefaults(p);

            if (cS != null)
            {
                if(cS.enabled)
                {
                    int cC = characters.FindIndex(x => x.name == p.name);
                    p.state = "Character Select";

                    if (cC < 0 || cC >= cS.characterGrabs.Count)
                    {
                        ApplyRememberedOrRandomCharacter(p);
                        cC = characters.FindIndex(x => x.name == p.name);
                    }
                }
            }

            AttachSelector(p, cL);

            result = index;

            if (cL != null && !controllers.Contains(cL))
            {
                controllers.Add(cL);
            }

            // 5+ players always require Team Mode
            if (players.Count >= TeamModeToggle.ForceTeamAtPlayerCount)
            {
                bool wasTeam = teamSelect;
                teamSelect = true;
                positionSelect = true;
                if (gametype == Gametype.Vs)
                    gametype = Gametype.Coop;
                if (!wasTeam)
                    RestoreTeamModeSeats();
            }
        }

        return result;
    }

    /// <summary>
    /// CPU slot left behind by Leave (or lobby fill) that a reconnecting human can take over.
    /// Prefers lowest index so the "last player" who DC'd reclaim their own seat.
    /// </summary>
    Player FindReclaimableCpuSlot()
    {
        Player best = null;
        for (int i = 0; i < players.Count; i++)
        {
            Player p = players[i];
            if (p == null || !p.computer || p.cLink != null)
                continue;
            if (best == null || p.index < best.index)
                best = p;
        }
        return best;
    }

    int BindControllerToExistingPlayer(Player p, ControllerLink cL)
    {
        if (p == null || cL == null)
            return -1;

        p.computer = false;
        p.cLink = cL;
        cL.index = p.index;
        if (p.inputLinks == null)
            p.inputLinks = new System.Collections.Generic.List<ControllerLink>();
        if (!p.inputLinks.Contains(cL))
            p.inputLinks.Add(cL);

        if (p.pso != null)
        {
            Destroy(p.pso.gameObject);
            p.pso = null;
        }

        AttachSelector(p, cL);

        if (!controllers.Contains(cL))
            controllers.Add(cL);

        return p.index;
    }

    void AttachSelector(Player p, ControllerLink cL)
    {
        if (p == null || playerSelectorGobj == null || playerSelectors == null)
            return;

        // CPU fillers don't get a cursor
        if (cL == null)
            return;

        GameObject go = Instantiate(playerSelectorGobj, playerSelectors);

        PlayerSelectorObj pso = go.GetComponent<PlayerSelectorObj>();
        pso.cLink = cL;
        if (p.skinColorIndex < 0)
            p.skinColorIndex = p.index;
        PlayerColors startColor = PlayerSkin.GetPlayerColors(p, this);
        pso.SetCircleColor(startColor != null
            ? startColor
            : playerColors[Mathf.Clamp(p.index, 0, playerColors.Count - 1)]);
        pso.pI = p.index;
        p.pso = pso;
    }

    public void UpdateCharSelect()
    {
        for(int i = players.Count - 1; i >= 0; i--)
        {
            Player p = players[i];

            if(p.characterSelected)
            {
                p.characterSelected = false;
                p.gridLock = false;
            }
        }
    }

    void CheckPlayerConstrants()
    {
        if(players.Count > 0)
        {
            for(int x = 0; x < players.Count; x++)
            {
                Player p = players[x];

                if (p.canMove.Count > 0)
                {
                    for (int i = p.canMove.Count - 1; i >= 0; i--)
                    {
                        if (p.canMove[i].endTime <= Time.time && p.canMove[i].endTime >= 0)
                        {
                            p.canMove.RemoveAt(i);
                        }
                    }
                }

                if (p.canBump.Count > 0)
                {
                    for (int i = p.canBump.Count - 1; i >= 0; i--)
                    {
                        if (p.canBump[i].endTime <= Time.time && p.canBump[i].endTime >= 0)
                        {
                            p.canBump.RemoveAt(i);
                        }
                    }
                }

                if (p.canSuper.Count > 0)
                {
                    for (int i = p.canSuper.Count - 1; i >= 0; i--)
                    {
                        if (p.canSuper[i].endTime <= Time.time && p.canSuper[i].endTime >= 0)
                        {
                            p.canSuper.RemoveAt(i);
                        }
                    }
                }

                if (p.canDash != null && p.canDash.Count > 0)
                {
                    for (int i = p.canDash.Count - 1; i >= 0; i--)
                    {
                        if (p.canDash[i].endTime <= Time.time && p.canDash[i].endTime >= 0)
                        {
                            p.canDash.RemoveAt(i);
                        }
                    }
                }

                if (p.moveSpeedModifiers != null && p.moveSpeedModifiers.Count > 0)
                {
                    for (int i = p.moveSpeedModifiers.Count - 1; i >= 0; i--)
                    {
                        MoveSpeedModifier mod = p.moveSpeedModifiers[i];
                        if (mod != null && mod.endTime <= Time.time && mod.endTime >= 0)
                        {
                            p.moveSpeedModifiers.RemoveAt(i);
                        }
                    }
                }
            }
        }
    }

    void BallCheck()
    {
        // Throttle respawn scan — was FindObjects every frame
        if (Time.time < nextBallCheckAt)
            return;
        nextBallCheckAt = Time.time + 0.2f;
        EnsureMatchBalls(true);
    }

    readonly List<Player> winnersBuffer = new List<Player>(8);
    readonly List<int> aliveTeamsBuffer = new List<int>(4);

    void CheckForWinner()
    {
        // Character Creation sandbox never ends a match
        if (CharacterCreationManager.IsActive)
            return;

        winnersBuffer.Clear();
        aliveTeamsBuffer.Clear();

        if (players.Count > 0)
        {
            // Players whose character was destroyed but health wasn't cleared still block the match end.
            // Skip while a rematch/new match is loading (gameStart false / startingGame).
            if (!startingGame)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    Player p = players[i];
                    if (p.currentHealth > 0 && p.spawnedPlayer == null)
                    {
                        p.currentHealth = 0;
                    }
                }
            }

            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];
                if (p == null || p.currentHealth <= 0)
                    continue;
                winnersBuffer.Add(p);
                int team = p.team;
                if (!aliveTeamsBuffer.Contains(team))
                    aliveTeamsBuffer.Add(team);
            }
        }

        int eliminated = players.Count - winnersBuffer.Count;
        // FFA / default: last person standing. Team select: last team standing.
        // Require someone eliminated so solo practice doesn't instantly end.
        bool matchOver = eliminated > 0 && (
            teamSelect
                ? aliveTeamsBuffer.Count <= 1
                : winnersBuffer.Count <= 1
        );

        if (matchOver)
        {
            //Set Winners and Losers
            if (players.Count > 0)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    Player p = players[i];
                    p.won = winnersBuffer.Contains(p);
                }
            }

            // AI learns from match outcome (persisted across sessions)
            ComputerAI.OnMatchEnd(players);

            //Run Time Slow
            if (!someoneWon)
            {
                winnerScreen = true;
                someoneWon = true;
                StartCoroutine(SomeoneWon());
            }
        }
    }

    /// <summary>
    /// Hard-stop an in-progress or ending match (training abort / cleanup).
    /// Does not open Main Menu.
    /// </summary>
    public void AbortCurrentMatch()
    {
        gameStart = false;
        someoneWon = false;
        startingGame = false;
        winnerScreen = false;
        Time.timeScale = 1f;

        ClearAfterTheGame[] toClear = FindObjectsByType<ClearAfterTheGame>(FindObjectsInactive.Include);
        for (int i = toClear.Length - 1; i >= 0; i--)
        {
            if (toClear[i] != null)
                Destroy(toClear[i].gameObject);
        }

        if (showWinner != null && showWinner.childCount > 0)
        {
            for (int i = showWinner.childCount - 1; i >= 0; i--)
                Destroy(showWinner.GetChild(i).gameObject);
        }

        if (players != null)
        {
            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];
                if (p == null)
                    continue;
                p.spawnedPlayer = null;
                p.spawnedLifeline = null;
                p.won = false;
                p.characterSelected = false;
                p.state = "";
                p.winScrollTarget = null;
            }
        }

        if (mm != null && mm.openMenu != null)
        {
            // Drop Playing / Winners overlays if present
            mm.RemoveMenu("Winners");
            mm.RemoveMenu("Playing");
        }
    }

    /// <summary>Reset match flags + player combat state without choosing a win-menu action.</summary>
    public void ResetPlayersForNewMatch()
    {
        gameStart = false;
        someoneWon = false;
        startingGame = false;
        winnerScreen = false;
        Time.timeScale = 1f;

        if (showWinner != null && showWinner.childCount > 0)
        {
            for (int i = showWinner.childCount - 1; i >= 0; i--)
                Destroy(showWinner.GetChild(i).gameObject);
        }

        if (players == null)
            return;

        for (int i = 0; i < players.Count; i++)
        {
            Player p = players[i];
            if (p == null)
                continue;

            p.characterSelected = false;
            p.lastGridUpdate = 0;
            p.gridLock = false;
            p.state = "";
            p.winScrollTarget = null;
            p.won = false;
            p.damageDealt = 0;
            p.damageTaken = 0;
            p.ballHits = 0;
            p.longestBallOwnership = 0;
            p.highestSingleDamgeDealt = 0;
            p.highestSingleDamageTaken = 0;
            p.ultsUsed = 0;
            p.numberOfDashes = 0;
            p.afterDeathHits = 0;
            p.afterDeathDamage = 0;
            p.spawnedPlayer = null;
            p.spawnedLifeline = null;
        }
    }

    IEnumerator SomeoneWon()
    {
        Time.timeScale = .3f;
        yield return null;
        yield return new WaitForSecondsRealtime(1.5f);

        Time.timeScale = 1;
        yield return null;

        // Only clear objects marked for match cleanup.
        // Do NOT SetActive/toggle every GameObject — that corrupts Canvas layout (Invalid AABB)
        // and can freeze the match with winnerScreen stuck true if the coroutine dies mid-loop.
        ClearAfterTheGame[] toClear = FindObjectsByType<ClearAfterTheGame>(FindObjectsInactive.Include);
        for (int i = toClear.Length - 1; i >= 0; i--)
        {
            if (toClear[i] != null)
            {
                Destroy(toClear[i].gameObject);
            }
        }
        yield return null;

        // Training auto-run: skip winners UI (TrainingManager continues the session)
        bool trainingAuto = aiTrainingSession
            && TrainingManager.instance != null
            && TrainingManager.instance.ShouldAutoContinue();

        if (!trainingAuto)
        {
            //Open Winners menu
            if (mm != null)
            {
                mm.OpenMenu("Winners");
            }
            yield return null;

            EnsureWinnersButtonsClickable();

            if (mmOp != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    mmOp.AddPlayer(i);
                }
            }

            //Add Win Box to Winner Menu
            if (players.Count > 0 && wonBox != null && showWinner != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    GameObject wB = Instantiate(wonBox, showWinner);
                    RectTransform rt = wB.transform as RectTransform;
                    if (rt != null)
                    {
                        rt.localScale = Vector3.one;
                        rt.localRotation = Quaternion.identity;
                    }

                    GameResultBreakdown grb = wB.GetComponent<GameResultBreakdown>();
                    if (grb != null)
                    {
                        grb.playerIndex = i;
                    }

                    // Free cursor + menu buttons; scroll only after clicking a stats card
                    players[i].state = "Winners";
                    players[i].winScrollTarget = null;
                }
            }
        }
        else
        {
            // Keep winnerScreen true briefly so TrainingManager.Update can see the end
            yield return null;
        }

        someoneWon = false;
        yield return null;
    }

    void EnsureWinnersButtonsClickable()
    {
        GridControl[] controls = FindObjectsByType<GridControl>(FindObjectsInactive.Include);
        for (int i = 0; i < controls.Length; i++)
        {
            GridControl gc = controls[i];
            if (gc == null || gc.group == null)
                continue;
            if (gc.group.ToLower().Trim() != "winners")
                continue;

            GameObject go = gc.gameObject;
            SelectorClickable.Ensure(go, 100f);

            WinMenuButton btn = go.GetComponent<WinMenuButton>();
            if (btn == null)
                btn = go.AddComponent<WinMenuButton>();

            string n = go.name.ToLowerInvariant();
            if (n.Contains("replay") || n.Contains("rematch"))
                btn.buttonPressed = "Rematch";
            else if (n.Contains("champion"))
                btn.buttonPressed = "Champion Select";
            else if (n.Contains("level") || n.Contains("field"))
                btn.buttonPressed = "Level Select";
            else if (n.Contains("ball"))
                btn.buttonPressed = "Ball Select";
            else if (n.Contains("main"))
                btn.buttonPressed = "Main Menu";
        }
    }

    public void WinButtonPressed(string buttonPressed)
    {
        if (winActionFrame == Time.frameCount && lastWinAction == buttonPressed)
            return;
        winActionFrame = Time.frameCount;
        lastWinAction = buttonPressed;

        // Stop match logic first — otherwise CheckForWinner sees destroyed
        // spawnedPlayer refs, zeros restored health, and instantly re-ends the match.
        gameStart = false;
        someoneWon = false;
        startingGame = false;

        //Clear WonBoxes
        if (showWinner != null && showWinner.childCount > 0)
        {
            for (int i = showWinner.childCount - 1; i >= 0; i--)
            {
                Destroy(showWinner.GetChild(i).gameObject);
            }
        }
        winnerScreen = false;

        //Update Players
        for (int i = 0; i < players.Count; i++)
        {
            Player p = players[i];

            //Clear Stats
            p.characterSelected = false;
            p.lastGridUpdate = 0;
            p.gridLock = false;
            p.state = "";
            p.winScrollTarget = null;
            p.won = false;
            p.damageDealt = 0;
            p.damageTaken = 0;
            p.ballHits = 0;
            p.longestBallOwnership = 0;
            p.highestSingleDamgeDealt = 0;
            p.highestSingleDamageTaken = 0;
            p.ultsUsed = 0;
            p.numberOfDashes = 0;
            p.afterDeathHits = 0;
            p.afterDeathDamage = 0;
            p.spawnedPlayer = null;
            p.spawnedLifeline = null;

            // Keep random picks deferred; otherwise restore roster bind for rematch / menus
            if (p.wantRandomCharacter || RemembersRandomCharacter(p.index))
            {
                p.SetRandomCharacterPending();
            }
            else
            {
                Characters character = characters.Find(x => x.name == p.name);
                if (character != null)
                    p.SetUpCharacter(character);
            }
        }

        //Perform action
        switch (buttonPressed)
        {
            case "Rematch":
                CharactersPicked("balls");
                break;
            case "Champion Select":
                mm.BackUnitl("Character Select");
                break;
            case "Level Select":
                if (mm != null)
                {
                    mm.RemoveMenu("Winners");
                    mm.RemoveMenu("Playing");
                    mm.OpenMenu("Field Select");
                }
                break;
            case "Ball Select":
                if (mm != null)
                {
                    mm.RemoveMenu("Winners");
                    mm.RemoveMenu("Playing");
                    mm.OpenMenu("Ball Select");
                }
                break;
            case "Main Menu":
                mm.openMenu.Clear();
                mm.OpenMenu("Main Menu");
                break;
            default://Main Menu
                mm.openMenu.Clear();
                mm.OpenMenu("Main Menu");
                break;
        }
    }

    public Characters RandomCharacter(bool actives = true)
    {
        List<Characters> avaliableChar = characters;

        if (actives)
        {
            avaliableChar = characters.FindAll(x => x.active);
        }

        if (avaliableChar == null || avaliableChar.Count == 0)
        {
            avaliableChar = characters.FindAll(x => x != null && x.character != null && x.character.prefabs != null);
        }

        if (avaliableChar == null || avaliableChar.Count == 0)
            return null;

        return new Characters(avaliableChar[UnityEngine.Random.Range(0, avaliableChar.Count)]);
    }

    public bool InSetup()
    {
        bool result = true;

        return result;
    }

    public void CharactersPicked(string fromSelect)
    {
        bool startGame = false;

        //Reset all player controls
        foreach(Player p in players)
        {
            if(p.pso != null)
            {
                p.pso.cpuControl = -1;
            }
        }

        switch(fromSelect.ToLower().Trim())
        {
            case "characters":
                if (levelSelect)
                {
                    mm.OpenMenu("Field Select");
                }
                else if (positionSelect)
                {
                    mm.OpenMenu("Position Select");
                }
                else if (teamSelect)
                {
                    mm.OpenMenu("Team Select");
                }
                else if (ballSelect)
                {
                    mm.OpenMenu("Ball Select");
                }
                else
                {
                    //Start Game
                    startGame = true;
                }
                break;
            case "fields":
                if (positionSelect)
                {
                    mm.OpenMenu("Position Select");
                }
                else if (teamSelect)
                {
                    mm.OpenMenu("Team Select");
                }
                else if (ballSelect)
                {
                    mm.OpenMenu("Ball Select");
                }
                else
                {
                    //Start Game
                    startGame = true;
                }
                break;
            case "positions":
                if (teamSelect)
                {
                    mm.OpenMenu("Team Select");
                }
                else if (ballSelect)
                {
                    mm.OpenMenu("Ball Select");
                }
                else
                {
                    //Start Game
                    startGame = true;
                }
                break;
            case "teams":
                if (ballSelect)
                {
                    mm.OpenMenu("Ball Select");
                }
                else
                {
                    //Start Game
                    startGame = true;
                }
                break;
            case "balls":
                //Start Game
                startGame = true;
                break;
        }

        for (int i = 0; i < players.Count; i++)
        {
            players[i].characterSelected = false;
            players[i].gridLock = false;
        }

        // Persist team-mode seats; VS skips menus so re-apply index walls/teams
        EnsureLobbySeatsForStart();

        if(startGame && !startingGame)
        {
            if (startRoutine != null)
                StopCoroutine(startRoutine);
            startRoutine = StartCoroutine(StartGame());
            startingGame = true;
        }
    }

    IEnumerator StartGame()
    {
        mm.OpenMenu("Playing");
        yield return null;

        Vector3 pPos = Vector3.zero;
        Vector3 fRot = Vector3.zero;

        if(gametype == Gametype.Coop || gametype == Gametype.Vs)
        {
            #region Create Field
            int sF = selectedField;

            if (selectedField < 0 || selectedField >= fields.Count)
                sF = RandomActiveFieldIndex();

            if (sF < 0 || sF >= fields.Count)
            {
                Debug.LogError("StartGame: no field available to spawn.");
                yield break;
            }

            Field field = fields[sF];
            fieldSize = field.size + 10;

            GameObject fSpawned = Instantiate(fieldObj);
            fSpawned.transform.position = new Vector3(0, 0, fieldSize);
            Field_Info fI = fSpawned.GetComponent<Field_Info>();
            fI.field = field;
            yield return null;
            Physics.SyncTransforms(); // walls ready for lifeline raycasts

            // Fit Z so top/bottom walls meet the camera frustum (keep Test Zone as-is)
            float framed = FieldCameraFit.Apply(fSpawned.transform, Camera.main, fieldSize);
            SetFieldPlaySize(framed);
            yield return null;
            Physics.SyncTransforms();

            // Hazards (wagons, towers, floating logs, …) — skipped when Options → Hazards is off
            FieldHazardSpawner.SpawnForField(fSpawned.transform, field);
            #endregion

            #region Add Players
            // Phase 1 — bind roster / clear match stats for every seat
            int spawnCount = Mathf.Min(maxPlayers, players.Count);
            for (int i = 0; i < spawnCount; i++)
            {
                Player p = players[i];
                if (p == null)
                    continue;

                p.won = false;
                p.damageDealt = 0;
                p.damageTaken = 0;
                p.ballHits = 0;
                p.longestBallOwnership = 0;
                p.highestSingleDamgeDealt = 0;
                p.highestSingleDamageTaken = 0;
                p.ultsUsed = 0;
                p.numberOfDashes = 0;
                p.afterDeathHits = 0;
                p.afterDeathDamage = 0;

                bool keepCreationDraft = CharacterCreationManager.IsActive
                    && p.index == 0
                    && p.character != null
                    && p.character.prefabs != null;

                if (!keepCreationDraft)
                {
                    Characters rosterChar = null;
                    if (p.wantRandomCharacter || RemembersRandomCharacter(p.index))
                    {
                        p.wantRandomCharacter = true;
                        rosterChar = RandomCharacter();
                    }
                    else if (!string.IsNullOrEmpty(p.name))
                    {
                        rosterChar = characters.Find(x => x != null && x.name == p.name);
                    }

                    if (rosterChar == null || rosterChar.character == null || rosterChar.character.prefabs == null)
                        rosterChar = RandomCharacter();

                    if (rosterChar != null)
                        p.SetUpCharacter(rosterChar);
                }

                if (p.computer)
                    ComputerAI.AssignMatchBrain(p);
            }

            // Tic same-wall pairs: side-by-side or fused machine + wing bumper
            TicCoopLayout.Prepare(players, fieldSize);

            // Phase 2 — spawn (hosts before wing forms so partners can find the machine)
            List<int> spawnOrder = new List<int>(spawnCount);
            for (int i = 0; i < spawnCount; i++)
            {
                if (players[i] != null && !players[i].ticWingForm)
                    spawnOrder.Add(i);
            }
            for (int i = 0; i < spawnCount; i++)
            {
                if (players[i] != null && players[i].ticWingForm)
                    spawnOrder.Add(i);
            }

            for (int oi = 0; oi < spawnOrder.Count; oi++)
            {
                int i = spawnOrder[oi];
                Player p = players[i];
                if (p == null)
                    continue;

                if (p.character == null || p.character.prefabs == null)
                {
                    if (CharacterCreationManager.IsActive && p.index == 0)
                    {
                        Debug.Log("[CharacterCreation] Draft has no character prefab yet — assign one in Object Assign (Ctrl+Shift+O).");
                        continue;
                    }
                    if (!p.ticWingForm)
                    {
                        Debug.LogError($"StartGame: Player index={p.index} name='{p.name}' has no character prefab after roster bind. Skipping spawn.");
                        continue;
                    }
                }

                switch (p.facing)
                {
                    case Facing.Up:
                        fRot = new Vector3(0, 0, 0);
                        break;
                    case Facing.Down:
                        fRot = new Vector3(0, 0, 180);
                        break;
                    case Facing.Left:
                        fRot = new Vector3(0, 0, 90);
                        break;
                    case Facing.Right:
                        fRot = new Vector3(0, 0, 270);
                        break;
                }

                // Snap to the real wall surface (large maps often don't match fieldSize/2 math)
                pPos = GetSpawnWallPos(p.facing);

                if (p.ticWingForm)
                {
                    SpawnTicWingBumperPlayer(p, pPos, fRot);
                }
                else
                {
                    p.spawnedPlayer = Instantiate(p.character.prefabs);
                    p.spawnedPlayer.transform.position = pPos;
                    p.spawnedPlayer.transform.rotation = Quaternion.Euler(fRot + p.character.rotationOffset);
                    p.spawnedPlayer.transform.position += p.spawnedPlayer.transform.right * p.character.positionOffset.x;

                    // Tic side-by-side: same depth, split along the wall. Otherwise legacy stack.
                    if (Mathf.Abs(p.ticWallSideOffset) > 0.01f)
                    {
                        p.spawnedPlayer.transform.position += p.spawnedPlayer.transform.up * p.character.positionOffset.y;
                        p.spawnedPlayer.transform.position += p.spawnedPlayer.transform.right * p.ticWallSideOffset;
                    }
                    else
                    {
                        p.spawnedPlayer.transform.position += p.spawnedPlayer.transform.up
                            * (p.character.positionOffset.y * ((p.position == 0) ? 1f : 1.5f));
                    }

                    p.spawnedPlayer.transform.position += p.spawnedPlayer.transform.forward * p.character.positionOffset.z;
                    PlayerGrab pG = p.spawnedPlayer.GetComponent<PlayerGrab>();
                    if (pG != null)
                        pG.playerIndex = p.index;

                    // Shared CPU brain — new characters get AI intents automatically
                    if (p.computer)
                        ComputerBrain.Ensure(p.spawnedPlayer);

                    if (!SkipPlayerSkin.ShouldSkipRoot(p.spawnedPlayer)
                        && p.spawnedPlayer.GetComponentInChildren<Field_Info>(true) == null)
                        PlayerSkin.Apply(p.spawnedPlayer, p, this);

                    MuriMove muri = p.spawnedPlayer.GetComponent<MuriMove>();
                    if (muri != null)
                        muri.PlaceOnBestSupport();

                    // Fused host keeps the only lifeline (double HP already applied in Prepare)
                    if (p.lifeline != null && p.lifeline.prefabs != null)
                    {
                        p.spawnedLifeline = Instantiate(p.lifeline.prefabs);
                        PlaceLifelineOnWall(p.spawnedLifeline, p.facing, p.spawnedPlayer.transform.position, fRot, p.lifeline);
                        SpreadTriggerLifelineParts(p.spawnedLifeline, p);
                        PlayerGrab lifePG = p.spawnedLifeline.GetComponent<PlayerGrab>();
                        if (lifePG != null)
                            lifePG.playerIndex = p.index;
                        if (!SkipPlayerSkin.ShouldSkipRoot(p.spawnedLifeline))
                            PlayerSkin.Apply(p.spawnedLifeline, p, this);
                    }
                }

                GameObject sIB = p.playerInfo != null ? p.playerInfo : playerInfo;
                if (sIB != null)
                {
                    GameObject iB = Instantiate(sIB);
                    if ((i % 2) == 0)
                        iB.transform.SetParent(playerInfoHolderRS);
                    else
                        iB.transform.SetParent(playerInfoHolderLS);

                    PlayerGrab ibpG = iB.GetComponent<PlayerGrab>();
                    if (ibpG != null)
                        ibpG.playerIndex = p.index;
                }
                yield return null;
            }
            #endregion

            #region Add Ball
            EnsureBallSlots();
            bool anySpawned = false;
            for (int slot = 0; slot < ballCount; slot++)
            {
                if (SpawnMatchBall(slot, false) != null)
                    anySpawned = true;
                yield return null;
            }

            if (!anySpawned)
            {
                Debug.LogError("StartGame: no ball available to spawn.");
                yield break;
            }
            #endregion

            #region Start Count Down
            gameplayinfo.gameObject.SetActive(true);
            int countdown = 5;
            yield return null;

            for (int c = countdown; c >= -1; c--)
            {
                if(c > 3)
                {
                    gameplayinfo.text = "Ready?";
                }
                else if(c >= 0)
                {
                    gameplayinfo.text = c.ToString();
                }
                else
                {
                    gameplayinfo.text = "Go!";
                }
                yield return WaitMatchSecond();
            }

            gameplayinfo.gameObject.SetActive(false);
            #endregion

            // Release all match-slot balls after countdown (extras keep matchSlot < 0)
            for (int i = 0; i < LiveBallRegistry.Count; i++)
            {
                BallInfo info = LiveBallRegistry.GetAt(i);
                if (info != null && info.matchSlot >= 0)
                    info.ballReady = true;
            }
            gameStart = true;
            yield return null;
        }

        startingGame = false;
        startRoutine = null;
        yield return null;
    }

    /// <summary>
    /// Trigger lifeline is several Capsules at fixed local X (±1.4 / ±2.8). That spacing fits the
    /// default test zone (fieldSize ≈ 20). On larger playfields, scale lateral spacing by wall span
    /// so the barrier stays even and more spread out relative to the left/right walls.
    /// </summary>
    void SpreadTriggerLifelineParts(GameObject lifeline, Player p)
    {
        if (lifeline == null || p == null || p.character == null)
            return;
        bool isTrigger =
            (!string.IsNullOrEmpty(p.name) &&
             p.name.Equals("Trigger", StringComparison.OrdinalIgnoreCase))
            || lifeline.name.StartsWith("Trigger", StringComparison.OrdinalIgnoreCase);
        if (!isTrigger)
            return;

        Transform root = lifeline.transform;
        if (root.childCount < 2)
            return;

        List<Transform> parts = new List<Transform>(root.childCount);
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c != null)
                parts.Add(c);
        }
        if (parts.Count < 2)
            return;

        parts.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));

        // Measure wall span along the lifeline's right axis
        Vector3 origin = root.position + root.up * 0.5f;
        float leftDist = RayDistanceToWall(origin, -root.right);
        float rightDist = RayDistanceToWall(origin, root.right);
        if (leftDist <= 0.01f || rightDist <= 0.01f)
        {
            float half = Mathf.Max(4f, fieldSize * 0.5f);
            leftDist = half;
            rightDist = half;
        }

        // Place outer capsules so their edge sits on the side walls (tiny skin only)
        float radius = OuterCapsuleRadius(parts[0]);
        float leftX = -(leftDist - radius);
        float rightX = rightDist - radius;
        if (rightX < leftX)
        {
            float mid = (leftX + rightX) * 0.5f;
            leftX = mid - 0.25f;
            rightX = mid + 0.25f;
        }

        for (int i = 0; i < parts.Count; i++)
        {
            float t = parts.Count == 1 ? 0.5f : i / (float)(parts.Count - 1);
            Vector3 lp = parts[i].localPosition;
            lp.x = Mathf.Lerp(leftX, rightX, t);
            parts[i].localPosition = lp;
        }
    }

    static float OuterCapsuleRadius(Transform part)
    {
        if (part == null)
            return 0.25f;

        CapsuleCollider cap = part.GetComponent<CapsuleCollider>();
        if (cap != null)
        {
            float s = Mathf.Max(part.lossyScale.x, part.lossyScale.z);
            return Mathf.Max(0.08f, cap.radius * s);
        }

        Collider col = part.GetComponent<Collider>();
        if (col != null)
            return Mathf.Max(0.08f, col.bounds.extents.x);

        return 0.25f;
    }

    float RayDistanceToWall(Vector3 origin, Vector3 dir)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, Mathf.Max(fieldSize * 2f, 100f), ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return -1f;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            string tag = hits[i].transform != null ? hits[i].transform.tag : null;
            if (tag == "Wall" || tag == "Walls" || tag == "Obstacle")
                return hits[i].distance;
        }
        return -1f;
    }

    /// <summary>
    /// Place a lifeline flush on that side's wall. Uses the player's lateral position along the wall
    /// (coop can sit further inward) but never inherits the player's depth off the wall.
    /// </summary>
    /// <summary>
    /// Second Tic on a cramped wall: controllable wing-bumper loaded into the host machine.
    /// </summary>
    void SpawnTicWingBumperPlayer(Player p, Vector3 wallPos, Vector3 facingEuler)
    {
        if (p == null)
            return;

        Tic host = null;
        Player hostPlayer = null;
        if (p.ticPartnerIndex >= 0 && p.ticPartnerIndex < players.Count)
            hostPlayer = players[p.ticPartnerIndex];
        if (hostPlayer != null && hostPlayer.spawnedPlayer != null)
            host = hostPlayer.spawnedPlayer.GetComponent<Tic>();

        GameObject prefab = ticWingBumperPrefab;
        if (prefab == null)
            prefab = Resources.Load<GameObject>("TicWing");

        Vector3 spawnPos = wallPos + Quaternion.Euler(facingEuler) * Vector3.up * 2f;
        Quaternion spawnRot = Quaternion.Euler(facingEuler);
        if (host != null && host.loadPoint != null)
        {
            spawnPos = host.loadPoint.position;
            spawnRot = host.transform.rotation;
        }

        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab, spawnPos, spawnRot);
        }
        else
        {
            go = TicWingBumper.CreateRuntimePrefabInstance();
            go.transform.position = spawnPos;
            go.transform.rotation = spawnRot;
        }

        go.name = "TicWingBumper_" + p.index;
        go.tag = "Paddle";
        p.spawnedPlayer = go;
        p.spawnedLifeline = null; // fused host owns the lifeline

        PlayerGrab grab = go.GetComponent<PlayerGrab>();
        if (grab == null)
            grab = go.AddComponent<PlayerGrab>();
        grab.playerIndex = p.index;

        TicBumper bumper = go.GetComponent<TicBumper>();
        if (bumper == null)
            bumper = go.AddComponent<TicBumper>();

        TicWingBumper wing = go.GetComponent<TicWingBumper>();
        if (wing == null)
            wing = go.AddComponent<TicWingBumper>();

        if (host != null)
        {
            wing.SetHost(host);
            bumper.Init(host, p.index);
            if (host.launchArea != null)
                host.launchArea.RegisterBumperPassThrough(bumper);
            host.RegisterLiveBumper(bumper);
        }

        PlayerSkin.Apply(go, p, this);
    }

    void PlaceLifelineOnWall(GameObject lifeline, Facing facing, Vector3 playerWorldPos, Vector3 facingEuler, ObjectInfo lifeInfo)
    {
        if (lifeline == null)
            return;

        Physics.SyncTransforms();

        Vector3 wallPos = GetMathematicalWallPos(facing);
        Vector3 rayWall;
        if (TryRaycastWall(facing, out rayWall))
        {
            // Keep ray depth only — lateral comes from the player
            switch (facing)
            {
                case Facing.Up:
                case Facing.Down:
                    wallPos.y = rayWall.y;
                    break;
                case Facing.Left:
                case Facing.Right:
                    wallPos.x = rayWall.x;
                    break;
            }
        }

        // Match player lane along the wall; ignore how far inward the character sits
        switch (facing)
        {
            case Facing.Up:
            case Facing.Down:
                wallPos.x = playerWorldPos.x;
                break;
            case Facing.Left:
            case Facing.Right:
                wallPos.y = playerWorldPos.y;
                break;
        }

        wallPos.z = fieldSize;

        Quaternion rot = Quaternion.Euler(facingEuler + (lifeInfo != null ? lifeInfo.rotationOffset : Vector3.zero));
        lifeline.transform.rotation = rot;
        lifeline.transform.position = wallPos;

        Vector3 offset = lifeInfo != null ? lifeInfo.positionOffset : Vector3.zero;
        // x = along wall, y = small inward inset from wall into playfield, z = forward
        lifeline.transform.position += lifeline.transform.right * offset.x;
        lifeline.transform.position += lifeline.transform.up * offset.y;
        lifeline.transform.position += lifeline.transform.forward * offset.z;

        // Keep physics bodies from drifting off the wall on spawn
        Rigidbody rb = lifeline.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = lifeline.transform.position;
            rb.rotation = lifeline.transform.rotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    Vector3 GetMathematicalWallPos(Facing facing)
    {
        float half = fieldSize * 0.5f;
        switch (facing)
        {
            case Facing.Up:
                return new Vector3(0f, -half, fieldSize);
            case Facing.Down:
                return new Vector3(0f, half, fieldSize);
            case Facing.Left:
                return new Vector3(half, 0f, fieldSize);
            case Facing.Right:
                return new Vector3(-half, 0f, fieldSize);
            default:
                return new Vector3(0f, 0f, fieldSize);
        }
    }

    /// <summary>
    /// Wall anchor for character spawn. Prefer a raycast hit so large/custom fields
    /// whose walls aren't exactly at fieldSize/2 still place Tic flush (then inset by offset.y).
    /// </summary>
    Vector3 GetSpawnWallPos(Facing facing)
    {
        Vector3 wallPos = GetMathematicalWallPos(facing);
        if (TryRaycastWall(facing, out Vector3 rayWall))
        {
            switch (facing)
            {
                case Facing.Up:
                case Facing.Down:
                    wallPos.y = rayWall.y;
                    break;
                case Facing.Left:
                case Facing.Right:
                    wallPos.x = rayWall.x;
                    break;
            }
        }
        wallPos.z = fieldSize;
        return wallPos;
    }

    bool TryRaycastWall(Facing facing, out Vector3 point)
    {
        point = Vector3.zero;
        Vector3 origin = new Vector3(0f, 0f, fieldSize);
        Vector3 dir;

        switch (facing)
        {
            case Facing.Up: dir = Vector3.down; break;
            case Facing.Down: dir = Vector3.up; break;
            case Facing.Left: dir = Vector3.right; break;
            case Facing.Right: dir = Vector3.left; break;
            default: return false;
        }

        float maxDist = Mathf.Max(fieldSize * 2f, 100f);
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, maxDist, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            string tag = hits[i].transform.tag;
            if (tag != "Wall" && tag != "Walls" && tag != "Obstacle")
                continue;

            point = hits[i].point;
            point.z = fieldSize;
            return true;
        }

        return false;
    }

    public void PlaySound(string sname)
    {   
        Sounds s = extraSounds.Find(x => x.name.ToLower().Trim() == sname.ToLower().Trim());

        if(s != null)
        {
            if(s.sound != null)
            {
                effectAudio.PlayOneShot(s.sound);
            }
        }
    }
}

public enum Effect
{
    Patrol, //Moves back and forth in an area
    LookOut, //Rotates Pawn
    Freeze, //Adds the Freeze effect to a ball hit
    Vamp, //Adds the Vamp effect to a ball hit
    Bunker, //Burst into a swarm of 1 shot balls
    WeaponUp //Will increase the balls damage on hit
}

[System.Serializable]
public class Effects
{
    public Effect effect;
    public GameObject obj;
}

[System.Serializable]
public enum Gametype
{
    Arcade,
    Story,
    Coop,
    Vs
}

[System.Serializable]
public class Sounds
{
    public string name;
    public AudioClip sound;
}

[System.Serializable]
public enum Thought
{
    Nothing,
    MoveLeft,
    MoveRight,
    MoveUp,
    MoveDown
}