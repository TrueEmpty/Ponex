using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registers unique themed fields + builder part palette (author #True Empty).
/// Generic retexture levels are purged; part assets stay available for Builder.
/// </summary>
public static class FieldCatalog
{
    const string Author = "#True Empty";
    static bool registered;

    static readonly string[] RemoveLevelNames =
    {
        "Master Pong Court",
        "Star Nursery",
        "Ember Yard",
        "Iyolit Candlehall",
        "Celarus Sundial",
        "Yuotay Dojo",
        "Guard Bastion",
        "Sunshine Plaza",
        "Garmen Scrap Pit",
        "Carrarow's Tide",
        "Trigger Circuit",
        "Tic Machine Floor",
        "Bahrue Coliseum",
        "Old Kingdom Courtyard",
    };

    public static void EnsureRegistered(Database db)
    {
        if (db == null)
            return;
        if (db.fields == null)
            db.fields = new List<Field>();
        if (db.parts == null)
            db.parts = new List<Part>();

        // Always purge generic levels (handles prior session registration)
        PurgeLevels(db);

        PrefabKit kit = ResolvePrefabs(db);
        if (kit.wall == null)
        {
            Debug.LogWarning("[FieldCatalog] No Wall prefab found — skipping field registration.");
            registered = true;
            return;
        }

        // Builder palette — keep assets even when not used as full levels
        RegisterPartTemplate(db, "Kingdom Stone", "Wall", kit.kingdom ?? kit.wall,
            new Color(0.55f, 0.52f, 0.48f), true, FieldTextureFactory.KingdomStone());
        RegisterPartTemplate(db, "Castle Battlement", "Wall", kit.battlement ?? kit.wall,
            new Color(0.42f, 0.4f, 0.38f), true, FieldTextureFactory.KingdomStone());
        RegisterPartTemplate(db, "Space Void", "Wall", kit.space ?? kit.wall,
            new Color(0.85f, 0.9f, 1f), true, FieldTextureFactory.SpaceVoidWall());
        RegisterPartTemplate(db, "Tide Log", "Wall", kit.log ?? kit.wall,
            new Color(1f, 1f, 1f), true, FieldTextureFactory.TideBark());
        RegisterPartTemplate(db, "Ember Brick", "Wall", kit.ember ?? kit.wall,
            new Color(0.75f, 0.28f, 0.1f), true, null);
        RegisterPartTemplate(db, "Circuit Panel", "Wall", kit.circuit ?? kit.wall,
            new Color(0.1f, 0.7f, 0.85f), true, null);
        RegisterPartTemplate(db, "Dojo Wood", "Wall", kit.dojo ?? kit.wall,
            new Color(0.55f, 0.35f, 0.2f), true, null);
        RegisterPartTemplate(db, "Scrap Plate", "Wall", kit.scrap ?? kit.wall,
            new Color(0.45f, 0.5f, 0.35f), true, null);
        RegisterPartTemplate(db, "Coliseum Stone", "Wall", kit.coliseum ?? kit.wall,
            new Color(0.7f, 0.62f, 0.48f), true, null);
        RegisterPartTemplate(db, "Animated Water", "Effect", kit.water ?? kit.stars ?? kit.wall,
            new Color(0.15f, 0.45f, 0.7f), false, null);
        RegisterPartTemplate(db, "Courtyard Pavers", "Effect", kit.pavers ?? kit.stars ?? kit.wall,
            new Color(1f, 1f, 1f), false, FieldTextureFactory.KingdomPavers());

        // Build unique levels once — rebuilding every Awake/Start hitch the main menu
        if (!registered
            || FieldNeedsGeometry(db, "Nari's Void Orbit")
            || FieldNeedsGeometry(db, "Kingdom of Nuoryn")
            || FieldNeedsGeometry(db, "Deep Harbor")
            || FieldNeedsGeometry(db, "Marajie Dessert"))
        {
            AddOrReplace(db, BuildNariVoidOrbit(kit));
            AddOrReplace(db, BuildKingdomOfNuoryn(kit));
            AddOrReplace(db, BuildDeepHarbor(kit));
            AddOrReplace(db, BuildMarajieDessert(kit));
            registered = true;
        }
    }

    static bool FieldNeedsGeometry(Database db, string name)
    {
        if (db.fields == null)
            return true;
        Field f = db.fields.Find(x => x != null && x.name == name);
        return f == null || f.parts == null || f.parts.Count == 0;
    }

    static void PurgeLevels(Database db)
    {
        for (int i = db.fields.Count - 1; i >= 0; i--)
        {
            Field f = db.fields[i];
            if (f == null || string.IsNullOrEmpty(f.name))
            {
                db.fields.RemoveAt(i);
                continue;
            }
            for (int r = 0; r < RemoveLevelNames.Length; r++)
            {
                if (f.name.Equals(RemoveLevelNames[r], System.StringComparison.OrdinalIgnoreCase))
                {
                    db.fields.RemoveAt(i);
                    break;
                }
            }
        }
    }

    struct PrefabKit
    {
        public GameObject wall;
        public GameObject log;
        public GameObject stars;
        public GameObject kingdom;
        public GameObject battlement;
        public GameObject space;
        public GameObject ember;
        public GameObject circuit;
        public GameObject dojo;
        public GameObject scrap;
        public GameObject coliseum;
        public GameObject water;
        public GameObject pavers;
    }

    enum BorderStyle
    {
        Uniform,
        CastleUneven,
        Logs,
        SpaceFlat
    }

    static PrefabKit ResolvePrefabs(Database db)
    {
        PrefabKit kit = new PrefabKit();
        for (int i = 0; i < db.fields.Count; i++)
        {
            Field f = db.fields[i];
            if (f == null || f.parts == null)
                continue;
            for (int p = 0; p < f.parts.Count; p++)
            {
                Part part = f.parts[p];
                if (part == null || part.prefab == null)
                    continue;
                string n = part.name != null ? part.name.ToLowerInvariant() : "";
                string t = part.type != null ? part.type.ToLowerInvariant() : "";
                if (kit.wall == null && (n == "wall" || t == "wall"))
                    kit.wall = part.prefab;
                if (kit.log == null && n.Contains("log"))
                    kit.log = part.prefab;
                if (kit.stars == null && (n.Contains("star") || t == "effect"))
                    kit.stars = part.prefab;
            }
        }

        kit.kingdom = Resources.Load<GameObject>("Parts/Wall/Kingdom Stone") ?? kit.wall;
        kit.battlement = Resources.Load<GameObject>("Parts/Wall/Castle Battlement") ?? kit.wall;
        kit.space = Resources.Load<GameObject>("Parts/Wall/Space Void") ?? kit.wall;
        kit.ember = Resources.Load<GameObject>("Parts/Wall/Ember Brick") ?? kit.wall;
        kit.circuit = Resources.Load<GameObject>("Parts/Wall/Circuit Panel") ?? kit.wall;
        kit.dojo = Resources.Load<GameObject>("Parts/Wall/Dojo Wood") ?? kit.wall;
        kit.scrap = Resources.Load<GameObject>("Parts/Wall/Scrap Plate") ?? kit.wall;
        kit.coliseum = Resources.Load<GameObject>("Parts/Wall/Coliseum Stone") ?? kit.wall;
        kit.water = Resources.Load<GameObject>("Parts/Effect/Animated Water") ?? kit.stars;
        kit.pavers = Resources.Load<GameObject>("Parts/Effect/Courtyard Pavers") ?? kit.stars;
        kit.log = Resources.Load<GameObject>("Parts/Wall/Tide Log") ?? kit.log;
        return kit;
    }

    static void RegisterPartTemplate(Database db, string name, string type, GameObject prefab, Color color, bool border, Texture tex)
    {
        if (prefab == null)
            return;
        Part existing = db.parts.Find(x => x != null && x.name == name);
        if (existing != null)
        {
            if (tex != null)
                existing.material = tex;
            existing.material_Color = color;
            return;
        }

        Part p = new Part();
        p.name = name;
        p.type = type;
        p.prefab = prefab;
        p.size = Vector3.one;
        p.material_Color = color;
        p.material = tex;
        p.canBeBorder = border;
        p.canMove = true;
        p.isHazard = false;
        p.CreateUID();
        db.parts.Add(p);
    }

    static void AddOrReplace(Database db, Field field)
    {
        if (field == null)
            return;
        int idx = db.fields.FindIndex(x => x != null && x.name == field.name);
        if (idx >= 0)
            db.fields[idx] = field;
        else
            db.fields.Add(field);
    }

    static Field BuildNariVoidOrbit(PrefabKit kit)
    {
        Field f = BuildSquareField(
            "Nari's Void Orbit", 12, kit.space ?? kit.wall,
            new Color(0.9f, 0.95f, 1f), new Color(0f, 0f, 0.01f),
            kit.stars, new Color(0.02f, 0.02f, 0.05f),
            BorderStyle.SpaceFlat, 1f,
            FieldTextureFactory.SpaceVoidWall(), null);
        f.backgroundMatallic = 0.2f;
        f.backgroundSmoothness = 0.15f;
        return f;
    }

    static Field BuildKingdomOfNuoryn(PrefabKit kit)
    {
        Field f = BuildSquareField(
            "Kingdom of Nuoryn", 18, kit.kingdom ?? kit.wall,
            new Color(1f, 1f, 1f), new Color(0.45f, 0.4f, 0.32f),
            kit.pavers ?? kit.stars, new Color(1f, 1f, 1f),
            BorderStyle.CastleUneven, 1f,
            FieldTextureFactory.KingdomStone(), FieldTextureFactory.KingdomPavers());
        f.backgroundMaterial = FieldTextureFactory.KingdomPavers();
        f.tilling = new Vector2(4f, 4f);
        f.backgroundMatallic = 0.25f;
        f.backgroundSmoothness = 0.35f;

        // Fixed central tower (intangible hazard — same spot every load)
        GameObject wallPf = kit.kingdom ?? kit.wall;
        Part tower = FieldPartFactory.MakeHazardPlaceholder(
            "Guard Tower", wallPf, Vector3.zero, new Vector3(2.2f, 2.2f, 2.4f),
            new Color(0.5f, 0.48f, 0.44f));
        tower.isHazard = true;
        tower.tangible = false; // fixed
        tower.material = FieldTextureFactory.KingdomStone();
        f.parts.Add(tower);

        // Tangible hazards — random count/position via spawn range each load
        float half = (f.size + 10) * 0.5f;
        AddTangibleHazard(f, "Watch Post", wallPf, half, new Vector3(0.55f, 1.2f, 0.55f),
            new Color(0.5f, 0.48f, 0.42f), 2, 4);
        AddTangibleHazard(f, "Castle Keep", wallPf, half, new Vector3(2.8f, 0.7f, 1f),
            new Color(0.55f, 0.52f, 0.48f), 1, 2);
        AddTangibleHazard(f, "Wagon", wallPf, half, new Vector3(0.7f, 0.4f, 0.45f),
            new Color(0.4f, 0.28f, 0.14f), 1, 3);

        return f;
    }

    static void AddTangibleHazard(Field f, string name, GameObject prefab, float half, Vector3 size, Color color, int minCount, int maxCount)
    {
        // Store one template; FieldHazardSpawner expands random count into spawn-range instances
        Part p = FieldPartFactory.MakeHazardPlaceholder(name, prefab, Vector3.zero, size, color);
        p.isHazard = true;
        p.tangible = true;
        p.spawnRange = new Vector2(half * 0.65f, half * 0.65f);
        p.material = FieldTextureFactory.KingdomStone();
        // Encode min/max count in components for the spawner
        p.components = new List<string> { "spawnCount#" + minCount + "#" + maxCount };
        f.parts.Add(p);
    }

    static Field BuildDeepHarbor(PrefabKit kit)
    {
        // No floor effect quad — OceanWaterTerrain fills the background at runtime
        Field f = BuildSquareField(
            "Deep Harbor", 22, kit.log ?? kit.wall,
            new Color(1f, 1f, 1f), new Color(0f, 0f, 0f, 0f),
            null, Color.clear,
            BorderStyle.Logs, 1.15f,
            FieldTextureFactory.TideBark(), null);
        f.backgroundColor = new Color(0f, 0f, 0f, 0f);
        f.backgroundMatallic = 0f;
        f.backgroundSmoothness = 0f;
        return f;
    }

    static Field BuildMarajieDessert(PrefabKit kit)
    {
        Texture stone = Database.instance != null
            ? Database.instance.WallTextureFor("Marajie Dessert")
            : null;
        Field f = BuildSquareField(
            "Marajie Dessert", 22, kit.wall,
            Color.white, new Color(0.82f, 0.68f, 0.42f),
            null, Color.clear,
            BorderStyle.Uniform, 1f,
            stone, null);
        f.backgroundMatallic = 0.2f;
        f.backgroundSmoothness = 0.62f;
        f.tilling = new Vector2(90f, 90f);
        return f;
    }

    static Field BuildSquareField(
        string name,
        int size,
        GameObject wallPrefab,
        Color wallColor,
        Color backgroundColor,
        GameObject effectPrefab,
        Color effectColor,
        BorderStyle style,
        float thicknessScale,
        Texture wallTex,
        Texture effectTex)
    {
        Field f = new Field(true);
        f.name = name;
        f.arthur = Author;
        f.size = size;
        f.backgroundColor = backgroundColor;
        f.backgroundMatallic = 0.55f;
        f.backgroundSmoothness = 0.4f;
        f.CreateUID();
        f.parts = new List<Part>();

        float half = (size + 10) * 0.5f;
        System.Random rng = new System.Random(StableSeed(name));

        if (effectPrefab != null)
        {
            Part fx = FieldPartFactory.MakeEffectPart(
                effectTex != null ? "Courtyard Pavers" : "Ground FX",
                effectPrefab,
                new Vector3(0f, 0f, 0.5f),
                new Vector3(half * 2f, half * 2f, 1f),
                effectColor);
            fx.material = effectTex;
            fx.isHazard = false;
            fx.tangible = false;
            f.parts.Add(fx);
        }

        BuildBorder(f.parts, wallPrefab, half, wallColor, style, thicknessScale, rng, wallTex);
        return f;
    }

    static int StableSeed(string name)
    {
        unchecked
        {
            int h = 17;
            if (name != null)
            {
                for (int i = 0; i < name.Length; i++)
                    h = h * 31 + name[i];
            }
            return h;
        }
    }

    static float RngRange(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    static void BuildBorder(
        List<Part> parts, GameObject prefab, float half, Color color,
        BorderStyle style, float thicknessScale, System.Random rng, Texture wallTex)
    {
        int steps = Mathf.Max(2, Mathf.RoundToInt(half * 2f));
        for (int i = 0; i <= steps; i++)
        {
            float t = -half + i;
            AddBorderCell(parts, prefab, new Vector3(t, -half, 0f), color, style, thicknessScale, true, rng, wallTex);
            AddBorderCell(parts, prefab, new Vector3(t, half, 0f), color, style, thicknessScale, true, rng, wallTex);
        }

        for (int i = 1; i < steps; i++)
        {
            float t = -half + i;
            AddBorderCell(parts, prefab, new Vector3(-half, t, 0f), color, style, thicknessScale, false, rng, wallTex);
            AddBorderCell(parts, prefab, new Vector3(half, t, 0f), color, style, thicknessScale, false, rng, wallTex);
        }
    }

    static GameObject PrefabForPartName(string partName, GameObject fallback)
    {
        if (string.IsNullOrEmpty(partName))
            return fallback;
        GameObject loaded = Resources.Load<GameObject>("Parts/Wall/" + partName);
        return loaded != null ? loaded : fallback;
    }

    static void AddBorderCell(
        List<Part> parts,
        GameObject prefab,
        Vector3 pos,
        Color color,
        BorderStyle style,
        float thicknessScale,
        bool horizontalEdge,
        System.Random rng,
        Texture wallTex)
    {
        Vector3 scale = Vector3.one * thicknessScale;
        string name = "Wall";

        switch (style)
        {
            case BorderStyle.CastleUneven:
                name = rng.NextDouble() > 0.55 ? "Castle Battlement" : "Kingdom Stone";
                bool battlement = name.Contains("Battlement");
                scale = battlement
                    ? new Vector3(RngRange(rng, 0.85f, 1.15f), RngRange(rng, 1.15f, 1.7f), RngRange(rng, 0.9f, 1.2f))
                    : new Vector3(RngRange(rng, 1f, 1.4f), RngRange(rng, 0.9f, 1.25f), RngRange(rng, 1f, 1.35f));
                if (rng.NextDouble() > 0.7)
                {
                    if (Mathf.Abs(pos.x) >= Mathf.Abs(pos.y))
                        pos.x += Mathf.Sign(pos.x) * RngRange(rng, 0.15f, 0.45f);
                    else
                        pos.y += Mathf.Sign(pos.y) * RngRange(rng, 0.15f, 0.45f);
                }
                break;

            case BorderStyle.Logs:
                name = "Tide Log";
                scale = new Vector3(
                    horizontalEdge ? RngRange(rng, 1.1f, 1.6f) : RngRange(rng, 0.7f, 1.1f),
                    horizontalEdge ? RngRange(rng, 0.7f, 1.1f) : RngRange(rng, 1.1f, 1.6f),
                    RngRange(rng, 0.8f, 1.2f));
                break;

            case BorderStyle.SpaceFlat:
                name = "Space Void";
                scale = Vector3.one * thicknessScale; // flat, identical cells
                break;

            default:
                scale = Vector3.one * thicknessScale;
                break;
        }

        GameObject usePrefab = PrefabForPartName(name, prefab);
        Part part = FieldPartFactory.MakeBorderPart(name, usePrefab, pos, scale, color);
        part.material = wallTex;
        part.isHazard = false;
        part.tangible = false;
        parts.Add(part);
    }
}
