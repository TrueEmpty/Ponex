using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerButtonSetupTest : MonoBehaviour
{
    Database db;
    string listening = null;

    public Button p1L;
    public Button p1R;
    public Button p1U;
    public Button p1D;

    public Text info;

    Event e;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
    }

    // Update is called once per frame
    void Update()
    {
        info.gameObject.SetActive(listening != null);

        if(listening != null)
        {
            int pL = -1;
            string dir = "";

            string[] split = listening.Split(',');

            if (split.Length > 1)
            {
                pL = int.Parse(split[0]);
                dir = split[1];
            }

            e = Event.current;

            if(e.keyCode != KeyCode.None)
            {
                if(e.keyCode == KeyCode.Escape)
                {
                    listening = null;
                }
                else
                {
                    if(pL == 1)
                    {
                        switch(dir.ToUpper().Trim())
                        {
                            case "L":
                                db.settings.p1Buttons.left.Add(e.keyCode);
                                break;
                            case "R":
                                db.settings.p1Buttons.right.Add(e.keyCode);
                                break;
                            case "U":
                                db.settings.p1Buttons.bump.Add(e.keyCode);
                                break;
                            case "D":
                                db.settings.p1Buttons.super.Add(e.keyCode);
                                break;
                        }
                    }

                    listening = null;
                }
            }

            info.text = "";
        }
    }

    public void ButtonSetup(string playerComaLRUD)
    {
        listening = playerComaLRUD;
    }
}
