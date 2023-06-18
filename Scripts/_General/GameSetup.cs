using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSetup : MonoBehaviour
{
    Database db;
    public MenuControl mC;

    public List<GameObject> screens = new List<GameObject>();
    public int screen = 0;

    public string gamplayStarting;
    
    public StageSelector stageSelector;
    public CharacterSelector characterSelector;
    public GameObject selectionButtons;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        db.SubscribeToSelectors(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        SetScreen();
    }

    void SetScreen()
    {
        if (screens.Count > 0)
        {
            for (int i = 0; i < screens.Count; i++)
            {
                if (i == screen)
                {
                    if (screens[i] != null)
                    {
                        if (!screens[i].activeInHierarchy)
                        {
                            screens[i].SetActive(true);
                        }
                    }
                }
                else
                {
                    if (screens[i] != null)
                    {
                        if (screens[i].activeInHierarchy)
                        {
                            screens[i].SetActive(false);
                        }
                    }
                }
            }

            selectionButtons.SetActive(screen < screens.Count && screen >= 0);
        }
    }

    public void Back()
    {
        if(screen == 0)
        {
            mC.BackMenu();
        }
        else
        {
            screen--;
        }
    }

    public void Confirm()
    {
        if(screen >= 0 && screen < screens.Count)
        {
            if(screens[screen] != null)
            {
                screens[screen].SendMessage("AddGameplayInfo", SendMessageOptions.DontRequireReceiver);
            }
        }

        screen++;

        if (screen >= screens.Count)
        {
            //Reset Screen
            screen += 100;

            StartCoroutine(StartUpGame());
        }
    }

    IEnumerator StartUpGame()
    {
        //Move on to gameplay starting
        db.StartGameplay(GameState.Loading);
        yield return new WaitForSeconds(.2f);
        yield return new WaitForSeconds(3.3f);

        //Create Field
        GameObject go = Instantiate(db.fieldObj) as GameObject;

        FieldInfo fI = go.GetComponent<FieldInfo>();

        yield return new WaitForSeconds(.2f);

        if (fI != null)
        {
            fI.field = stageSelector.GetCurrentField();
            fI.StartFieldSpawn();

            db.currentField = go;

            //Spawn Each player
            SpawnPlayer(fI, 1, characterSelector.p1DisabledActiveComputer);
            SpawnPlayer(fI, 2, characterSelector.p2DisabledActiveComputer);
            SpawnPlayer(fI, 3, characterSelector.p3DisabledActiveComputer);
            SpawnPlayer(fI, 4, characterSelector.p4DisabledActiveComputer);

            yield return new WaitForSeconds(.2f);

            //For Now always spawn a random Ball
            Ball ba = db.balls[Random.Range(0,db.balls.Count)];
            yield return new WaitForSeconds(.2f);

            //Move on to gameplay starting
            db.StartGameplay(GameState.StartUp);
            yield return new WaitForSeconds(.2f);

            //StartGameFully
            StartCoroutine(fI.BeginGame(ba));
            yield return new WaitUntil(() => fI.started);
        }

        //Turn off Screen
        mC.OpenMenu("None");
        screen = 0;
        yield return null;
    }

    public void SpawnPlayer(FieldInfo fI,int player,int state)
    {
        if (state > -1)
        {
            GameObject p = fI.GetSpawn("P" + player);

            if (p != null)
            {                
                Stats ps = characterSelector.GetPlayer(player);

                if (ps != null)
                {
                    if (state == 1)
                    {
                        ps.computer = true;
                    }

                    GameObject playerObj = null;

                    //Spawn Player
                    if (ps.character != null)
                    {
                        ObjectInfo pOi = ps.character;

                        if (pOi.prefabs != null)
                        {
                            playerObj = Instantiate(pOi.prefabs, p.transform.position,Quaternion.identity) as GameObject;

                            Player pI = playerObj.GetComponent<Player>();

                            playerObj.transform.position = p.transform.position + p.transform.up * (pOi.positionOffset.y);
                            playerObj.transform.position += p.transform.right * (pOi.positionOffset.x);
                            playerObj.transform.position += p.transform.forward * (pOi.positionOffset.z);

                            playerObj.transform.eulerAngles = p.transform.rotation.eulerAngles + pOi.rotationOffset;

                            if (pI != null)
                            {
                                pI.player = ps;
                                pI.position = player;
                                pI.team = player;

                                GameObject pOb = Instantiate(db.playerDisplay,db.characterDisplay);
                                PlayerDisplay poD = pOb.GetComponent<PlayerDisplay>();

                                if(poD != null)
                                {
                                    poD.player = pI;
                                }
                            }
                        }
                    }
                    
                    //Spawn Lifelines
                    if(ps.lifeline != null)
                    {
                        ObjectInfo pOi = ps.lifeline;

                        if (pOi.prefabs != null)
                        {
                            GameObject go = Instantiate(pOi.prefabs, p.transform.position, Quaternion.identity) as GameObject;

                            PlayerGrab pG = go.GetComponent<PlayerGrab>();

                            go.transform.position = p.transform.position + p.transform.up * (pOi.positionOffset.y);
                            go.transform.position += p.transform.right * (pOi.positionOffset.x);
                            go.transform.position += p.transform.forward * (pOi.positionOffset.z);

                            go.transform.eulerAngles = p.transform.rotation.eulerAngles + pOi.rotationOffset;

                            if (pG != null)
                            {
                                pG.player = playerObj.GetComponent<Player>();
                            }
                        }
                    }
                }
            }
        }
    }

    public void StageShift(int dir)
    {
        stageSelector.ChangeGroup(dir);
    }
}
