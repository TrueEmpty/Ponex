using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Database : MonoBehaviour
{
    public static Database instance;
    MenuManager mm;
    CharacterSelect cS;

    #region Setting
    public float sensitivity = .25f;
    public List<Buttons> buttons;
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
    public List<Player> characters;
    public List<Player> players;
    public Player allplay;
    public Color apColor = Color.yellow;
    public List<Color> playerColors;

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

    private void Awake()
    {
        if(instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        mm = MenuManager.instance;
        cS = CharacterSelect.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if(gameStart)
        {
            if(!winnerScreen)
            {
                CheckPlayerConstrants();
                BallCheck();
                CheckForWinner();
            }
        }
        else
        {
            bool canAdd = false;

            if(cS == null)
            {
                cS = CharacterSelect.instance;
            }
            else
            {
                if(cS.gameObject.activeInHierarchy)
                {
                    canAdd = true;
                }
            }

            if(!canAdd)
            {
                if(mm.GetOpenMenu().title == "Main Menu")
                {
                    canAdd = true;
                }
            }

            if(canAdd)
            {
                PlayerAddCheck();
            }
        }
    }

    void PlayerAddCheck()
    {
        if (players.Count < 8)
        {
            Buttons pB = buttons.Find(x => x.ButtonBeingPressed() && !x.InUse());

            if (pB != null)
            {
                players.Add(new Player(pB));

                int pc = players.Count - 1;
                Player p = players[pc];

                switch(pc)
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
                            Player ch = RandomCharacter();
                            p.SetUpCharacter(ch);

                            cC = characters.FindIndex(x => x.name == ch.name);
                        }

                        GridControl gC = cS.characterGrabs[cC].gridControl;

                        if (!gC.playersInControl.Exists(x => x == pc))
                        {
                            gC.AddPlayer(pc);
                        }

                        if(pc >= 0 && pc < cS.portaits.Count)
                        {
                            GridControl pgC = cS.portaits[pc].gridControl;

                            if (!pgC.playersInControl.Exists(x => x == pc))
                            {
                                pgC.AddPlayer(pc);
                            }
                        }

                        if (!cS.back.playersInControl.Exists(x => x == pc))
                        {
                            cS.back.AddPlayer(pc);
                        }
                    }
                }
            }
        }
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
                sB = Random.Range(0, fields.Count);
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

        if(players.Count > 0)
        {
            winners = players.FindAll(x=> x.currentHealth > 0);
            
            for(int i = 0; i < players.Count; i++)
            {
                Player p = players[i];

                if(winners.Contains(p))
                {
                    if(!aliveTeams.Contains(p.team))
                    {
                        aliveTeams.Add(p.team);
                    }
                }
            }
        }

        if(aliveTeams.Count <= 1)
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

        //Clear all GameObjects and Field
        GameObject[] allGo = FindObjectsOfType<GameObject>();

        if (allGo.Length > 0)
        {
            for (int i = allGo.Length - 1; i >= 0; i--)
            {
                if (allGo[i] != null)
                {
                    bool prevAS = allGo[i].activeInHierarchy;

                    allGo[i].SetActive(true);
                    allGo[i].SendMessage("ClearAllForNewGame", SendMessageOptions.DontRequireReceiver);
                    yield return null;

                    if (allGo[i] != null)
                    {
                        allGo[i].SetActive(prevAS);
                    }
                }
            }
        }

        //Open Winners menu
        mm.OpenMenu("Winners");
        yield return null;
        for (int i = 0; i < players.Count; i++)
        {
            mmOp.AddPlayer(i);
        }

        //Add Win Box to Winner Menu
        if (players.Count > 0)
        {
            for (int i = 0; i < players.Count; i++)
            {
                GameObject wB = Instantiate(wonBox);
                wB.transform.SetParent(showWinner);

                GameResultBreakdown grb = wB.GetComponent<GameResultBreakdown>();

                if(grb != null)
                {
                    grb.playerIndex = i;
                }

                players[i].state = "";
            }
        }

        someoneWon = false;
        yield return null;
    }

    public Player RandomCharacter(bool actives = true)
    {
        List<Player> avaliableChar = characters;

        if(actives)
        {
            avaliableChar = characters.FindAll(x => x.active);
        }

        return new Player(avaliableChar[Random.Range(0,avaliableChar.Count)]);
    }

    public bool InSetup()
    {
        bool result = true;

        return result;
    }

    public void CharactersPicked(string fromSelect)
    {
        bool startGame = false;

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
                if (p.character.prefabs != null)
                {
                    p.spawnedPlayer = Instantiate(p.character.prefabs);
                    p.spawnedPlayer.transform.position = pPos;
                    p.spawnedPlayer.transform.rotation = Quaternion.Euler(fRot + p.character.rotationOffset);
                    p.spawnedPlayer.transform.position += p.spawnedPlayer.transform.right * p.character.positionOffset.x;
                    p.spawnedPlayer.transform.position += p.spawnedPlayer.transform.up * (p.character.positionOffset.y * ((p.position == 0) ? 1 : 1.5f));
                    p.spawnedPlayer.transform.position += p.spawnedPlayer.transform.forward * p.character.positionOffset.z;
                    PlayerGrab pG = p.spawnedPlayer.GetComponent<PlayerGrab>();

                    if(pG != null)
                    {
                        pG.playerIndex = i;
                    }
                }

                //Spawn Lifeline
                if (p.lifeline.prefabs != null)
                {
                    p.spawnedLifeline = Instantiate(p.lifeline.prefabs);
                    p.spawnedLifeline.transform.position = pPos;
                    p.spawnedLifeline.transform.rotation = Quaternion.Euler(fRot + p.lifeline.rotationOffset);
                    p.spawnedLifeline.transform.position += p.spawnedLifeline.transform.right * p.lifeline.positionOffset.x;
                    p.spawnedLifeline.transform.position += p.spawnedLifeline.transform.up * p.lifeline.positionOffset.y;
                    p.spawnedLifeline.transform.position += p.spawnedLifeline.transform.forward * p.lifeline.positionOffset.z;
                    PlayerGrab pG = p.spawnedLifeline.GetComponent<PlayerGrab>();

                    if (pG != null)
                    {
                        pG.playerIndex = i;
                    }
                }

                //Spawn Info box
                GameObject sIB = playerInfo;

                if(p.playerInfo != null)
                {
                    sIB = p.playerInfo;
                }

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
                    ibpG.playerIndex = i;
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