using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerButtonLine : MonoBehaviour
{
    public int index = -1;

    public Text pName;
    public Text pName2;
    public Text left;
    public Image left_I;
    public Text right;
    public Image right_I;
    public Text up;
    public Image up_I;
    public Text down;
    public Image down_I;

    public GameObject norm;
    public GameObject addNew;

    public PlayerButtonSetup pBS;

    string playerNumber;
    int playerIndex;

    string lString;
    string RString;
    string UString;
    string DString;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(pBS != null)
        {
            //Get Info
            if (index < pBS.buttonShowList.Count)
            {
                string bSL = pBS.buttonShowList[index];
                string[] pOuts = bSL.Split(',');

                playerNumber = pOuts[0];
                playerIndex = int.Parse(pOuts[1]);

                lString = pOuts[2];
                RString = pOuts[3];
                UString = pOuts[4];
                DString = pOuts[5];

                bool addRow = false;

                if(lString == "UnBound" && RString == "UnBound" && UString == "UnBound" && DString == "UnBound")
                {
                    addRow = true;
                }

                norm.SetActive(!addRow);
                addNew.SetActive(addRow);

                if(!addRow)
                {
                    pName.text = (playerIndex == 0) ? "Player " + playerNumber: "";
                    left.text = lString;
                    right.text = RString;
                    up.text = UString;
                    down.text = DString;
                }
                else
                {
                    pName2.text = (playerIndex == 0) ? "Player " + playerNumber : "";
                }

                if(pBS.listening != null && pBS.listening != "")
                {
                    int pL = -1;
                    int pI = -1;
                    string dir = "";

                    string[] split = pBS.listening.Split(',');

                    if (split.Length > 2)
                    {
                        pL = int.Parse(split[0]);
                        dir = split[1];
                        pI = int.Parse(split[2]);
                    }

                    left_I.color = (playerNumber == pL.ToString() && (dir == "L" || dir == "FL") && playerIndex == pI) ? Color.yellow : Color.white;
                    right_I.color = (playerNumber == pL.ToString() && (dir == "R" || dir == "FR") && playerIndex == pI) ? Color.yellow : Color.white;
                    up_I.color = (playerNumber == pL.ToString() && (dir == "U" || dir == "FU") && playerIndex == pI) ? Color.yellow : Color.white;
                    down_I.color = (playerNumber == pL.ToString() && (dir == "D" || dir == "FD") && playerIndex == pI) ? Color.yellow : Color.white;
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    public void ButtonAdd(string dir)
    {
        if(index < pBS.buttonShowList.Count)
        {
            pBS.ButtonSetup(playerNumber + "," + dir + "," + playerIndex);
        }
    }

    public void AddNew()
    {
        Database db = Database.instance;
        PlayerButtons pb = null;

        switch(int.Parse(playerNumber))
        {
            case 1:
                pb = db.settings.p1Buttons;
                break;
            case 2:
                pb = db.settings.p2Buttons;
                break;
            case 3:
                pb = db.settings.p3Buttons;
                break;
            case 4:
                pb = db.settings.p4Buttons;
                break;
            case 5:
                pb = db.settings.p5Buttons;
                break;
            case 6:
                pb = db.settings.p6Buttons;
                break;
            case 7:
                pb = db.settings.p7Buttons;
                break;
            case 8:
                pb = db.settings.p8Buttons;
                break;
        }

        if(pb != null)
        {
            pb.left.Add("UnBound");
            pb.right.Add("UnBound");
            pb.bump.Add("UnBound");
            pb.super.Add("UnBound");
        }
    }

    public void Remove()
    {
        Database db = Database.instance;
        PlayerButtons pb = null;

        switch (int.Parse(playerNumber))
        {
            case 1:
                pb = db.settings.p1Buttons;
                break;
            case 2:
                pb = db.settings.p2Buttons;
                break;
            case 3:
                pb = db.settings.p3Buttons;
                break;
            case 4:
                pb = db.settings.p4Buttons;
                break;
            case 5:
                pb = db.settings.p5Buttons;
                break;
            case 6:
                pb = db.settings.p6Buttons;
                break;
            case 7:
                pb = db.settings.p7Buttons;
                break;
            case 8:
                pb = db.settings.p8Buttons;
                break;
        }

        if (pb != null)
        {
            if(playerIndex >= 0 && playerIndex < pb.left.Count)
            {
                pb.left.RemoveAt(playerIndex);
            }

            if (playerIndex >= 0 && playerIndex < pb.right.Count)
            {
                pb.right.RemoveAt(playerIndex);
            }

            if (playerIndex >= 0 && playerIndex < pb.bump.Count)
            {
                pb.bump.RemoveAt(playerIndex);
            }

            if (playerIndex >= 0 && playerIndex < pb.super.Count)
            {
                pb.super.RemoveAt(playerIndex);
            }
        }
    }
}
