using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PositionSelect : MonoBehaviour
{
    public static PositionSelect instance;
    ButtonManager bm;
    Database db;
    MenuManager mm;
    public List<Portait> postionGrabs = new List<Portait>();
    Vector3 setPos = new Vector3(225,175,25);
    public Color outlineColor = Color.blue;
    public Color readyColor = Color.green;

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
        mm = MenuManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (setup)
        {
            LoadPosition();
        }
        else
        {
            Setup();
        }
    }

    void Setup()
    {
        if (!setup)
        {
            //Portraits
            Transform porC = transform.GetChild(2);

            for (int i = 0; i < porC.childCount; i++)
            {
                GameObject container = porC.GetChild(i).gameObject;

                Portait p = new Portait();
                p.container = container;
                p.playerText = container.GetComponent<Text>();
                p.outline = container.GetComponent<Outline>();

                postionGrabs.Add(p);
            }

            setup = true;
        }
    }

    void LoadPosition()
    {
        for(int i = 0; i < postionGrabs.Count; i++)
        {
            Portait pG = postionGrabs[i];

            if(i < db.players.Count)
            {
                Player p = db.players[i];
                Color c = db.playerColors[i];

                pG.container.SetActive(true);

                if (p.computer)
                {
                    pG.playerText.text = "CPU" + (i + 1);
                    pG.playerText.color = Color.gray;
                }
                else
                {
                    pG.playerText.text = (p.nickName == "" || p.nickName == null) ? "P" + (i + 1) : p.nickName;
                    pG.playerText.color = c;
                }

                pG.outline.effectColor = p.characterSelected ? readyColor : outlineColor;

                #region Preset off others
                if (i > 0)
                {
                    Player sP = db.players.Find(x => x.facing == p.facing && x.position == p.position && x != p);

                    if (sP != null)
                    {
                        if (db.players.Exists(x => x.facing == p.facing && x.position != p.position))
                        {
                            switch (p.facing)
                            {
                                case Facing.Up:
                                    p.facing = Facing.Left;
                                    break;
                                case Facing.Down:
                                    p.facing = Facing.Right;
                                    break;
                                case Facing.Right:
                                    p.facing = Facing.Up;
                                    break;
                                case Facing.Left:
                                    p.facing = Facing.Down;
                                    break;
                            }
                        }
                        else
                        {
                            p.position = (p.position == 0) ? 1 : 0;
                        }
                    }
                }
                #endregion

                #region Set Position
                if(p.WithinLastUpdate && !p.characterSelected)
                {
                    List<Player> rP = new List<Player>();
                    List<string> l = new List<string>();
                    List<string> r = new List<string>();
                    List<string> u = new List<string>();
                    List<string> d = new List<string>();
                    List<string> g = new List<string>();
                    List<string> x = new List<string>();

                    if ((p.computer && p == db.players.Find(x => x.computer && !x.characterSelected)))
                    {
                        int fC = db.players.FindIndex(x => x.computer && !x.characterSelected);

                        if (fC == i)
                        {
                            rP = db.players.FindAll(x => x.characterSelected && !x.computer && x.WithinLastUpdate);

                            if (rP.Count > 0)
                            {
                                for (int z = 0; z < rP.Count; z++)
                                {
                                    l.AddRange(rP[z].buttons.left);
                                    r.AddRange(rP[z].buttons.right);
                                    u.AddRange(rP[z].buttons.up);
                                    d.AddRange(rP[z].buttons.down);
                                    g.AddRange(rP[z].buttons.confirm);
                                    x.AddRange(rP[z].buttons.cancel);
                                }
                            }
                        }
                    }
                    else
                    {
                        l.AddRange(p.buttons.left);
                        r.AddRange(p.buttons.right);
                        u.AddRange(p.buttons.up);
                        d.AddRange(p.buttons.down);
                        g.AddRange(p.buttons.confirm);
                        x.AddRange(p.buttons.cancel);
                    }

                    //Get Button Inputs
                    Facing oF = p.facing;
                    int oP = p.position;

                    TryAgain:
                    if (bm.KeyDown(r))
                    {
                        p.lastGridUpdate = Time.time;

                        if (rP.Count > 0)
                        {
                            for (int rpl = 0; rpl < rP.Count; rpl++)
                            {
                                rP[rpl].lastGridUpdate = Time.time;
                            }
                        }

                        switch (p.facing)
                        {
                            case Facing.Up:
                                p.facing = Facing.Left;
                                p.position = 0;
                                break;
                            case Facing.Down:
                                p.facing = Facing.Left;
                                p.position = 0;
                                break;
                            case Facing.Right:
                                p.position = (p.position == 0) ? 1 : 0;
                                break;
                            case Facing.Left:
                                p.position = (p.position == 0) ? 1 : 0;
                                break;
                        }
                    }
                    else if (bm.KeyDown(l))
                    {
                        p.lastGridUpdate = Time.time;

                        if (rP.Count > 0)
                        {
                            for (int rpl = 0; rpl < rP.Count; rpl++)
                            {
                                rP[rpl].lastGridUpdate = Time.time;
                            }
                        }

                        switch (p.facing)
                        {
                            case Facing.Up:
                                p.facing = Facing.Right;
                                p.position = 0;
                                break;
                            case Facing.Down:
                                p.facing = Facing.Right;
                                p.position = 0;
                                break;
                            case Facing.Right:
                                p.position = (p.position == 0) ? 1 : 0;
                                break;
                            case Facing.Left:
                                p.position = (p.position == 0) ? 1 : 0;
                                break;
                        }
                    }
                    else if (bm.KeyDown(u))
                    {
                        p.lastGridUpdate = Time.time;

                        if (rP.Count > 0)
                        {
                            for (int rpl = 0; rpl < rP.Count; rpl++)
                            {
                                rP[rpl].lastGridUpdate = Time.time;
                            }
                        }

                        switch (p.facing)
                        {
                            case Facing.Up:
                                p.position = (p.position == 0) ? 1 : 0;
                                break;
                            case Facing.Down:
                                p.position = (p.position == 0) ? 1 : 0;
                                break;
                            case Facing.Right:
                                p.facing = Facing.Down;
                                p.position = 0;
                                break;
                            case Facing.Left:
                                p.facing = Facing.Down;
                                p.position = 0;
                                break;
                        }
                    }
                    else if (bm.KeyDown(d))
                    {
                        p.lastGridUpdate = Time.time;

                        if (rP.Count > 0)
                        {
                            for (int rpl = 0; rpl < rP.Count; rpl++)
                            {
                                rP[rpl].lastGridUpdate = Time.time;
                            }
                        }

                        switch (p.facing)
                        {
                            case Facing.Up:
                                p.position = (p.position == 0) ? 1 : 0;
                                break;
                            case Facing.Down:
                                p.position = (p.position == 0) ? 1 : 0;
                                break;
                            case Facing.Right:
                                p.facing = Facing.Up;
                                p.position = 0;
                                break;
                            case Facing.Left:
                                p.facing = Facing.Up;
                                p.position = 0;
                                break;
                        }
                    }

                    Player sP = db.players.Find(x => x.facing == p.facing && x.position == p.position && x != p);

                    if (sP != null)
                    {
                        if (db.players.Exists(x => x.facing == p.facing && x.position != p.position && x != p))
                        {
                            if(sP.characterSelected)
                            {
                                goto TryAgain;
                            }
                            else
                            {
                                sP.facing = oF;
                                sP.position = oP;
                            }
                        }
                        else
                        {
                            if (sP.characterSelected)
                            {
                                p.position = (p.position == 0) ? 1 : 0;
                            }
                            else
                            {
                                sP.position = (p.position == 0) ? 1 : 0;
                            }
                        }
                    }

                    if (bm.KeyDown(g))
                    {
                        p.lastGridUpdate = Time.time;
                        p.characterSelected = true;

                        if(rP.Count > 0)
                        {
                            for(int rpl = 0; rpl < rP.Count; rpl++)
                            {
                                rP[rpl].lastGridUpdate = Time.time;
                            }
                        }

                        if(!db.players.Exists(x=> !x.characterSelected))
                        {
                            db.CharactersPicked("positions");
                        }
                    }
                    else if (bm.KeyDown(x))
                    {
                        p.lastGridUpdate = Time.time;

                        if (rP.Count > 0)
                        {
                            for (int rpl = 0; rpl < rP.Count; rpl++)
                            {
                                rP[rpl].lastGridUpdate = Time.time;
                            }
                        }

                        if (p.characterSelected)
                        {
                            p.characterSelected = false;
                        }
                        else
                        {
                            for(int z = 0; z < db.players.Count; z++)
                            {
                                db.players[z].characterSelected = false;
                            }

                            mm.BackMenu();
                        }
                    }
                }                
                #endregion

                #region Position Update
                Vector2 cP = new Vector2(((p.position == 0) ? setPos.z : -setPos.z), ((p.position == 0) ? setPos.x : setPos.y));
                switch(p.facing)
                {
                    case Facing.Up:
                        pG.container.transform.localPosition = new Vector3(cP.x, -cP.y, 0);
                        break;
                    case Facing.Down:
                        pG.container.transform.localPosition = new Vector3(cP.x, cP.y, 0);
                        break;
                    case Facing.Right:
                        pG.container.transform.localPosition = new Vector3(-cP.y, cP.x, 0);
                        break;
                    case Facing.Left:
                        pG.container.transform.localPosition = new Vector3(cP.y, cP.x, 0);
                        break;
                }
                #endregion
            }
            else
            {
                pG.container.SetActive(false);
            }
        }
    }
}
