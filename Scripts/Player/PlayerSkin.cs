using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves per-player skin colors and applies them to character / lifeline / projectile renderers.
/// - Authored textures: soft tint (detail stays readable)
/// - Flat / untextured (Nari, Test, Trigger, …): full color overwrite
/// - Multi-flat parts (Tic, Master Pong, …): each distinct authored color remaps into the skin
///   family while keeping relative shade / accent separation
/// - Objects with <see cref="SkipPlayerSkin"/> keep authored colors
/// </summary>
public static class PlayerSkin
{
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static readonly int TintColorId = Shader.PropertyToID("_TintColor");
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    /// <summary>How strongly textured materials multiply albedo by skin.</summary>
    const float TexturedAlbedoTint = 0.45f;
    /// <summary>How strongly textured emission is hue-shifted.</summary>
    const float TexturedEmissionTint = 0.40f;
    const float ParticleTintStrength = 0.55f;

    struct MatSlot
    {
        public Renderer renderer;
        public int matIndex;
        public Material material;
        public bool textured;
        public float luminance;
        public Color sourceColor;
    }

    /// <summary>Usable skin slots (skips trailing white CPU cursor color when present).</summary>
    public static int UsableColorCount(Database db)
    {
        if (db == null || db.playerColors == null || db.playerColors.Count == 0)
            return 0;

        int count = db.playerColors.Count;
        if (count > 1)
        {
            Color last = db.playerColors[count - 1].color;
            if (last.r > 0.95f && last.g > 0.95f && last.b > 0.95f)
                count--;
        }

        return Mathf.Max(1, count);
    }

    public static int EffectiveSkinIndex(Player p, Database db = null)
    {
        if (p == null)
            return 0;

        db = db != null ? db : Database.instance;
        int usable = UsableColorCount(db);
        if (usable <= 0)
            return 0;

        int idx = p.skinColorIndex >= 0 ? p.skinColorIndex : p.index;
        idx %= usable;
        if (idx < 0)
            idx += usable;
        return idx;
    }

    public static Color GetColor(Player p, Database db = null)
    {
        db = db != null ? db : Database.instance;
        if (p == null || db == null || db.playerColors == null || db.playerColors.Count == 0)
            return Color.white;

        int idx = EffectiveSkinIndex(p, db);
        if (idx < 0 || idx >= db.playerColors.Count)
            return Color.white;

        return db.playerColors[idx].color;
    }

    public static PlayerColors GetPlayerColors(Player p, Database db = null)
    {
        db = db != null ? db : Database.instance;
        if (p == null || db == null || db.playerColors == null || db.playerColors.Count == 0)
            return null;

        int idx = EffectiveSkinIndex(p, db);
        if (idx < 0 || idx >= db.playerColors.Count)
            return null;

        return db.playerColors[idx];
    }

    /// <summary>
    /// True when two character names share a skin pool (exact match, or Celarus family).
    /// Sunshine / Moonlight Celarus conflict with main Celarus and each other.
    /// </summary>
    public static bool SharesSkinGroup(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return false;
        if (a == b)
            return true;
        return IsCelarusFamily(a) && IsCelarusFamily(b);
    }

    static bool IsCelarusFamily(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        return name == "Celarus"
            || name == "Sunshine Celarus"
            || name == "Moonlight Celarus";
    }

    /// <summary>Skins already used by other players in the same skin group.</summary>
    public static bool[] GetTakenSkins(Player p, Database db, int usable)
    {
        bool[] taken = new bool[Mathf.Max(0, usable)];
        if (p == null || db == null || db.players == null || usable <= 0)
            return taken;

        for (int i = 0; i < db.players.Count; i++)
        {
            Player other = db.players[i];
            if (other == null || other == p)
                continue;
            if (!SharesSkinGroup(other.name, p.name))
                continue;

            int oi = EffectiveSkinIndex(other, db);
            if (oi >= 0 && oi < usable)
                taken[oi] = true;
        }

        return taken;
    }

