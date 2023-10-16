using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Buttons
{
    public string left = "";
    public string right = "";
    public string up = "";
    public string down = "";
    public string confirm = "";
    public string cancel = "";

    public Buttons()
    {

    }

    public Buttons(Buttons b)
    {
        left = b.left;
        right = b.right;
        up = b.up;
        down = b.down;
        confirm = b.confirm;
        cancel = b.cancel;
    }

    public string Up(Facing facing)
    {
        string result = up;

        switch(facing)
        {
            case Facing.Up:
                result = up;
                break;
            case Facing.Down:
                result = down;
                break;
            case Facing.Left:
                result = left;
                break;
            case Facing.Right:
                result = right;
                break;
        }

        return result;
    }

    public string Down(Facing facing)
    {
        string result = down;

        switch(facing)
        {
            case Facing.Up:
                result = down;
                break;
            case Facing.Down:
                result = up;
                break;
            case Facing.Left:
                result = right;
                break;
            case Facing.Right:
                result = left;
                break;
        }

        return result;
    }

    public string Left(Facing facing)
    {
        string result = left;

        switch(facing)
        {
            case Facing.Up:
                result = left;
                break;
            case Facing.Down:
                result = right;
                break;
            case Facing.Left:
                result = down;
                break;
            case Facing.Right:
                result = up;
                break;
        }

        return result;
    }

    public string Right(Facing facing)
    {
        string result = right;

        switch(facing)
        {
            case Facing.Up:
                result = right;
                break;
            case Facing.Down:
                result = left;
                break;
            case Facing.Left:
                result = up;
                break;
            case Facing.Right:
                result = down;
                break;
        }

        return result;
    }

    public static bool operator ==(Buttons a, Buttons b)
    {
        bool result = true;

        string aC = null;
        string bC = null;

        try
        {
            aC = a.ToString();
        }
        catch
        {

        }

        try
        {
            bC = b.ToString();
        }
        catch
        {

        }

        if (aC == null || bC == null)
        {
            if(aC == null && bC == null)
            {
                result = true;
            }
            else
            {
                result = false;
            }
        }
        else
        {
            if (a.left.ToLower().Trim() != b.left.ToLower().Trim())
            {
                result = false;
            }

            if (a.right.ToLower().Trim() != b.right.ToLower().Trim())
            {
                result = false;
            }

            if (a.up.ToLower().Trim() != b.up.ToLower().Trim())
            {
                result = false;
            }

            if (a.down.ToLower().Trim() != b.down.ToLower().Trim())
            {
                result = false;
            }

            if (a.confirm.ToLower().Trim() != b.confirm.ToLower().Trim())
            {
                result = false;
            }

            if (a.cancel.ToLower().Trim() != b.cancel.ToLower().Trim())
            {
                result = false;
            }
        }

        return result;
    }

    public static bool operator !=(Buttons a, Buttons b)
    {
        return !(a == b);
    }

    public bool InUse()
    {
        return Database.instance.players.Exists(x=> x.buttons == this);
    }

    public bool ButtonBeingPressed()
    {
        ButtonManager bm = ButtonManager.instance;
        bool result = false;

        if (bm.KeyDown(left))
        {
            result = true;
        }
        else if (bm.KeyDown(right))
        {
            result = true;
        }
        else if (bm.KeyDown(up))
        {
            result = true;
        }
        else if (bm.KeyDown(down))
        {
            result = true;
        }
        else if (bm.KeyDown(confirm))
        {
            result = true;
        }
        else if (bm.KeyDown(cancel))
        {
            result = true;
        }

        return result;
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return base.ToString();
    }
}
