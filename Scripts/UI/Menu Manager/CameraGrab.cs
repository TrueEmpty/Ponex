using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraGrab : MonoBehaviour
{
    Canvas canvas;

    // Start is called before the first frame update
    void Start()
    {
        canvas = GetComponent<Canvas>();
    }

    // Update is called once per frame
    void Update()
    {
        if(canvas != null)
        {
            if(canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }
        }
    }
}
