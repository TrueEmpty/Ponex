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

    }
    public void Remove()
    {

    }
}
