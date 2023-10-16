using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Player
{
    public string name = "";
    public UniqueId uid = new UniqueId();

    public string nickName = "";
    public int team = 0;
    public int position = 0;

    public int currentHealth = 10;
    public int maxHealth = 10;

    public float movementSpeed = 50;
    public float pushBack = 0;

    public Buttons buttons;
    public Facing facing = Facing.Up;

    public Skill bump;
    public Skill super;
    public Skill dash;

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

    public GameObject spawnedPlayer;
    public GameObject spawnedLifeline;

    #region Selections
    public bool characterSelected = false;
    public float lastGridUpdate = 0;
    public bool gridLock = false;
    public string state = "";

    #endregion

    [SerializeField]
    public List<PlayerConstraints> canMove = new List<PlayerConstraints>();
    [SerializeField]
    public List<PlayerConstraints> canBump = new List<PlayerConstraints>();
    [SerializeField]
    public List<PlayerConstraints> canSuper = new List<PlayerConstraints>();
    public Player()
    {

    }

    public Player(Player p)
    {
        name = p.name;
        uid = new UniqueId();

        currentHealth = p.currentHealth;
        maxHealth = p.maxHealth;

        movementSpeed = p.movementSpeed;
        pushBack = p.pushBack;

        buttons = new Buttons(p.buttons);
        facing = p.facing;

        bump = new Skill(p.bump);
        super = new Skill(p.super);
        dash = new Skill(p.dash);

        character = new ObjectInfo(p.character);

        lifeline = new ObjectInfo(p.lifeline);
        selector = p.selector;

        superName = p.superName;
        superDescription = p.superDescription;

        portraitColor = p.portraitColor;

        portrait = p.portrait;
        icon = p.icon;

        active = p.active;
        computer = p.computer;

        spawnedPlayer = p.spawnedPlayer;
        spawnedLifeline = p.spawnedLifeline;
    }
    
    public Player(Buttons b)
    {
        uid = new UniqueId();

        buttons = b;
    }

    public void SetUpCharacter(Player p)
    {
        name = p.name;

        currentHealth = p.currentHealth;
        maxHealth = p.maxHealth;

        movementSpeed = p.movementSpeed;
        pushBack = p.pushBack;

        facing = p.facing;

        bump = new Skill(p.bump);
        super = new Skill(p.super);
        dash = new Skill(p.dash);

        character = new ObjectInfo(p.character);

        lifeline = new ObjectInfo(p.lifeline);
        selector = p.selector;

        superName = p.superName;
        superDescription = p.superDescription;

        portraitColor = p.portraitColor;

        portrait = p.portrait;
        icon = p.icon;

        spawnedPlayer = p.spawnedPlayer;
        spawnedLifeline = p.spawnedLifeline;
    }

    public void CreateUID()
    {
        uid = new UniqueId(10, "pl_");
    }

    public void AddConstraint(GameObject caller, float length = -1, PlayerConstraint constraint = PlayerConstraint.Move)
    {
        switch (constraint)
        {
            case PlayerConstraint.Move:
                PlayerConstraints pC_M = canMove.Find(x => x.caller == caller);

                if (pC_M != null)
                {
                    pC_M.endTime = length >= 0 ? Time.time + length : -1;
                }
                else
                {
                    canMove.Add(new PlayerConstraints(caller, length >= 0 ? Time.time + length : -1));
                }
                break;
            case PlayerConstraint.Bump:
                PlayerConstraints pC_B = canBump.Find(x => x.caller == caller);

                if (pC_B != null)
                {
                    pC_B.endTime = length >= 0 ? Time.time + length : -1;
                }
                else
                {
                    canBump.Add(new PlayerConstraints(caller, length >= 0 ? Time.time + length : -1));
                }
                break;
            case PlayerConstraint.Super:
                PlayerConstraints pC_S = canSuper.Find(x => x.caller == caller);

                if (pC_S != null)
                {
                    pC_S.endTime = length >= 0 ? Time.time + length : -1;
                }
                else
                {
                    canSuper.Add(new PlayerConstraints(caller, length >= 0 ? Time.time + length : -1));
                }
                break;
        }
    }

    public void RemoveConstraint(GameObject caller, PlayerConstraint constraint = PlayerConstraint.Move)
    {
        switch (constraint)
        {
            case PlayerConstraint.Move:
                PlayerConstraints pMC = canMove.Find(x => x.caller == caller);

                if (pMC != null)
                {
                    pMC.endTime = 0;
                }
                break;
            case PlayerConstraint.Bump:
                PlayerConstraints pBC = canBump.Find(x => x.caller == caller);

                if (pBC != null)
                {
                    pBC.endTime = 0;
                }
                break;
            case PlayerConstraint.Super:
                PlayerConstraints pSC = canSuper.Find(x => x.caller == caller);

                if (pSC != null)
                {
                    pSC.endTime = 0;
                }
                break;
        }
    }

    public bool CanMove
    {
        get
        {
            return !(canMove.Count > 0);
        }
    }

    public bool CanBump
    {
        get
        {
            return !(canBump.Count > 0);
        }
    }

    public bool CanSuper
    {
        get
        {
            return !(canSuper.Count > 0);
        }
    }

    public bool WithinLastUpdate
    {
        get
        {
            return lastGridUpdate +.1f < Time.time;
        }
    }

    public void Damage(int amount)
    {
        currentHealth -= amount;

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

    public void Heal(int amount)
    {
        Damage(-amount);
    }
}

[System.Serializable]
public class ObjectInfo
{
    public GameObject prefabs;
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;

    public ObjectInfo()
    {

    }

    public ObjectInfo(ObjectInfo oI)
    {
        prefabs = oI.prefabs;
        positionOffset = oI.positionOffset;
        rotationOffset = oI.rotationOffset;
    }

    public ObjectInfo(string fromString)
    {
        FromString(fromString);
    }

    public override string ToString()
    {
        string result = "";

        result += prefabs.name;
        result += "|";

        result += positionOffset.x.ToString();
        result += "|";
        result += positionOffset.y.ToString();
        result += "|";
        result += positionOffset.z.ToString();
        result += "|";

        result += rotationOffset.x.ToString();
        result += "|";
        result += rotationOffset.y.ToString();
        result += "|";
        result += rotationOffset.z.ToString();
        result += "|";

        return result;
    }

    public void FromString(string str)
    {
        int sai = 0;
        string[] sA = str.Split('|');

        if (sA[sai] != null && sA[sai] != "")
        {
            prefabs = (GameObject)Resources.Load("Parts/" + "/" + sA[sai]);
        }
        sai++;

        positionOffset.x = float.Parse(sA[sai]);
        sai++;
        positionOffset.y = float.Parse(sA[sai]);
        sai++;
        positionOffset.z = float.Parse(sA[sai]);
        sai++;

        rotationOffset.x = float.Parse(sA[sai]);
        sai++;
        rotationOffset.y = float.Parse(sA[sai]);
        sai++;
        rotationOffset.z = float.Parse(sA[sai]);
        sai++;
    }
}

[System.Serializable]
public class Skill
{
    public float amount = 0;
    public float max = 20;
    public float cost = 10;
    public float speed = 50;
    public float readyPercent = 1;

    public Skill()
    {

    }

    public Skill(Skill s)
    {
        amount = s.amount;
        max = s.max;
        cost = s.cost;
        speed = s.speed;
        readyPercent = s.readyPercent;
    }

    public void Spend(float lose = float.NaN)
    {
        amount -= (float.IsNaN(lose)) ? cost : lose;
        
        if(amount < 0)
        {
            amount = 0;
        }

        if(amount > max)
        {
            amount = max;
        }
    }

    public void Gain(float gain)
    {
        amount += gain;

        if (amount < 0)
        {
            amount = 0;
        }

        if (amount > max)
        {
            amount = max;
        }
    }

    public void Charge()
    {
        if (amount < max)
        {
            readyPercent += Time.deltaTime;

            if (readyPercent >= speed)
            {
                Gain(1);
                readyPercent = 0;
            }
        }
    }

    public bool Enough()
    {
        return (amount >= cost);
    }
}

[System.Serializable]
public class PlayerConstraints
{
    public GameObject caller;
    public float endTime = 0;

    public PlayerConstraints(GameObject caller, float endTime = -1)
    {
        this.caller = caller;
        this.endTime = endTime;
    }
}

[System.Serializable]
public class PlayerConstraintHolder
{
    public PlayerConstraint constraint;
    public float length = 0;

    public PlayerConstraintHolder(PlayerConstraint theConstraint, float theLength = -1)
    {
        constraint = theConstraint;
        length = theLength;
    }
}

public enum PlayerConstraint
{
    Move,
    Bump,
    Super
}