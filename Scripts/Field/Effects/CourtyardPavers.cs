using UnityEngine;

/// <summary>Subtle tiled courtyard floor pulse for kingdom fields.</summary>
[RequireComponent(typeof(Renderer))]
public class CourtyardPavers : MonoBehaviour
{
    public Vector2 tileScale = new Vector2(6f, 6f);
    Renderer ren;

    void Start()
    {
        ren = GetComponent<Renderer>();
        if (ren != null && ren.material != null && ren.material.HasProperty("_MainTex"))
            ren.material.mainTextureScale = tileScale;
    }

    void Update()
    {
        if (ren == null || ren.material == null)
            return;
        float v = 0.92f + Mathf.Sin(Time.time * 0.35f) * 0.04f;
        Color c = ren.material.color;
        c.r = Mathf.Clamp01(c.r * 0.999f + 0.001f * v);
        ren.material.SetFloat("_Glossiness", 0.25f + Mathf.Sin(Time.time * 0.5f) * 0.05f);
    }
}
