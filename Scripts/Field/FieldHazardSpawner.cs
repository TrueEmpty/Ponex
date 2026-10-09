using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns / places field hazards from Part flags (isHazard, tangible, spawnRange).
/// Honors <see cref="GameSettings.HazardsEnabled"/>.
/// </summary>
public static class FieldHazardSpawner
{
    public static void SpawnForField(Transform fieldRoot, Field field)
    {
        if (fieldRoot == null || field == null)
            return;

        GameSettings.EnsureLoaded();
        string name = field.name != null ? field.name.Trim() : "";

        // Level directors that are not hazard-gated (portal wrap always on for Nari)
        if (name.IndexOf("Nari", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Void", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Orbit", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            EnsureDirector<SpacePortalBounds>(fieldRoot);
        }

        if (name.IndexOf("Harbor", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Carrarow", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            SetupDeepHarborWater(fieldRoot, field);
            if (GameSettings.HazardsEnabled)
                EnsureDirector<OceanWindHazards>(fieldRoot);
        }

        ProcessAuthoredHazardParts(fieldRoot, field);

        if (!GameSettings.HazardsEnabled)
            return;

        // Kingdom of Nuoryn — extras only if authored templates missing (legacy)
        if (name.IndexOf("Nuoryn", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Old Kingdom", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (!HasAuthoredHazard(field, "Guard Tower"))
                EnsureDirector<GuardKingdomHazards>(fieldRoot);
        }
    }

    static bool HasAuthoredHazard(Field field, string nameContains)
    {
        if (field.parts == null)
            return false;
        for (int i = 0; i < field.parts.Count; i++)
        {
            Part p = field.parts[i];
            if (p == null || !p.isHazard)
                continue;
            if (p.name != null && p.name.IndexOf(nameContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    static void ProcessAuthoredHazardParts(Transform fieldRoot, Field field)
    {
        if (field.parts == null)
            return;

        bool hazardsOn = GameSettings.HazardsEnabled;
        float z = Database.instance != null ? Database.instance.FieldPlaySize : fieldRoot.position.z;
        List<Vector3> occupied = new List<Vector3>();

        // Reserve border / center clearance
        occupied.Add(Vector3.zero);

        for (int i = 0; i < field.parts.Count; i++)
        {
            Part p = field.parts[i];
            if (p == null)
                continue;

            bool hazard = p.isHazard
                || (p.type != null && (p.type.Equals("Hazard", System.StringComparison.OrdinalIgnoreCase)
                    || p.type.Equals("Hazards", System.StringComparison.OrdinalIgnoreCase)));

            if (!hazard)
                continue;

            string partName = p.name != null ? p.name.ToLowerInvariant() : "";
            if (partName.Contains("sky target"))
            {
                if (p.spawned != null)
                {
                    Object.Destroy(p.spawned);
                    p.spawned = null;
                }
                continue;
            }

            // Hide / destroy the template instance from LoadField
            if (p.spawned != null)
            {
                if (!hazardsOn)
                {
                    Object.Destroy(p.spawned);
                    p.spawned = null;
                    continue;
                }

                if (p.tangible)
                {
                    // Template is only a definition — remove the zero-pos instance
                    Object.Destroy(p.spawned);
                    p.spawned = null;
                    SpawnTangibleCopies(fieldRoot, p, z, occupied);
                }
                else
                {
                    // Intangible: fixed position/rotation every load
                    p.spawned.transform.localPosition = p.position;
                    p.spawned.transform.localEulerAngles = p.rotation;
                    p.spawned.transform.localScale = p.size;
                    p.spawned.SetActive(true);
                    SnapWorldZ(p.spawned, z);
                    occupied.Add(p.spawned.transform.position);
                    AttachBehaviourByName(p.spawned, p.name);
                    ApplyPartTexture(p.spawned, p);
                }
            }
            else if (hazardsOn && p.prefab != null)
            {
                if (p.tangible)
                    SpawnTangibleCopies(fieldRoot, p, z, occupied);
                else
                {
                    GameObject go = Object.Instantiate(p.prefab, fieldRoot);
                    go.transform.localPosition = p.position;
                    go.transform.localEulerAngles = p.rotation;
                    go.transform.localScale = p.size;
                    p.spawned = go;
                    SnapWorldZ(go, z);
                    occupied.Add(go.transform.position);
                    AttachBehaviourByName(go, p.name);
                    ApplyPartTexture(go, p);
                }
            }
        }
    }

    static void SpawnTangibleCopies(Transform fieldRoot, Part template, float z, List<Vector3> occupied)
    {
        int minC = 1;
        int maxC = 1;
        ParseSpawnCount(template, ref minC, ref maxC);
        int count = Random.Range(minC, maxC + 1);

        float rangeX = template.spawnRange.x > 0.1f ? template.spawnRange.x : 8f;
        float rangeY = template.spawnRange.y > 0.1f ? template.spawnRange.y : 8f;
        float minSep = Mathf.Max(2.8f, Mathf.Max(template.size.x, template.size.y) + 1.5f);

        for (int n = 0; n < count; n++)
        {
            Vector3 local = Vector3.zero;
            bool placed = false;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                local = new Vector3(
                    template.position.x + Random.Range(-rangeX, rangeX),
                    template.position.y + Random.Range(-rangeY, rangeY),
                    template.position.z);
                // Keep out of dead center (players / tower)
                if (local.sqrMagnitude < 9f)
                    continue;
                Vector3 world = fieldRoot.TransformPoint(local);
                world.z = z;
                if (TooClose(world, occupied, minSep))
                    continue;
                placed = true;
                break;
            }
            if (!placed)
                continue;

            GameObject go = SpawnHazardVisual(template, fieldRoot, local);
            SnapWorldZ(go, z);
            occupied.Add(go.transform.position);
            go.SetActive(true);
            AttachBehaviourByName(go, template.name);
            ApplyPartTexture(go, template);
        }
    }

    static GameObject SpawnHazardVisual(Part template, Transform fieldRoot, Vector3 localPos)
    {
        string n = template.name != null ? template.name.ToLowerInvariant() : "";
        Vector3 scale = template.size;
        PrimitiveType prim = PrimitiveType.Cube;

        if (n.Contains("watch"))
        {
            prim = PrimitiveType.Cylinder;
            float h = Random.Range(0.9f, 2.0f);
            float w = Random.Range(0.35f, 0.7f);
            scale = new Vector3(w, h, w);
        }
        else if (n.Contains("wagon"))
        {
            prim = PrimitiveType.Cube;
            scale = new Vector3(
                Random.Range(0.55f, 0.8f),
                Random.Range(0.3f, 0.45f),
                Random.Range(0.35f, 0.5f));
        }
        else if (n.Contains("keep") || n.Contains("castle"))
        {
            prim = PrimitiveType.Cube;
            // Skinny long wall remnants
            bool alongX = Random.value > 0.5f;
            scale = alongX
                ? new Vector3(Random.Range(2.4f, 3.4f), Random.Range(0.55f, 0.85f), Random.Range(0.7f, 1.1f))
                : new Vector3(Random.Range(0.55f, 0.85f), Random.Range(2.4f, 3.4f), Random.Range(0.7f, 1.1f));
        }
        else if (n.Contains("tower"))
        {
            prim = PrimitiveType.Cube;
            scale = template.size.sqrMagnitude > 0.01f ? template.size : new Vector3(2.2f, 2.2f, 2.4f);
        }

        GameObject go = FieldPartFactory.MakePrimitivePart(
            template.name, prim, fieldRoot.TransformPoint(localPos), scale,
            template.material_Color, "Obstacle");
        go.transform.SetParent(fieldRoot, true);
        go.transform.localPosition = localPos;
        go.transform.localEulerAngles = template.rotation;
        go.transform.localScale = scale;
        return go;
    }

    static void ParseSpawnCount(Part p, ref int minC, ref int maxC)
    {
        if (p.components == null)
            return;
        for (int i = 0; i < p.components.Count; i++)
        {
            string c = p.components[i];
            if (string.IsNullOrEmpty(c) || !c.StartsWith("spawnCount#", System.StringComparison.OrdinalIgnoreCase))
                continue;
            string[] bits = c.Split('#');
            if (bits.Length >= 3)
            {
                int.TryParse(bits[1], out minC);
                int.TryParse(bits[2], out maxC);
                minC = Mathf.Max(0, minC);
                maxC = Mathf.Max(minC, maxC);
            }
        }
    }

    static bool TooClose(Vector3 world, List<Vector3> occupied, float minSep)
    {
        for (int i = 0; i < occupied.Count; i++)
        {
            Vector3 o = occupied[i];
            o.z = world.z;
            if ((o - world).sqrMagnitude < minSep * minSep)
                return true;
        }
        return false;
    }

    static void SnapWorldZ(GameObject go, float z)
    {
        Vector3 p = go.transform.position;
        p.z = z;
        go.transform.position = p;
    }

    static void ApplyPartTexture(GameObject go, Part p)
    {
        if (go == null || p == null)
            return;
        Renderer r = go.GetComponent<Renderer>();
        if (r == null)
            return;
        FieldTextureFactory.ApplyAlbedo(r, p.material, p.material_Color, p.material_Matallic, p.material_Smoothness);
    }

    static void SetupDeepHarborWater(Transform fieldRoot, Field field)
    {
        // Hide flat background quad
        for (int i = 0; i < fieldRoot.childCount; i++)
        {
            Transform c = fieldRoot.GetChild(i);
            if (c != null && c.name.IndexOf("Background", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Renderer r = c.GetComponent<Renderer>();
                if (r != null)
                    r.enabled = false;
                c.gameObject.SetActive(false);
            }
        }

        float half = (field.size + 10) * 0.5f;
        if (fieldRoot.GetComponentInChildren<OceanWaterTerrain>(true) == null)
            OceanWaterTerrain.Create(fieldRoot, half);
    }

    static void AttachBehaviourByName(GameObject go, string name)
    {
        if (go == null || string.IsNullOrEmpty(name))
            return;
        string n = name.ToLowerInvariant();

        if (n.Contains("wagon"))
        {
            Ensure<BreakableHazard>(go).hitsToBreak = 4;
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null)
                rb = go.AddComponent<Rigidbody>();
            Ensure<WanderingWagon>(go);
        }
        else if (n.Contains("watch"))
        {
            Ensure<BreakableHazard>(go).hitsToBreak = 5;
        }
        else if (n.Contains("castle") || n.Contains("keep"))
        {
            Ensure<BreakableHazard>(go).hitsToBreak = 8;
        }
        else if (n.Contains("tower") || n.Contains("cannon"))
        {
            Ensure<GuardCannonTower>(go);
        }
        else if (n.Contains("ice") || n.Contains("berg"))
        {
            Ensure<BreakableHazard>(go).hitsToBreak = 6;
        }
        else if (n.Contains("drift") || n.Contains("float log"))
        {
            Ensure<DriftingLogHazard>(go);
        }
    }

    static T Ensure<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null)
            c = go.AddComponent<T>();
        return c;
    }

    static void EnsureDirector<T>(Transform fieldRoot) where T : Component
    {
        if (fieldRoot.GetComponent<T>() == null)
            fieldRoot.gameObject.AddComponent<T>();
    }
}
