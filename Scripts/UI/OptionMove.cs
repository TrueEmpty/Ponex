using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionMove : MonoBehaviour
{
    public bool active = true;
    public Database db;
    public OptionSelector oS;

    // Start is called before the first frame update
    void Start()
    {
        if (db == null)
        {
            db = Database.instance;
        }
        db.SubscribeToSelectors(gameObject);

        if (oS == null)
        {
            oS = GetComponent<OptionSelector>();
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}
