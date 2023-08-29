using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGrab))]
public class PlayerInfo : MonoBehaviour
{
    public Player player;
    Database db;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        SetKeys();
    }

    // Update is called once per frame
    void Update()
    {
        if (player.canMove.Count > 0)
        {
            for (int i = player.canMove.Count - 1; i >= 0; i--)
            {
                if (player.canMove[i].endTime <= Time.time && player.canMove[i].endTime >= 0)
                {
                    player.canMove.RemoveAt(i);
                }
            }
        }

        if (player.canBump.Count > 0)
        {
            for (int i = player.canBump.Count - 1; i >= 0; i--)
            {
                if (player.canBump[i].endTime <= Time.time && player.canBump[i].endTime >= 0)
                {
                    player.canBump.RemoveAt(i);
                }
            }
        }

        if (player.canSuper.Count > 0)
        {
            for (int i = player.canSuper.Count - 1; i >= 0; i--)
            {
                if (player.canSuper[i].endTime <= Time.time && player.canSuper[i].endTime >= 0)
                {
                    player.canSuper.RemoveAt(i);
                }
            }
        }
    }

    void SetKeys()
    {
        switch (player.position)
        {
            case 1:
                player.keys = db.settings.p1Buttons.RotateKeysBasedOnObjRotation(transform.rotation.eulerAngles);
                break;
            case 2:
                player.keys = db.settings.p2Buttons.RotateKeysBasedOnObjRotation(transform.rotation.eulerAngles);
                break;
            case 3:
                player.keys = db.settings.p3Buttons.RotateKeysBasedOnObjRotation(transform.rotation.eulerAngles);
                break;
            case 4:
                player.keys = db.settings.p4Buttons.RotateKeysBasedOnObjRotation(transform.rotation.eulerAngles);
                break;
            default:
                player.keys = db.settings.p1Buttons.RotateKeysBasedOnObjRotation(transform.rotation.eulerAngles);
                break;
        }
    }

    public void AddConstraint(GameObject caller, float length = -1, PlayerConstraint constraint = PlayerConstraint.Move)
    {
        switch (constraint)
        {
            case PlayerConstraint.Move:
                PlayerConstraints pC_M = player.canMove.Find(x => x.caller == caller);

                if(pC_M != null)
                {
                    pC_M.endTime = length >= 0 ? Time.time + length : -1;
                }
                else
                {
                    player.canMove.Add(new PlayerConstraints(caller, length >= 0 ? Time.time + length : -1));
                }
                break;
            case PlayerConstraint.Bump:
                PlayerConstraints pC_B = player.canBump.Find(x => x.caller == caller);

                if (pC_B != null)
                {
                    pC_B.endTime = length >= 0 ? Time.time + length : -1;
                }
                else
                {
                    player.canBump.Add(new PlayerConstraints(caller, length >= 0 ? Time.time + length : -1));
                }
                break;
            case PlayerConstraint.Super:
                PlayerConstraints pC_S = player.canSuper.Find(x => x.caller == caller);

                if (pC_S != null)
                {
                    pC_S.endTime = length >= 0 ? Time.time + length : -1;
                }
                else
                {
                    player.canSuper.Add(new PlayerConstraints(caller, length >= 0 ? Time.time + length : -1));
                }
                break;
        }
    }

    public void RemoveConstraint(GameObject caller, PlayerConstraint constraint = PlayerConstraint.Move)
    {
        switch (constraint)
        {
            case PlayerConstraint.Move:
                PlayerConstraints pMC = player.canMove.Find(x => x.caller == caller);

                if (pMC != null)
                {
                    pMC.endTime = 0;
                }
                break;
            case PlayerConstraint.Bump:
                PlayerConstraints pBC = player.canBump.Find(x => x.caller == caller);

                if (pBC != null)
                {
                    pBC.endTime = 0;
                }
                break;
            case PlayerConstraint.Super:
                PlayerConstraints pSC = player.canSuper.Find(x => x.caller == caller);

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
            switch (player.position)
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
        get { return player.player.Speed; }

        set { player.player.Speed = value; }
    }

    public float BumpSpeed
    {
        get { return player.player.bumpSpeed; }

        set { player.player.bumpSpeed = value; }
    }

    public bool CanMove
    {
        get
        {
            return !(player.canMove.Count > 0);
        }
    }

    public bool CanBump
    {
        get
        {
            return !(player.canBump.Count > 0);
        }
    }

    public bool CanSuper
    {
        get
        {
            return !(player.canSuper.Count > 0);
        }
    }

    public void Damage(int amount)
    {
        player.player.Health -= amount;

        if (player.player.Health < 0)
        {
            player.player.Health = 0;
        }

        if (player.player.Health > player.player.maxHealth)
        {
            player.player.Health = player.player.maxHealth;
        }
    }

    public void Heal(int amount)
    {
        player.player.Health += amount;

        if (player.player.Health < 0)
        {
            player.player.Health = 0;
        }

        if (player.player.Health > player.player.maxHealth)
        {
            player.player.Health = player.player.maxHealth;
        }
    }

    public void CostSuper(float amount)
    {
        player.player.superAmount -= amount;

        if (player.player.superAmount < 0)
        {
            player.player.superAmount = 0;
        }

        if (player.player.superAmount > player.player.superMax)
        {
            player.player.superAmount = player.player.superMax;
        }
    }

    public void GainSuper(float amount)
    {
        player.player.superAmount += amount;

        if (player.player.superAmount < 0)
        {
            player.player.superAmount = 0;
        }

        if (player.player.superAmount > player.player.superMax)
        {
            player.player.superAmount = player.player.superMax;
        }
    }
}
