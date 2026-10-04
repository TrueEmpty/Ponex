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
    #endregion

    #region Sounds
    public List<Sounds> extraSounds = new List<Sounds>();
    public AudioSource effectAudio;
    #endregion

    const string SelectedCharacterPref = "Ponex.SelectedCharacter.";

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

    public Characters GetRememberedCharacter(int playerIndex)
    {
        if (playerIndex < 0 || characters == null)
            return null;

        string saved = PlayerPrefs.GetString(SelectedCharacterPref + playerIndex, "");
        if (string.IsNullOrEmpty(saved))
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

        Characters remembered = GetRememberedCharacter(p.index);
        if (remembered != null)
            p.SetUpCharacter(remembered);
        else
            p.SetUpCharacter(RandomCharacter());
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
        bool show = true;

        if(gameStart || startingGame)
        {
            show = false;
        }

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

            players.Add(new Player());

            int pc = players.Count - 1;
            Player p = players[pc];

            p.computer = computerMode;
            p.cLink = cL;

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
            pso.SetCircleColor(playerColors[pc]);
            pso.pI = index;
            p.pso = pso;

            result = index;

            if(!controllers.Contains(cL))
            {
                controllers.Add(cL);
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
            {
                sB = Random.Range(0, balls.Count);
            }

            if (selectedBall == -1)
            {
                selectedBall = sB;
            }

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
            // Players whose character was destroyed but health wasn't cleared still block the match end
            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];
                if (p.currentHealth > 0 && p.spawnedPlayer == null)
                {
                    p.currentHealth = 0;
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

        //Open Winners menu
        if (mm != null)
        {
            mm.OpenMenu("Winners");
        }
        yield return null;

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

                players[i].state = "";
            }
        }

        someoneWon = false;
        yield return null;
    }

    public void WinButtonPressed(string buttonPressed)
    {
        //Clear WonBoxes
        if(showWinner.childCount > 0)
        {
            for(int i = showWinner.childCount - 1; i >= 0; i--)
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

            //Reset Character info with main Char
            Characters character = characters.Find(x => x.name == p.name);

            if(character != null)
            {
                p.SetUpCharacter(character);
            }
        }

        //Perform action
        switch(buttonPressed)
        {
            case "Rematch":
                CharactersPicked("balls");
                break;
            case "Champion Select":
                gameStart = false;
                mm.BackUnitl("Character Select");
                break;
            case "Main Menu":
                gameStart = false;
                mm.openMenu.Clear();
                mm.OpenMenu("Main Menu");
                break;
            default://Main Menu
                gameStart = false;
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

        return new Characters(avaliableChar[Random.Range(0, avaliableChar.Count)]);
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
            {
                sF = Random.Range(0, fields.Count);
            }

            Field field = fields[sF];
            fieldSize = field.size + 10;

            GameObject fSpawned = Instantiate(fieldObj);
            fSpawned.transform.position = new Vector3(0, 0, fieldSize);
            Field_Info fI = fSpawned.GetComponent<Field_Info>();
            fI.field = field;
            yield return null;
            #endregion

            #region Add Players
            for(int i = 0; i < maxPlayers; i++)
            {
                if(i >= players.Count)
                {
                    break;
                }

                Player p = players[i];

                // Always re-bind from the character roster by name before spawn.
                // Portraits/UI can show the right fighter while Player.character was never
                // copied (or was wiped on rematch / domain reload) — that caused the NRE.
                Characters rosterChar = null;
                if (!string.IsNullOrEmpty(p.name))
                    rosterChar = characters.Find(x => x != null && x.name == p.name);

                if (rosterChar == null || rosterChar.character == null || rosterChar.character.prefabs == null)
                    rosterChar = RandomCharacter();

                if (rosterChar != null)
                    p.SetUpCharacter(rosterChar);

                if (p.character == null || p.character.prefabs == null)
                {
                    Debug.LogError($"StartGame: Player index={p.index} name='{p.name}' has no character prefab after roster bind. Skipping spawn.");
                    continue;
                }

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

                //Spawn Lifeline — place on the wall hit by a ray from map center toward that side
                if (p.lifeline != null && p.lifeline.prefabs != null)
                {
                    Vector3 lifePos = GetLifelineWallSpawnPos(p.facing, pPos);

                    p.spawnedLifeline = Instantiate(p.lifeline.prefabs);
                    p.spawnedLifeline.transform.position = lifePos;
                    p.spawnedLifeline.transform.rotation = Quaternion.Euler(fRot + p.lifeline.rotationOffset);
                    p.spawnedLifeline.transform.position += p.spawnedLifeline.transform.right * p.lifeline.positionOffset.x;
                    p.spawnedLifeline.transform.position += p.spawnedLifeline.transform.up * p.lifeline.positionOffset.y;
                    p.spawnedLifeline.transform.position += p.spawnedLifeline.transform.forward * p.lifeline.positionOffset.z;
                    PlayerGrab lifePG = p.spawnedLifeline.GetComponent<PlayerGrab>();

                    if (lifePG != null)
                    {
                        lifePG.playerIndex = p.index;
                    }
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
            int sB = selectedBall;

            if (selectedBall < 0 || selectedBall >= balls.Count)
            {
                sB = Random.Range(0, balls.Count);
            }

            if(selectedBall == -1)
            {
                selectedBall = sB;
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
    /// Ray from map center toward the player's side; first Wall/Walls hit is the lifeline base position.
    /// Falls back to <paramref name="fallback"/> if nothing is hit.
    /// </summary>
    Vector3 GetLifelineWallSpawnPos(Facing facing, Vector3 fallback)
    {
        Vector3 origin = new Vector3(0f, 0f, fieldSize);
        Vector3 dir;

        switch (facing)
        {
            case Facing.Up:
                dir = Vector3.down;
                break;
            case Facing.Down:
                dir = Vector3.up;
                break;
            case Facing.Left:
                dir = Vector3.right;
                break;
            case Facing.Right:
                dir = Vector3.left;
                break;
            default:
                return fallback;
        }

        float maxDist = Mathf.Max(fieldSize * 2f, 100f);
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, maxDist);
        if (hits == null || hits.Length == 0)
            return fallback;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            string tag = hits[i].transform.tag;
            if (tag != "Wall" && tag != "Walls")
                continue;

            Vector3 point = hits[i].point;
            point.z = fieldSize;
            return point;
        }

        return fallback;
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