using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared visual setup for character and field selection tiles.</summary>
public static class SelectorTileVisual
{
    public static readonly Color RandomBackground = new Color(0.4f, 0.4f, 0.4f, 1f);
    public static readonly Color RandomText = new Color(0.72f, 0.72f, 0.72f, 1f);
    public static readonly Color RandomOutline = new Color(0.55f, 0.55f, 0.55f, 1f);

    public static void ApplyRandom(
        Image background,
        Outline outline,
        RawImage icon,
        Text nameText,
        ButtonInteraction interaction)
    {
        if (background != null)
            background.color = RandomBackground;
        if (outline != null)
            outline.effectColor = RandomOutline;

        if (icon != null)
        {
            icon.texture = null;
            icon.enabled = false;
        }

        if (nameText != null)
        {
            nameText.text = "?";
            nameText.color = RandomText;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 20;
            nameText.resizeTextMaxSize = 72;

            RectTransform rect = nameText.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        if (interaction != null)
            interaction.Activate();
    }

    public static void ApplyEntry(
        Image background,
        Color backgroundColor,
        RawImage icon,
        Texture texture,
        bool active,
        Text nameText,
        string displayName,
        ButtonInteraction interaction)
    {
        if (background != null)
            background.color = backgroundColor;

        if (icon != null)
        {
            icon.enabled = true;
            icon.texture = texture;
            icon.color = active ? Color.white : Color.black;
        }

        if (nameText != null)
        {
            nameText.text = displayName;
            nameText.color = Color.white;
        }

        if (interaction == null)
            return;
        if (active)
            interaction.Activate();
        else
            interaction.Deactivate();
    }
}
