using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGrab : MonoBehaviour
{
    public Player player;

    private void Start()
    {
        Player pl = GetComponent<Player>();

        if(pl != null)
        {
            player = pl;
        }
    }
}
