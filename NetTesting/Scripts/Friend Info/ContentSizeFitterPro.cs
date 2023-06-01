using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContentSizeFitterPro : MonoBehaviour
{
    RectTransform rT;
    public bool horizontal = true;
    public bool vertical = true;

    // Start is called before the first frame update
    void Start()
    {
        rT = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 size = Vector2.zero;
        Vector2 largeDistance = Vector2.zero;

        if(transform.childCount > 0)
        {
            for(int i = 0; i < transform.childCount; i++)
            {
                RectTransform cRt = transform.GetChild(i).GetComponent<RectTransform>();

                if(cRt != null)
                {
                    if (cRt.gameObject.activeInHierarchy)
                    {
                        if(horizontal)
                        {
                            float hD = Mathf.Abs(cRt.anchoredPosition.x);

                            if(hD > largeDistance.x)
                            {
                                largeDistance.x = hD;
                                size.x = hD + (cRt.sizeDelta.x/2);
                            }
                        }


                        if (vertical)
                        {
                            float vD = Mathf.Abs(cRt.anchoredPosition.y);

                            if (vD > largeDistance.y)
                            {
                                largeDistance.y = vD;
                                size.y= vD + (cRt.sizeDelta.y / 2);
                            }
                        }
                    }
                }
            }
        }

        rT.sizeDelta = new Vector2(horizontal ? size.x : rT.sizeDelta.x, vertical ? size.y : rT.sizeDelta.y);
    }
}
