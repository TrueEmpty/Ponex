using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantSpin : MonoBehaviour
{
    Database db;
    public float spinSpeed = 10;
    public Vector3 spinAxis = new Vector3(0,0,1);

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if(db.gameStart)
        {
            Vector3 curRot = transform.rotation.eulerAngles;
            Vector3 newRot = curRot + spinAxis * spinSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(newRot);
        }
    }
}
