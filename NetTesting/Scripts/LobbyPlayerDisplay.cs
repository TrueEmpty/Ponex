using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerDisplay : MonoBehaviour
{
    LobbyManager loM;
    public Text position;
    public Text username;
    public int selection = -1;

    // Start is called before the first frame update
    void Start()
    {
        loM = LobbyManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if(selection >= 0 && selection < loM.players.Count)
        {
            LobbyPlayer lp = loM.players[selection];

            string posOut = "P" + lp.position;

            if(lp.position < 0 || lp.position >= 8)
            {
                posOut = "Sp";
            }

            position.text = posOut;

            username.text = lp.username;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
