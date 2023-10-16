using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGrab : MonoBehaviour
{
    Database db;
    public int playerIndex = -1;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
    }

    public Player player
    {
        get 
        {
            Player result = null;

            if(playerIndex >= 0 && playerIndex < db.players.Count)
            {
                result = db.players[playerIndex];
            }

            return result;
        }
    }

    public bool IsLinked()
    {
        return  playerIndex >= 0 && playerIndex < db.players.Count;
    }
}