    public static bool IsSkinTakenBySameCharacter(Player p, int skinIndex, Database db = null)
    {
        if (p == null)
            return false;

        db = db != null ? db : Database.instance;
        int usable = UsableColorCount(db);
        if (skinIndex < 0 || skinIndex >= usable)
            return false;

        bool[] taken = GetTakenSkins(p, db, usable);
        return taken[skinIndex];
    }

    /// <summary>
    /// Cycle to the next free skin for this character.
    /// direction &gt; 0 goes forward; direction &lt; 0 goes backward.
    /// </summary>
    public static void Cycle(Player p, Database db = null, int direction = 1)
    {
        if (p == null)
            return;

        db = db != null ? db : Database.instance;
        int usable = UsableColorCount(db);
        if (usable <= 0)
            return;

        int dir = direction < 0 ? -1 : 1;
        int cur = EffectiveSkinIndex(p, db);
        bool[] taken = GetTakenSkins(p, db, usable);

        for (int step = 1; step <= usable; step++)
        {
            int next = cur + dir * step;
            next %= usable;
            if (next < 0)
                next += usable;

            if (!taken[next])
            {
                p.skinColorIndex = next;
                return;
            }
        }
    }

    /// <summary>
    /// Pick a skin not already used by another player on the same character.
    /// Prefers the player's slot color when free.
    /// </summary>
    public static void AssignUniqueForCharacter(Player p, Database db = null)
    {
        if (p == null)
            return;

        db = db != null ? db : Database.instance;
        int usable = UsableColorCount(db);
        if (usable <= 0)
        {
            p.skinColorIndex = p.index;
            return;
        }

        bool[] taken = GetTakenSkins(p, db, usable);

        int current = EffectiveSkinIndex(p, db);
        if (!taken[current])
        {
            p.skinColorIndex = current;
            return;
        }

        int prefer = ((p.index % usable) + usable) % usable;
        if (!taken[prefer])
        {
            p.skinColorIndex = prefer;
            return;
        }

        for (int i = 0; i < usable; i++)
        {
            if (!taken[i])
            {
                p.skinColorIndex = i;
                return;
            }
        }

        p.skinColorIndex = current;
    }

    public static void Apply(GameObject root, Player p, Database db = null)
    {
        if (root == null || p == null)
            return;

        Apply(root, GetColor(p, db), p.name);
    }

    public static void Apply(GameObject root, Color skin)
    {
        Apply(root, skin, null);
    }

