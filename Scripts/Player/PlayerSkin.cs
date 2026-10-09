using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves per-player skin colors and applies them to character / lifeline / projectile renderers.
/// - Authored textures: soft tint (detail stays readable)
/// - Flat parts: each distinct authored color maps onto PlayerColors slots 1-10
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

        PlayerColors pc = db.playerColors[idx];
        if (pc != null)
            pc.EnsureScheme();
        return pc;
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

        PlayerColors pc = GetPlayerColors(p, db);
        if (pc != null)
            Apply(root, pc, p.name);
        else
            Apply(root, GetColor(p, db), p.name);
    }

    public static void Apply(GameObject root, Color skin)
    {
        Apply(root, skin, null);
    }

    public static void Apply(GameObject root, Color skin, string characterName)
    {
        PlayerColors temp = new PlayerColors { color = skin };
        temp.EnsureScheme(true);
        Apply(root, temp, characterName);
    }

    /// <summary>
    /// Apply the 10-slot player scheme. Distinct authored flat colors map onto
    /// colors 1-10 so parts that started different stay different.
    /// </summary>
    public static void Apply(GameObject root, PlayerColors schemeColors, string characterName)
    {
        if (root == null || schemeColors == null)
            return;
        if (SkipPlayerSkin.ShouldSkipRoot(root))
            return;

        schemeColors.EnsureScheme();
        Color[] scheme = schemeColors.GetScheme();
        Color skin = scheme[0];

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

                bool textured = HasAuthoredTexture(mat);
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
                    int key = QuantizeColor(src);
                    if (!uniqueFlatSrc.ContainsKey(key))
                        uniqueFlatSrc[key] = src;
                }
            }
        }

        Dictionary<int, Color> flatColorByKey = AssignSchemeToUniques(uniqueFlatSrc, scheme);

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
                if (flatColorByKey.TryGetValue(QuantizeColor(slot.sourceColor), out Color schemeColor))
                    target = schemeColor;
                target.a = slot.sourceColor.a;
                ApplyFullOverwrite(block, mat, target, slot.sourceColor);
            }

            ren.SetPropertyBlock(block, slot.matIndex);
        }

        bool fullParticles = uniqueFlatSrc.Count > 0 && CountTextured(slots) == 0;
        if (!SkipPlayerSkin.ShouldSkipRoot(root))
            ApplyParticlesAndTrails(root, scheme, fullParticles);
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

    static int QuantizeColor(Color c)
    {
        int r = Mathf.Clamp(Mathf.RoundToInt(c.r * 24f), 0, 24);
        int g = Mathf.Clamp(Mathf.RoundToInt(c.g * 24f), 0, 24);
        int b = Mathf.Clamp(Mathf.RoundToInt(c.b * 24f), 0, 24);
        return (r << 10) | (g << 5) | b;
    }

    /// <summary>
    /// Map each distinct authored flat onto scheme slots 1-10.
    /// The most saturated part (the character's identity color) gets color 1;
    /// darker parts take shadow/outline slots and lighter parts take highlights.
    /// </summary>
    static Dictionary<int, Color> AssignSchemeToUniques(Dictionary<int, Color> uniqueFlatSrc, Color[] scheme)
    {
        Dictionary<int, Color> result = new Dictionary<int, Color>();
        if (uniqueFlatSrc == null || uniqueFlatSrc.Count == 0 || scheme == null || scheme.Length == 0)
            return result;

        List<KeyValuePair<int, Color>> list = new List<KeyValuePair<int, Color>>(uniqueFlatSrc);
        if (list.Count == 1)
        {
            result[list[0].Key] = scheme[0];
            return result;
        }

        list.Sort((a, b) => Luminance(a.Value).CompareTo(Luminance(b.Value)));

        int mainIdx = list.Count / 2;
        float bestSat = -1f;
        for (int i = 0; i < list.Count; i++)
        {
            Color.RGBToHSV(list[i].Value, out _, out float sat, out _);
            if (sat > bestSat)
            {
                bestSat = sat;
                mainIdx = i;
            }
        }
        if (bestSat < 0.08f)
            mainIdx = list.Count / 2;

        result[list[mainIdx].Key] = scheme[0];

        // Remaining slots, dark → light (skip the main brand color)
        int[] darkSlots = { 9, 1, 2, 8, 3 };
        int[] lightSlots = { 6, 7, 4, 5 };

        int darkCount = mainIdx;
        for (int i = 0; i < darkCount; i++)
        {
            float t = darkCount <= 1 ? 0f : (float)i / (darkCount - 1);
            int si = Mathf.Clamp(Mathf.RoundToInt(t * (darkSlots.Length - 1)), 0, darkSlots.Length - 1);
            int slot = darkSlots[si];
            result[list[i].Key] = scheme[Mathf.Clamp(slot, 0, scheme.Length - 1)];
        }

        int lightCount = list.Count - mainIdx - 1;
        for (int i = 0; i < lightCount; i++)
        {
            float t = lightCount <= 1 ? 1f : (float)i / (lightCount - 1);
            int si = Mathf.Clamp(Mathf.RoundToInt(t * (lightSlots.Length - 1)), 0, lightSlots.Length - 1);
            int slot = lightSlots[si];
            result[list[mainIdx + 1 + i].Key] = scheme[Mathf.Clamp(slot, 0, scheme.Length - 1)];
        }

        return result;
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

    static void ApplyParticlesAndTrails(GameObject root, Color[] scheme, bool fullOverwrite)
    {
        Color skin = scheme != null && scheme.Length > 0 ? scheme[0] : Color.white;
        Color highlight = scheme != null && scheme.Length > 4 ? scheme[4] : Color.Lerp(skin, Color.white, 0.35f);
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
                    Color b = WithAlpha(highlight, g.colorMax.a);
                    main.startColor = new ParticleSystem.MinMaxGradient(a, b);
                }
                else
                {
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        MultiplyTint(g.colorMin, skin, strength),
                        MultiplyTint(g.colorMax, highlight, strength));
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
