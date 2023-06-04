using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageSelector : MonoBehaviour
{
    Database db;

    public List<Field> curFields = new List<Field>();
    [SerializeField]
    List<StageDsplay> stageScreens = new List<StageDsplay>();

    public int stageSelected = 0;
    public int groupCount = 0;

    public RawImage stagePortrait;
    public Text stageName;

    public Color inactiveColor = Color.black;

    public bool active = true;
    float minClick = 0;
    float nextClick = .25f;

    int lockedInt = 12;

    public Color selectedColor = Color.red;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;

        GetStageScreens();
        GetCurrentFieldsWithSkippingInserted();
    }

    // Update is called once per frame
    void Update()
    {
        if(stageSelected >= 0 && stageSelected < curFields.Count)
        {
            ShowSelection();
        }
    }

    void GetStageScreens()
    {
        if (stageScreens.Count == 0)
        {
            if (transform.childCount > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    StageDsplay sD = transform.GetChild(i).GetComponent<StageDsplay>();

                    if (sD != null)
                    {
                        if (!stageScreens.Contains(sD))
                        {
                            sD.inactiveColor = inactiveColor;
                            sD.selection = i;
                            sD.sS = this;
                            stageScreens.Add(sD);
                        }
                    }
                }
            }
        }
    }

    void GetCurrentFieldsWithSkippingInserted()
    {
        curFields.Clear();
        int fI = 0;

        if(db.fields.Count > 0)
        {
            for(int i = 0; i < db.fields.Count; i++)
            {
                bool stay = (stageScreens[fI].skip);

                while (stay)
                {
                    curFields.Add(new Field(false));
                    fI++;

                    if (fI >= stageScreens.Count)
                    {
                        fI = 0;
                    }

                    stay = (stageScreens[fI].skip);
                }

                curFields.Add(db.fields[i]);
                fI++;

                if(fI >= stageScreens.Count)
                {
                    fI = 0;
                }
            }
        }
    }

    void ShowSelection()
    {
        Field tSS = GetCurrentField();

        stageName.text = tSS.name;
        stagePortrait.texture = tSS.portrait;
    }

    public Field GetCurrentField()
    {
        Field result = curFields[TrueStageSelected(stageSelected)];

        return result;
    }

    public int TrueStageSelected(int sS)
    {
        return (groupCount * stageScreens.Count) + sS;
    }

    public int UpOption(int current)
    {
        int result = -1;

        Vector4 sI = new Vector4(CurrentRow(current), CurrentColumn(current), LargestRow(), LargestColumn());
        int num = current;
        int tic = 0;
        bool pass = false;

        while (!pass && tic < 10)
        {
            num -= lockedInt;

            if (num < 0)
            {
                result = (db.characters.Count - lockedInt) + (int)(sI.y - 1);
            }
            else if (num < db.characters.Count)
            {
                result = num;
            }

            if (result >= 0 && result < db.characters.Count)
            {
                pass = true;
            }

            tic++;
        }

        if (!pass)
        {
            result = current;
        }

        return result;
    }

    public int DownOption(int current)
    {
        int result = -1;

        Vector4 sI = new Vector4(CurrentRow(current), CurrentColumn(current), LargestRow(), LargestColumn());
        int num = current;
        int tic = 0;
        bool pass = false;

        while (!pass && tic < 10)
        {
            num += lockedInt;

            if (num >= db.characters.Count)
            {
                result = (int)(sI.y - 1);
            }
            else if (num >= 0)
            {
                result = num;
            }

            if (result >= 0 && result < db.characters.Count)
            {
                pass = true;
            }

            tic++;
        }

        if (!pass)
        {
            result = current;
        }

        return result;
    }

    public int LeftOption(int current)
    {
        int result = -1;

        Vector4 sI = new Vector4(CurrentRow(current), CurrentColumn(current), LargestRow(), LargestColumn());
        int num = current;
        int tic = 0;
        bool pass = false;

        while (!pass && tic < 10)
        {
            num -= 1;

            if (CurrentRow(num) != sI.x || num < 0)
            {
                result = (lockedInt * ((int)sI.x - 1)) + (lockedInt - 1);

                if (result >= db.characters.Count)
                {
                    result = db.characters.Count - 1;
                }
            }
            else if (num >= 0 && num < db.characters.Count)
            {
                result = num;
            }

            if (result >= 0 && result < db.characters.Count)
            {
                pass = true;
            }

            tic++;
            //Debug.Log("Left Check (" + tic + "): Pass(" + pass + "), Num(" + num + "), Result(" + result + ")" + "\n" + CurrentRow(num) + " | " + sI);
        }

        if (!pass)
        {
            result = current;
        }

        return result;
    }

    public int RightOption(int current)
    {
        int result = -1;

        Vector4 sI = new Vector4(CurrentRow(current), CurrentColumn(current), LargestRow(), LargestColumn());
        int num = current;
        int tic = 0;
        bool pass = false;

        while (!pass && tic < 10)
        {
            num += 1;

            if (CurrentRow(num) != sI.x || num >= db.characters.Count)
            {
                result = (lockedInt * ((int)sI.x - 1));
            }
            else if (num >= 0 && num < db.characters.Count)
            {
                result = num;
            }

            if (result >= 0 && result < db.characters.Count)
            {
                pass = true;
            }

            tic++;
            //Debug.Log("Right Check ("+ tic + "): Pass(" + pass + "), Num(" + num + "), Result(" + result + ")" +"\n" + CurrentRow(num) + " | " + sI);
        }

        if (!pass)
        {
            result = current;
        }

        return result;
    }

    public int LargestRow()
    {
        int result = 0;
        int lInt = 1;

        if (lockedInt > 0)
        {
            lInt = lockedInt;
        }

        result = Mathf.CeilToInt((float)(db.characters.Count) / (float)lInt);

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

        if (db.characters.Count < lInt)
        {
            result = 1;
        }
        else
        {
            result = lInt;
        }

        return result;
    }

    public int CurrentRow(int sel = -1)
    {
        int result = -1;
        bool cancelCurr = false;

        if (sel < 0 || sel >= db.characters.Count)
        {
            cancelCurr = true;
        }

        if (!cancelCurr)
        {
            if (sel >= 0 && sel < db.characters.Count)
            {
                int lInt = 1;
                int sl = sel + 1;

                if (lockedInt > 0)
                {
                    lInt = lockedInt;
                }

                if (lInt >= db.characters.Count)
                {
                    lInt = db.characters.Count - 1;
                }

                result = Mathf.CeilToInt(((float)sl / (float)lInt));

                //Debug.Log(sl + "/" + lInt + " = " + result);

                if (result <= 0)
                {
                    result = 1;
                }
            }
        }

        return result;
    }

    public int CurrentColumn(int sel = -1)
    {
        int result = -1;
        bool cancelCurr = false;

        if (sel < 0 || sel >= db.characters.Count)
        {
            cancelCurr = true;
        }

        if (!cancelCurr)
        {
            if (sel >= 0 && sel < db.characters.Count)
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

                if (lInt >= db.characters.Count)
                {
                    lInt = db.characters.Count;
                }

                result = (sl + 1) % lInt;

                if (result == 0)
                {
                    result = lInt;
                }
            }
        }

        return result;
    }

    public void ChangeGroup(int dir)
    {
        int tss = TrueStageSelected(0) + (stageScreens.Count * dir);

        if (tss >= 0 && tss < curFields.Count)
        {
            groupCount += dir;
        }
    }

    //Add to all Gamesetup Objects that you want to use to change the gamplay info
    public void AddGameplayInfo()
    {
        GetCurrentFieldsWithSkippingInserted();
    }
}
