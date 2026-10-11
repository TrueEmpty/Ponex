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
    const float TexturedAlbedoTint = 0.16f;
    /// <summary>How strongly textured emission is hue-shifted.</summary>
    const float TexturedEmissionTint = 0.12f;
    /// <summary>Bahrue's blocks stay close to the authored texture.</summary>
    const float BlockAlbedoTint = 0.1f;
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

    /// <summary>skinColorIndex -1 with a character picked means that character's base color.</summary>
    public static bool UsesBaseColor(Player p)
    {
        return p != null && p.skinColorIndex < 0 && !string.IsNullOrEmpty(p.name);
    }

    public static Color BaseColor(string characterName, Database db = null)
    {
        switch (characterName)
        {
            case "Test": return new Color(0.12f, 0.28f, 0.95f);
            case "Trigger": return new Color(0.86f, 0.08f, 0.08f);
            case "Nari": return new Color(0.55f, 0.12f, 0.92f);
            case "Garmen": return new Color(0.18f, 0.78f, 0.12f);
            case "Gaurd": return new Color(0.48f, 0.10f, 0.90f);
            case "Tic": return new Color(0.95f, 0.48f, 0.06f);
        }

        db = db != null ? db : Database.instance;
        if (db != null && db.characters != null)
        {
            for (int i = 0; i < db.characters.Count; i++)
            {
                Characters c = db.characters[i];
                if (c == null || c.name != characterName)
                    continue;
                if (c.portraitColor.maxColorComponent > 0.02f)
                    return c.portraitColor;
            }
        }

        return Color.white;
    }

    public static bool ColorsClose(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float dbc = a.b - b.b;
        if (dr * dr + dg * dg + dbc * dbc < 0.045f)
            return true;

        Color.RGBToHSV(a, out float ha, out float sa, out float va);
        Color.RGBToHSV(b, out float hb, out float sb, out float vb);
        if (sa < 0.18f && sb < 0.18f)
            return Mathf.Abs(va - vb) < 0.15f;
        if (sa < 0.22f || sb < 0.22f)
            return false;

        float dh = Mathf.Abs(ha - hb);
        if (dh > 0.5f)
            dh = 1f - dh;
        return dh < 0.07f;
    }

    static bool SchemeAllowed(string characterName, Color scheme, Database db)
    {
        return !ColorsClose(BaseColor(characterName, db), scheme);
    }

    public static Color GetColor(Player p, Database db = null)
    {
        db = db != null ? db : Database.instance;
        if (p == null || db == null || db.playerColors == null || db.playerColors.Count == 0)
            return Color.white;

        if (UsesBaseColor(p))
            return BaseColor(p.name, db);

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

        if (UsesBaseColor(p))
            return SchemeForColor(BaseColor(p.name, db), db);

        int idx = EffectiveSkinIndex(p, db);
        if (idx < 0 || idx >= db.playerColors.Count)
            return null;

        PlayerColors pc = db.playerColors[idx];
        if (pc != null)
            pc.EnsureScheme();
        return pc;
    }

    static PlayerColors SchemeForColor(Color color, Database db)
    {
        PlayerColors pc = new PlayerColors { color = color };
        int usable = UsableColorCount(db);
        int best = -1;
        float bestD = float.MaxValue;
        for (int i = 0; i < usable; i++)
        {
            PlayerColors slot = db.playerColors[i];
            if (slot == null)
                continue;
            float d = ColorDistance(slot.color, color);
            if (d < bestD)
            {
                bestD = d;
                best = i;
            }
        }

        if (best >= 0)
            pc.sprite = db.playerColors[best].sprite;
        pc.EnsureScheme(true);
        return pc;
    }

    static float ColorDistance(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float dbc = a.b - b.b;
        return dr * dr + dg * dg + dbc * dbc;
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

    static bool ColorTakenByGroup(Player p, Color candidate, Database db)
    {
        if (p == null || db == null || db.players == null)
            return false;

        for (int i = 0; i < db.players.Count; i++)
        {
            Player other = db.players[i];
            if (other == null || other == p)
                continue;
            if (!SharesSkinGroup(other.name, p.name))
                continue;
            if (ColorsClose(GetColor(other, db), candidate))
                return true;
        }

        return false;
    }

    /// <summary>Base color first, then scheme slots that are not the same hue as that base.</summary>
    static List<int> SkinOptions(Player p, Database db)
    {
        List<int> options = new List<int>();
        if (p == null || string.IsNullOrEmpty(p.name))
            return options;

        options.Add(-1);
        int usable = UsableColorCount(db);
        for (int i = 0; i < usable; i++)
        {
            PlayerColors slot = db.playerColors[i];
            if (slot == null)
                continue;
            if (!SchemeAllowed(p.name, slot.color, db))
                continue;
            options.Add(i);
        }

        return options;
    }

    static Color ColorForOption(Player p, int option, Database db)
    {
        if (option < 0)
            return BaseColor(p.name, db);
        if (db == null || db.playerColors == null || option >= db.playerColors.Count || db.playerColors[option] == null)
            return BaseColor(p.name, db);
        return db.playerColors[option].color;
    }

    public static bool IsSkinTakenBySameCharacter(Player p, int skinIndex, Database db = null)
    {
        if (p == null)
            return false;

        db = db != null ? db : Database.instance;
        if (skinIndex < 0)
            return ColorTakenByGroup(p, BaseColor(p.name, db), db);

        int usable = UsableColorCount(db);
        if (skinIndex >= usable || db.playerColors == null || db.playerColors[skinIndex] == null)
            return false;

        return ColorTakenByGroup(p, db.playerColors[skinIndex].color, db);
    }

    /// <summary>
    /// Cycle to the next free skin for this character.
    /// direction &gt; 0 goes forward; direction &lt; 0 goes backward.
    /// The base color is included. Schemes close to that base are not.
    /// </summary>
    public static void Cycle(Player p, Database db = null, int direction = 1)
    {
        if (p == null)
            return;

        db = db != null ? db : Database.instance;
        List<int> options = SkinOptions(p, db);
        if (options.Count == 0)
            return;

        int dir = direction < 0 ? -1 : 1;
        int cur = UsesBaseColor(p) ? -1 : p.skinColorIndex;
        int pos = options.IndexOf(cur);
        if (pos < 0)
            pos = 0;

        for (int step = 1; step <= options.Count; step++)
        {
            int nextPos = pos + dir * step;
            nextPos %= options.Count;
            if (nextPos < 0)
                nextPos += options.Count;

            int next = options[nextPos];
            if (!ColorTakenByGroup(p, ColorForOption(p, next, db), db))
            {
                p.skinColorIndex = next;
                return;
            }
        }
    }

    /// <summary>
    /// Default to this character's base color.
    /// A duplicate (Celarus counts across her variations) gets the next free scheme instead.
    /// </summary>
    public static void AssignUniqueForCharacter(Player p, Database db = null)
    {
        if (p == null)
            return;

        db = db != null ? db : Database.instance;
        if (string.IsNullOrEmpty(p.name))
        {
            if (p.skinColorIndex < 0)
                p.skinColorIndex = Mathf.Max(0, p.index);
            return;
        }

        int usable = UsableColorCount(db);
        if (usable <= 0)
        {
            p.skinColorIndex = -1;
            return;
        }

        if (!ColorTakenByGroup(p, BaseColor(p.name, db), db))
        {
            p.skinColorIndex = -1;
            return;
        }

        for (int i = 0; i < usable; i++)
        {
            PlayerColors slot = db.playerColors[i];
            if (slot == null || !SchemeAllowed(p.name, slot.color, db))
                continue;
            if (!ColorTakenByGroup(p, slot.color, db))
            {
                p.skinColorIndex = i;
                return;
            }
        }

        p.skinColorIndex = -1;
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

                bool blocks = IsBahrueBlocks(mat);
                bool textured = HasAuthoredTexture(mat) || blocks;
                Color src = blocks ? Color.white : ReadAlbedo(mat);
                if (blocks)
                    src.a = ReadAlbedo(mat).a;
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
                bool blocks = IsBahrueBlocks(mat);
                ApplySoftTint(block, mat, skin, slot.sourceColor, blocks ? BlockAlbedoTint : TexturedAlbedoTint);
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

    static bool IsBahrueBlocks(Material mat)
    {
        return mat != null && mat.name.IndexOf("Bahrue Blocks", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static void ApplySoftTint(MaterialPropertyBlock block, Material mat, Color skin, Color src, float albedoStrength)
    {
        if (mat.HasProperty(BaseColorId))
            block.SetColor(BaseColorId, MultiplyTint(src, skin, albedoStrength));
        if (mat.HasProperty(ColorId))
        {
            Color c = IsBahrueBlocks(mat) || !mat.HasProperty(BaseColorId) ? src : mat.GetColor(ColorId);
            block.SetColor(ColorId, MultiplyTint(c, skin, albedoStrength));
        }
        if (mat.HasProperty(TintColorId))
        {
            Color c = mat.GetColor(TintColorId);
            block.SetColor(TintColorId, MultiplyTint(c, skin, albedoStrength));
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
