using UnityEngine;

/// <summary>Subtle tiled courtyard floor pulse for kingdom fields.</summary>
[RequireComponent(typeof(Renderer))]
public class CourtyardPavers : MonoBehaviour
{
    public Vector2 tileScale = new Vector2(6f, 6f);
    Renderer ren;
    Material material;
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    static readonly int GlossinessId = Shader.PropertyToID("_Glossiness");

    void Start()
    {
        ren = GetComponent<Renderer>();
        if (ren != null)
            material = ren.material;
        if (material != null && material.HasProperty(MainTexId))
            material.mainTextureScale = tileScale;
    }

    void Update()
    {
        if (material == null)
            return;
        material.SetFloat(GlossinessId, 0.25f + Mathf.Sin(Time.time * 0.5f) * 0.05f);
    }
}
