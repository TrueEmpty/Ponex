using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuControls : MonoBehaviour
{
    List<Button> options;

    // Start is called before the first frame update
    void Start()
    {
        if(transform.childCount > 0)
        {
            for(int i = 0; i < transform.childCount; i++)
            {
                Button b = transform.GetChild(i).GetComponent<Button>();

                if(b != null)
                {
                    options.Add(b);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
