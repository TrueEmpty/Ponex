using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WinnerDisplay : MonoBehaviour
{
    public RawImage rI;
    public Text playerName;

    public void Setup(PlayerWinStats player)
    {
        rI.texture = player.player.icon;

        string nameToUse = player.nickName;

        if (nameToUse == null || nameToUse == "")
        {
            nameToUse = "P" + player.position;
        }

        playerName.text = nameToUse;
    }
}
