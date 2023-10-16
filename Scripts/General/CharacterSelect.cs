using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelect : MonoBehaviour
{
    public static CharacterSelect instance;
    ButtonManager bm;
    Database db;
    public List<CharacterGrab> characterGrabs = new List<CharacterGrab>();
    public List<Portait> portaits = new List<Portait>();

    public GridControl back;
    public GridControl removePlayer;
    public GridControl addPlayer;
    public GridControl confirm;
    bool setup = false;

    private void Awake()
    {
        if (instance != null)
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
        db = Database.instance;
        bm = ButtonManager.instance;
    }

    public void Setup()
    {
        if(!setup)
        {
            //Character Grabs
            Transform pgC =  transform.GetChild(1);

            int row = 0;
            int column = 0;

            for (int i = 0; i < pgC.childCount; i++)
            {
                GameObject container = pgC.GetChild(i).gameObject;

                CharacterGrab c = new CharacterGrab();
                c.container = container;
                c.gridControl = container.GetComponent<GridControl>();
                c.background = container.GetComponent<Image>();
                c.image = container.transform.GetChild(0).GetComponent<RawImage>();
                c.charName = container.transform.GetChild(1).GetComponent<Text>();

                characterGrabs.Add(c);

                c.gridControl.position = new Vector2(column,row);

                column++;

                if(column > 14)
                {
                    column = 0;
                    row--;
                }
            }


            //Portraits
            Transform porC = transform.GetChild(0);

            for (int i = 0; i < porC.childCount; i++)
            {
                GameObject container = porC.GetChild(i).gameObject;

                Portait p = new Portait();
                p.container = container;
                p.image = container.GetComponent<RawImage>();
                p.gridControl = container.GetComponent<GridControl>();
                p.gridControl.ol = container.transform.GetChild(0).GetChild(0).GetComponent<Outline>();
                p.playerText = container.transform.GetChild(0).GetChild(0).GetComponent<Text>();
                p.ready = container.transform.GetChild(0).GetChild(1).gameObject;

                //Add CPUs when needed
                int tic = 0;
                while (db.players.Count < db.minPlayers && tic < db.maxPlayers)
                {
                    AddComputer();
                    tic++;
                }

                //Attach Player to it
                if (i < db.players.Count && i < db.maxPlayers)
                {
                    p.gridControl.AddPlayer(i);
                }

                portaits.Add(p);
            }

            //Add Players to Character Grab and Back
            for (int i = 0; i < db.players.Count; i++)
            {
                if(i >= db.maxPlayers)
                {
                    break;
                }

                Player p = db.players[i];
                int cC = db.characters.FindIndex(x => x.name == p.name);

                if (cC < 0 || cC >= characterGrabs.Count)
                {
                    Player ch = db.RandomCharacter();
                    p.SetUpCharacter(ch);

                    cC = db.characters.FindIndex(x => x.name == ch.name);

                    GridControl gC = characterGrabs[cC].gridControl;

                    if (!gC.playersInControl.Contains(i))
                    {
                        gC.AddPlayer(i);
                    }
                }

                if (!back.playersInControl.Contains(i))
                {
                    back.AddPlayer(i);
                }
            }
            
            setup = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(setup)
        {
            LoadPortrait();
            LoadInfo();

            if(db.players.Count < db.minPlayers)
            {
                AddComputer();
            }
        }
        else
        {
            Setup();
        }
    }

    void LoadInfo()
    {  
        //Load Character grabs
        for(int i = 0; i < characterGrabs.Count; i++)
        {
            CharacterGrab cG = characterGrabs[i];

            if (i < db.characters.Count)
            {
                Player ch = db.characters[i];

                cG.gridControl.canSelect = ch.active;
                cG.background.color = ch.portraitColor;
                cG.image.texture = ch.icon;
                cG.image.color = (ch.active) ? Color.white : Color.black;                
                cG.charName.text = ch.name;
            }
            else
            {
                cG.gridControl.canSelect = false;
                cG.background.color = Color.clear;
                cG.image.color = Color.clear;
                cG.charName.color = Color.clear;
            }
        }
    }

    void LoadPortrait()
    {
        //Load in portaits
        if (portaits.Count > 0)
        {
            List<Portait> activePortait = portaits.FindAll(x => x.container.activeInHierarchy);

            for (int i = 0; i < portaits.Count; i++)
            {
                Portait por = portaits[i];

                if (i < db.players.Count)
                {
                    por.container.SetActive(true);
                    Player p = db.players[i];

                    Color c = db.playerColors[i];

                    //Check if a character is attached to the player if not set to a random one
                    int cC = db.characters.FindIndex(x => x.name == p.name);
                    if (cC < 0 || cC >= characterGrabs.Count)
                    {
                        Player ch = db.RandomCharacter();
                        p.SetUpCharacter(ch);

                        cC = db.characters.FindIndex(x => x.name == ch.name);

                        GridControl gC = characterGrabs[cC].gridControl;

                        if (!gC.playersInControl.Contains(i))
                        {
                            gC.AddPlayer(i);
                        }
                    }

                    por.image.texture = p.portrait;
                    if (p.computer)
                    {
                        por.playerText.text = "CPU" + (i + 1);
                        por.playerText.color = Color.gray;
                    }
                    else
                    {
                        por.playerText.text = (p.nickName == "" || p.nickName == null) ? "P" + (i + 1) : p.nickName;
                        por.playerText.color = c;
                    }

                    por.ready.SetActive(p.characterSelected);
                }
                else
                {
                    por.container.SetActive(false);
                }
            }
        }
    }

    public void AddComputer()
    {
        if (db.players.Count < db.maxPlayers)
        {
            Buttons pB = new Buttons();
            db.players.Add(new Player(pB));
            int pc = db.players.Count - 1;
            db.players[pc].computer = true;

            if(pc < portaits.Count)
            {
                portaits[pc].gridControl.AddPlayer(pc);
            }

            Player p = db.players[pc];

            switch (pc)
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

            int cC = db.characters.FindIndex(x => x.name == p.name);
            p.state = "Character Select";

            if (cC < 0 || cC >= characterGrabs.Count)
            {
                Player ch = db.RandomCharacter();
                p.SetUpCharacter(ch);

                cC = db.characters.FindIndex(x => x.name == ch.name);
            }

            GridControl gC = characterGrabs[cC].gridControl;

            if (!gC.playersInControl.Exists(x => x == pc))
            {
                gC.AddPlayer(pc);
            }

            if (!back.playersInControl.Exists(x => x == pc))
            {
                back.AddPlayer(pc);
            }
        }
    }

    public void PlayerConfirm(int player)
    {
        if (player >= 0 && player < db.players.Count)
        {
            Player p = db.players[player];

            p.characterSelected = true;
            p.gridLock = true;

            //Check if all charactersAreSelected
            if (!db.players.Exists(x=> !x.characterSelected))
            {
                db.CharactersPicked("characters");
            }
        }
    }
}

[System.Serializable]
public class Portait
{
    public GameObject container;
    public RawImage image;
    public Text playerText;
    public GridControl gridControl;
    public Text infoText;
    public GameObject ready;
    public Outline outline;
}

[System.Serializable]
public class CharacterGrab
{
    public GameObject container;
    public GridControl gridControl;
    public Image background;
    public RawImage image;
    public Text charName;
    
}
