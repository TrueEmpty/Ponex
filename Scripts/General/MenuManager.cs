using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;

    public List<string> openMenu = new List<string>(); //Which menu is open

    public List<MenuClass> menus = new List<MenuClass>();
    public List<GridControl> controls;

    public float inLineTolorance = 5;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

    // Use this for initialization
    void Start()
    {
        //Will check weather it should be active or not
        DisplayCheck();
    }

    // Will Check everytime a new menu is made
    void DisplayCheck()
    {
        //Check if menu has anything in it
        if (openMenu.Count > 0)
        {
            List<string> curOM = new List<string>();
            for (int i = openMenu.Count - 1; i >= 0; i--)
            {
                MenuClass mc = FindMenu(openMenu[i]);

                if (mc.title == "...No Menu...")
                {
                    //There is no menu to set anything active
                }
                else
                {
                    if (mc.holder != null)
                    {
                        mc.holder.SetActive(true);
                        curOM.Add(mc.title);
                    }
                }

                //Check if it is an overlay
                if (mc.overlay == false)
                {
                    //it is not an overlay so leave fr loop
                    break;
                }
            }

            for(int i = menus.Count - 1; i >= 0; i--)
            {
                MenuClass mC = menus[i];

                if(mC.holder != null)
                {
                    mC.holder.SetActive(curOM.Contains(mC.title));
                }
            }
        }
    }

    public GridControl GetFirstControl()
    {
        GridControl result = null;

        if(controls.Count > 0)
        {
            List<GridControl> aag = controls.FindAll(x => x.group == GetOpenMenu().title && x.isActiveAndEnabled);

            if(aag.Count > 0)
            {
                aag.Sort((p1, p2) => p2.OrderValue().CompareTo(p1.OrderValue()));

                result = aag[0];
            }
        }

        return result;
    }

    public void OpenMenu(string menuName)
    {
        if(GetOpenMenu().title != menuName && GetOpenMenu(true).title != menuName)
        {
            openMenu.Add(menuName);

            //Will check weather it should be active or not
            DisplayCheck();
        }
    }

    public void RemoveMenu(string menuName)
    {
        //Debug.Log("Test Remove");
        if (openMenu.Contains(menuName))
        {
            MenuClass mC = menus.Find(x=> x.title == menuName);

            openMenu.Remove(menuName);
            //Will check weather it should be active or not
            DisplayCheck();
        }
    }

    public void BackMenu(int amount = 1)
    {
        if (amount < 1)
        {
            amount = 1;
        }

        if (openMenu.Count > amount)
        {
            for(int i = 1; i <= amount; i++)
            {
                RemoveMenu(openMenu[openMenu.Count - 1]);
            }

            //Will check weather it should be active or not
            DisplayCheck();
        }
    }

    public void BackUnitl(string menu)
    {
        bool keepGoing = true;

        while (keepGoing)
        {
            BackMenu();

            if (openMenu[openMenu.Count - 1].ToLower().Trim() == menu.ToLower().Trim())
            {
                keepGoing = false;
            }
            else if (openMenu.Count <= 1)
            {
                keepGoing = false;
            }
        }
    }

    public void BackUnitlPrev(string menu)
    {
        bool keepGoing = true;

        while (keepGoing)
        {
            BackMenu();

            if (openMenu[openMenu.Count - 2].ToLower().Trim() == menu.ToLower().Trim())
            {
                keepGoing = false;
            }
            else if (openMenu.Count <= 2)
            {
                keepGoing = false;
            }
        }
    }

    public void ToggleMenu(string menuName)
    {
        //Debug.Log("Test Toggle");
        MenuClass mC = FindMenu(menuName);

        if (mC.toggle)
        {
            RemoveMenu(menuName);
        }
        else
        {
            OpenMenu(menuName);
        }

        mC.toggle = !mC.toggle;
    }

    public MenuClass FindMenu(string menuName)
    {
        MenuClass mC = new MenuClass("...No Menu...", false, null);

        //Check if menu with that title exist
        if (menus.Exists(x => x.title == menuName))
        {
            //If it does exsit finds it
            mC = menus.Find(x => x.title == menuName);
        }

        return mC;
    }

    public MenuClass GetOpenMenu(bool nonOverlay = false)
    {
        MenuClass mC = new MenuClass("...No Menu...", false, null);

        //Check if open menu has a count
        if (openMenu.Count > 0)
        {
            for (int i = openMenu.Count - 1; i >= 0; i--)
            {
                mC = FindMenu(openMenu[i]);

                if (nonOverlay && mC.overlay == false)
                {
                    break;
                }
                else if (!nonOverlay)
                {
                    break;
                }
            }
        }

        return mC;
    }

}
