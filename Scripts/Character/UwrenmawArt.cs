using UnityEngine;

/// <summary>Crack and sand-aura textures for Uwrenmaw's pillars.</summary>
public static class UwrenmawArt
{
    static Texture2D[] cracks;
    static Texture2D aura;
    static Texture2D soft;
    static Texture2D body;

    public static Texture2D Body()
    {
        if (body != null)
            return body;
        body = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        body.name = "Uwrenmaw Sand";
        body.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.18f, y * 0.18f);
                Color sand = Color.Lerp(new Color(0.72f, 0.55f, 0.32f), new Color(0.9f, 0.78f, 0.48f), n);
                body.SetPixel(x, y, sand);
            }
        }
        body.Apply(false, false);
        return body;
    }

    /// <summary>0 = intact, 1 = light cracks, 2 = heavy, 3 = about to break.</summary>
    public static Texture2D Cracks(int damage)
    {
        damage = Mathf.Clamp(damage, 0, 3);
        if (cracks == null)
            cracks = new Texture2D[4];
        if (cracks[damage] != null)
            return cracks[damage];

        const int s = 64;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.name = "Uwrenmaw Pillar " + damage;
        tex.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.12f, y * 0.12f);
                Color sand = Color.Lerp(new Color(0.78f, 0.64f, 0.4f), new Color(0.55f, 0.42f, 0.26f), n);
                float crack = 0f;
                if (damage > 0)
                {
                    float line = Mathf.Abs(Mathf.Sin(x * 0.35f + y * 0.08f));
                    float branch = Mathf.Abs(Mathf.Sin(y * 0.42f - x * 0.05f + 1.7f));
                    float jag = Mathf.PerlinNoise(x * 0.4f, y * 0.15f);
                    crack = (line < 0.08f + damage * 0.03f ? 1f : 0f);
                    if (damage >= 2)
                        crack = Mathf.Max(crack, branch < 0.07f + damage * 0.02f ? 1f : 0f);
                    if (damage >= 3)
                        crack = Mathf.Max(crack, jag > 0.72f ? 0.85f : 0f);
                }
                if (crack > 0f)
                    sand = Color.Lerp(sand, new Color(0.18f, 0.1f, 0.06f), crack);
                tex.SetPixel(x, y, sand);
            }
        }
        tex.Apply(false, false);
        cracks[damage] = tex;
        return tex;
    }

    public static Texture2D Aura()
    {
        if (aura != null)
            return aura;
        const int s = 64;
        aura = new Texture2D(s, s, TextureFormat.RGBA32, false);
        aura.name = "Uwrenmaw Sand Aura";
        aura.wrapMode = TextureWrapMode.Clamp;
        Vector2 c = new Vector2(0.5f, 0.5f);
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x / (s - 1f), y / (s - 1f)), c);
                float grain = Mathf.PerlinNoise(x * 0.35f, y * 0.35f);
                float a = Mathf.Clamp01(1f - d * 1.7f);
                a = Mathf.Pow(a, 1.4f) * (0.35f + grain * 0.65f);
                Color sand = Color.Lerp(new Color(0.95f, 0.82f, 0.45f), new Color(0.72f, 0.48f, 0.22f), grain);
                sand.a = a;
                aura.SetPixel(x, y, sand);
            }
        }
        aura.Apply(false, false);
        return aura;
    }

    /// <summary>Soft round mask with white color so particles take the palette exactly.</summary>
    public static Texture2D Soft()
    {
        if (soft != null)
            return soft;
        const int s = 64;
        soft = new Texture2D(s, s, TextureFormat.RGBA32, false);
        soft.name = "Uwrenmaw Soft";
        soft.wrapMode = TextureWrapMode.Clamp;
        Vector2 c = new Vector2(0.5f, 0.5f);
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x / (s - 1f), y / (s - 1f)), c);
                float a = Mathf.Clamp01(1f - d * 1.7f);
                a = Mathf.Pow(a, 1.35f);
                soft.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        soft.Apply(false, false);
        return soft;
    }
}
