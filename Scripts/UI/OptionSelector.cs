using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionSelector : MonoBehaviour
{
    [SerializeField]
    public List<GameObject> options = new List<GameObject>();

    public int lockedInt = -1;
    public bool lockHorizontal = true;

    public int selection = 0;

    #region Debug Info
    [SerializeField]
    Vector2 largest = Vector2.zero;
    [SerializeField]
    Vector2 current = Vector2.zero;
    [SerializeField]
    Vector4 selectionUDLR = Vector4.zero;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        
    }


    private void Update()
    {
        UpOption();
        DownOption();
        RightOption();
        LeftOption();
    }

    public int UpOption()
    {
        int result = -1;

        Vector4 sI = new Vector4(CurrentRow(), CurrentColumn(), LargestRow(), LargestColumn());
        int num;

        if (lockHorizontal)
        {
            num = selection - lockedInt;

            if (num < 0)
            {
                result = (options.Count - lockedInt)+(int)(sI.y-1);
            }
            else if (num < options.Count)
            {
                result = num;
            }
        }
        else
        {
            num = selection - 1;

            if (CurrentColumn(num) != sI.y || num < 0)
            {
                result = lockedInt * ((int)sI.y - 1) + (lockedInt - 1);
            }
            else if (num >= 0 && num < options.Count)
            {
                result = num;
            }
        }

        selectionUDLR.x = result;
        return result;
    }

    public int DownOption()
    {
        int result = -1;

        Vector4 sI = new Vector4(CurrentRow(), CurrentColumn(), LargestRow(), LargestColumn());
        int num;

        if (lockHorizontal)
        {
            num = selection + lockedInt;

            if (num >= options.Count)
            {
                result = (int)(sI.y - 1);
            }
            else if (num >= 0)
            {
                result = num;
            }
        }
        else
        {
            num = selection + 1;

            if (CurrentColumn(num) != sI.y || num >= options.Count)
            {
                result = (lockedInt * ((int)sI.y - 1));
            }
            else if (num >= 0 && num < options.Count)
            {
                result = num;
            }
        }
        selectionUDLR.y = result;
        return result;
    }

    public int LeftOption()
    {
        int result = -1;

        Vector4 sI = new Vector4(CurrentRow(), CurrentColumn(), LargestRow(), LargestColumn());
        int num;

        if (lockHorizontal)
        {
            num = selection - 1;

            if (CurrentRow(num) != sI.x || num < 0)
            {
                result = (lockedInt * ((int)sI.x - 1)) + (lockedInt - 1);
            }
            else if (num >= 0 && num < options.Count)
            {
                result = num;
            }
        }
        else
        {
            num = selection - lockedInt;

            if (num < 0)
            {
                result = (options.Count - lockedInt) + (int)(sI.x-1);
            }
            else if (num < options.Count)
            {
                result = num;
            }
        }

        selectionUDLR.z = result;
        return result;
    }

    public int RightOption()
    {
        int result = -1;

        Vector4 sI = new Vector4(CurrentRow(), CurrentColumn(), LargestRow(), LargestColumn());
        int num;

        if (lockHorizontal)
        {
            num = selection + 1;

            if (CurrentRow(num) != sI.x || num >= options.Count)
            {
                result = (lockedInt * ((int)sI.x -1));
            }
            else if (num >= 0 && num < options.Count)
            {
                result = num;
            }
        }
        else
        {
            num = selection + lockedInt;

            if (num >= options.Count)
            {
                result = (int)(sI.x - 1);
            }
            else if (num >= 0)
            {
                result = num;
            }
        }
        selectionUDLR.w = result;
        return result;
    }

    public int LargestRow()
    {
        int result = 0;
        int lInt = 1;

        if(lockedInt > 0)
        {
            lInt = lockedInt;
        }

        if (lockHorizontal)
        {
            result = Mathf.CeilToInt((float)(options.Count) / (float)lInt);
        }
        else
        {
            if(options.Count < lInt)
            {
                result = 1;
            }
            else
            {
                result = lInt;
            }
        }

        largest.x = result;
        return result;
    }

    public int LargestColumn()
    {
        int result = 0;
        int lInt = 1;

        if (lockedInt > 0)
        {
            lInt = lockedInt;
        }

        if (lockHorizontal)
        {
            if (options.Count < lInt)
            {
                result = 1;
            }
            else
            {
                result = lInt;
            }
        }
        else
        {
            result = Mathf.CeilToInt((float)(options.Count) / (float)lInt);
        }

        largest.y = result;
        return result;
    }

    public GameObject CurrentOption(int sel = -1)
    {
        GameObject result = null;

        if (sel < 0 || sel >= options.Count)
        {
            sel = selection;
        }

        if (sel >= 0 && sel < options.Count)
        {
            result = options[sel];
        }

        return result;
    }

    public int CurrentRow(int sel = -1)
    {
        int result = -1;
        bool updateCur = false;

        if (sel < 0 || sel >= options.Count)
        {
            sel = selection;
            updateCur = true;
        }

        if (sel >= 0 && sel < options.Count)
        {
            int lInt = 1;
            int sl = sel;

            if (lockedInt > 0)
            {
                lInt = lockedInt;
            }

            if (lInt >= options.Count)
            {
                lInt = options.Count;
            }

            if (lockHorizontal)
            {
                result = Mathf.CeilToInt(((float)sl/ (float)lInt) + .1f);
            }
            else
            {
                result = ((sl + 1) % lInt);

                if (result == 0)
                {
                    result = lInt;
                }
            }
        }

        if(updateCur)
        {
            current.x = result;
        }
        return result;
    }

    public int CurrentColumn(int sel = -1)
    {
        int result = -1;
        bool updateCur = false;

        if (sel < 0 || sel >= options.Count)
        {
            sel = selection;
            updateCur = true;
        }

        if (sel >= 0 && sel < options.Count)
        {
            int lInt = 1;

            int sl = sel;

            if (sel < 0)
            {
                sl = 0;
            }


            if (lockedInt > 0)
            {
                lInt = lockedInt;
            }

            if(lInt >= options.Count)
            {
                lInt = options.Count;
            }

            if (lockHorizontal)
            {
                result = (sl+1) % lInt;

                //Debug.Log(sl + " and " + lInt + " = " + sl % lInt);

                if (result == 0)
                {
                    result = lInt;
                }
            }
            else
            {
                result = Mathf.CeilToInt(((float)sl / (float)lInt) + .1f);
            }
        }

        if (updateCur)
        {
            current.y = result;
        }
        return result;
    }
}