    /// <summary>
    /// Apply skin. Flat materials fully overwrite; multi-flat parts keep relative shade/hue
    /// separation inside the skin family; textured materials keep a soft tint.
    /// </summary>
    public static void Apply(GameObject root, Color skin, string characterName)
    {
        if (root == null)
            return;
        if (SkipPlayerSkin.ShouldSkipRoot(root))
            return;

        bool forceFull = ForcesFullOverwrite(characterName);

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        List<MatSlot> slots = new List<MatSlot>(16);
        Dictionary<int, Color> uniqueFlatSrc = new Dictionary<int, Color>();

        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer ren = renderers[r];
            if (ren == null || ren is ParticleSystemRenderer)
                continue;
            if (SkipPlayerSkin.ShouldSkipRenderer(ren))
                continue;

            Material[] mats = ren.sharedMaterials;
            if (mats == null)
                continue;

            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if (mat == null)
                    continue;

                bool textured = !forceFull && HasAuthoredTexture(mat);
                Color src = ReadAlbedo(mat);
                float lum = Luminance(src);

                slots.Add(new MatSlot
                {
                    renderer = ren,
                    matIndex = m,
                    material = mat,
                    textured = textured,
                    luminance = lum,
                    sourceColor = src
                });

                if (!textured)
                {
                    int id = mat.GetEntityId().GetHashCode();
                    if (!uniqueFlatSrc.ContainsKey(id))
                        uniqueFlatSrc[id] = src;
                }
            }
        }

        // Remap each distinct flat color into the skin family (preserves shade + accent gaps).
        Color baseFlat = PickBaseFlatColor(uniqueFlatSrc);
        Dictionary<int, Color> flatColorByMat = new Dictionary<int, Color>(uniqueFlatSrc.Count);
        foreach (var kv in uniqueFlatSrc)
            flatColorByMat[kv.Key] = RemapRelativeToSkin(kv.Value, baseFlat, skin);

        MaterialPropertyBlock block = new MaterialPropertyBlock();

        for (int i = 0; i < slots.Count; i++)
        {
            MatSlot slot = slots[i];
            Renderer ren = slot.renderer;
            Material mat = slot.material;
            ren.GetPropertyBlock(block, slot.matIndex);

            if (slot.textured)
            {
                ApplySoftTint(block, mat, skin, slot.sourceColor);
            }
            else
            {
                Color target = skin;
                if (flatColorByMat.TryGetValue(mat.GetEntityId().GetHashCode(), out Color schemeColor))
                    target = schemeColor;
                // Preserve authored alpha (transparency on Iyolit-like flats, etc.)
                target.a = slot.sourceColor.a;
                ApplyFullOverwrite(block, mat, target, slot.sourceColor);
            }

            ren.SetPropertyBlock(block, slot.matIndex);
        }

        bool fullParticles = forceFull || uniqueFlatSrc.Count > 0 && CountTextured(slots) == 0;
        if (!SkipPlayerSkin.ShouldSkipRoot(root))
            ApplyParticlesAndTrails(root, skin, fullParticles);
    }

    static int CountTextured(List<MatSlot> slots)
    {
        int n = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].textured)
                n++;
        }
        return n;
    }

    static bool ForcesFullOverwrite(string characterName)
    {
        if (string.IsNullOrEmpty(characterName))
            return false;
        // Known untextured single-color characters — always full overwrite
        return characterName.Equals("Nari", System.StringComparison.OrdinalIgnoreCase)
            || characterName.Equals("Test", System.StringComparison.OrdinalIgnoreCase)
            || characterName.Equals("Trigger", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when the material has a real albedo map (not null / not Unity's tiny default white).
    /// </summary>
    public static bool HasAuthoredTexture(Material mat)
    {
        if (mat == null)
            return false;

        Texture tex = null;
        if (mat.HasProperty(BaseMapId))
            tex = mat.GetTexture(BaseMapId);
        if (tex == null && mat.HasProperty(MainTexId))
            tex = mat.GetTexture(MainTexId);
        if (tex == null)
            return false;

        // Built-in / placeholder whites are not authored character textures
        string n = tex.name;
        if (string.IsNullOrEmpty(n))
            return false;
        if (n.StartsWith("Default", System.StringComparison.OrdinalIgnoreCase))
            return false;
        if (n.IndexOf("white", System.StringComparison.OrdinalIgnoreCase) >= 0 && tex.width <= 8 && tex.height <= 8)
            return false;
        // Tiny placeholder maps don't count as surface detail
        if (tex.width <= 2 && tex.height <= 2)
            return false;

        return true;
    }

    static Color ReadAlbedo(Material mat)
    {
        if (mat.HasProperty(BaseColorId))
            return mat.GetColor(BaseColorId);
        if (mat.HasProperty(ColorId))
            return mat.GetColor(ColorId);
        if (mat.HasProperty(TintColorId))
            return mat.GetColor(TintColorId);
        return Color.white;
    }

    static float Luminance(Color c)
    {
        return c.r * 0.2126f + c.g * 0.7152f + c.b * 0.0722f;
    }

    /// <summary>
    /// Pick the "main" authored flat color (median luminance) so remaps keep relative contrast.
    /// </summary>
    static Color PickBaseFlatColor(Dictionary<int, Color> uniqueFlatSrc)
    {
        if (uniqueFlatSrc == null || uniqueFlatSrc.Count == 0)
            return Color.white;

        List<Color> colors = new List<Color>(uniqueFlatSrc.Values);
        colors.Sort((a, b) => Luminance(a).CompareTo(Luminance(b)));
        return colors[colors.Count / 2];
    }

    /// <summary>
    /// Map an authored part color into the player skin family while keeping its relative
    /// darkness/lightness and accent separation vs the character's base flat color.
    /// Different hues on Tic (etc.) become different skin-family shades, not one solid fill.
    /// </summary>
    static Color RemapRelativeToSkin(Color source, Color baseCol, Color skin)
    {
        Color.RGBToHSV(source, out float sh, out float ss, out float sv);
        Color.RGBToHSV(baseCol, out float bh, out float bs, out float bv);
        Color.RGBToHSV(skin, out float kh, out float ks, out float kv);

        float valueScale = bv > 0.02f ? sv / bv : 1f;
        float satScale = bs > 0.02f ? ss / bs : 1f;

        float hueOffset = sh - bh;
        if (hueOffset > 0.5f) hueOffset -= 1f;
        if (hueOffset < -0.5f) hueOffset += 1f;

        // Stronger authored hue gaps → more analogous drift under the skin hue
        float chromaSep = Mathf.Abs(hueOffset) * Mathf.Clamp01(Mathf.Max(ss, bs));
        float nh = kh + hueOffset * Mathf.Lerp(0.06f, 0.42f, Mathf.Clamp01(chromaSep * 2.2f));
        if (nh < 0f) nh += 1f;
        if (nh > 1f) nh -= 1f;

        float nv = Mathf.Clamp01(kv * Mathf.Clamp(valueScale, 0.22f, 1.7f));
        float ns = Mathf.Clamp01(ks * Mathf.Clamp(satScale, 0.3f, 1.4f));

        // Keep dark accents dark and light panels light relative to the skin
        if (sv < bv * 0.6f)
            nv = Mathf.Min(nv, Mathf.Lerp(nv, kv * 0.32f, 0.55f));
        if (sv > bv * 1.2f)
            nv = Mathf.Max(nv, Mathf.Lerp(nv, Mathf.Lerp(kv, 1f, 0.55f), 0.45f));

        // Near-base parts stay closest to the exact player skin
        float nearBase = 1f - Mathf.Clamp01(Mathf.Abs(Luminance(source) - Luminance(baseCol)) * 2.5f
            + chromaSep * 1.5f);
        Color remapped = Color.HSVToRGB(nh, ns, nv);
        remapped = Color.Lerp(remapped, skin, nearBase * 0.55f);
        remapped.a = source.a;
        return remapped;
    }

    /// <summary>
    /// Dark → light tones that stay in the skin's hue family (fallback / particles).
    /// </summary>
    static Color[] BuildColorScheme(Color skin, int count)
    {
        count = Mathf.Max(1, count);
        Color[] colors = new Color[count];
        Color.RGBToHSV(skin, out float h, out float s, out float v);

        if (count == 1)
        {
            colors[0] = skin;
            return colors;
        }

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            float nv = Mathf.Lerp(Mathf.Clamp01(v * 0.28f + 0.04f), Mathf.Clamp01(Mathf.Lerp(v, 1f, 0.55f)), t);
            float ns = Mathf.Lerp(Mathf.Clamp01(s * 1.05f), Mathf.Clamp01(s * 0.45f), t);
            float nh = h + (t - 0.5f) * 0.035f;
            if (nh < 0f) nh += 1f;
            if (nh > 1f) nh -= 1f;

            Color c = Color.HSVToRGB(nh, ns, nv);
            if (count >= 2)
            {
                float mid = (count - 1) * 0.5f;
                float midBlend = 1f - Mathf.Clamp01(Mathf.Abs(i - mid) / Mathf.Max(0.5f, mid));
                c = Color.Lerp(c, skin, midBlend * 0.65f);
            }
            c.a = skin.a;
            colors[i] = c;
        }

        colors[count / 2] = skin;
        return colors;
    }

    static void ApplySoftTint(MaterialPropertyBlock block, Material mat, Color skin, Color src)
    {
        if (mat.HasProperty(BaseColorId))
            block.SetColor(BaseColorId, MultiplyTint(src, skin, TexturedAlbedoTint));
        if (mat.HasProperty(ColorId))
        {
            Color c = mat.HasProperty(BaseColorId) ? mat.GetColor(ColorId) : src;
            block.SetColor(ColorId, MultiplyTint(c, skin, TexturedAlbedoTint));
        }
        if (mat.HasProperty(TintColorId))
        {
            Color c = mat.GetColor(TintColorId);
            block.SetColor(TintColorId, MultiplyTint(c, skin, TexturedAlbedoTint));
        }
        if (mat.HasProperty(EmissionId))
        {
            Color srcEmit = mat.GetColor(EmissionId);
            if (srcEmit.maxColorComponent > 0.001f)
                block.SetColor(EmissionId, MultiplyTint(srcEmit, skin, TexturedEmissionTint));
        }
    }

    static void ApplyFullOverwrite(MaterialPropertyBlock block, Material mat, Color target, Color src)
    {
        if (mat.HasProperty(BaseColorId))
            block.SetColor(BaseColorId, target);
        if (mat.HasProperty(ColorId))
            block.SetColor(ColorId, target);
        if (mat.HasProperty(TintColorId))
        {
            Color tint = target;
            // Keep additive particle-style tint alpha from source when present
            if (mat.HasProperty(TintColorId))
                tint.a = Mathf.Clamp01(src.a > 0.001f ? src.a : target.a);
            block.SetColor(TintColorId, tint);
        }
        if (mat.HasProperty(EmissionId))
        {
            Color srcEmit = mat.GetColor(EmissionId);
            if (srcEmit.maxColorComponent > 0.001f)
            {
                Color emit = target * srcEmit.maxColorComponent;
                emit.a = srcEmit.a;
                block.SetColor(EmissionId, emit);
            }
        }
    }

    static void ApplyParticlesAndTrails(GameObject root, Color skin, bool fullOverwrite)
    {
        float strength = fullOverwrite ? 1f : ParticleTintStrength;

        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem ps = particles[i];
            if (ps == null)
                continue;

            var main = ps.main;
            ParticleSystem.MinMaxGradient g = main.startColor;
            if (g.mode == ParticleSystemGradientMode.Color)
            {
                main.startColor = fullOverwrite
                    ? WithAlpha(skin, g.color.a)
                    : MultiplyTint(g.color, skin, strength);
            }
            else if (g.mode == ParticleSystemGradientMode.TwoColors)
            {
                if (fullOverwrite)
                {
                    Color a = WithAlpha(skin, g.colorMin.a);
                    Color b = WithAlpha(Color.Lerp(skin, Color.white, 0.35f), g.colorMax.a);
                    main.startColor = new ParticleSystem.MinMaxGradient(a, b);
                }
                else
                {
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        MultiplyTint(g.colorMin, skin, strength),
                        MultiplyTint(g.colorMax, skin, strength));
                }
            }
        }

        TrailRenderer[] trails = root.GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trails.Length; i++)
        {
            TrailRenderer tr = trails[i];
            if (tr == null)
                continue;
            if (fullOverwrite)
            {
                tr.startColor = WithAlpha(skin, tr.startColor.a);
                tr.endColor = WithAlpha(Color.Lerp(skin, Color.clear, 0.5f), tr.endColor.a);
            }
            else
            {
                tr.startColor = MultiplyTint(tr.startColor, skin, 0.5f);
                tr.endColor = MultiplyTint(tr.endColor, skin, 0.5f);
            }
        }

        LineRenderer[] lines = root.GetComponentsInChildren<LineRenderer>(true);
        for (int i = 0; i < lines.Length; i++)
        {
            LineRenderer lr = lines[i];
            if (lr == null)
                continue;
            if (fullOverwrite)
            {
                lr.startColor = WithAlpha(skin, lr.startColor.a);
                lr.endColor = WithAlpha(skin, lr.endColor.a);
            }
            else
            {
                lr.startColor = MultiplyTint(lr.startColor, skin, 0.5f);
                lr.endColor = MultiplyTint(lr.endColor, skin, 0.5f);
            }
        }
    }

    static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }

    /// <summary>
    /// Multiply original by a soft skin factor so albedo/emission textures still read.
    /// </summary>
    static Color MultiplyTint(Color original, Color skin, float strength)
    {
        strength = Mathf.Clamp01(strength);
        Color factor = Color.Lerp(Color.white, skin, strength);
        return new Color(
            original.r * factor.r,
            original.g * factor.g,
            original.b * factor.b,
            original.a);
    }
}
