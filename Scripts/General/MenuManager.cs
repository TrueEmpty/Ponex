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
    readonly HashSet<string> visibleMenus = new HashSet<string>(StringComparer.Ordinal);
    readonly Dictionary<string, MenuClass> menuLookup =
        new Dictionary<string, MenuClass>(StringComparer.Ordinal);
    readonly MenuClass noMenu = new MenuClass("...No Menu...", false, null);
    int cachedMenuCount = -1;

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

        RebuildMenuLookup();
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
            visibleMenus.Clear();
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
                        if (!mc.holder.activeSelf)
                            mc.holder.SetActive(true);
                        visibleMenus.Add(mc.title);
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
                    bool shouldBeActive = visibleMenus.Contains(mC.title);
                    if (mC.holder.activeSelf != shouldBeActive)
                        mC.holder.SetActive(shouldBeActive);
                }
            }
        }
    }

    public GridControl GetFirstControl()
    {
        GridControl result = null;

        if(controls != null && controls.Count > 0)
        {
            string group = GetOpenMenu().title;
            for (int i = 0; i < controls.Count; i++)
            {
                GridControl candidate = controls[i];
                if (candidate == null || !candidate.isActiveAndEnabled || candidate.group != group)
                    continue;
                if (result == null || candidate.OrderValue() > result.OrderValue())
                    result = candidate;
            }
        }

        return result;
    }

    public void OpenMenu(string menuName)
    {
        MenuClass top = GetOpenMenu();
        MenuClass baseMenu = GetOpenMenu(true);
        if(top.title != menuName && baseMenu.title != menuName)
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
            openMenu.RemoveRange(openMenu.Count - amount, amount);
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
        EnsureMenuLookup();
        if (!string.IsNullOrEmpty(menuName)
            && menuLookup.TryGetValue(menuName, out MenuClass menu))
            return menu;
        return noMenu;
    }

    /// <summary>
    /// True when this title is on screen. Overlays stay visible on top of the menu under them.
    /// </summary>
    public bool IsMenuVisible(string title)
    {
        if (string.IsNullOrEmpty(title) || openMenu == null || openMenu.Count == 0)
            return false;

        for (int i = openMenu.Count - 1; i >= 0; i--)
        {
            string open = openMenu[i];
            if (!string.IsNullOrEmpty(open)
                && open.Trim().Equals(title.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            MenuClass mc = FindMenu(open);
            if (mc == null || mc.title == "...No Menu..." || !mc.overlay)
                break;
        }

        return false;
    }

    public MenuClass GetOpenMenu(bool nonOverlay = false)
    {
        MenuClass mC = noMenu;

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

    void EnsureMenuLookup()
    {
        int menuCount = menus != null ? menus.Count : 0;
        if (cachedMenuCount != menuCount)
            RebuildMenuLookup();
    }

    void RebuildMenuLookup()
    {
        menuLookup.Clear();
        if (menus != null)
        {
            for (int i = 0; i < menus.Count; i++)
            {
                MenuClass menu = menus[i];
                if (menu == null || string.IsNullOrEmpty(menu.title))
                    continue;
                menuLookup[menu.title] = menu;
            }
            cachedMenuCount = menus.Count;
        }
        else
        {
            cachedMenuCount = 0;
        }
    }

}
