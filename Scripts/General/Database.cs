using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public GameObject outofBounds;
    public GameObject background;
    #endregion

    #region Players
    public List<Player> characters;
    public List<Player> players;
    public Player allplay;
    public Color apColor = Color.yellow;
    public List<Color> playerColors;

    public List<Color> pawnColors;
    public List<Effects> effects;

    #endregion

    #region Setup
    public GridControl mmGC;
    public int minPlayers = 1;
    public int maxPlayers = 8;

    public bool levelSelect = false;
    public bool positionSelect = false;
    public bool teamSelect = false;
    public bool ballSelect = false;

    public bool gameStart = false;
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
            CheckPlayerConstrants();
        }
        else
        {
            PlayerAddCheck();

            if(cS == null)
            {
                cS = CharacterSelect.instance;
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

                if(mmGC != null)
                {
                    if (mmGC != null)
                    {
                        if (!mmGC.playersInControl.Contains(pc))
                        {
                            mmGC.playersInControl.Add(pc);
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
                }
                break;
            case "balls":
                    //Start Game

                break;
        }

        for (int i = 0; i < players.Count; i++)
        {
            players[i].characterSelected = false;
            players[i].gridLock = false;
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