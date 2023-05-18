using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuControl : MonoBehaviour
{
    public List<string> openMenu = new List<string>(); //Which menu is open

    public List<MenuClass> menus = new List<MenuClass>();

    // Use this for initialization
    void Start()
    {
        //Will check weather it should be active or not
        DisplayCheck();
    }

    // Will Check everytime a new menu is made
    void DisplayCheck ()
    {
        //Check if menu has anything in it
        if (openMenu.Count > 0)
        {
            CloseAllMenus();

            for (int i = openMenu.Count - 1; i >= 0; i--)
            {
                MenuClass mc = FindMenu(openMenu[i]);

               if(mc.title == "...No Menu...")
               {
                    //There is no menu do set anything active
               }
               else
               {
                    if (mc.holder != null)
                    {
                        mc.holder.SetActive(true);
                    }
               }

               //Check if it is an overlay
               if(mc.overlay == false)
               {
                    //it is not an overlay so leave fr loop
                    break;
               }

            }
        }
        else
        {
            CloseAllMenus();
        }
    }

    public void OpenMenu(string menuName)
    {
        //Debug.Log("Test Open");
        openMenu.Add(menuName);
        //Will check weather it should be active or not
        DisplayCheck();
    }

    public void RemoveMenu(string menuName)
    {
        //Debug.Log("Test Remove");
        if (openMenu.Contains(menuName))
        {
            openMenu.Remove(menuName);
            //Will check weather it should be active or not
            DisplayCheck();
        }
    }

    public void BackMenu(int amount = 1)
    {
        if(amount < 1)
        {
            amount = 1;
        }

        //Debug.Log("Test Remove");
        if (openMenu.Count > amount)
        {
            openMenu.RemoveRange(openMenu.Count - amount,amount);
            //Will check weather it should be active or not
            DisplayCheck();
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
        MenuClass mC = new MenuClass("...No Menu...",false,null);

        //Check if menu with that title exist
        if (menus.Exists(x=> x.title == menuName))
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
        if(openMenu.Count > 0)
        {
            for (int i = openMenu.Count - 1; i >= 0; i--)
            {
                mC = FindMenu(openMenu[i]);

                if(nonOverlay && mC.overlay == false)
                {
                    break;
                }
                else if(!nonOverlay)
                {
                    break;
                }
            }
        }

        return mC;
    }

    void CloseAllMenus()
    {
        for(int i = 0; i < menus.Count;i++)
        {
            if(menus[i].holder != null)
            {
                menus[i].holder.SetActive(false);
            }            
        }
    }
}
