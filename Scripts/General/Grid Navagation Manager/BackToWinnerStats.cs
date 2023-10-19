using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackToWinnerStats : MonoBehaviour
{
    Database db;
    FieldSelect fS;
    GridControl gC;
    MenuManager mm;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        fS = FieldSelect.instance;
        mm = MenuManager.instance;
        gC = GetComponent<GridControl>();
    }

    public void OnClick(int player)
    {

    }

    public void OnCancel(int player)
    {
        if(player >= 0 && player < db.players.Count)
        {
            Player p = db.players[player];

            p.state = "";
        }
    }
}
