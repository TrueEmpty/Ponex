using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateStateOnLoop : MonoBehaviour
{
    Database db;
    public string upState = "";
    public string downState = "";
    public string leftState = "";
    public string rightState = "";

    // Use this for initialization
    void Start()
    {
        db = Database.instance;
    }

    public void OnLoop(Looped looped)
    {
        if (looped.pA.p >= 0 && looped.pA.p < db.players.Count)
        {
            Player p = db.players[looped.pA.p];

            switch(looped.direction)
            {
                case LoopDirection.Up:
                    if(upState != "")
                    {
                        p.state = upState;
                    }
                    break;
                case LoopDirection.Down:
                    if (downState != "")
                    {
                        p.state = downState;
                    }
                    break;
                case LoopDirection.Left:
                    if (leftState != "")
                    {
                        p.state = leftState;
                    }
                    break;
                case LoopDirection.Right:
                    if (rightState != "")
                    {
                        p.state = rightState;
                    }
                    break;
            }
        }
    }
}
