using System;
using UnityEngine;
using UnityEngine.UI;

public class StoryButton : MonoBehaviour
{
    public Action<int> clicked;
    Image image;
    Color baseColor = Color.white;

    void Awake()
    {
        image = GetComponent<Image>();
        if (image != null)
            baseColor = image.color;
        SelectorClickable.Ensure(gameObject, 160f);
    }

    public void SetBaseColor(Color color)
    {
        baseColor = color;
        if (image != null)
            image.color = color;
    }

    public void OnClick(int player)
    {
        AudioSettings.PlaySelection();
        if (clicked != null)
            clicked(player);
    }

    public void OnHighlighted(int player)
    {
        AudioSettings.PlaySelected();
        if (image != null)
            image.color = Color.Lerp(baseColor, Color.white, 0.35f);
    }

    public void OnUnHighlighted(int player)
    {
        if (image != null)
            image.color = baseColor;
    }
}
