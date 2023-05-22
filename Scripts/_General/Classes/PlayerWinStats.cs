using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWinStats
{
    [SerializeField]
    public Stats player = new Stats();

    public string nickName = "";
    public int team = 0;
    public int position = 1;

    public PlayerWinStats()
    {

    }

    public PlayerWinStats(Player p)
    {
        player = new Stats(p.player);
        nickName = p.nickName;
        team = p.team;
        position = p.position;
    }
}
