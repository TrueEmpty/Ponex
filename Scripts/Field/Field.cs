using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Field
{
    public string name = "";
    public string arthur = "";
    public UniqueId uid = new UniqueId();

    public int size = 10; //Should be a range from 0-100

    public List<Part> parts = new List<Part>();

    public Texture portrait;
    public Texture icon;
    public bool active = true;

    public Texture backgroundMaterial;
    public Color backgroundColor = Color.black;
    public float backgroundMatallic = .91f;
    public float backgroundSmoothness = .74f;
    public Vector2 tilling = new Vector2(1, 1);

    public Field()
    {

    }

    public Field(bool setActive)
    {
        active = setActive;
    }

    public void CreateUID()
    {
        uid = new UniqueId(10, "fe_");
    }

    public Field(Field field)
    {
        name = field.name;
        arthur = field.arthur;
        uid = field.uid;

        size = field.size; //smallest should be 10

        parts = new List<Part>();
        for (int i = 0; i < field.parts.Count; i++)
        {
            parts.Add(field.parts[i]);
        }

        portrait = field.portrait;
        icon = field.icon;
        active = field.active;
    }

    public Field(string fromString)
    {
        //FromString(fromString);
    }

    public override string ToString()
    {
        string result = "";

        result += name;
        result += "|";

        result += arthur;
        result += "|";

        result += uid;
        result += "|";

        result += size.ToString();
        result += "|";

        if (parts.Count > 0)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                result += parts[i].ToString().Replace('|', ']');

                if (i < parts.Count - 1)
                {
                    result += ",";
                }
            }
        }
        result += "|";

        if (portrait != null)
        {
            result += portrait.name;
        }
        result += "|";

        if (icon != null)
        {
            result += icon.name;
        }
        result += "|";

        result += active.ToString();

        return result;
    }

    /*public void FromString(string str)
    {
        int sai = 0;
        string[] sA = str.Split('|');

        name = sA[sai];
        sai++;

        arthur = sA[sai];
        sai++;

        uid = sA[sai];
        sai++;

        size = int.Parse(sA[sai]);
        sai++;

        if (sA[sai] != null && sA[sai] != "")
        {
            string[] pA = sA[sai].Split(',');

            if (pA.Length > 0)
            {
                for (int i = 0; i < pA.Length; i++)
                {
                    string pSp = pA[i].Replace(']', '|');
                    parts.Add(new Part(pSp));
                }
            }
        }
        sai++;

        if (sA[sai] != null && sA[sai] != "")
        {
            portrait = (Texture)Resources.Load("Portraits/Fields/" + sA[sai]);

            if (portrait == null)
            {
                portrait = Database.instance.default_Texture;
            }
        }
        else
        {
            portrait = Database.instance.default_Texture;
        }
        sai++;

        if (sA[sai] != null && sA[sai] != "")
        {
            icon = (Texture)Resources.Load("Icons/Fields/" + sA[sai]);

            if (icon == null)
            {
                icon = Database.instance.default_Texture;
            }
        }
        else
        {
            icon = Database.instance.default_Texture;
        }
        sai++;

        active = bool.Parse(sA[sai]);
        sai++;
    }*/

    public List<Part> GetSpawns(bool includeBall = true, bool includeCharacters = true)
    {
        List<Part> result = new List<Part>();

        if (parts.Count > 0)
        {
            if (includeBall && includeCharacters)
            {
                result = parts.FindAll(x => x.type.ToLower() == "spawn");
            }
            else if (includeBall)
            {
                result = parts.FindAll(x => x.type.ToLower() == "spawn" && x.name.ToLower() == "ball");
            }
            else if (includeCharacters)
            {
                result = parts.FindAll(x => x.type.ToLower() == "spawn" && x.name.ToLower() != "ball");
            }
        }

        return result;
    }

    public int MaxPlayers()
    {
        int result = 0;

        if (parts.Count > 0)
        {
            List<Part> fp = GetSpawns(false);
            result = fp.Count;
        }

        return result;
    }
}

