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
        if(!loading)
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

        if (loadingGo != null)
        {
            loadingGo.SetActive(gamestate == GameState.Loading);
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
                DisplayWinners();
                break;
        }
    }

    void DisplayWinners()
    {
        
    }

    void GameRunning()
    {
        //Check for winner
        GameObject[] alllifelines = GameObject.FindGameObjectsWithTag("Lifeline");

        //If there is only 1 team left they win
        if (alllifelines.Length > 0)
        {
            List<int> lifelineTeams = new List<int>();

            for (int i = 0; i < alllifelines.Length; i++)
            {
                PlayerGrab pG = alllifelines[i].GetComponent<PlayerGrab>();

                if (pG != null)
                {
                    if (!lifelineTeams.Contains(pG.player.team))
                    {
                        lifelineTeams.Add(pG.player.team);
                    }
                }
            }

            switch (lifelineTeams.Count)
            {
                case 0: //There was a draw somehow
                    mD.Write("Draw");
                    DestroyGameplayObjects();
                    gamestate = GameState.Winner;
                    break;
                case 1: //There are winners display them
                    //db.mD.Write("Draw");
                    DestroyGameplayObjects();
                    gamestate = GameState.Winner;
                    break;
                default: //No Winner keep playing
                    break;
            }
        }
        else //Somthing happened there are no winners (Draw)
        {
            mD.Write("Draw");
            DestroyGameplayObjects();
            gamestate = GameState.Winner;
        }

    }

    public void DestroyGameplayObjects()
    {

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