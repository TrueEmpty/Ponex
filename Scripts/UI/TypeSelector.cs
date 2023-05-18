using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TypeSelector : MonoBehaviour
{
    public Button button;
    public Text text;
    string actualtype;

    public FieldCreator fc = null;

    public void SetupType(FieldCreator caller,string type_Name,Color color)
    {
        fc = caller;
        text.text = type_Name;
        button.image.color = color;
        actualtype = type_Name;
    }

    public void SetupType(FieldCreator caller,string type_Name,Color color, Color tcolor)
    {
        fc = caller;
        text.text = type_Name == "" ? "Back" : type_Name;
        text.color = tcolor;
        Shadow tsh = text.gameObject.GetComponent<Shadow>();

        if(tsh != null)
        {
            tsh.effectColor = ReverseColor(tcolor);
        }

        button.image.color = color;
        actualtype = type_Name;
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
        if(fc != null)
        {
            fc.SetType(actualtype);
        }
    }
}
