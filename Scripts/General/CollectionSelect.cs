using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectionSelect : MonoBehaviour
{
    public static CollectionSelect instance;
    public string showing = "People";

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
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
