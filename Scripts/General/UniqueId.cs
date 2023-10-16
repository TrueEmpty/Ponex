using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UniqueId
{
    string avalableChars = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    string uid_Value = "";

    public UniqueId()
    {

    }

    public UniqueId(int baseAmount = 10, string preDefine = "")
    {
        uid_Value = CreateUID(baseAmount, preDefine);
    }

    public UniqueId(string uid)
    {
        uid_Value = uid;
    }

    public string Value
    {
        set
        {
            uid_Value = value;
        }
        get
        {
            return uid_Value;
        }
    }

    public string CreateUID(int baseAmount = 10, string preDefine = "")
    {
        string result = preDefine;

        for (int i = 0; i < baseAmount; i++)
        {
            result += avalableChars.Substring(Random.Range(0, avalableChars.Length), 1);
        }

        return result;
    }

    public static implicit operator UniqueId(string a)
    {
        return new UniqueId(a);
    }

    public override string ToString()
    {
        return uid_Value;
    }

    public static implicit operator string(UniqueId a)
    {
        return a.uid_Value;
    }
}
