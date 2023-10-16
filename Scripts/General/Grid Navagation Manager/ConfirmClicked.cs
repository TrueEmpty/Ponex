using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfirmClicked : MonoBehaviour
{
    CharacterSelect cS;

    // Use this for initialization
    void Start()
    {
        cS = CharacterSelect.instance;
    }

    public void OnClick(int player)
    {
        cS.PlayerConfirm(player);
    }
}
