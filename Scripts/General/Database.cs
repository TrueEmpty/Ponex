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
    int fieldSize = 0;
    /// <summary>Current match playfield width (field.size + 10). 0 before a match starts.</summary>
    public float FieldPlaySize => fieldSize;

    public GameObject outofBounds;
    public GameObject background;
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
            LoadCharactersFromAssets();
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

        // In-game unlink control: long-press Leave / Go CPU (bottom-left with cursors)
        DisconnectPlayerButton.EnsureExists(playerSelectors);

        BackButtonClick.EnsureAll();
        TrainingManager.EnsureExists();
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
    /// Unlinks a human controller + selector. Removes the player in Character Select / Main Menu
    /// (when above minPlayers); otherwise converts them to a CPU.
    /// </summary>
    public bool DisconnectPlayer(int playerIndex)
    {
        Player p = players != null ? players.Find(x => x.index == playerIndex) : null;
        if (p == null || p.computer)
            return false;

        ControllerLink link = p.cLink;
        p.cLink = null;

        if (p.pso != null)
        {
            Destroy(p.pso.gameObject);
            p.pso = null;
        }

        controllers.RemoveAll(x => x == null || x == link || x.index == playerIndex);
        if (link != null)
            Destroy(link.gameObject);

        bool removeSlot = !IsPastCharacterSelect() && players.Count > minPlayers;
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
        PlayerPrefs.Save();
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
        ShowAndHidePlayerSelectors();

        if (gameStart)
        {
            if (!winnerScreen)
            {
                CheckPlayerConstrants();
                BallCheck();
                CheckForWinner();
            }
        }
    }

    void ShowAndHidePlayerSelectors()
    {
        // Hide during active match; show again on the win screen for menu navigation
        bool show = winnerScreen || !(gameStart || startingGame);
        playerSelectors.gameObject.SetActive(show);
    }

    public int PlayerAdd(ControllerLink cL)
    {
        int result = -1;
        bool computerMode = false;

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

            switch(index)
            {
                case 0:
                    p.team = 1;
                    p.facing = Facing.Up;
                    p.position = 0;
                    break;
                case 1:
                    p.team = 2;
                    p.facing = Facing.Down;
                    p.position = 0;
                    break;
                case 2:
                    p.team = 3;
                    p.facing = Facing.Right;
                    p.position = 0;
                    break;
                case 3:
                    p.team = 4;
                    p.facing = Facing.Left;
                    p.position = 0;
                    break;
                case 4:
                    p.team = 1;
                    p.facing = Facing.Up;
                    p.position = 1;
                    break;
                case 5:
                    p.team = 2;
                    p.facing = Facing.Down;
                    p.position = 1;
                    break;
                case 6:
                    p.team = 3;
                    p.facing = Facing.Right;
                    p.position = 1;
                    break;
                case 7:
                    p.team = 4;
                    p.facing = Facing.Left;
                    p.position = 1;
                    break;
            }

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

            GameObject go = Instantiate(playerSelectorGobj, playerSelectors);

            PlayerSelectorObj pso = go.GetComponent<PlayerSelectorObj>();
            pso.cLink = cL;
            if (p.skinColorIndex < 0)
                p.skinColorIndex = index;
            PlayerColors startColor = PlayerSkin.GetPlayerColors(p, this);
            pso.SetCircleColor(startColor != null ? startColor : playerColors[Mathf.Clamp(pc, 0, playerColors.Count - 1)]);
            pso.pI = index;
            p.pso = pso;

            result = index;

            if(!controllers.Contains(cL))
            {
                controllers.Add(cL);
            }

            // 5+ players always require Team Mode
            if (players.Count >= TeamModeToggle.ForceTeamAtPlayerCount)
            {
                teamSelect = true;
                positionSelect = true;
                if (gametype == Gametype.Vs)
                    gametype = Gametype.Coop;
            }
        }

        return result;
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
            }
        }
    }

    void BallCheck()
    {
        GameObject[] allBalls = GameObject.FindGameObjectsWithTag("Ball");

        if(allBalls.Length <= 0)
        {
            int sB = selectedBall;

            if (selectedBall < 0 || selectedBall >= balls.Count)
                sB = RandomBallIndex();

            if (sB < 0 || sB >= balls.Count)
                return;

            Ball ball = balls[sB];

            GameObject bSpawned = Instantiate(ball.prefab);
            bSpawned.transform.position = new Vector3(0, 0, fieldSize);
            BallInfo bI = bSpawned.GetComponent<BallInfo>();

            if (bI != null)
            {
                bI.ballReady = true;
            }
        }
    }

    void CheckForWinner()
    {
        List<int> aliveTeams = new List<int>();
        List<Player> winners = new List<Player>();

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

            winners = players.FindAll(x => x.currentHealth > 0);

            for (int i = 0; i < winners.Count; i++)
            {
                int team = winners[i].team;
                if (!aliveTeams.Contains(team))
                {
                    aliveTeams.Add(team);
                }
            }
        }

        int eliminated = players.Count - winners.Count;
        // FFA / default: last person standing. Team select: last team standing.
        // Require someone eliminated so solo practice doesn't instantly end.
        bool matchOver = eliminated > 0 && (
            teamSelect
                ? aliveTeams.Count <= 1
                : winners.Count <= 1
        );

        if (matchOver)
        {
            //Set Winners and Losers
            if (players.Count > 0)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    Player p = players[i];
                    p.won = winners.Contains(p);
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

        ClearAfterTheGame[] toClear = FindObjectsByType<ClearAfterTheGame>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
        ClearAfterTheGame[] toClear = FindObjectsByType<ClearAfterTheGame>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
        GridControl[] controls = FindObjectsByType<GridControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
            else if (n.Contains("main"))
                btn.buttonPressed = "Main Menu";
        }
    }

    public void WinButtonPressed(string buttonPressed)
    {
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

        if(startGame && !startingGame)
        {
            StartCoroutine(StartGame());
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
            #endregion

            #region Add Players
            for(int i = 0; i < maxPlayers; i++)
            {
                if(i >= players.Count)
                {
                    break;
                }

                Player p = players[i];

                // Fresh match stats (also cleared on win-menu exit; belt-and-suspenders here)
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

                // Always re-bind from the character roster before spawn.
                // Random stays flagged so rematch re-rolls a new fighter each match.
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

                if (p.character == null || p.character.prefabs == null)
                {
                    Debug.LogError($"StartGame: Player index={p.index} name='{p.name}' has no character prefab after roster bind. Skipping spawn.");
                    continue;
                }

                // Assign difficulty style variant for this match (re-rolls each StartGame / rematch)
                if (p.computer)
                    ComputerAI.AssignMatchBrain(p);

                switch(p.facing)
                {
                    case Facing.Up:
                        pPos = new Vector3(0,-(fieldSize/2),fieldSize);
                        fRot = new Vector3(0,0,0);
                        break;
                    case Facing.Down:
                        pPos = new Vector3(0, (fieldSize / 2), fieldSize);
                        fRot = new Vector3(0, 0, 180);
                        break;
                    case Facing.Left:
                        pPos = new Vector3((fieldSize / 2), 0, fieldSize);
                        fRot = new Vector3(0, 0, 90);
                        break;
                    case Facing.Right:
                        pPos = new Vector3(-(fieldSize / 2),0, fieldSize);
                        fRot = new Vector3(0, 0, 270);
                        break;
                }

                //Spawn Player
                p.spawnedPlayer = Instantiate(p.character.prefabs);
                p.spawnedPlayer.transform.position = pPos;
                p.spawnedPlayer.transform.rotation = Quaternion.Euler(fRot + p.character.rotationOffset);
                p.spawnedPlayer.transform.position += p.spawnedPlayer.transform.right * p.character.positionOffset.x;
                p.spawnedPlayer.transform.position += p.spawnedPlayer.transform.up * (p.character.positionOffset.y * ((p.position == 0) ? 1 : 1.5f));
                p.spawnedPlayer.transform.position += p.spawnedPlayer.transform.forward * p.character.positionOffset.z;
                PlayerGrab pG = p.spawnedPlayer.GetComponent<PlayerGrab>();

                if(pG != null)
                {
                    pG.playerIndex = p.index;
                }
                PlayerSkin.Apply(p.spawnedPlayer, p, this);

                //Spawn Lifeline — always on that side's wall (not at coop player depth)
                if (p.lifeline != null && p.lifeline.prefabs != null)
                {
                    p.spawnedLifeline = Instantiate(p.lifeline.prefabs);
                    PlaceLifelineOnWall(p.spawnedLifeline, p.facing, p.spawnedPlayer.transform.position, fRot, p.lifeline);
                    // Trigger's multi-capsule barrier: keep test-zone spacing, spread with larger walls
                    SpreadTriggerLifelineParts(p.spawnedLifeline, p);
                    PlayerGrab lifePG = p.spawnedLifeline.GetComponent<PlayerGrab>();

                    if (lifePG != null)
                    {
                        lifePG.playerIndex = p.index;
                    }
                    PlayerSkin.Apply(p.spawnedLifeline, p, this);
                }

                //Spawn Info box
                GameObject sIB = playerInfo;

                if(p.playerInfo != null)
                {
                    sIB = p.playerInfo;
                }

                if (sIB != null)
                {
                    GameObject iB = Instantiate(sIB);

                    if((i % 2) == 0)
                    {
                        iB.transform.SetParent(playerInfoHolderRS);
                    }
                    else
                    {
                        iB.transform.SetParent(playerInfoHolderLS);
                    }

                    PlayerGrab ibpG = iB.GetComponent<PlayerGrab>();

                    if (ibpG != null)
                    {
                        ibpG.playerIndex = p.index;
                    }
                }
                yield return null;
            }
            #endregion

            #region Add Ball
            // selectedBall == -1 means Random — keep it so rematch re-rolls
            int sB = selectedBall;

            if (selectedBall < 0 || selectedBall >= balls.Count)
                sB = RandomBallIndex();

            if (sB < 0 || sB >= balls.Count)
            {
                Debug.LogError("StartGame: no ball available to spawn.");
                yield break;
            }

            Ball ball = balls[sB];

            GameObject bSpawned = Instantiate(ball.prefab);
            bSpawned.transform.position = new Vector3(0, 0, fieldSize);
            BallInfo bI = bSpawned.GetComponent<BallInfo>();

            if (bI != null)
            {
                bI.ballReady = false;
            }
            yield return null;
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
                yield return new WaitForSecondsRealtime(1);
            }

            gameplayinfo.gameObject.SetActive(false);
            #endregion

            if (bI != null)
            {
                bI.ballReady = true;
            }
            gameStart = true;
            yield return null;
        }

        startingGame = false;
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
        float wallSpan = (leftDist > 0.01f && rightDist > 0.01f)
            ? leftDist + rightDist
            : Mathf.Max(8f, fieldSize);

        // Prefab tuned for fieldSize ≈ 20 (size 10 test zone)
        const float referenceSpan = 20f;
        float scale = Mathf.Clamp(wallSpan / referenceSpan, 0.55f, 4f);

        // Scale existing even spacing; keep relative layout
        for (int i = 0; i < parts.Count; i++)
        {
            Vector3 lp = parts[i].localPosition;
            lp.x *= scale;
            parts[i].localPosition = lp;
        }

        // Soft clamp: if outer parts would sit past the walls, re-fit evenly inside margins
        float outer = Mathf.Abs(parts[parts.Count - 1].localPosition.x);
        float halfSpan = wallSpan * 0.5f;
        float partHalf = Mathf.Max(0.25f, Mathf.Abs(parts[0].localScale.x) * 0.55f);
        float maxOuter = Mathf.Max(0.5f, halfSpan - partHalf * 1.25f);
        if (outer > maxOuter + 0.01f && outer > 0.01f)
        {
            float fit = maxOuter / outer;
            for (int i = 0; i < parts.Count; i++)
            {
                Vector3 lp = parts[i].localPosition;
                lp.x *= fit;
                parts[i].localPosition = lp;
            }
        }
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