using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Loading : MonoBehaviour
{
    public Text loadingText;

    int phase = 0;
    int maxPhase = 3;
    float nextPhase = 0;
    float phaseTime = .5f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(nextPhase < Time.time)
        {
            phase++;

            if(phase > maxPhase)
            {
                phase = 0;
            }

            string dots = "";

            for(int i = 0; i < maxPhase; i++)
            {
                if(i <= phase)
                {
                    dots += ".";
                }
                else
                {
                    dots += " ";
                }
            }

            loadingText.text = "Loading" + dots;

            nextPhase = Time.time + phaseTime;
        }
    }
}
