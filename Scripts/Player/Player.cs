using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGrab))]
public class Player : MonoBehaviour
{
    [SerializeField]
    public Stats player = new Stats();

    public string nickName = "";
    public int team = 0;
    public int position = 1;

    Database db;

    public PlayerButtons keys = new PlayerButtons();

    [SerializeField]
    public List<PlayerConstraints> canMove = new List<PlayerConstraints>();
    [SerializeField]
    public List<PlayerConstraints> canBump = new List<PlayerConstraints>();
    [SerializeField]
    public List<PlayerConstraints> canSuper = new List<PlayerConstraints>();

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        SetKeys();
    }

    // Update is called once per frame
    void Update()
    {
        if(canMove.Count > 0)
        {
            for(int i = canMove.Count -1; i >= 0; i--)
            {
                if(canMove[i].endTime <= Time.time && canMove[i].endTime >= 0)
                {
                    canMove.RemoveAt(i);
                }
            }
        }

        if(canBump.Count > 0)
        {
            for(int i = canBump.Count -1; i >= 0; i--)
            {
                if(canBump[i].endTime <= Time.time && canBump[i].endTime >= 0)
                {
                    canBump.RemoveAt(i);
                }
            }
        }

        if (canSuper.Count > 0)
        {
            for (int i = canSuper.Count - 1; i >= 0; i--)
            {
                if (canSuper[i].endTime <= Time.time && canSuper[i].endTime >= 0)
                {
                    canSuper.RemoveAt(i);
                }
            }
        }
    }

    void SetKeys()
    {
        switch (position)
        {
            case 1:
                keys = db.settings.p1Buttons.RotateKeysBasedOnObjRotation(transform.rotation.eulerAngles);
                break;
            case 2:
                keys = db.settings.p2Buttons.RotateKeysBasedOnObjRotation(transform.rotation.eulerAngles);
                break;
            case 3:
                keys = db.settings.p3Buttons.RotateKeysBasedOnObjRotation(transform.rotation.eulerAngles);
                break;
            case 4:
                keys = db.settings.p4Buttons.RotateKeysBasedOnObjRotation(transform.rotation.eulerAngles);
                break;
            default:
                keys = db.settings.p1Buttons.RotateKeysBasedOnObjRotation(transform.rotation.eulerAngles);
                break;
        }
    }

    public void AddConstraint(GameObject caller,float length = -1, PlayerConstraint constraint = PlayerConstraint.Move)
    {
        switch(constraint)
        {
            case PlayerConstraint.Move:
                canMove.Add(new PlayerConstraints(caller,length >= 0 ? Time.time + length : -1));
                break;
            case PlayerConstraint.Bump:
                canBump.Add(new PlayerConstraints(caller, length >= 0 ? Time.time + length : -1));
                break;
            case PlayerConstraint.Super:
                canSuper.Add(new PlayerConstraints(caller, length >= 0 ? Time.time + length : -1));
                break;
        }
    }

    public void RemoveConstraint(GameObject caller, PlayerConstraint constraint = PlayerConstraint.Move)
    {
        switch(constraint)
        {
            case PlayerConstraint.Move:
                PlayerConstraints pMC = canMove.Find(x => x.caller == caller);

                if(pMC != null)
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

    public bool Direction
    {
        get
        {
            switch (position)
            {
                case 3:
                    return false;
                case 4:
                    return false;
                default:
                    return true;
            }
        }
    }

    public float Speed
    {
        get { return player.Speed; }

        set { player.Speed = value; }
    }

    public float BumpSpeed
    {
        get { return player.bumpSpeed; }

        set { player.bumpSpeed = value; }
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

    public void Damage(int amount)
    {
        player.Health -= amount;

        if(player.Health < 0)
        {
            player.Health = 0;
        }

        if(player.Health > player.maxHealth)
        {
            player.Health = player.maxHealth;
        }
    }

    public void Heal(int amount)
    {
        player.Health += amount;

        if (player.Health < 0)
        {
            player.Health = 0;
        }

        if (player.Health > player.maxHealth)
        {
            player.Health = player.maxHealth;
        }
    }

    public void CostSuper(float amount)
    {
        player.superAmount -= amount;

        if (player.superAmount < 0)
        {
            player.superAmount = 0;
        }

        if (player.superAmount > player.superMax)
        {
            player.superAmount = player.superMax;
        }
    }

    public void GainSuper(float amount)
    {
        player.superAmount += amount;

        if (player.superAmount < 0)
        {
            player.superAmount = 0;
        }

        if (player.superAmount > player.superMax)
        {
            player.superAmount = player.superMax;
        }
    }
}
