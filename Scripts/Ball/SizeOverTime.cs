using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SizeOverTime : MonoBehaviour
{
    public float growthRate = .1f;
    public Vector2 growthRange = new Vector2(.1f,.4f);

    // Update is called once per frame
    void Update()
    {
        if(transform.localScale.x >= growthRange.x &&
            transform.localScale.x <= growthRange.y)
        {
            transform.localScale += Vector3.one * (growthRate * Time.deltaTime);
        }
    }
}
