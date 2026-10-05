using UnityEngine;

/// <summary>
/// Resolves per-player skin colors (for duplicate character picks) and lightly tints
/// character / lifeline / projectile renderers — textures stay; only color/emission are nudged.
/// </summary>
public static class PlayerSkin
{
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static readonly int TintColorId = Shader.PropertyToID("_TintColor");

    /// <summary>How strongly the skin hue multiplies albedo (0 = original, 1 = full skin multiply).</summary>
    const float AlbedoTintStrength = 0.45f;
    /// <summary>How strongly existing emission is hue-shifted (never replaces emission maps).</summary>
    const float EmissionTintStrength = 0.40f;

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
        // Main + phase-locked variants
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
        // Every skin taken by same-character clones — keep current
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

        // Keep current if already unique
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

        // Overflow: more clones than colors — leave as-is
        p.skinColorIndex = current;
    }

    public static void Apply(GameObject root, Player p, Database db = null)
    {
        if (root == null || p == null)
            return;

        Apply(root, GetColor(p, db));
    }

    /// <summary>
    /// Soft color tint only — albedo/emission maps and textures are left on the material.
    /// Uses MaterialPropertyBlock so shared materials are never replaced.
    /// </summary>
    public static void Apply(GameObject root, Color skin)
    {
        if (root == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer ren = renderers[r];
            if (ren == null)
                continue;

            // Particle meshes tinted via ParticleSystem.startColor below
            if (ren is ParticleSystemRenderer)
                continue;

            Material[] mats = ren.sharedMaterials;
            if (mats == null)
                continue;

            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if (mat == null)
                    continue;

                ren.GetPropertyBlock(block, m);

                // Albedo tint multiplies with _MainTex / base maps — textures stay visible
                if (mat.HasProperty(BaseColorId))
                {
                    Color src = mat.GetColor(BaseColorId);
                    block.SetColor(BaseColorId, MultiplyTint(src, skin, AlbedoTintStrength));
                }

                if (mat.HasProperty(ColorId))
                {
                    Color src = mat.GetColor(ColorId);
                    block.SetColor(ColorId, MultiplyTint(src, skin, AlbedoTintStrength));
                }

                if (mat.HasProperty(TintColorId))
                {
                    Color src = mat.GetColor(TintColorId);
                    block.SetColor(TintColorId, MultiplyTint(src, skin, AlbedoTintStrength));
                }

                // Only nudge existing emission — never paint solid skin over emission maps
                if (mat.HasProperty(EmissionId))
                {
                    Color srcEmit = mat.GetColor(EmissionId);
                    if (srcEmit.maxColorComponent > 0.001f)
                    {
                        Color tinted = MultiplyTint(srcEmit, skin, EmissionTintStrength);
                        block.SetColor(EmissionId, tinted);
                    }
                }

                ren.SetPropertyBlock(block, m);
            }
        }

        // Soft particle tint (keeps gradient structure when using startColor)
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
                main.startColor = MultiplyTint(g.color, skin, 0.55f);
            }
            else if (g.mode == ParticleSystemGradientMode.TwoColors)
            {
                main.startColor = new ParticleSystem.MinMaxGradient(
                    MultiplyTint(g.colorMin, skin, 0.55f),
                    MultiplyTint(g.colorMax, skin, 0.55f));
            }
            // Gradient modes left alone so authored particle textures/gradients stay
        }

        TrailRenderer[] trails = root.GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trails.Length; i++)
        {
            TrailRenderer tr = trails[i];
            if (tr == null)
                continue;
            Color start = MultiplyTint(tr.startColor, skin, 0.5f);
            Color end = MultiplyTint(tr.endColor, skin, 0.5f);
            tr.startColor = start;
            tr.endColor = end;
        }

        LineRenderer[] lines = root.GetComponentsInChildren<LineRenderer>(true);
        for (int i = 0; i < lines.Length; i++)
        {
            LineRenderer lr = lines[i];
            if (lr == null)
                continue;
            lr.startColor = MultiplyTint(lr.startColor, skin, 0.5f);
            lr.endColor = MultiplyTint(lr.endColor, skin, 0.5f);
        }
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
