using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamSelect : MonoBehaviour
{
    public static TeamSelect instance;
    ButtonManager bm;
    Database db;
    MenuManager mm;

    List<Portait> teamGrabs = new List<Portait>();
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
            for (int i = 0; i < teamGrabs.Count; i++)
            {
                Portait tg = teamGrabs[i];

                tg.container.SetActive(i < db.players.Count);

                if (i < db.players.Count)
                {
                    Player p = db.players[i];
                    Color c = db.playerColors[i];

                    if (p.computer)
                    {
                        tg.playerText.text = "CPU" + (i + 1);
                        tg.playerText.color = Color.gray;
                    }
                    else
                    {
                        tg.playerText.text = (p.nickName == "" || p.nickName == null) ? "P" + (i + 1) : p.nickName;
                        tg.playerText.color = c;
                    }

                    if (p.WithinLastUpdate && !p.characterSelected)
                    {
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
                                List<Player> rP = db.players.FindAll(x => x.characterSelected && !x.computer && x.WithinLastUpdate);

                                if (rP.Count > 0)
                                {
                                    for (int z = 0; z < rP.Count; z++)
                                    {
                                        l.Add(rP[z].buttons.left);
                                        r.Add(rP[z].buttons.right);
                                        u.Add(rP[z].buttons.up);
                                        d.Add(rP[z].buttons.down);
                                        g.Add(rP[z].buttons.confirm);
                                        x.Add(rP[z].buttons.cancel);
                                    }
                                }
                            }
                        }
                        else
                        {
                            l.Add(p.buttons.left);
                            r.Add(p.buttons.right);
                            u.Add(p.buttons.up);
                            d.Add(p.buttons.down);
                            g.Add(p.buttons.confirm);
                            x.Add(p.buttons.cancel);
                        }

                        Player partner = null;
                        if (bm.KeyDown(r) || bm.KeyDown(u))
                        {
                            p.team++;

                            if(p.team > 4)
                            {
                                p.team = 1;
                            }

                            partner = db.players.Find(x=> x.facing == p.facing && x != p);

                            if(partner != null)
                            {
                                partner.team = p.team;
                            }
                        }
                        else if(bm.KeyDown(l) || bm.KeyDown(d))
                        {
                            p.team--;

                            if (p.team < 1)
                            {
                                p.team = 4;
                            }

                            partner = db.players.Find(x => x.facing == p.facing && x != p);

                            if (partner != null)
                            {
                                partner.team = p.team;
                            }
                        }
                        else if(bm.KeyDown(g))
                        {
                            p.lastGridUpdate = Time.time;
                            p.characterSelected = true;

                            if (!db.players.Exists(x => !x.characterSelected))
                            {
                                List<int> differentTeams = new List<int>();

                                for(int t = 0; t < db.players.Count; t++)
                                {
                                    Player tp = db.players[t];

                                    if(!differentTeams.Contains(tp.team))
                                    {
                                        differentTeams.Add(tp.team);
                                    }
                                }

                                if(differentTeams.Count >= 2)
                                {
                                    db.CharactersPicked("teams");
                                }
                                else
                                {
                                    p.characterSelected = false;
                                }
                            }
                        }
                        else if (bm.KeyDown(x))
                        {
                            p.lastGridUpdate = Time.time;

                            if (p.characterSelected)
                            {
                                p.characterSelected = false;
                            }
                            else
                            {
                                for (int z = 0; z < db.players.Count; z++)
                                {
                                    db.players[z].characterSelected = false;
                                }

                                mm.BackMenu();
                            }
                        }
                    }

                    switch (p.team)
                    {
                        case 1://Team Blue
                            tg.infoText.text = "Team: Blue";
                            tg.image.color = Color.blue;
                            break;
                        case 2://Team Red
                            tg.infoText.text = "Team: Red";
                            tg.image.color = Color.red;
                            break;
                        case 3://Team Green
                            tg.infoText.text = "Team: Green";
                            tg.image.color = Color.green;
                            break;
                        case 4://Team Yellow
                            tg.infoText.text = "Team: Yellow";
                            tg.image.color = Color.yellow;
                            break;
                        default:
                            p.team = 1;
                            tg.infoText.text = "Team: Blue";
                            tg.image.color = Color.blue;
                            break;
                    }

                    tg.ready.SetActive(p.characterSelected);
                }
            }
        }
        else
        {
            Setup();
        }
    }

    public void Setup()
    {
        if (!setup)
        {
            //Team Portraits
            Transform por = transform.GetChild(1);

            for (int i = 0; i < por.childCount; i++)
            {
                GameObject container = por.GetChild(i).gameObject;

                Portait p = new Portait();
                p.container = container;
                p.image = container.GetComponent<RawImage>();
                p.playerText = container.transform.GetChild(0).GetComponent<Text>();
                p.outline = container.transform.GetChild(0).GetComponent<Outline>();
                p.infoText = container.transform.GetChild(1).GetComponent<Text>();
                p.ready = container.transform.GetChild(2).gameObject;

                teamGrabs.Add(p);
            }
            setup = true;
        }
    }
}
