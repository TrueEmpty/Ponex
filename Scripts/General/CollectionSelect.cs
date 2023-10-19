using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectionSelect : MonoBehaviour
{
    public static CollectionSelect instance;
    MenuManager mm;
    Database db;

    public string showing = "People";

    public GridControl fS;
    public GridControl bS;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        mm = MenuManager.instance;
        db = Database.instance;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddPlayer(int player)
    {
        fS.playersInControl.Add(player);
        db.players[player].state = "Collection";
    }

    public void Leave()
    {
        bS.playersInControl.Clear();
        mm.BackMenu();
    }
}
