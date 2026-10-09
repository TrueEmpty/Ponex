using UnityEngine;

[System.Serializable]
public class PlayerColors : ISerializationCallbackReceiver
{
    [Tooltip("Color 1 — main / brand color.")]
    public Color color = Color.white;
    [Tooltip("Color 2 — deep shadow.")]
    public Color color2 = Color.white;
    [Tooltip("Color 3 — mid-dark.")]
    public Color color3 = Color.white;
    [Tooltip("Color 4 — deeper main.")]
    public Color color4 = Color.white;
    [Tooltip("Color 5 — highlight.")]
    public Color color5 = Color.white;
    [Tooltip("Color 6 — pale accent.")]
    public Color color6 = Color.white;
    [Tooltip("Color 7 — cool accent.")]
    public Color color7 = Color.white;
    [Tooltip("Color 8 — warm accent.")]
    public Color color8 = Color.white;
    [Tooltip("Color 9 — muted cloth / metal.")]
    public Color color9 = Color.white;
    [Tooltip("Color 10 — rim / outline.")]
    public Color color10 = Color.white;
    public Sprite sprite;

    public const int SlotCount = 10;

    public Color GetSlot(int index)
    {
        switch (Mathf.Clamp(index, 0, SlotCount - 1))
        {
            case 0: return color;
            case 1: return color2;
            case 2: return color3;
            case 3: return color4;
            case 4: return color5;
            case 5: return color6;
            case 6: return color7;
            case 7: return color8;
            case 8: return color9;
            default: return color10;
        }
    }

    public void SetSlot(int index, Color value)
    {
        switch (Mathf.Clamp(index, 0, SlotCount - 1))
        {
            case 0: color = value; break;
            case 1: color2 = value; break;
            case 2: color3 = value; break;
            case 3: color4 = value; break;
            case 4: color5 = value; break;
            case 5: color6 = value; break;
            case 6: color7 = value; break;
            case 7: color8 = value; break;
            case 8: color9 = value; break;
            default: color10 = value; break;
        }
    }

    public Color[] GetScheme()
    {
        EnsureScheme();
        return new Color[]
        {
            color, color2, color3, color4, color5,
            color6, color7, color8, color9, color10
        };
    }

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize()
    {
        EnsureScheme();
    }

    /// <summary>
    /// Fills empty slots (or overwrites when <paramref name="force"/>) with a
    /// 10-color scheme derived from the main color.
    /// </summary>
    public void EnsureScheme(bool force = false)
    {
        Color main = color.a < 0.02f ? Color.white : color;
        color = main;

        for (int i = 1; i < SlotCount; i++)
        {
            Color existing = GetSlot(i);
            if (force || IsUnsetSlot(existing, main))
                SetSlot(i, SchemeColor(main, i));
        }
    }

    static bool IsUnsetSlot(Color slot, Color main)
    {
        if (slot.a < 0.02f) return true;
        if (Approximately(slot, Color.white))
            return true;
        if (Approximately(slot, Color.black))
            return true;
        if (Approximately(slot, main))
            return true;
        return false;
    }

    static bool Approximately(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.02f
            && Mathf.Abs(a.g - b.g) < 0.02f
            && Mathf.Abs(a.b - b.b) < 0.02f;
    }

    /// <summary>
    /// Slot 0 is the main color. Remaining slots keep the hue family while
    /// spreading value/saturation so authored mesh parts stay distinct.
    /// </summary>
    public static Color SchemeColor(Color main, int slot)
    {
        slot = Mathf.Clamp(slot, 0, SlotCount - 1);
        if (slot == 0)
            return main;

        Color.RGBToHSV(main, out float h, out float s, out float v);
        bool grayscale = s < 0.08f;

        float hue = h;
        float sat = s;
        float val = v;
        float a = Mathf.Max(main.a, 1f);

        switch (slot)
        {
            case 1: // deep shadow
                sat = grayscale ? 0f : Mathf.Clamp01(s * 1.12f);
                val = Mathf.Clamp01(v * 0.38f);
                hue = grayscale ? h : WrapHue(h - 0.018f);
                break;
            case 2: // mid-dark
                sat = grayscale ? 0f : Mathf.Clamp01(s * 1.05f);
                val = Mathf.Clamp01(v * 0.58f);
                hue = grayscale ? h : WrapHue(h - 0.008f);
                break;
            case 3: // slightly deeper main
                sat = grayscale ? 0.04f : Mathf.Clamp01(s * 1.08f);
                val = Mathf.Clamp01(v * 0.78f);
                break;
            case 4: // light / highlight
                sat = grayscale ? 0.02f : Mathf.Clamp01(s * 0.55f);
                val = Mathf.Clamp01(Mathf.Max(v * 1.18f, 0.88f));
                hue = grayscale ? h : WrapHue(h + 0.012f);
                break;
            case 5: // pale accent
                sat = grayscale ? 0.03f : Mathf.Clamp01(s * 0.28f);
                val = Mathf.Clamp01(Mathf.Max(v * 1.22f, 0.94f));
                hue = grayscale ? h : WrapHue(h + 0.03f);
                break;
            case 6: // complementary / cool accent
                if (grayscale)
                {
                    sat = 0.08f;
                    val = Mathf.Clamp01(v * 0.72f);
                    hue = 0.58f;
                }
                else
                {
                    hue = WrapHue(h + 0.08f);
                    sat = Mathf.Clamp01(s * 0.85f);
                    val = Mathf.Clamp01(v * 0.86f);
                }
                break;
            case 7: // warm / analogous accent
                if (grayscale)
                {
                    sat = 0.1f;
                    val = Mathf.Clamp01(v * 0.82f);
                    hue = 0.08f;
                }
                else
                {
                    hue = WrapHue(h - 0.07f);
                    sat = Mathf.Clamp01(s * 0.92f);
                    val = Mathf.Clamp01(v * 0.92f);
                }
                break;
            case 8: // muted cloth / metal
                sat = grayscale ? 0.04f : Mathf.Clamp01(s * 0.42f);
                val = Mathf.Clamp01(v * 0.48f + 0.18f);
                hue = grayscale ? 0.62f : WrapHue(h + 0.045f);
                break;
            default: // near-black rim / outline
                sat = grayscale ? 0f : Mathf.Clamp01(s * 0.7f);
                val = Mathf.Clamp01(v * 0.16f + 0.04f);
                hue = grayscale ? h : WrapHue(h - 0.03f);
                break;
        }

        Color c = Color.HSVToRGB(hue, sat, val);
        c.a = a;
        return c;
    }

    static float WrapHue(float h)
    {
        h %= 1f;
        if (h < 0f) h += 1f;
        return h;
    }
}
