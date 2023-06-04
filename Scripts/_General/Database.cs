using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Database : MonoBehaviour
{
    public static Database instance;
    public bool actualGame = false;

    public MenuControl mC;
    public Settings settings = new Settings();

    [SerializeField]
    public List<Stats> characters = new List<Stats>();
    public List<Field> fields = new List<Field>();
    public List<Ball> balls = new List<Ball>();

    public List<Part> parts = new List<Part>();
    public List<GameObject> backgrounds = new List<GameObject>();

    public GameState gamestate = GameState.Main;
    public Texture default_Texture;

    #region Gameplay
    public GameObject fieldObj;

    public GameObject currentField;
    public GameObject lightGo;
    public GameObject loadingGo;

    public MessageDisplay mD;

    public List<Color> teamColors = new List<Color>();

    public GameObject playerDisplay;
    public Transform characterDisplay;
    #endregion

    string root = "/Data";

    [SerializeField]
    bool saving = false;
    bool loading = false;

    public List<TypeIdentifier> typeIds = new List<TypeIdentifier>();

    public Transform borderTop;
    public Transform borderRight;
    public Transform borderBot;
    public Transform borderLeft;

    public GameObject borderASelectors;

    public GameObject showWinner;
    public GameObject showWinnerContent;
    public GameObject winnerDisplay;
    public GameObject winnerTitle;
    public GameObject winnersTitle;
    List<GameObject> winnerGos = new List<GameObject>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LoadData());
    }

    // Update is called once per frame
    void Update()
    {
        settings.p1Buttons.ButtonPressUpdate();
        settings.p2Buttons.ButtonPressUpdate();
        settings.p3Buttons.ButtonPressUpdate();
        settings.p4Buttons.ButtonPressUpdate();

        if (!loading)
        {
            //Dont forget to remove later
            if (Input.GetKey(KeyCode.F1) && !saving)
            {
                saving = true;
                StartCoroutine(SaveData());
            }
        }

        if(lightGo != null)
        {
            lightGo.SetActive(!(gamestate == GameState.Playing || gamestate == GameState.StartUp));
        }

        if(borderASelectors != null)
        {
            borderASelectors.SetActive(!(gamestate == GameState.Playing || gamestate == GameState.StartUp));
        }

        if (loadingGo != null)
        {
            loadingGo.SetActive(gamestate == GameState.Loading);
        }

        if (showWinner != null)
        {
            showWinner.SetActive(gamestate == GameState.Winner);
        }

        if(gamestate != GameState.Winner)
        {
            if(winnerGos.Count > 0)
            {
                for(int i = winnerGos.Count - 1; i >= 0; i--)
                {
                    Destroy(winnerGos[i]);
                    winnerGos.RemoveAt(i);
                }
            }
        }

        switch(gamestate)
        {
            case GameState.Main:
                break;
            case GameState.Loading:
                break;
            case GameState.StartUp:
                break;
            case GameState.Playing:
                GameRunning();
                break;
            case GameState.Winner:
                break;
        }
    }

    void DisplayWinners(List<PlayerWinStats> allPlayers)
    {
        gamestate = GameState.Winner;

        if (allPlayers != null)
        {
            for(int i = 0; i < allPlayers.Count; i++)
            {
                GameObject go = Instantiate(winnerDisplay, showWinnerContent.transform);

                WinnerDisplay wD = go.GetComponent<WinnerDisplay>();

                if(wD != null)
                {
                    wD.Setup(allPlayers[i]);
                }

                winnerGos.Add(go);
            }
        }

        ClearAll();
    }

    void ClearAll()
    {
        GameObject[] allGo = GameObject.FindObjectsOfType<GameObject>();

        if (allGo.Length > 0)
        {
            for (int i = allGo.Length - 1; i >= 0; i--)
            {
                allGo[i].SendMessage("ClearAllForNewGame", SendMessageOptions.DontRequireReceiver);

                if(allGo[i] != null)
                {
                    if(!allGo[i].activeInHierarchy)
                    {
                        Destroy(allGo[i]);
                    }
                }
            }
        }
    }

    void GameRunning()
    {
        //Check for winner
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");

        //If there is only 1 team left they win
        if (allPlayers.Length > 0)
        {
            List<PlayerWinStats> teams = new List<PlayerWinStats>();
            int teamCount = 0;

            for (int i = 0; i < allPlayers.Length; i++)
            {
                Player p = allPlayers[i].GetComponent<Player>();

                if (p != null)
                {
                    if(p.player.currentHealth > 0)
                    {
                        if (!teams.Exists(x=> x.team == p.team))
                        {
                            teamCount++;
                        }
                    }

                    if(!teams.Exists(x=> x.team == p.team && x.nickName == p.nickName && x.position == p.position))
                    {
                        teams.Add(new PlayerWinStats(p));
                    }
                }
            }

            switch (teamCount)
            {
                case 0: //There was a draw somehow
                    DisplayWinners(teams);
                    break;
                case 1: //There are winners display them
                    DisplayWinners(teams);
                    break;
                default: //No Winner keep playing
                    break;
            }
        }
        else //Somthing happened there are no winners (Draw)
        {
            DisplayWinners(null);
        }

    }

    public void ChangeGameState(int state)
    {
        gamestate = (GameState)state;
    }

    public void StartSaveData()
    {
        if(saving == false)
        {
            saving = true;
            StartCoroutine(SaveData());
        }
    }

    public bool IsSaving()
    {
        return saving;
    }

    public bool IsLoading()
    {
        return loading;
    }

    public void StartGameplay(GameState newGamestate)
    {
        gamestate = newGamestate;
    }

    IEnumerator LoadData()
    {
        #region Load Fields
        string path = "TrueEmptyFields";
        string fullPath = Application.dataPath + root + "/" + path;

        //Check if path exist if not create it
        if (Directory.Exists(fullPath))
        {
            DirectoryInfo dir = new DirectoryInfo(fullPath);
            FileInfo[] info = dir.GetFiles("*.pnx");

            if(info.Length > 0)
            {
                foreach (FileInfo f in info)
                {
                    string data = File.ReadAllText(f.FullName);

                    if (data != null)
                    {
                        Field cF = new Field(data);
                        cF.arthur = "True Empty";

                        if(cF.icon == null)
                        {
                            cF.icon = default_Texture;
                        }

                        if(cF.portrait == null)
                        {
                            cF.portrait = default_Texture;
                        }

                        if (cF.uid != null)
                        {
                            if (!fields.Exists(x => x.uid == cF.uid))
                            {
                                fields.Add(cF);
                            }
                        }
                        else
                        {
                            cF.CreateUID();
                            fields.Add(cF);
                        }
                    }

                    yield return null;
                }
            }

            if (fields.Count > 0)
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    if (fields[i].uid == null || fields[i].uid == "")
                    {
                        fields[i].CreateUID();
                    }
                }
            }
        }

        path = "Fields";
        fullPath = Application.dataPath + root + "/" + path;

        //Check if path exist if not create it
        if (Directory.Exists(fullPath))
        {
            DirectoryInfo dir = new DirectoryInfo(fullPath);
            FileInfo[] info = dir.GetFiles("*.pnx");

            if(info.Length > 0)
            {
                foreach (FileInfo f in info)
                {
                    string data = File.ReadAllText(f.FullName);

                    if (data != null)
                    {
                        Field cF = new Field(data);

                        if (cF.arthur == "True Empty")
                        {
                            cF.arthur = "Not True Empty";
                        }

                        if (cF.icon == null)
                        {
                            cF.icon = default_Texture;
                        }

                        if (cF.portrait == null)
                        {
                            cF.portrait = default_Texture;
                        }

                        if (cF.uid != null)
                        {
                            if (!fields.Exists(x => x.uid == cF.uid))
                            {
                                fields.Add(cF);
                            }
                        }
                        else
                        {
                            cF.CreateUID();
                            fields.Add(cF);
                        }
                    }

                    yield return null;
                }
            }

            if (fields.Count > 0)
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    if (fields[i].uid == null || fields[i].uid == "")
                    {
                        fields[i].CreateUID();
                    }
                }
            }
        }

        #endregion

        #region Load Parts
        path = "Parts";
        fullPath = Application.dataPath + root + "/" + path;

        //Check if path exist if not create it
        if (Directory.Exists(fullPath))
        {
            DirectoryInfo dir = new DirectoryInfo(fullPath);
            FileInfo[] info = dir.GetFiles("*.pnx");

            foreach (FileInfo f in info)
            {
                string data = File.ReadAllText(f.FullName);

                if (data != null)
                {
                    Part cP = new Part(data);

                    if (cP.icon == null)
                    {
                        cP.icon = default_Texture;
                    }

                    if (cP.uid != null)
                    {
                        if (!parts.Exists(x => x.uid == cP.uid))
                        {
                            parts.Add(cP);
                        }
                    }
                    else
                    {
                        cP.CreateUID();
                        parts.Add(cP);
                    }
                }

                yield return null;
            }

            if (parts.Count > 0)
            {
                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i].uid == null || parts[i].uid == "")
                    {
                        parts[i].CreateUID();
                    }

                    if(typeIds.Count > 0)
                    {
                        if(!typeIds.Exists(x=> x.type.ToLower().Trim() == parts[i].type.ToLower().Trim()))
                        {
                            typeIds.Add(new TypeIdentifier(parts[i].type));
                        }
                    }
                    else
                    {
                        typeIds.Add(new TypeIdentifier(parts[i].type));
                    }
                }
            }
        }

        #endregion

        loading = false;
        yield return null;
    }

    IEnumerator SaveData()
    {
        #region Save Fields

        if (fields.Count > 0)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                string filename = fields[i].uid;

                string data = fields[i].ToString();
                string path = "Fields";

                if(fields[i].arthur == "True Empty")
                {
                    path = "TrueEmptyFields";
                }

                string fullPath = Application.dataPath + root + "/" + path;
                string fullFilePath = fullPath + "/" + filename + ".pnx";

                //Check if path exist if not create it
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    yield return new WaitForEndOfFrame();
                }

                if (data != "")
                {
                    //Save File
                    StreamWriter writer = new StreamWriter(fullFilePath, false);
                                        
                    //Split data
                    writer.Write(data);

                    writer.Close();
                }

                yield return new WaitForEndOfFrame();
            }
        }

        #endregion

        #region Save Parts

        if (parts.Count > 0)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                string filename = parts[i].uid;

                string data = parts[i].ToString();
                string path = "Parts";
                string fullPath = Application.dataPath + root + "/" + path;
                string fullFilePath = fullPath + "/" + filename + ".pnx";

                //Check if path exist if not create it
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    yield return new WaitForEndOfFrame();
                }

                if (data != "")
                {
                    //Save File
                    StreamWriter writer = new StreamWriter(fullFilePath, false);

                    //Split data
                    writer.Write(data);

                    writer.Close();
                }

                yield return new WaitForEndOfFrame();
            }
        }

        #endregion

        saving = false;
        yield return null;
    }
}

public enum GameState
{
    Main,
    Loading,
    StartUp,
    Playing,
    Winner
}