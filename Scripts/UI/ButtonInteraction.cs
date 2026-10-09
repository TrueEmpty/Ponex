using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonInteraction : MonoBehaviour
{
    Database db;
    [SerializeField]
    List<int> highlighted = new List<int> ();
    bool pressed = true;
    bool deactivated = false;

    Image background;
    Outline outline;

    [Tooltip("Will fade the background color for highlight, pressed, etc..")]
    public bool fade = false;
    public float fadeAmount = .5f;

    public Color highlightColor = Color.blue;
    public Color pressedColor = Color.white;
    public Color deactivatedColor = Color.white;
    public Color deactivatedColor_outline = Color.white;


    Color base_background;
    Color base_outline;

    void Start()
    {
        db = Database.instance;
        background = GetComponent<Image>();
        outline = GetComponent<Outline>();

        if(background != null)
        {
            base_background = background.color;
        }

        if(outline != null)
        {
            base_outline = outline.effectColor;
        }
    }

    void LateUpdate()
    {
        Color targetBackground = base_background;
        Color targetOutline = base_outline;

        if (deactivated)
        {
            targetBackground = deactivatedColor;
            targetOutline = deactivatedColor_outline;
        }
        else if (pressed)
        {
            targetBackground = Color.Lerp(base_background, pressedColor, fadeAmount);
            targetOutline = HighlightOutlineColor();
        }
        else if (highlighted.Count > 0)
        {
            targetBackground = Color.Lerp(base_background, highlightColor, fadeAmount);
            targetOutline = HighlightOutlineColor();
        }

        // Avoid dirtying the Canvas every frame when the visual state did not change.
        if (background != null && background.color != targetBackground)
            background.color = targetBackground;
        if (outline != null && outline.effectColor != targetOutline)
            outline.effectColor = targetOutline;

        pressed = false;
    }

    Color HighlightOutlineColor()
    {
        Color c = Color.white;

        if (db != null && db.playerColors != null)
        {
            foreach (int i in highlighted)
            {
                if (i >= 0 && i < db.playerColors.Count)
                {
                    c = Color.Lerp(c, db.playerColors[i].color, .5f);
                }
            }
        }
        return c;
    }

    public void Pressed()
    {
        if (deactivated) return;

        pressed = true;
    }

    public void Activate()
    {
        deactivated = false;
    }

    public void Deactivate()
    {
        deactivated = true;
    }

    public void OnHighlighted(int player)
    {
        if (deactivated) return;

        if (!highlighted.Contains(player))
        {
            highlighted.Add(player);
        }
    }

    public void OnUnHighlighted(int player)
    {
        if (deactivated) return;

        if (highlighted.Contains(player))
        {
            highlighted.Remove(player);
        }
    }

    /// <summary>Update the colors LateUpdate restores to when idle/highlighted.</summary>
    public void SetBaseColors(Color backgroundColor, Color outlineColor)
    {
        base_background = backgroundColor;
        base_outline = outlineColor;
    }

    private void OnDisable()
    {
        highlighted.Clear ();
    }
}