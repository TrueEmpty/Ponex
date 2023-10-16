using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Part
{
    //Pre Setup
    public string name = "";
    public string type = "";

    public GameObject prefab;

    public List<string> components = new List<string>(); //This will be script#perameters

    //Setup (can change)
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 size;

    public Texture icon;
    public Texture material;
    public Color material_Color = Color.white;
    public float material_Matallic = .72f;
    public float material_Smoothness = .5f;

    public UniqueId uid = new UniqueId();

    //Edit
    public bool scaling = false;
    public bool canMove = true;
    public bool setPos = false;
    public bool canBeBorder = false;

    public GameObject spawned;

    public Part()
    {
    }

    public Part(Part p)
    {
        //Pre Setup
        name = p.name;
        type = p.type;

        prefab = p.prefab;

        components = p.components; //This will be script#perameters

        //Setup (can change)
        position = p.position;
        rotation = p.rotation;
        size = p.size;

        icon = p.icon;
        material = p.material;
        material_Color = p.material_Color;
        material_Matallic = p.material_Matallic;
        material_Smoothness = p.material_Smoothness;

        uid = new UniqueId();

        //Edit
        scaling = p.scaling;
        canMove = p.canMove;
        setPos = p.setPos;
        canBeBorder = p.canBeBorder;

        spawned = p.spawned;
    }

    public void CreateUID()
    {
        uid = new UniqueId(10, "pa_");
    }

    public Part(string fromString)
    {
        FromString(fromString);
    }

    public override string ToString()
    {
        string result = "";

        result += name;
        result += "|";

        result += type;
        result += "|";

        if(prefab != null)
        {
            result += prefab.name;
        }
        result += "|";

        if(components.Count > 0)
        {
            for(int i = 0; i < components.Count; i++)
            {
                if(i > 0)
                {
                    result += "#";
                }

                result += components[i];
            }
        }
        result += "|";

        result += position.x.ToString();
        result += "|";
        result += position.y.ToString();
        result += "|";
        result += position.z.ToString();
        result += "|";

        result += rotation.x.ToString();
        result += "|";
        result += rotation.y.ToString();
        result += "|";
        result += rotation.z.ToString();
        result += "|";

        result += size.x.ToString();
        result += "|";
        result += size.y.ToString();
        result += "|";
        result += size.z.ToString();
        result += "|";
        result += uid;
        result += "|";

        if(icon != null)
        {
            result += icon.name;
        }

        return result;
    }

    public void FromString(string str)
    {
        int sai = 0;
        string[] sA = str.Split('|');

        name = sA[sai];
        sai++;

        type = sA[sai];
        sai++;

        if (sA[sai] != null && sA[sai] != "")
        {
            prefab = (GameObject)Resources.Load("Parts/" + type + "/" + sA[sai]);
        }
        sai++;

        if (sA[sai] != null && sA[sai] != "")
        {
            string[] pA = sA[sai].Split('#');
            if (pA.Length > 0)
            {
                for (int i = 0; i < pA.Length; i++)
                {
                    components.Add(pA[i]);
                }
            }
        }
        sai++;

        position.x = float.Parse(sA[sai]);
        sai++;
        position.y = float.Parse(sA[sai]);
        sai++;
        position.z = float.Parse(sA[sai]);
        sai++;

        rotation.x = float.Parse(sA[sai]);
        sai++;
        rotation.y = float.Parse(sA[sai]);
        sai++;
        rotation.z = float.Parse(sA[sai]);
        sai++;

        size.x = float.Parse(sA[sai]);
        sai++;
        size.y = float.Parse(sA[sai]);
        sai++;
        size.z = float.Parse(sA[sai]);
        sai++;
        if (sA[sai] != null && sA[sai] != "")
        {
            uid = sA[sai];
        }
        sai++;
        if (sA[sai] != null && sA[sai] != "")
        {
            icon = (Texture)Resources.Load("Icons/Parts/" + sA[sai]);

            if (icon == null)
            {
                //icon = Database.instance.default_Texture;
            }
        }
        else
        {
            //icon = Database.instance.default_Texture;
        }
        sai++;
    }
}
