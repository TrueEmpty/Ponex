using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageDsplay : MonoBehaviour
{
    [SerializeField]
    Image pSelect;

    [SerializeField]
    RawImage portrait;

    public int selection = -1;
    public bool active = false;
    public bool skip = false;
    public Color inactiveColor = Color.black;

    public StageSelector sS;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        active = false;
        int trueIndex = sS.TrueStageSelected(selection);
        bool inSelection = (trueIndex >= 0 && trueIndex < sS.curFields.Count);

        pSelect.gameObject.SetActive(inSelection);
        portrait.gameObject.SetActive(inSelection);

        if (inSelection)
        {
            Field ft = sS.curFields[trueIndex];

            if (ft != null)
            {
                if (ft.active)
                {
                    active = true;
                    portrait.color = Color.white;

                    if (ft.icon != null)
                    {
                        portrait.texture = ft.icon;
                    }
                }
                else
                {
                    portrait.color = inactiveColor;
                }
            }
        }
    }

    void OnClick(int player)
    {
        if(!skip)
        {
            sS.stageSelected = selection;
        }
    }
}
