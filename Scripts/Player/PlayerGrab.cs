using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGrab : MonoBehaviour
{
    public PlayerInfo player;

    private void Start()
    {
        PlayerInfo pl = GetComponent<PlayerInfo>();

        if(pl != null)
        {
            player = pl;
        }
    }
}
