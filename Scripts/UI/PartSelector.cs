using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartSelector : MonoBehaviour
{
    public Part part = null;
    public FieldCreator fc = null;

    public Button button;
    public Text text;
    public RawImage image;

    public void Setup(FieldCreator caller, Part part_info, Color color, Color tcolor,string name_override = null)
    {
        fc = caller;
        part = part_info;
        text.text = name_override == null ? part.name : name_override;
        text.color = tcolor;
        Shadow tsh = text.gameObject.GetComponent<Shadow>();

        if (tsh != null)
        {
            tsh.effectColor = ReverseColor(tcolor);
        }

        button.image.color = color;

        if(part.icon != null)
        {
            image.texture = part.icon;
        }
        else
        {
            image.color = Color.clear;
        }
    }

    Color ReverseColor(Color c)
    {
        Color result = Color.white;

        result -= c;
        result.a = 255;

        return result;
    }

    public void Clicked()
    {
        if (fc != null && part != null)
        {
            fc.SpawnPart(part);
        }
    }
}
