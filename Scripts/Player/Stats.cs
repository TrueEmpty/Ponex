using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Stats
{
    public string name = "";

    public int currentHealth = 10;

    public int maxHealth = 10;

    public float movementSpeed = 50;

    public float bumpSpeed = 50;
    public float bumpReadyPercent = 1;

    public float superAmount = 0;    
    public float superMax = 20;
    public float superCost = 10;
    public float superReadyPercent = 1;

    public int dashAmount = 2;    
    public float dashCooldown = 3;    
    public int dashMax = 2;
    public float dashReadyPercent = 1;

    public ObjectInfo character = null;

    public ObjectInfo lifeline = null;
    public GameObject selector = null;

    public string superName;
    public string superDescription;

    public Color portraitColor = Color.cyan;

    public Texture portrait;
    public Texture icon;

    public bool active = false;
    public bool computer = false;

    public Stats()
    {
        
    }

    public Stats(string fromString)
    {
        FromString(fromString);
    }

    public Stats(Stats stat)
    {
        name = stat.name;

        currentHealth = stat.currentHealth;
        maxHealth = stat.maxHealth;

        movementSpeed = stat.movementSpeed;

        bumpSpeed = stat.bumpSpeed;
        bumpReadyPercent = stat.bumpReadyPercent;

        superAmount = stat.superAmount;
        superMax = stat.superMax;
        superCost = stat.superCost;
        superReadyPercent = stat.superReadyPercent;

        dashAmount = stat.dashAmount;
        dashCooldown = stat.dashCooldown;
        dashMax = stat.dashMax;
        dashReadyPercent = stat.dashReadyPercent;

        character = stat.character;
        lifeline = stat.lifeline;

        superName = stat.superName;
        superDescription = stat.superDescription;

        portraitColor = stat.portraitColor;

        portrait = stat.portrait;
        icon = stat.icon;

        active = stat.active;
        computer = stat.computer;
    }

    public override string ToString()
    {
        string result = "";

        result += name;
        result += "|";

        result += currentHealth.ToString();
        result += "|";

        result += maxHealth.ToString();
        result += "|";

        result += movementSpeed.ToString();
        result += "|";

        result += bumpSpeed.ToString();
        result += "|";

        result += superAmount.ToString();
        result += "|";
        result += superMax.ToString();
        result += "|";
        result += superCost.ToString();
        result += "|";

        result += dashAmount.ToString();
        result += "|";
        result += dashCooldown.ToString();
        result += "|";
        result += dashMax.ToString();
        result += "|";

        if (character != null)
        {
            result += character.ToString();
        }
        result += "|";

        if (lifeline != null)
        {
            result += lifeline.ToString();
        }
        result += "|";

        result += superName;
        result += "|";

        result += superDescription;
        result += "|";

        result += portraitColor.r.ToString();
        result += "|";
        result += portraitColor.g.ToString();
        result += "|";
        result += portraitColor.b.ToString();
        result += "|";
        result += portraitColor.a.ToString();
        result += "|";

        result += portrait.name;
        result += "|";

        result += icon.name;
        result += "|";

        result += active.ToString();

        return result;
    }

    public void FromString(string str)
    {
        int sai = 0;
        string[] sA = str.Split('|');

        name = sA[sai];
        sai++;

        currentHealth = int.Parse(sA[sai]);
        sai++;

        maxHealth = int.Parse(sA[sai]);
        sai++;

        movementSpeed = float.Parse(sA[sai]);
        sai++;

        bumpSpeed = float.Parse(sA[sai]);
        sai++;

        superAmount = float.Parse(sA[sai]);
        sai++;
        superMax = float.Parse(sA[sai]);
        sai++;
        superCost = float.Parse(sA[sai]);
        sai++;

        dashAmount = int.Parse(sA[sai]);
        sai++;
        dashCooldown = float.Parse(sA[sai]);
        sai++;
        dashMax = int.Parse(sA[sai]);
        sai++;

        string[] pA = sA[sai].Split(']');
        for (int i = 0; i < pA.Length; i++)
        {
            character = new ObjectInfo(pA[i]);
        }
        sai++;

        pA = sA[sai].Split(']');
        for (int i = 0; i < pA.Length; i++)
        {
            lifeline = new ObjectInfo(pA[i]);
        }
        sai++;

        superName = sA[sai];
        sai++;

        superDescription = sA[sai];
        sai++;

        Color pC = Color.white;
        pC.r = float.Parse(sA[sai]);
        sai++;
        pC.g = float.Parse(sA[sai]);
        sai++;
        pC.b = float.Parse(sA[sai]);
        sai++;
        pC.a = float.Parse(sA[sai]);
        sai++;
        portraitColor = pC;


        if (sA[sai] != null && sA[sai] != "")
        {
            portrait = (Texture)Resources.Load("Portraits/" + sA[sai]);
        }
        sai++;

        if (sA[sai] != null && sA[sai] != "")
        {
            icon = (Texture)Resources.Load("Icons/" + sA[sai]);
        }
        sai++;

        active = bool.Parse(sA[sai]);
        sai++;
    }

    public string Name
    {
        get {return name;}
    }

    public int Health
    {
        get { return currentHealth; }
        set { currentHealth = value; }
    }

    public int MaxHealth
    {
        get { return maxHealth; }
        set { maxHealth = value; }
    }

    public float Speed
    {
        get { return movementSpeed; }
        set { movementSpeed = value; }
    }

    public float Bump
    {
        get { return bumpSpeed; }
        set { bumpSpeed = value; }
    }

    public ObjectInfo Prefabs
    {
        get
        {return character; }
    }
}
