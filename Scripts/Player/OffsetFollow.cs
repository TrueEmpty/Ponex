using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OffsetFollow : MonoBehaviour
{
    public Transform parent;
    //public GameObject sOC;
    public float unGap = 3;
    public float delay = 1;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if(parent != null)
        {
            Vector3 offPos = parent.position - parent.forward * ((parent.localScale.x / unGap) + (transform.localScale.x / unGap));
            transform.position = Vector3.LerpUnclamped(transform.position, offPos, delay);

            transform.LookAt(parent.position);        
        }
    }
}
