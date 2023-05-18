using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerConstraints
{
    public GameObject caller;
    public float endTime = 0;

    public PlayerConstraints(GameObject caller,float endTime = -1)
    {
        this.caller = caller;
        this.endTime = endTime;
    }
}

public enum PlayerConstraint
{
    Move,
    Bump,
    Super
}