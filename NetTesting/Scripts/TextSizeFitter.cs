using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Text))]
public class TextSizeFitter : MonoBehaviour
{
    RectTransform rt;
    Text text;

    // Start is called before the first frame update
    void Start()
    {
        rt = GetComponent<RectTransform>();
        text = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
