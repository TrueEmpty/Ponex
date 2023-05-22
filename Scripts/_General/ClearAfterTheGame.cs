using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearAfterTheGame : MonoBehaviour
{
    
    void ClearAllForNewGame()
    {
        Destroy(gameObject);
    }
}
