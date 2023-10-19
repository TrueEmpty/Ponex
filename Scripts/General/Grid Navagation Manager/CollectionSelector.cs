using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectionSelector : MonoBehaviour
{
    GridControl gC;
    CollectionSelect cS;
    Database db;

    // Start is called before the first frame update
    void Start()
    {
        cS = CollectionSelect.instance;
        gC = GetComponent<GridControl>();
        db = Database.instance;
    }

    public void OnClick(int player)
    {

        //gC.playersInControl.Clear();
        //Send to Collection Select to open Menu and add player to the Grid

    }

    public void OnCancel(int player)
    {
        cS.bS.playersInControl.Add(player);
        db.players[player].state = "Collection";
        gC.playersInControl.Clear();
    }
}
