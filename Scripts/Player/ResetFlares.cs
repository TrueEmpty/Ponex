using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetFlares : MonoBehaviour
{
    public List<string> targetTags = new List<string>();

    public Celarus script;

    private void OnCollisionEnter(Collision collision)
    {
        if (targetTags.Exists(x=> x.ToLower().Trim() == collision.transform.tag))
        {
            if(script != null)
            {
                script.sunLeft = 0;
                script.sunMiddle = 0;
                script.sunRight = 0;
            }

            enabled = false;
        }
    }
}
