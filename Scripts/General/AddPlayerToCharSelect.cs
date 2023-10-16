using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddPlayerToCharSelect : MonoBehaviour
{
    Database db;
    public int minPlayers = 1;
    public int maxPlayers = 1;
    public string state = "";

    public bool levelSelect = false;
    public bool positionSelect = false;
    public bool teamSelect = false;
    public bool ballSelect = false;

    // Use this for initialization
    void Start()
    {
        db = Database.instance;
    }

    public void OnClick(int player)
    {
        db.minPlayers = minPlayers;
        db.maxPlayers = maxPlayers;

        db.levelSelect = levelSelect;
        db.positionSelect = positionSelect;
        db.teamSelect = teamSelect;
        db.ballSelect = ballSelect;

        for(int i = 0; i < db.players.Count; i++)
        {
            db.players[i].state = state;
        }
    }
}
