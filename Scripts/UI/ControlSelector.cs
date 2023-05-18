using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ControlSelector : MonoBehaviour
{
    public OptionSelector oS;

    // Update is called once per frame
    void Update()
    {
        if(oS != null)
        {
            if(EventSystem.current.currentSelectedGameObject != gameObject && oS.CurrentOption() == gameObject)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }
    }
}
