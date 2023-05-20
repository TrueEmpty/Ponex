using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelector : MonoBehaviour
{
    Database db;
    public GameSetup gS;

    List<ItemDisplay> charScreens = new List<ItemDisplay>();

    public int p1Selected = 0;
    public int p2Selected = 0;
    public int p3Selected = 0;
    public int p4Selected = 0;

    public int p1DisabledActiveComputer = 0; //-1 Disabled,0 Player,1 CPU
    public int p2DisabledActiveComputer = 0; //-1 Disabled,0 Player,1 CPU
    public int p3DisabledActiveComputer = 0; //-1 Disabled,0 Player,1 CPU
    public int p4DisabledActiveComputer = 0; //-1 Disabled,0 Player,1 CPU

    public Text p1Text;
    public Text p2Text;
    public Text p3Text;
    public Text p4Text;

    public RawImage p1Portrait;
    public RawImage p2Portrait;
    public RawImage p3Portrait;
    public RawImage p4Portrait;

    public Color p1Color;
    public Color p2Color;
    public Color p3Color;
    public Color p4Color;

    public Color disabledColor;

    public Color inactiveColor = Color.black;

    public bool active = true;

    public SelectionHighlight p1Selector;
    public SelectionHighlight p2Selector;
    public SelectionHighlight p3Selector;
    public SelectionHighlight p4Selector;

    float p1Clicked = 0;
    float p2Clicked = 0;
    float p3Clicked = 0;
    float p4Clicked = 0;

    public Transform borderTop;
    public Transform borderRight;
    public Transform borderBot;
    public Transform borderLeft;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;

        GetCharScreens();
        LoadChars();
    }

    // Update is called once per frame
    void Update()
    {
        SelectorMove();
        ShowSelection();

        ShowPlayerIndicator();
    }

    void ShowPlayerIndicator()
    {
        Color dC = Color.white;
        dC.a = 0;

        #region P1
        if (p1DisabledActiveComputer < 0)
        {
            p1Portrait.color = dC;
            p1Text.enabled = false;
            p1Text.color = disabledColor;
        }
        else if(p1DisabledActiveComputer > 0)
        {
            p1Portrait.color = Color.white;
            p1Text.enabled = true;
            p1Text.text = "CPU";
            p1Text.color = Color.gray;
        }
        else
        {
            p1Text.color = p1Color;
            p1Portrait.color = Color.white;
            p1Text.enabled = true;
            p1Text.text = "P1";
        }
        #endregion

        #region P2
        if (p2DisabledActiveComputer < 0)
        {
            p2Portrait.color = dC;
            p2Text.enabled = false;
            p2Text.color = disabledColor;
        }
        else if (p2DisabledActiveComputer > 0)
        {
            p2Portrait.color = Color.white;
            p2Text.enabled = true;
            p2Text.text = "CPU";
            p2Text.color = Color.gray;
        }
        else
        {
            p2Text.color = p2Color;
            p2Portrait.color = Color.white;
            p2Text.enabled = true;
            p2Text.text = "P2";
        }
        #endregion

        #region P3
        if (p3DisabledActiveComputer < 0)
        {
            p3Portrait.color = dC;
            p3Text.enabled = false;
            p3Text.color = disabledColor;
        }
        else if (p3DisabledActiveComputer > 0)
        {
            p3Portrait.color = Color.white;
            p3Text.enabled = true;
            p3Text.text = "CPU";
            p3Text.color = Color.gray;
        }
        else
        {
            p3Text.color = p3Color;
            p3Portrait.color = Color.white;
            p3Text.enabled = true;
            p3Text.text = "P3";
        }
        #endregion

        #region P4
        if (p4DisabledActiveComputer < 0)
        {
            p4Portrait.color = dC;
            p4Text.enabled = false;
            p4Text.color = disabledColor;
        }
        else if (p4DisabledActiveComputer > 0)
        {
            p4Portrait.color = Color.white;
            p4Text.enabled = true;
            p4Text.text = "CPU";
            p4Text.color = Color.gray;
        }
        else
        {
            p4Text.color = p4Color;
            p4Portrait.color = Color.white;
            p4Text.enabled = true;
            p4Text.text = "P4";
        }
        #endregion
    }

    void GetCharScreens()
    {
        if (charScreens.Count == 0)
        {
            if (transform.childCount > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                {

                    ItemDisplay iD = transform.GetChild(i).GetComponent<ItemDisplay>();

                    if (iD != null)
                    {
                        if (!charScreens.Contains(iD))
                        {
                            iD.inactiveColor = inactiveColor;
                            charScreens.Add(iD);
                        }
                    }
                }
            }
        }
    }

    void LoadChars()
    {
        if (charScreens.Count > 0)
        {
            for (int i = 0; i < charScreens.Count; i++)
            {
                if (i >= 0 && i < db.characters.Count)
                {
                    charScreens[i].selection = i;
                }
                else
                {
                    charScreens[i].selection = -1;
                }
            }
        }
    }

    void SelectorMove()
    {
        p1Selector.transform.position += GetPlayerInput(db.settings.p1Buttons, p1Selector.transform.position) * Time.deltaTime;
        p2Selector.transform.position += GetPlayerInput(db.settings.p2Buttons, p2Selector.transform.position) * Time.deltaTime;
        p3Selector.transform.position += GetPlayerInput(db.settings.p3Buttons, p3Selector.transform.position) * Time.deltaTime;
        p4Selector.transform.position += GetPlayerInput(db.settings.p4Buttons, p4Selector.transform.position) * Time.deltaTime;

        //Check if it is over anything
        #region P1
        if (Input.GetKey(db.settings.p1Buttons.left) && Input.GetKey(db.settings.p1Buttons.right))
        {
            p1Clicked += Time.deltaTime;
        }

        if(!Input.GetKey(db.settings.p1Buttons.left) && !Input.GetKey(db.settings.p1Buttons.right) &&
            !Input.GetKey(db.settings.p1Buttons.bump) && !Input.GetKey(db.settings.p1Buttons.super))
        {
            p1Clicked = 0;
        }

        if (p1Clicked > 0 && p1Clicked < .25f)
        {
            p1Clicked = 100;

            if(p1Selector.selectedUI != null)
            {
                switch(p1Selector.selectedUI.name.ToLower().Trim())
                {
                    case "back":
                        gS.Back();
                        break;
                    case "confirm":
                        gS.Confirm();
                        break;
                    case "charactergrab":
                        ItemDisplay iD = p1Selector.selectedUI.GetComponent<ItemDisplay>();

                        if(iD != null)
                        {
                            if(iD.active)
                            {
                                p1Selected = iD.selection;
                            }
                        }
                        break;
                    case "p1":
                        TogglePlayer(1);
                        break;
                    case "p2":
                        TogglePlayer(2);
                        break;
                    case "p3":
                        TogglePlayer(3);
                        break;
                    case "p4":
                        TogglePlayer(4);
                        break;
                }
            }
        }
        #endregion

        #region P2
        if (Input.GetKey(db.settings.p2Buttons.left) && Input.GetKey(db.settings.p2Buttons.right))
        {
            p2Clicked += Time.deltaTime;
        }

        if (!Input.GetKey(db.settings.p2Buttons.left) && !Input.GetKey(db.settings.p2Buttons.right) &&
            !Input.GetKey(db.settings.p2Buttons.bump) && !Input.GetKey(db.settings.p2Buttons.super))
        {
            p2Clicked = 0;
        }

        if (p2Clicked > 0 && p2Clicked < .25f)
        {
            p2Clicked = 100;

            if (p2Selector.selectedUI != null)
            {
                switch (p2Selector.selectedUI.name.ToLower().Trim())
                {
                    case "back":
                        gS.Back();
                        break;
                    case "confirm":
                        gS.Confirm();
                        break;
                    case "charactergrab":
                        ItemDisplay iD = p2Selector.selectedUI.GetComponent<ItemDisplay>();

                        if (iD != null)
                        {
                            if (iD.active)
                            {
                                p2Selected = iD.selection;
                            }
                        }
                        break;
                    case "p1":
                        TogglePlayer(1);
                        break;
                    case "p2":
                        TogglePlayer(2);
                        break;
                    case "p3":
                        TogglePlayer(3);
                        break;
                    case "p4":
                        TogglePlayer(4);
                        break;
                }
            }
        }
        #endregion

        #region P3
        if (Input.GetKey(db.settings.p3Buttons.left) && Input.GetKey(db.settings.p3Buttons.right))
        {
            p3Clicked += Time.deltaTime;
        }

        if (!Input.GetKey(db.settings.p3Buttons.left) && !Input.GetKey(db.settings.p3Buttons.right) &&
            !Input.GetKey(db.settings.p3Buttons.bump) && !Input.GetKey(db.settings.p3Buttons.super))
        {
            p3Clicked = 0;
        }

        if (p3Clicked > 0 && p3Clicked < .25f)
        {
            p3Clicked = 100;

            if (p3Selector.selectedUI != null)
            {
                switch (p3Selector.selectedUI.name.ToLower().Trim())
                {
                    case "back":
                        gS.Back();
                        break;
                    case "confirm":
                        gS.Confirm();
                        break;
                    case "charactergrab":
                        ItemDisplay iD = p3Selector.selectedUI.GetComponent<ItemDisplay>();

                        if (iD != null)
                        {
                            if (iD.active)
                            {
                                p3Selected = iD.selection;
                            }
                        }
                        break;
                    case "p1":
                        TogglePlayer(1);
                        break;
                    case "p2":
                        TogglePlayer(2);
                        break;
                    case "p3":
                        TogglePlayer(3);
                        break;
                    case "p4":
                        TogglePlayer(4);
                        break;
                }
            }
        }
        #endregion

        #region P4
        if (Input.GetKey(db.settings.p4Buttons.left) && Input.GetKey(db.settings.p4Buttons.right))
        {
            p4Clicked += Time.deltaTime;
        }

        if (!Input.GetKey(db.settings.p4Buttons.left) && !Input.GetKey(db.settings.p4Buttons.right) &&
            !Input.GetKey(db.settings.p4Buttons.bump) && !Input.GetKey(db.settings.p4Buttons.super))
        {
            p4Clicked = 0;
        }

        if (p4Clicked > 0 && p4Clicked < .25f)
        {
            p4Clicked = 100;

            if (p4Selector.selectedUI != null)
            {
                switch (p4Selector.selectedUI.name.ToLower().Trim())
                {
                    case "back":
                        gS.Back();
                        break;
                    case "confirm":
                        gS.Confirm();
                        break;
                    case "charactergrab":
                        ItemDisplay iD = p4Selector.selectedUI.GetComponent<ItemDisplay>();

                        if (iD != null)
                        {
                            if (iD.active)
                            {
                                p4Selected = iD.selection;
                            }
                        }
                        break;
                    case "p1":
                        TogglePlayer(1);
                        break;
                    case "p2":
                        TogglePlayer(2);
                        break;
                    case "p3":
                        TogglePlayer(3);
                        break;
                    case "p4":
                        TogglePlayer(4);
                        break;
                }
            }
        }
        #endregion
    }

    Vector3 GetPlayerInput(PlayerButtons pB,Vector3 curPos)
    {
        Vector3 result = Vector3.zero;

        if(Input.GetKey(pB.bump) && curPos.y < borderTop.position.y)
        {
            result.y += 1;
        }

        if(Input.GetKey(pB.super) && curPos.y > borderBot.position.y)
        {
            result.y -= 1;
        }

        if(Input.GetKey(pB.right) && curPos.x < borderRight.position.x)
        {
            result.x += 1;
        }

        if(Input.GetKey(pB.left) && curPos.x > borderLeft.position.x)
        {
            result.x -= 1;
        }

        return result;
    }

    void ShowSelection()
    {
        if(charScreens.Count > 0)
        {
            for(int i = 0; i < charScreens.Count; i++)
            {
                charScreens[i].p1Selected = (p1Selected == i);
                charScreens[i].p2Selected = (p2Selected == i);
                charScreens[i].p3Selected = (p3Selected == i);
                charScreens[i].p4Selected = (p4Selected == i);
            }
        }
    }

    public void TogglePlayer(int p)
    {
        switch (p)
        {
            case 1:
                if (p1DisabledActiveComputer < 0)
                {
                    p1DisabledActiveComputer = 0;
                }
                else if(p1DisabledActiveComputer > 0)
                {
                    p1DisabledActiveComputer = -1;
                }
                else
                {
                    p1DisabledActiveComputer = 1;
                }
                break;
            case 2:
                if (p2DisabledActiveComputer < 0)
                {
                    p2DisabledActiveComputer = 0;
                }
                else if (p2DisabledActiveComputer > 0)
                {
                    p2DisabledActiveComputer = -1;
                }
                else
                {
                    p2DisabledActiveComputer = 1;
                }
                break;
            case 3:
                if (p3DisabledActiveComputer < 0)
                {
                    p3DisabledActiveComputer = 0;
                }
                else if (p3DisabledActiveComputer > 0)
                {
                    p3DisabledActiveComputer = -1;
                }
                else
                {
                    p3DisabledActiveComputer = 1;
                }
                break;
            case 4:
                if (p4DisabledActiveComputer < 0)
                {
                    p4DisabledActiveComputer = 0;
                }
                else if (p4DisabledActiveComputer > 0)
                {
                    p4DisabledActiveComputer = -1;
                }
                else
                {
                    p4DisabledActiveComputer = 1;
                }
                break;
        }
    }

    public Stats GetPlayer(int p)
    {
        Stats result = null;

        switch(p)
        {
            case 1:
                if(p1Selected >= 0 && p1Selected < db.characters.Count)
                {
                    result = new Stats(db.characters[p1Selected]);
                }
                break;
            case 2:
                if (p2Selected >= 0 && p2Selected < db.characters.Count)
                {
                    result = new Stats(db.characters[p2Selected]);
                }
                break;
            case 3:
                if (p3Selected >= 0 && p3Selected < db.characters.Count)
                {
                    result = new Stats(db.characters[p3Selected]);
                }
                break;
            case 4:
                if (p4Selected >= 0 && p4Selected < db.characters.Count)
                {
                    result = new Stats(db.characters[p4Selected]);
                }
                break;
        }

        return result;
    }

    //Add to all Gamesetup Objects that you want to use to change the gamplay info
    public void AddGameplayInfo()
    {

    }
}
