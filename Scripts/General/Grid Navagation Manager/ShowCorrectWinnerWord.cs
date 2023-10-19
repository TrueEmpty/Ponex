using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowCorrectWinnerWord : MonoBehaviour
{
    Database db;
    public GameObject single;
    public GameObject multi;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
    }

    // Update is called once per frame
    void Update()
    {
        int count = 0;
        
        if(db.players.Count > 0)
        {
            count = db.players.FindAll(x => x.won).Count;
        }

        if(count > 1)
        {
            multi.SetActive(true);
            single.SetActive(false);
        }
        else
        {
            single.SetActive(true);
            multi.SetActive(false);
        }
    }
}
