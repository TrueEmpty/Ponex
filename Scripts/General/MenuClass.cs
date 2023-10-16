using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MenuClass
{
    public string title = ""; //The name of the Menu, what will be looked up to activate the menu
    public bool overlay = true; //If this screen will display on top of the previous screen
    public GameObject holder; //The GameObject that holds the menu Items

    [HideInInspector]
    public bool toggle = false;

    public MenuClass(string menuName, bool willOverlay,GameObject menuHolder)
    {
        title = menuName;
        overlay = willOverlay;
        holder = menuHolder;
    }
}
