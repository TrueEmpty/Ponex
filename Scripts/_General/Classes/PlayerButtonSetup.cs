using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerButtonSetup : MonoBehaviour
{
    Database db;
    ButtonManager bm;
    string listening = null;

    public Text p1L;
    public Text p1R;
    public Text p1U;
    public Text p1D;

    public Text info;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        bm = ButtonManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        info.gameObject.SetActive(listening != null);

        if(listening != null)
        {
            int pL = -1;
            string dir = "";
            string direction = "";

            string[] split = listening.Split(',');

            if (split.Length > 1)
            {
                pL = int.Parse(split[0]);
                dir = split[1];

                switch (dir.ToUpper().Trim())
                {
                    case "L":
                        direction = "Left";
                        break;
                    case "R":
                        direction = "Right";
                        break;
                    case "U":
                        direction = "Up";
                        break;
                    case "D":
                        direction = "Down";
                        break;
                }
            }

            List<ButtonCapture> kp = bm.AllKeysPressed();


            if (kp.Count > 0)
            {
                string keysPressed = "";
                for(int i = 0; i < kp.Count; i++)
                {
                    if(i > 0)
                    {
                        keysPressed += "\n";
                    }

                    keysPressed += kp[i].StringOut();
                }

                Debug.Log("Keys Pressed = \n" + keysPressed);

                if (kp.Exists(x=> (KeyCode)x.Value == KeyCode.Escape))
                {
                    listening = null;
                }
                else
                {
                    if (pL == 1)
                    {
                        switch (dir.ToUpper().Trim())
                        {
                            case "L":
                                db.settings.p1Buttons.left.Add(kp[0].name);
                                break;
                            case "R":
                                db.settings.p1Buttons.right.Add(kp[0].name);
                                break;
                            case "U":
                                db.settings.p1Buttons.bump.Add(kp[0].name);
                                break;
                            case "D":
                                db.settings.p1Buttons.super.Add(kp[0].name);
                                break;
                        }
                    }

                    listening = null;
                }
            }

            info.text = "Press a button to set it to Player " + pL.ToString() + "'s " + direction;
        }

        PlayerButtons pb1 = db.settings.p1Buttons;

        p1L.text = pb1.left.Count > 0 ? pb1.left[^1].ToString() : "";
        p1R.text = pb1.right.Count > 0 ? pb1.right[^1].ToString() : "";
        p1U.text = pb1.bump.Count > 0 ? pb1.bump[^1].ToString() : "";
        p1D.text = pb1.super.Count > 0 ? pb1.super[^1].ToString() : "";
    }

    public void ButtonSetup(string playerComaLRUD)
    {
        listening = playerComaLRUD;
    }
}
