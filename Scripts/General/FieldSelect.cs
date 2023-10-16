using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FieldSelect : MonoBehaviour
{
    public static FieldSelect instance;
    Database db;
    public List<CharacterGrab> characterGrabs = new List<CharacterGrab>();
    public Portait portrait;
    bool setup = false;
    List<Field> fieldPage = new List<Field>(); //Fist 6-10 records in the first 4 rows are perma Blanked
    public Vector3Int skipAmount = new Vector3Int(6,5,4);
    public int maxInrow = 15;
    int page = 0;
    int maxpage = 1;
    int lastFieldsCount = 0;
    public Field skipField = new Field();
    public Field blankField = new Field();
    public Text pageDisplay;

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
    }

    // Update is called once per frame
    void Update()
    {
        if (setup)
        {
            if(lastFieldsCount != db.fields.Count)
            {
                UpdateFieldPage();
            }
            
            LoadPortrait();
            LoadInfo();
        }
        else
        {
            Setup();
        }
    }

    public void PageChange(int d)
    {
        page += d;

        if (page > maxpage)
        {
            page = maxpage;
        }

        if(page < 0)
        {
            page = 0;
        }

        lastFieldsCount = -1;
    }

    public void Setup()
    {
        if (!setup)
        {
            //Character Grabs
            Transform pgC = transform.GetChild(1);

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

                c.gridControl.position = new Vector2(column, row);

                column++;

                if (column >= 15)
                {
                    column = 0;
                    row--;
                }
            }


            //Portraits
            Transform porC = transform.GetChild(0);

            portrait.container = porC.gameObject;
            portrait.image = portrait.container.GetComponent<RawImage>();
            portrait.playerText = portrait.container.transform.GetChild(0).GetComponent<Text>();
            portrait.infoText = portrait.container.transform.GetChild(1).GetComponent<Text>();

            setup = true;
        }
    }

    public void UpdateFieldPage()
    {
        fieldPage.Clear();

        if(db.fields.Count > 0)
        {
            int skipping = skipAmount.y * skipAmount.z;
            int amountPerPage = characterGrabs.Count;
            int startingField = (amountPerPage - skipping) * page;

            maxpage = Mathf.FloorToInt(db.fields.Count / (amountPerPage - skipping));

            if (startingField >= db.fields.Count)
            {
                page = maxpage;
            }

            Vector3Int curSkip = new Vector3Int(1,1,1);
            int actField = startingField;
            int mir = 1;

            for (int i = 0; i < amountPerPage; i++)
            {
                if(curSkip.x >= skipAmount.x && curSkip.z <= skipAmount.z)
                {
                    if(curSkip.y <= skipAmount.y)
                    {
                        fieldPage.Add(skipField);
                        curSkip.y++;

                        if(curSkip.y > skipAmount.y)
                        {
                            curSkip.x = -1000;
                            curSkip.y = 1;
                        }
                    }

                }
                else
                {
                    if (actField >= db.fields.Count)
                    {
                        fieldPage.Add(blankField);
                    }
                    else
                    {
                        fieldPage.Add(db.fields[actField]);
                    }
                    
                    actField++;
                    curSkip.x++;
                }

                mir++;

                if(mir > maxInrow)
                {
                    mir = 1;
                    curSkip.x = 1;
                    curSkip.z++;
                }
            }

            pageDisplay.text = "Page " + (page + 1) + " of " + (maxpage + 1);
        }

        lastFieldsCount = db.fields.Count;
    }

    void LoadInfo()
    {
        //Load Character grabs
        for (int i = 0; i < characterGrabs.Count; i++)
        {
            CharacterGrab cG = characterGrabs[i];

            if (i < fieldPage.Count)
            {
                Field f = fieldPage[i];

                //Check if it is an auto skip block
                if(f.size == -100) //Skip Field
                {
                    cG.gridControl.canSelect = false;
                    cG.background.color = Color.clear;
                    cG.image.color = Color.clear;
                    cG.charName.color = Color.clear;
                }
                else if(f.size == -10) //Blank Field
                {
                    cG.gridControl.canSelect = false;
                    cG.background.color = Color.white;
                    cG.image.color = Color.clear;
                    cG.charName.color = Color.clear;
                }
                else
                {
                    cG.gridControl.canSelect = f.active;
                    cG.background.color = Color.white;
                    cG.image.texture = f.icon;
                    cG.image.color = (f.active) ? Color.white : Color.black;
                    cG.charName.color = Color.white;
                    cG.charName.text = f.name;
                }
            }
            else
            {
                cG.gridControl.canSelect = false;
                cG.background.color = Color.white;
                cG.image.color = Color.clear;
                cG.charName.color = Color.clear;
            }
        }
    }

    void LoadPortrait()
    {
        //Load in portaits
        if (db.selectedField >= 0 && db.selectedField < db.fields.Count)
        {
            Field f = db.fields[db.selectedField];
            portrait.container.SetActive(true);

            portrait.image.texture = f.icon;

            portrait.playerText.text = f.name;

            string authur = f.arthur;
            bool tE = false;

            if(authur == "#True Empty")
            {
                authur = "True Empty";
                tE = true;
            }

            portrait.infoText.text = "Size: " + f.size + "   " + ((tE) ? "<color=#1BFF00>" : "<color=#FFED00>") +"Aurthur: " + authur + "</color>";
        }
        else
        {
            portrait.container.SetActive(false);
        }
    }
}
