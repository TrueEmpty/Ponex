using UnityEngine;

/// <summary>Runtime-generated textures for unique field walls / floors.</summary>
public static class FieldTextureFactory
{
    static Texture2D spaceVoid;
    static Texture2D portalSoft;
    static Texture2D kingdomStone;
    static Texture2D kingdomPavers;
    static Texture2D tideBark;
    static Texture2D waterNormal;

    public static Texture2D SpaceVoidWall()
    {
        if (spaceVoid != null) return spaceVoid;
        spaceVoid = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        spaceVoid.name = "SpaceVoidWall";
        spaceVoid.wrapMode = TextureWrapMode.Repeat;
        spaceVoid.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float nx = x / 128f;
                float ny = y / 128f;
                float grain = Mathf.PerlinNoise(nx * 7f, ny * 7f);
                float edge = Mathf.PerlinNoise(nx * 18f + 3f, ny * 18f);
                float v = 0.015f + grain * 0.04f + edge * 0.02f;
                float glow = Mathf.Pow(Mathf.PerlinNoise(nx * 3f + 10f, ny * 3f), 4f) * 0.12f;
                spaceVoid.SetPixel(x, y, new Color(0.02f + glow * 0.3f, 0.03f + glow * 0.5f, 0.06f + glow, 1f) + Color.white * v * 0.15f);
            }
        }
        spaceVoid.Apply(false, false);
        return spaceVoid;
    }

    public static Texture2D PortalSoftGlow()
    {
        if (portalSoft != null) return portalSoft;
        portalSoft = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        portalSoft.name = "PortalSoftGlow";
        portalSoft.wrapMode = TextureWrapMode.Clamp;
        portalSoft.filterMode = FilterMode.Bilinear;
        Vector2 c = new Vector2(0.5f, 0.5f);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float d = Vector2.Distance(new Vector2(x / 63f, y / 63f), c);
                float a = Mathf.Clamp01(1f - d * 1.65f);
                a = Mathf.Pow(a, 1.6f) * 0.55f;
                float ring = Mathf.Exp(-Mathf.Pow((d - 0.28f) * 8f, 2f)) * 0.35f;
                float alpha = Mathf.Clamp01(a + ring);
                portalSoft.SetPixel(x, y, new Color(0.45f, 0.75f, 1f, alpha));
            }
        }
        portalSoft.Apply(false, false);
        return portalSoft;
    }

    public static Texture2D KingdomStone()
    {
        if (kingdomStone != null) return kingdomStone;
        kingdomStone = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        kingdomStone.name = "KingdomStone";
        kingdomStone.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                // Block mortar grid
                int bx = x % 32;
                int by = y % 20;
                bool mortar = bx < 2 || by < 2;
                float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                Color stone = Color.Lerp(
                    new Color(0.48f, 0.44f, 0.38f),
                    new Color(0.62f, 0.58f, 0.5f),
                    n);
                if (((x / 32) + (y / 20)) % 2 == 0)
                    stone *= 0.92f;
                if (mortar)
                    stone = new Color(0.28f, 0.25f, 0.22f);
                kingdomStone.SetPixel(x, y, stone);
            }
        }
        kingdomStone.Apply(false, false);
        return kingdomStone;
    }

    public static Texture2D KingdomPavers()
    {
        if (kingdomPavers != null) return kingdomPavers;
        kingdomPavers = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        kingdomPavers.name = "KingdomPavers";
        kingdomPavers.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                int px = x % 16;
                int py = y % 16;
                bool joint = px < 1 || py < 1;
                float n = Mathf.PerlinNoise(x * 0.11f + 2f, y * 0.11f);
                Color tile = Color.Lerp(
                    new Color(0.52f, 0.46f, 0.36f),
                    new Color(0.64f, 0.56f, 0.42f),
                    n);
                if (joint)
                    tile = new Color(0.32f, 0.28f, 0.22f);
                kingdomPavers.SetPixel(x, y, tile);
            }
        }
        kingdomPavers.Apply(false, false);
        return kingdomPavers;
    }

    public static Texture2D TideBark()
    {
        if (tideBark != null) return tideBark;
        tideBark = new Texture2D(128, 64, TextureFormat.RGBA32, false);
        tideBark.name = "TideBark";
        tideBark.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float rings = Mathf.Abs(Mathf.Sin(x * 0.35f + Mathf.PerlinNoise(x * 0.05f, y * 0.2f) * 2f));
                float wet = Mathf.PerlinNoise(x * 0.04f, y * 0.15f + 5f);
                Color bark = Color.Lerp(
                    new Color(0.28f, 0.18f, 0.08f),
                    new Color(0.5f, 0.34f, 0.16f),
                    rings);
                bark = Color.Lerp(bark, new Color(0.18f, 0.22f, 0.12f), wet * 0.25f);
                // Moss flecks
                if (Mathf.PerlinNoise(x * 0.3f, y * 0.3f) > 0.72f)
                    bark = Color.Lerp(bark, new Color(0.22f, 0.38f, 0.14f), 0.55f);
                tideBark.SetPixel(x, y, bark);
            }
        }
        tideBark.Apply(false, false);
        return tideBark;
    }

    public static void ApplyAlbedo(Renderer ren, Texture tex, Color tint, float metallic, float smoothness)
    {
        if (ren == null)
            return;
        if (ren.material == null)
            ren.material = new Material(Shader.Find("Standard"));
        ren.material.color = tint;
        ren.material.SetFloat("_Metallic", metallic);
        ren.material.SetFloat("_Glossiness", smoothness);
        if (tex != null)
        {
            ren.material.SetTexture("_MainTex", tex);
            ren.material.mainTexture = tex;
        }
    }

    public static void ApplyTransparentGlow(Renderer ren, Texture tex, Color tint)
    {
        if (ren == null)
            return;
        Material m = new Material(Shader.Find("Standard"));
        m.SetFloat("_Mode", 3f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
        m.color = tint;
        if (tex != null)
        {
            m.SetTexture("_MainTex", tex);
            m.mainTexture = tex;
        }
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", new Color(tint.r, tint.g, tint.b, 1f) * 0.45f);
        ren.material = m;
    }
}
