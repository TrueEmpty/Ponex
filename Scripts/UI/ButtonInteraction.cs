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
        if (deactivated)
        {
            if (background != null)
            {
                background.color = deactivatedColor;
            }

            if (outline != null)
            {
                outline.effectColor = deactivatedColor_outline;
            }

        }
        else if (pressed)
        {
            if (background != null)
            {
                background.color = Color.Lerp(base_background, pressedColor, fadeAmount);
            }

            AddHighlightOutline();
        }
        else if (highlighted.Count > 0)
        {
            if (background != null)
            {
                background.color = Color.Lerp(base_background, highlightColor, fadeAmount);
            }

            AddHighlightOutline();
        }
        else
        {
            if (background != null)
            {
                background.color = base_background;
            }

            if (outline != null)
            {
                outline.effectColor = base_outline;
            }
        }

        pressed = false;
    }

    void AddHighlightOutline()
    {
        Color c = Color.white;

        if (outline != null)
        {
            foreach (int i in highlighted)
            {
                if (i >= 0 && i < 8)
                {
                    c = Color.Lerp(c, db.playerColors[i].color, .5f);
                }
            }

            outline.effectColor = c;
        }
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

    private void OnDisable()
    {
        highlighted.Clear ();
    }
}