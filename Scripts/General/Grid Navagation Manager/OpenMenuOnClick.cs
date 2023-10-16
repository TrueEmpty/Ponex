using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenMenuOnClick : MonoBehaviour
{
    MenuManager mm;
    public string menuName = "";
    public bool setToName = false;

    // Start is called before the first frame update
    void Start()
    {
        mm = MenuManager.instance;

        if(setToName)
        {
            menuName = gameObject.name;
        }
    }

    public void OnClick(int player)
    {
        if(mm != null && menuName != "")
        {
            mm.OpenMenu(menuName);
        }
    }
}
