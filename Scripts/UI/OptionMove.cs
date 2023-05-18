using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionMove : MonoBehaviour
{
    public bool active = true;
    public Database db;
    public OptionSelector oS;
    float minClick = 0;
    float nextClick = .5f;

    // Start is called before the first frame update
    void Start()
    {
        if (db == null)
        {
            db = Database.instance;
        }

        if(oS == null)
        {
            oS = GetComponent<OptionSelector>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(oS != null && db != null && active)
        {
            if (Input.GetKey(db.settings.p1Buttons.bump) || Input.GetKey(db.settings.p2Buttons.super) ||
                Input.GetKey(db.settings.p3Buttons.right) || Input.GetKey(db.settings.p4Buttons.right))
            {
                //Move Up
                if (minClick < Time.time)
                {
                    oS.selection = oS.UpOption();
                    minClick = Time.time + nextClick;
                }
            }

            if (Input.GetKey(db.settings.p1Buttons.super) || Input.GetKey(db.settings.p2Buttons.bump) ||
                Input.GetKey(db.settings.p3Buttons.left) || Input.GetKey(db.settings.p4Buttons.left))
            {
                //Move Down
                if (minClick < Time.time)
                {
                    oS.selection = oS.DownOption();
                    minClick = Time.time + nextClick;
                }
            }

            if (Input.GetKey(db.settings.p1Buttons.left) || Input.GetKey(db.settings.p2Buttons.left) ||
                Input.GetKey(db.settings.p3Buttons.bump) || Input.GetKey(db.settings.p4Buttons.super))
            {
                //Move Left
                if (minClick < Time.time)
                {
                    oS.selection = oS.LeftOption();
                    minClick = Time.time + nextClick;
                }
            }

            if (Input.GetKey(db.settings.p1Buttons.right) || Input.GetKey(db.settings.p2Buttons.right) ||
                Input.GetKey(db.settings.p3Buttons.super) || Input.GetKey(db.settings.p4Buttons.bump))
            {
                //Move Right
                if (minClick < Time.time)
                {
                    oS.selection = oS.RightOption();
                    minClick = Time.time + nextClick;
                }
            }
        }
    }
}
