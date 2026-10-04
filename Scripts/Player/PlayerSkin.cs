using UnityEngine;

/// <summary>
/// Resolves per-player skin colors (for duplicate character picks) and tints
/// character / lifeline / projectile renderers + particle glows.
/// </summary>
public static class PlayerSkin
{
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static readonly int TintColorId = Shader.PropertyToID("_TintColor");

    /// <summary>Usable skin slots (skips trailing white CPU cursor color when present).</summary>
    public static int UsableColorCount(Database db)
    {
        if (db == null || db.playerColors == null || db.playerColors.Count == 0)
            return 0;

        int count = db.playerColors.Count;
        // Last entry is the white CPU cursor spare in Gameplay
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

    public static void Cycle(Player p, Database db = null)
    {
        if (p == null)
            return;

        db = db != null ? db : Database.instance;
        int usable = UsableColorCount(db);
        if (usable <= 0)
            return;

        int cur = EffectiveSkinIndex(p, db);
        p.skinColorIndex = (cur + 1) % usable;
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

        bool[] taken = new bool[usable];
        if (db.players != null)
        {
            for (int i = 0; i < db.players.Count; i++)
            {
                Player other = db.players[i];
                if (other == null || other == p)
                    continue;
                if (string.IsNullOrEmpty(other.name) || other.name != p.name)
                    continue;

                int oi = EffectiveSkinIndex(other, db);
                if (oi >= 0 && oi < usable)
                    taken[oi] = true;
            }
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

        p.skinColorIndex = prefer;
    }

    public static void Apply(GameObject root, Player p, Database db = null)
    {
        if (root == null || p == null)
            return;

        Apply(root, GetColor(p, db));
    }

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

            // ParticleSystemRenderer is handled via startColor below
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

                if (mat.HasProperty(BaseColorId))
                {
                    Color src = mat.GetColor(BaseColorId);
                    block.SetColor(BaseColorId, Blend(src, skin));
                }

                if (mat.HasProperty(ColorId))
                {
                    Color src = mat.GetColor(ColorId);
                    block.SetColor(ColorId, Blend(src, skin));
                }

                if (mat.HasProperty(TintColorId))
                {
                    Color src = mat.GetColor(TintColorId);
                    block.SetColor(TintColorId, Blend(src, skin));
                }

                if (mat.HasProperty(EmissionId))
                {
                    Color emit = skin * 1.35f;
                    emit.a = 1f;
                    block.SetColor(EmissionId, emit);
                }

                ren.SetPropertyBlock(block, m);
            }
        }

        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem ps = particles[i];
            if (ps == null)
                continue;

            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(skin);
        }

        TrailRenderer[] trails = root.GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trails.Length; i++)
        {
            TrailRenderer tr = trails[i];
            if (tr == null)
                continue;
            tr.startColor = skin;
            tr.endColor = new Color(skin.r, skin.g, skin.b, 0f);
        }

        LineRenderer[] lines = root.GetComponentsInChildren<LineRenderer>(true);
        for (int i = 0; i < lines.Length; i++)
        {
            LineRenderer lr = lines[i];
            if (lr == null)
                continue;
            lr.startColor = skin;
            lr.endColor = skin;
        }
    }

    static Color Blend(Color original, Color skin)
    {
        // Keep some of the mesh's shading, pull strongly toward the skin color
        Color blended = Color.Lerp(original, skin, 0.72f);
        blended.a = original.a;
        return blended;
    }
}
