using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemDisplay : MonoBehaviour
{
    Database db;
    
    [SerializeField]
    Image p1Select;
    [SerializeField]
    Image p2Select;
    [SerializeField]
    Image p3Select;
    [SerializeField]
    Image p4Select;

    [SerializeField]
    RawImage portrait;

    public int selection = -1;
    public bool active = false;
    public bool p1Selected = false;
    public bool p2Selected = false;
    public bool p3Selected = false;
    public bool p4Selected = false;
    public Color inactiveColor = Color.black;

    public CharacterSelector cS;
    //public ButtonClickedEvent onClick;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
    }

    // Update is called once per frame
    void Update()
    {
        active = false;
        bool inSelection = (selection >= 0 && selection < db.characters.Count);

        p1Select.gameObject.SetActive(inSelection && p1Selected);
        p2Select.gameObject.SetActive(inSelection && p2Selected);
        p3Select.gameObject.SetActive(inSelection && p3Selected);
        p4Select.gameObject.SetActive(inSelection && p4Selected);
        portrait.gameObject.SetActive(inSelection);

        if (inSelection)
        {
            Stats ct = db.characters[selection];

            if (ct != null)
            {
                if (ct.active)
                {
                    active = true;
                    portrait.color = Color.white;

                    if (ct.icon != null)
                    {
                        portrait.texture = ct.icon;
                    }
                }
                else
                {
                    portrait.color = inactiveColor;
                }
            }
        }
    }

    void OnClick(int player)
    {
        switch(player)
        {
            case 1:
                cS.p1Selected = selection;
                break;
            case 2:
                cS.p2Selected = selection;
                break;
            case 3:
                cS.p3Selected = selection;
                break;
            case 4:
                cS.p4Selected = selection;
                break;
            default:
                break;
        }
    }
}
