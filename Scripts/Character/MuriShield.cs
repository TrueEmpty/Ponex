using UnityEngine;

/// <summary>
/// One-hit shield around Muri. Restored when a kunai claims a ball.
/// </summary>
public class MuriShield : MonoBehaviour
{
    public Transform visual;
    public bool HasShield { get; private set; } = true;

    Renderer visRend;
    MaterialPropertyBlock block;
    Color baseColor = new Color(0.35f, 0.95f, 1f, 0.14f);
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        if (visual == null)
        {
            Transform child = transform.Find("Shield");
            if (child != null)
                visual = child;
        }
        if (visual != null)
        {
            visRend = visual.GetComponent<Renderer>();
            if (visual.GetComponent<SkipPlayerSkin>() == null)
            {
                SkipPlayerSkin skip = visual.gameObject.AddComponent<SkipPlayerSkin>();
                skip.includeChildren = true;
            }
        }
        block = new MaterialPropertyBlock();
        SetVisible(true);
        ApplyPulse(0.12f);
    }

    public bool TryAbsorb()
    {
        if (!HasShield)
            return false;
        HasShield = false;
        SetVisible(false);
        return true;
    }

    public void Restore()
    {
        if (HasShield)
            return;
        HasShield = true;
        SetVisible(true);
    }

    void Update()
    {
        if (!HasShield || visRend == null)
            return;
        float pulse = 0.08f + 0.05f * Mathf.Abs(Mathf.Sin(Time.time * 3.4f));
        ApplyPulse(pulse);
    }

    void ApplyPulse(float alpha)
    {
        if (visRend == null)
            return;
        Color c = baseColor;
        c.a = alpha;
        visRend.GetPropertyBlock(block);
        block.SetColor(ColorId, c);
        block.SetColor(EmissionId, new Color(0.05f, 0.22f, 0.28f, 1f) * (0.35f + alpha));
        visRend.SetPropertyBlock(block);
    }

    void SetVisible(bool on)
    {
        if (visual != null)
            visual.gameObject.SetActive(on);
    }
}
