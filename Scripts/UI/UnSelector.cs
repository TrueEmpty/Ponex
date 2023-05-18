using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnSelector : MonoBehaviour
{
    public EventSystem eS;

    // Update is called once per frame
    void Update()
    {
        if (eS.currentSelectedGameObject != null)
        {
            if (eS.currentSelectedGameObject == gameObject)
            {
                eS.SetSelectedGameObject(null);
            }
        }
    }
}
