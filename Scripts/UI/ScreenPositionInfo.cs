using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenPositionInfo : MonoBehaviour
{
    Camera cam;
    public float screenWidth = 0;
    public float screenHeight = 0;

    public float currentWidth = 0;
    public float currentHeight = 0;

    public float percentWidth = 0;
    public float percentHeight = 0;

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        screenWidth = Screen.width;
        screenHeight = Screen.height;

        Vector3 sP = cam.WorldToScreenPoint(transform.position);

        currentWidth = sP.x;
        currentHeight = sP.y;

        percentWidth = currentWidth / screenWidth;
        percentHeight = currentHeight / screenHeight;
    }
}
