#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds Muri character / kunai prefabs, materials, and CharacterData.
/// Menu: Ponex → Build Muri Prefabs
/// </summary>
[InitializeOnLoad]
public static class MuriPrefabBuilder
{
    const string CharPath = "Assets/Prefabs/_Characters/Muri.prefab";
    const string KunaiPath = "Assets/Prefabs/_Bump/MuriKunai.prefab";
    const string KunaiResPath = "Assets/Resources/MuriKunai.prefab";
    const string AssetPath = "Assets/Resources/Characters/Muri.asset";
    const string BodyMatPath = "Assets/Materials/Colors/Characters/Muri.mat";
    const string ShieldMatPath = "Assets/Materials/Colors/Characters/Muri Shield.mat";
    const string KunaiMatPath = "Assets/Materials/Colors/Bump/Muri Kunai.mat";
    const string PortraitPath = "Assets/Textures/Characters/Portrait/Muri Wabbit.png";
    const string IconPath = "Assets/Textures/Characters/Icons/Muri Wabbit.png";
    const string ShieldTexPath = "Assets/Textures/Characters/Muri Shield.png";

    static MuriPrefabBuilder()
    {
        EditorApplication.delayCall += AutoBuildIfMissing;
    }

    static void AutoBuildIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (!File.Exists(CharPath) || !File.Exists(KunaiPath) || !File.Exists(AssetPath))
            Build();
    }

    [MenuItem("Ponex/Build Muri Prefabs")]
    public static void Build()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/_Characters");
        EnsureFolder("Assets/Prefabs/_Bump");
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Characters");
        EnsureFolder("Assets/Materials/Colors/Characters");
        EnsureFolder("Assets/Materials/Colors/Bump");

        Material bodyMat = CreateBodyMat();
        Material shieldMat = CreateShieldMat();
        Material kunaiMat = CreateKunaiMat();

        GameObject kunai = BuildKunai(kunaiMat);
        GameObject kunaiPrefab = PrefabUtility.SaveAsPrefabAsset(kunai, KunaiPath);
        PrefabUtility.SaveAsPrefabAsset(kunai, KunaiResPath);
        Object.DestroyImmediate(kunai);

        GameObject muri = BuildMuri(bodyMat, shieldMat, kunaiPrefab);
        GameObject muriPrefab = PrefabUtility.SaveAsPrefabAsset(muri, CharPath);
        Object.DestroyImmediate(muri);

        WriteCharacterData(muriPrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Muri] Built prefabs, materials, and CharacterData.");
    }

    static GameObject BuildMuri(Material bodyMat, Material shieldMat, GameObject kunaiPrefab)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Muri";
        root.tag = "Lifeline";
        root.layer = 3;
        root.transform.localScale = Vector3.one;
        Object.DestroyImmediate(root.GetComponent<SphereCollider>());

        SphereCollider bodyCol = root.AddComponent<SphereCollider>();
        bodyCol.radius = 0.22f;
        bodyCol.isTrigger = false;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 1.2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezePositionZ
            | RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationY;

        root.AddComponent<PlayerGrab>().playerIndex = -1;
        root.AddComponent<ClearAfterTheGame>();

        MeshRenderer bodyRend = root.GetComponent<MeshRenderer>();
        if (bodyRend != null && bodyMat != null)
            bodyRend.sharedMaterial = bodyMat;

        MuriMove move = root.AddComponent<MuriMove>();
        move.bodyRadius = 0.22f;
        MuriKunai kunai = root.AddComponent<MuriKunai>();
        kunai.kunaiPrefab = kunaiPrefab;
        MuriShield shield = root.AddComponent<MuriShield>();

        DamageOnTagHit dmg = root.AddComponent<DamageOnTagHit>();
        dmg.tagHit = "Ball";
        SetBallOwnerOnTagHit own = root.AddComponent<SetBallOwnerOnTagHit>();
        own.targetTags = new System.Collections.Generic.List<string> { "Ball" };

        GameObject shieldGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldGo.name = "Shield";
        shieldGo.tag = "Untagged";
        shieldGo.layer = 3;
        shieldGo.transform.SetParent(root.transform, false);
        shieldGo.transform.localScale = Vector3.one * 1.85f;
        Object.DestroyImmediate(shieldGo.GetComponent<Collider>());
        MeshRenderer shieldRend = shieldGo.GetComponent<MeshRenderer>();
        if (shieldRend != null && shieldMat != null)
            shieldRend.sharedMaterial = shieldMat;
        SkipPlayerSkin skip = shieldGo.AddComponent<SkipPlayerSkin>();
        skip.includeChildren = true;
        shield.visual = shieldGo.transform;

        GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arrow.name = "AimArrow";
        arrow.layer = 3;
        arrow.transform.SetParent(root.transform, false);
        arrow.transform.localPosition = new Vector3(0f, 0.42f, 0f);
        arrow.transform.localScale = new Vector3(0.08f, 0.28f, 0.08f);
        Object.DestroyImmediate(arrow.GetComponent<Collider>());
        MeshRenderer arrowRend = arrow.GetComponent<MeshRenderer>();
        if (arrowRend != null && kunaiPrefab != null)
        {
            MeshRenderer kRend = kunaiPrefab.GetComponentInChildren<MeshRenderer>();
            if (kRend != null)
                arrowRend.sharedMaterial = kRend.sharedMaterial;
        }
        kunai.aimArrow = arrow.transform;

        return root;
    }

    static GameObject BuildKunai(Material mat)
    {
        GameObject root = new GameObject("MuriKunai");
        root.tag = "Paddle";
        root.layer = 3;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.mass = 0.2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezePositionZ
            | RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationY;

        CapsuleCollider cap = root.AddComponent<CapsuleCollider>();
        cap.direction = 1;
        cap.radius = 0.04f;
        cap.height = 0.24f;
        cap.isTrigger = true;

        root.AddComponent<PlayerGrab>().playerIndex = -1;
        root.AddComponent<ClearAfterTheGame>();
        root.AddComponent<PassPlayerGrabToChildren>();
        SetBallOwnerOnTagHit own = root.AddComponent<SetBallOwnerOnTagHit>();
        own.targetTags = new System.Collections.Generic.List<string> { "Ball" };
        root.AddComponent<MuriKunaiProjectile>();

        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "Blade";
        blade.tag = "Paddle";
        blade.layer = 3;
        blade.transform.SetParent(root.transform, false);
        blade.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        blade.transform.localScale = new Vector3(0.05f, 0.18f, 0.022f);
        Object.DestroyImmediate(blade.GetComponent<Rigidbody>());
        SetupKunaiPart(blade, mat);

        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tail.name = "Tail";
        tail.tag = "Paddle";
        tail.layer = 3;
        tail.transform.SetParent(root.transform, false);
        tail.transform.localPosition = new Vector3(0f, -0.125f, 0f);
        tail.transform.localScale = Vector3.one * 0.11f;
        Object.DestroyImmediate(tail.GetComponent<Rigidbody>());
        SetupKunaiPart(tail, mat);

        return root;
    }

    static void SetupKunaiPart(GameObject part, Material mat)
    {
        Collider col = part.GetComponent<Collider>();
        if (col != null)
            col.isTrigger = false;
        if (mat != null)
        {
            Renderer rend = part.GetComponent<Renderer>();
            if (rend != null)
                rend.sharedMaterial = mat;
        }
        part.AddComponent<PlayerGrab>().playerIndex = -1;
        SetBallOwnerOnTagHit own = part.AddComponent<SetBallOwnerOnTagHit>();
        own.targetTags = new System.Collections.Generic.List<string> { "Ball" };
    }

    static Material CreateBodyMat()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(BodyMatPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, BodyMatPath);
        }
        mat.color = new Color(0.12f, 0.14f, 0.2f, 1f);
        mat.SetFloat("_Metallic", 0.25f);
        mat.SetFloat("_Glossiness", 0.45f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material CreateShieldMat()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(ShieldMatPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, ShieldMatPath);
        }
        Texture shieldTex = AssetDatabase.LoadAssetAtPath<Texture>(ShieldTexPath);
        mat.color = new Color(0.45f, 0.95f, 1f, 0.16f);
        if (shieldTex != null)
        {
            mat.SetTexture("_MainTex", shieldTex);
            mat.SetTexture("_EmissionMap", shieldTex);
        }
        mat.SetFloat("_Mode", 2f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.05f, 0.22f, 0.28f, 1f));
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.renderQueue = 3000;
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Glossiness", 0.72f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material CreateKunaiMat()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(KunaiMatPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, KunaiMatPath);
        }
        mat.color = new Color(0.18f, 0.55f, 0.95f, 1f);
        mat.SetFloat("_Metallic", 0.55f);
        mat.SetFloat("_Glossiness", 0.7f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void WriteCharacterData(GameObject muriPrefab)
    {
        CharacterData asset = AssetDatabase.LoadAssetAtPath<CharacterData>(AssetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<CharacterData>();
            AssetDatabase.CreateAsset(asset, AssetPath);
        }

        asset.rosterOrder = 14;
        asset.characterName = "Muri";
        asset.maxHealth = 1;
        asset.movementSpeed = 0f;
        asset.pushBack = 0f;
        asset.ignoreFacing = false;
        asset.bump = new Skill { amount = 10, max = 10, cost = 999, speed = 0 };
        asset.super = new Skill { amount = 3, max = 3, cost = 1, speed = 0 };
        asset.dash = new Skill { amount = 0, max = 0, cost = 0, speed = 0 };
        asset.character = new ObjectInfo
        {
            prefabs = muriPrefab,
            positionOffset = new Vector3(0f, 0.16f, 0f),
            rotationOffset = Vector3.zero
        };
        asset.lifeline = new ObjectInfo();
        asset.selector = null;
        asset.playerInfo = null;
        asset.superName = "Kunai Bind";
        asset.superDescription = "Throw up to 3 kunai, then blink to a stuck blade. Shield lasts 10s, absorbs one hit, and a kunai-to-ball hit restores up to 3s.";
        asset.portraitColor = new Color(0.15f, 0.55f, 0.95f, 1f);
        asset.portrait = AssetDatabase.LoadAssetAtPath<Texture>(PortraitPath);
        Texture icon = AssetDatabase.LoadAssetAtPath<Texture>(IconPath);
        asset.icon = icon != null ? icon : asset.portrait;
        asset.active = true;
        EditorUtility.SetDirty(asset);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        if (!string.IsNullOrEmpty(parent))
            AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
