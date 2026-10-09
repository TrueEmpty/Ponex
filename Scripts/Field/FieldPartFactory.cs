using UnityEngine;

/// <summary>Runtime helpers for building field part GameObjects (walls, hazards, FX).</summary>
public static class FieldPartFactory
{
    static Material sharedStandard;
    static MaterialPropertyBlock propertyBlock;

    public static GameObject MakePrimitivePart(
        string name,
        PrimitiveType type,
        Vector3 worldPos,
        Vector3 scale,
        Color color,
        string tag)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.tag = string.IsNullOrEmpty(tag) ? "Untagged" : tag;
        go.transform.position = worldPos;
        go.transform.localScale = scale;

        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            if (sharedStandard == null)
            {
                Shader s = Shader.Find("Standard");
                sharedStandard = s != null ? new Material(s) : r.sharedMaterial;
            }
            r.sharedMaterial = sharedStandard != null ? sharedStandard : r.sharedMaterial;
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
            propertyBlock.Clear();
            propertyBlock.SetColor("_Color", color);
            propertyBlock.SetFloat("_Metallic", 0.35f);
            propertyBlock.SetFloat("_Glossiness", 0.4f);
            r.SetPropertyBlock(propertyBlock);
        }

        PartInfo pi = go.GetComponent<PartInfo>();
        if (pi == null)
            pi = go.AddComponent<PartInfo>();
        pi.part = new Part
        {
            name = name,
            type = tag == "Walls" || tag == "Wall" ? "Wall" : "Hazard",
            position = worldPos,
            size = scale,
            material_Color = color,
            canBeBorder = tag == "Walls" || tag == "Wall",
            isHazard = true,
            tangible = false,
            prefab = go
        };

        return go;
    }

    public static Part MakeBorderPart(
        string name,
        GameObject prefab,
        Vector3 localPos,
        Vector3 localScale,
        Color color,
        float metallic = 0.5f,
        float smoothness = 0.45f)
    {
        Part p = new Part();
        p.name = name;
        p.type = "Wall";
        p.prefab = prefab;
        p.position = localPos;
        p.rotation = Vector3.zero;
        p.size = localScale;
        p.material_Color = color;
        p.material_Matallic = metallic;
        p.material_Smoothness = smoothness;
        p.canBeBorder = true;
        p.canMove = true;
        p.scaling = false;
        p.isHazard = false;
        p.tangible = false;
        p.CreateUID();
        return p;
    }

    public static Part MakeEffectPart(
        string name,
        GameObject prefab,
        Vector3 localPos,
        Vector3 localScale,
        Color color)
    {
        Part p = new Part();
        p.name = name;
        p.type = "Effect";
        p.prefab = prefab;
        p.position = localPos;
        p.rotation = Vector3.zero;
        p.size = localScale;
        p.material_Color = color;
        p.canBeBorder = false;
        p.canMove = false;
        p.setPos = true;
        p.scaling = true;
        p.isHazard = false;
        p.tangible = false;
        p.CreateUID();
        return p;
    }

    public static Part MakeHazardPlaceholder(
        string name,
        GameObject prefab,
        Vector3 localPos,
        Vector3 localScale,
        Color color)
    {
        Part p = new Part();
        p.name = name;
        p.type = "Hazard";
        p.prefab = prefab;
        p.position = localPos;
        p.rotation = Vector3.zero;
        p.size = localScale;
        p.material_Color = color;
        p.canBeBorder = false;
        p.canMove = true;
        p.isHazard = true;
        p.tangible = false;
        p.CreateUID();
        return p;
    }
}
