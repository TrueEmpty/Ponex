using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AINetwork : MonoBehaviour
{
    #region Inputs
    public int cpuLevel = 1;
    public string character = "";
    public int currentHealth = 0;
    public Vector3 position = Vector3.zero;
    #endregion

    #region Outputs
    public bool up = false;
    public bool down = false;
    public bool left = false;
    public bool right = false;
    #endregion

    IEnumerator PlanActions()
    {

        yield return null;
    }
}


