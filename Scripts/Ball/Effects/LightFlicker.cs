using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    Light li;
    int dir = -1;
    public float speed = 150;

    // Start is called before the first frame update
    void Start()
    {
        li = GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        if(li.intensity > 5000)
        {
            dir = -1;
            li.intensity = 5000;
        }

        if(li.intensity < 50)
        {
            dir = 1;
            li.intensity = 50;
        }

        li.intensity += speed * dir * Time.deltaTime;
    }
}
