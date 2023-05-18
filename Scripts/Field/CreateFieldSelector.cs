using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreateFieldSelector : MonoBehaviour
{
    BuilderSetup builderSetup;

    int index = -1;
    string fname = "";
    string creator = "";

    public Image buttonImage;
    public RawImage icon;

    public void Setup(BuilderSetup bS, int ind,string na,string cre,Texture iconTexture)
    {
        builderSetup = bS;

        index = ind;
        fname = na;
        creator = cre;

        if(iconTexture != null)
        {
            icon.texture = iconTexture;
        }
    }

    private void Update()
    {
        if(builderSetup.fieldSelected == index)
        {
            buttonImage.color = Color.yellow;
        }
        else
        {
            buttonImage.color = Color.white;
        }
    }

    public void Clicked()
    {
        builderSetup.SelectField(index, fname, creator);
    }
}
