using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TypeIdentifier
{
    public string type = "";
    public Color color = Color.black;
    public Color tcolor = Color.black;
    public Texture texture = null;

    public TypeIdentifier(string ty_name)
    {
        type = ty_name;
        color = Random.ColorHSV();
        color.a = 255;
        tcolor = Random.ColorHSV();
        tcolor.a = 255;
    }

    public static implicit operator string(TypeIdentifier a)
    {
        return a.type;
    }

    public static implicit operator Color(TypeIdentifier a)
    {
        return a.color;
    }

    public static implicit operator Texture(TypeIdentifier a)
    {
        return a.texture;
    }
}
