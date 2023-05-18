using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollContentLock : MonoBehaviour
{
    RectTransform rt;
    public float minPos = 0;

    // Start is called before the first frame update
    void Start()
    {
        rt = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 pos = rt.anchoredPosition;

        if (pos.y < minPos)
        {
            pos.y = minPos;
        }
        else if(pos.y > rt.sizeDelta.y)
        {
            pos.y = rt.sizeDelta.y;
        }

        rt.anchoredPosition = pos;
    }
}
