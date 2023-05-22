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
