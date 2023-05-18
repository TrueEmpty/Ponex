using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionHighlight : MonoBehaviour
{
    public GameObject selectedUI;

    public List<string> names = new List<string>();
    public List<string> tags = new List<string>();

    private void OnTriggerEnter(Collider other)
    {
        if(names.Exists(x=> x.ToLower().Trim() == other.gameObject.name.ToLower().Trim()) || names.Count <= 0)
        {
            if (tags.Exists(x => x.ToLower().Trim() == other.gameObject.tag.ToLower().Trim()) || tags.Count <= 0)
            {
                if(selectedUI != other.gameObject)
                {
                    selectedUI = other.gameObject;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (names.Exists(x => x.ToLower().Trim() == other.gameObject.name.ToLower().Trim()) || names.Count <= 0)
        {
            if (tags.Exists(x => x.ToLower().Trim() == other.gameObject.tag.ToLower().Trim()) || tags.Count <= 0)
            {
                if (selectedUI == other.gameObject)
                {
                    selectedUI = null;
                }
            }
        }
    }
}
