using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelect : MonoBehaviour
{
    public static CharacterSelect instance;
    Database db;
    public List<CharacterGrab> characterGrabs = new List<CharacterGrab>();
    public List<PortraitClicked> portaits = new List<PortraitClicked>();

    public Transform characterGrabHolder;
    public GameObject characterGrab_pf;

    Transform porC;
    public GameObject portrait_pf;

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
        porC = transform.GetChild(0);
    }

    public void Setup()
    {
        if(!setup)
        {
            //Character Grabs — random (?) first, then roster
            GameObject randomGo = Instantiate(characterGrab_pf, characterGrabHolder);
            CharacterGrab randomGrab = randomGo.GetComponent<CharacterGrab>();
            randomGrab.isRandom = true;
            randomGrab.ch = null;
            randomGo.name = "Character Grab (Random)";
            randomGo.transform.SetAsFirstSibling();
            characterGrabs.Add(randomGrab);

            for (int i = 0; i < db.characters.Count; i++)
            {
                Characters c = db.characters[i];

                GameObject cGo = Instantiate(characterGrab_pf, characterGrabHolder);
                CharacterGrab cG = cGo.GetComponent<CharacterGrab>();

                cG.ch = c;

                characterGrabs.Add(cG);
            }

            //Portraits
            for (int i = 0; i < db.minPlayers; i++)
            {
                int pind = i;
                if (!db.players.Exists(x=> x.index == i) && db.players.Count < db.minPlayers)
                {
                    pind = db.PlayerAdd(null);
                }

                GameObject pGo = Instantiate(portrait_pf, porC);
                PortraitClicked pC = pGo.GetComponent<PortraitClicked>();
                pC.attachedIndex = pind;

                portaits.Add(pC);
            }
        }

        setup = true;
    }

    // Update is called once per frame
    void Update()
    {
        if(setup)
        {
            if (db.players.Count < db.minPlayers)
            {
                int pind = db.PlayerAdd(null);
            }

            if (portaits.Count < db.players.Count)
            {
                List<Player> anap = db.players.FindAll(x => !portaits.Exists(y => y.attachedIndex == x.index));

                if (anap.Count > 0)
                {
                    GameObject pGo = Instantiate(portrait_pf, porC);
                    PortraitClicked pC = pGo.GetComponent<PortraitClicked>();
                    pC.attachedIndex = anap[0].index;

                    portaits.Add(pC);
                }


                //Need to order them
            }
        }
        else
        {
            Setup();
        }
    }

    public void AddComputer()
    {
        if (db == null)
            return;

        // Multiplayer lobbies allow up to 8 (VS or Team)
        if (db.gametype == Gametype.Vs || db.gametype == Gametype.Coop)
            db.maxPlayers = 8;

        if (db.players != null && db.players.Count >= db.maxPlayers)
            return;

        db.PlayerAdd(null);
        EnforceTeamModeForPlayerCount();
    }

    public void RemovePlayer()
    {
        if(db.players.Count > db.minPlayers)
        {
            //Look for a computer first
            Player p = db.players.FindLast(x => x.computer);
            if (p != null)
            {
                db.players.Remove(p);
            }
            else
            {
                p = db.players[^1];
                if(p.cLink != null)
                {
                    Destroy(p.cLink.gameObject);
                    db.controllers.RemoveAll(x=> x.index == p.index);
                }

                db.players.Remove(p);
            }

            EnforceTeamModeForPlayerCount();
        }
    }

    /// <summary>5+ players must use Team Mode (skips only when under 5 and VS).</summary>
    public void EnforceTeamModeForPlayerCount()
    {
        if (db == null || db.players == null)
            return;

        if (db.players.Count >= TeamModeToggle.ForceTeamAtPlayerCount)
        {
            db.teamSelect = true;
            db.positionSelect = true;
            if (db.gametype == Gametype.Vs)
                db.gametype = Gametype.Coop;
        }
    }

    public void PlayerConfirm(int player)
    {
        Player p = db.players.Find(x => x.index == player);

        if(p != null)
        {
            if (p.wantRandomCharacter)
            {
                if (db.RandomCharacter() == null)
                {
                    Debug.LogWarning($"Player {p.index} confirmed Random but no active characters exist.");
                    return;
                }

                db.RememberSelectedCharacter(p.index, Database.RandomCharacterSentinel);
                p.characterSelected = true;
            }
            else
            {
                // Don't lock in until a real prefab is bound (portrait alone isn't enough)
                if (p.character == null || p.character.prefabs == null)
                {
                    Characters ch = db.characters.Find(x => x != null && x.name == p.name);
                    if (ch == null)
                        ch = db.RandomCharacter();
                    if (ch != null)
                        p.SetUpCharacter(ch);
                }

                if (p.character == null || p.character.prefabs == null)
                {
                    Debug.LogWarning($"Player {p.index} confirmed without a character prefab — ignoring confirm.");
                    return;
                }

                db.RememberSelectedCharacter(p.index, p.name);
                p.characterSelected = true;
            }

            //Check if all charactersAreSelected
            if (!db.players.Exists(x => !x.characterSelected))
            {
                db.CharactersPicked("characters");
            }
        }
    }

    /// <summary>Undo ready so the player can change their character pick.</summary>
    public void PlayerUnconfirm(int player)
    {
        if (db == null || db.players == null)
            return;

        Player p = db.players.Find(x => x.index == player);
        if (p == null || !p.characterSelected)
            return;

        p.characterSelected = false;
        p.gridLock = false;

        if (p.pso != null)
            p.pso.cpuControl = -1;
    }
}
