using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerButtonSetup : MonoBehaviour
{
    Database db;
    ButtonManager bm;
    string listening = null;

    public Text info;

    public Transform playerButtonHolder;
    public GameObject playerButtonLine;

    public List<string> buttonShowList = new();
    
    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        bm = ButtonManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        Listener();
        ContentUpkeep();
    }

    void ContentUpkeep()
    {
        List<string> newList = new();
        string lineS = "";
        PlayerButtons cPB;

        for (int p = 1; p <= 8; p++)
        {
            switch(p)
            {
                case 1:
                    cPB = db.settings.p1Buttons;
                break;
                case 2:
                    cPB = db.settings.p2Buttons;
                break;
                case 3:
                    cPB = db.settings.p3Buttons;
                break;
                case 4:
                    cPB = db.settings.p4Buttons;
                break;
                case 5:
                    cPB = db.settings.p5Buttons;
                break;
                case 6:
                    cPB = db.settings.p6Buttons;
                break;
                case 7:
                    cPB = db.settings.p7Buttons;
                break;
                case 8:
                    cPB = db.settings.p8Buttons;
                break;
                default:
                    cPB = db.settings.p1Buttons;
                break;
            }

            int bC = cPB.left.Count + 1;

            for (int i = 0; i < bC; i++)
            {
                lineS = p + ",";
                lineS += i + ",";
                lineS += (cPB.left.Count > i) ? cPB.left : "UnBound";
                lineS += ",";
                lineS += (cPB.right.Count > i) ? cPB.right : "UnBound";
                lineS += ",";
                lineS += (cPB.bump.Count > i) ? cPB.bump : "UnBound";
                lineS += ",";
                lineS += (cPB.super.Count > i) ? cPB.super : "UnBound";
                newList.Add(lineS);
            }
        }

        buttonShowList = newList;

        while(playerButtonHolder.childCount < buttonShowList.Count)
        {
            GameObject pbl = Instantiate(playerButtonLine, playerButtonHolder);

            PlayerButtonLine pBL = pbl.GetComponent<PlayerButtonLine>();

            if(pBL != null)
            {
                pBL.index = playerButtonHolder.childCount - 1;
                pBL.pBS = this;
            }
        }
    }

    void Listener()
    {
        info.gameObject.SetActive(listening != null);

        if(listening != null)
        {
            int pL = -1;
            int pI = -1;
            string dir = "";
            string direction = "";

            string[] split = listening.Split(',');

            if (split.Length > 2)
            {
                pL = int.Parse(split[0]);
                dir = split[1];
                pI = int.Parse(split[2]);

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
                if (kp.Exists(x=> (KeyCode)x.Value == KeyCode.Escape))
                {
                    listening = null;
                }
                else
                {
                    switch (pL)
                    {
                        case 1:
                            switch (dir.ToUpper().Trim())
                            {
                                case "L":
                                    if(pI >= 0 && pI < db.settings.p1Buttons.left.Count)
                                    {
                                        db.settings.p1Buttons.left[pI] = kp[0].name;
                                    }
                                    break;
                                case "R":
                                    if (pI >= 0 && pI < db.settings.p1Buttons.right.Count)
                                    {
                                        db.settings.p1Buttons.right[pI] = kp[0].name;
                                    }
                                    break;
                                case "U":
                                    if (pI >= 0 && pI < db.settings.p1Buttons.bump.Count)
                                    {
                                        db.settings.p1Buttons.bump[pI] = kp[0].name;
                                    }
                                    break;
                                case "D":
                                    if (pI >= 0 && pI < db.settings.p1Buttons.super.Count)
                                    {
                                        db.settings.p1Buttons.super[pI] = kp[0].name;
                                    }
                                    break;
                            }
                        break;
                        case 2:
                            switch (dir.ToUpper().Trim())
                            {
                                case "L":
                                    if(pI >= 0 && pI < db.settings.p2Buttons.left.Count)
                                    {
                                        db.settings.p2Buttons.left[pI] = kp[0].name;
                                    }
                                    break;
                                case "R":
                                    if (pI >= 0 && pI < db.settings.p2Buttons.right.Count)
                                    {
                                        db.settings.p2Buttons.right[pI] = kp[0].name;
                                    }
                                    break;
                                case "U":
                                    if (pI >= 0 && pI < db.settings.p2Buttons.bump.Count)
                                    {
                                        db.settings.p2Buttons.bump[pI] = kp[0].name;
                                    }
                                    break;
                                case "D":
                                    if (pI >= 0 && pI < db.settings.p2Buttons.super.Count)
                                    {
                                        db.settings.p2Buttons.super[pI] = kp[0].name;
                                    }
                                    break;
                            }
                        break;
                        case 3:
                            switch (dir.ToUpper().Trim())
                            {
                                case "L":
                                    if(pI >= 0 && pI < db.settings.p3Buttons.left.Count)
                                    {
                                        db.settings.p3Buttons.left[pI] = kp[0].name;
                                    }
                                    break;
                                case "R":
                                    if (pI >= 0 && pI < db.settings.p3Buttons.right.Count)
                                    {
                                        db.settings.p3Buttons.right[pI] = kp[0].name;
                                    }
                                    break;
                                case "U":
                                    if (pI >= 0 && pI < db.settings.p3Buttons.bump.Count)
                                    {
                                        db.settings.p3Buttons.bump[pI] = kp[0].name;
                                    }
                                    break;
                                case "D":
                                    if (pI >= 0 && pI < db.settings.p3Buttons.super.Count)
                                    {
                                        db.settings.p3Buttons.super[pI] = kp[0].name;
                                    }
                                    break;
                            }
                        break;
                        case 4:
                            switch (dir.ToUpper().Trim())
                            {
                                case "L":
                                    if(pI >= 0 && pI < db.settings.p4Buttons.left.Count)
                                    {
                                        db.settings.p4Buttons.left[pI] = kp[0].name;
                                    }
                                    break;
                                case "R":
                                    if (pI >= 0 && pI < db.settings.p4Buttons.right.Count)
                                    {
                                        db.settings.p4Buttons.right[pI] = kp[0].name;
                                    }
                                    break;
                                case "U":
                                    if (pI >= 0 && pI < db.settings.p4Buttons.bump.Count)
                                    {
                                        db.settings.p4Buttons.bump[pI] = kp[0].name;
                                    }
                                    break;
                                case "D":
                                    if (pI >= 0 && pI < db.settings.p4Buttons.super.Count)
                                    {
                                        db.settings.p4Buttons.super[pI] = kp[0].name;
                                    }
                                    break;
                            }
                        break;
                        case 5:
                            switch (dir.ToUpper().Trim())
                            {
                                case "L":
                                    if(pI >= 0 && pI < db.settings.p5Buttons.left.Count)
                                    {
                                        db.settings.p5Buttons.left[pI] = kp[0].name;
                                    }
                                    break;
                                case "R":
                                    if (pI >= 0 && pI < db.settings.p5Buttons.right.Count)
                                    {
                                        db.settings.p5Buttons.right[pI] = kp[0].name;
                                    }
                                    break;
                                case "U":
                                    if (pI >= 0 && pI < db.settings.p5Buttons.bump.Count)
                                    {
                                        db.settings.p5Buttons.bump[pI] = kp[0].name;
                                    }
                                    break;
                                case "D":
                                    if (pI >= 0 && pI < db.settings.p5Buttons.super.Count)
                                    {
                                        db.settings.p5Buttons.super[pI] = kp[0].name;
                                    }
                                    break;
                            }
                        break;
                        case 6:
                            switch (dir.ToUpper().Trim())
                            {
                                case "L":
                                    if(pI >= 0 && pI < db.settings.p6Buttons.left.Count)
                                    {
                                        db.settings.p6Buttons.left[pI] = kp[0].name;
                                    }
                                    break;
                                case "R":
                                    if (pI >= 0 && pI < db.settings.p6Buttons.right.Count)
                                    {
                                        db.settings.p6Buttons.right[pI] = kp[0].name;
                                    }
                                    break;
                                case "U":
                                    if (pI >= 0 && pI < db.settings.p6Buttons.bump.Count)
                                    {
                                        db.settings.p6Buttons.bump[pI] = kp[0].name;
                                    }
                                    break;
                                case "D":
                                    if (pI >= 0 && pI < db.settings.p6Buttons.super.Count)
                                    {
                                        db.settings.p6Buttons.super[pI] = kp[0].name;
                                    }
                                    break;
                            }
                        break;
                        case 7:
                            switch (dir.ToUpper().Trim())
                            {
                                case "L":
                                    if(pI >= 0 && pI < db.settings.p7Buttons.left.Count)
                                    {
                                        db.settings.p7Buttons.left[pI] = kp[0].name;
                                    }
                                    break;
                                case "R":
                                    if (pI >= 0 && pI < db.settings.p7Buttons.right.Count)
                                    {
                                        db.settings.p7Buttons.right[pI] = kp[0].name;
                                    }
                                    break;
                                case "U":
                                    if (pI >= 0 && pI < db.settings.p7Buttons.bump.Count)
                                    {
                                        db.settings.p7Buttons.bump[pI] = kp[0].name;
                                    }
                                    break;
                                case "D":
                                    if (pI >= 0 && pI < db.settings.p7Buttons.super.Count)
                                    {
                                        db.settings.p7Buttons.super[pI] = kp[0].name;
                                    }
                                    break;
                            }
                        break;
                        case 8:
                            switch (dir.ToUpper().Trim())
                            {
                                case "L":
                                    if(pI >= 0 && pI < db.settings.p8Buttons.left.Count)
                                    {
                                        db.settings.p8Buttons.left[pI] = kp[0].name;
                                    }
                                    break;
                                case "R":
                                    if (pI >= 0 && pI < db.settings.p8Buttons.right.Count)
                                    {
                                        db.settings.p8Buttons.right[pI] = kp[0].name;
                                    }
                                    break;
                                case "U":
                                    if (pI >= 0 && pI < db.settings.p8Buttons.bump.Count)
                                    {
                                        db.settings.p1Buttons.bump[pI] = kp[0].name;
                                    }
                                    break;
                                case "D":
                                    if (pI >= 0 && pI < db.settings.p8Buttons.super.Count)
                                    {
                                        db.settings.p8Buttons.super[pI] = kp[0].name;
                                    }
                                    break;
                            }
                        break;
                    }

                    listening = null;
                }
            }

            info.text = "Press a button to set it to Player " + pL.ToString() + "'s " + direction;
        }
    }

    public void ButtonSetup(string playerComaLRUD)
    {
        listening = playerComaLRUD;
    }
}
