#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds TicWing.prefab from Tic character meshes/materials (Top / Piston / Paddle).
/// Menu: Ponex → Build Tic Wing Bumper Prefab
/// </summary>
public static class TicWingPrefabBuilder
{
    const string TicPrefabPath = "Assets/Prefabs/_Characters/Tic.prefab";
    const string OutPath = "Assets/_Temp Prefabs/Bumps/TicWing.prefab";
    const string OutPathAlt = "Assets/Prefabs/_Bump/TicWing.prefab";
    const string ResourcesPath = "Assets/Resources/TicWing.prefab";

    const string MatMain = "Assets/Materials/Colors/Bump/Tic.mat";
    const string MatInner = "Assets/Materials/Colors/Characters/Tic Piston.mat";
    const string MatWing = "Assets/Materials/Colors/Characters/Tic.mat";

    [MenuItem("Ponex/Build Tic Wing Bumper Prefab")]
    public static void Build()
    {
        GameObject tic = AssetDatabase.LoadAssetAtPath<GameObject>(TicPrefabPath);
        Material matMain = AssetDatabase.LoadAssetAtPath<Material>(MatMain);
        Material matInner = AssetDatabase.LoadAssetAtPath<Material>(MatInner);
        Material matWing = AssetDatabase.LoadAssetAtPath<Material>(MatWing);

        Mesh paddleMesh = null;
        if (tic != null)
        {
            MeshFilter[] filters = tic.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i] != null && filters[i].gameObject.name == "Paddle" && filters[i].sharedMesh != null)
                {
                    paddleMesh = filters[i].sharedMesh;
                    break;
                }
            }
        }

        GameObject root = new GameObject("TicWing");
        root.tag = "Paddle";
        root.layer = 3;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 0.8f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezePositionZ
            | RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationY;

        root.AddComponent<PlayerGrab>().playerIndex = -1;
        root.AddComponent<ClearAfterTheGame>();
        root.AddComponent<PassPlayerGrabToChildren>();

        TicBumper bumper = root.AddComponent<TicBumper>();
        bumper.loadGravity = 8f;
        bumper.launchedGravity = 1.4f;
        bumper.minLaunchSpeed = 5f;
        bumper.maxSpeed = 9f;

        SphereCollider hub = root.AddComponent<SphereCollider>();
        hub.radius = TicWingBumper.HubRadius;

        // Main body = same scale as normal Tic bumper visual
        GameObject main = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        main.name = "MainBody";
        main.transform.SetParent(root.transform, false);
        main.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        main.transform.localScale = new Vector3(TicWingBumper.BodyScale, 0.115f, TicWingBumper.BodyScale);
        Object.DestroyImmediate(main.GetComponent<Rigidbody>());
        Object.DestroyImmediate(main.GetComponent<Collider>());
        if (matMain != null)
            main.GetComponent<Renderer>().sharedMaterial = matMain;
        main.tag = "Paddle";
        main.AddComponent<PlayerGrab>().playerIndex = -1;

        GameObject inner = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        inner.name = "InnerBumper";
        inner.transform.SetParent(root.transform, false);
        inner.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        inner.transform.localScale = new Vector3(
            TicWingBumper.BodyScale * 0.92f, 0.09f, TicWingBumper.BodyScale * 0.35f);
        Object.DestroyImmediate(inner.GetComponent<Rigidbody>());
        Object.DestroyImmediate(inner.GetComponent<Collider>());
        if (matInner != null)
            inner.GetComponent<Renderer>().sharedMaterial = matInner;
        inner.tag = "Paddle";
        inner.AddComponent<PlayerGrab>().playerIndex = -1;

        Transform left = BuildWing("LeftWing", root.transform, new Vector3(-TicWingBumper.WingOffsetX, 0f, 0f), true, paddleMesh, matWing);
        Transform right = BuildWing("RightWing", root.transform, new Vector3(TicWingBumper.WingOffsetX, 0f, 0f), false, paddleMesh, matWing);
        left.gameObject.SetActive(false);
        right.gameObject.SetActive(false);

        TicWingBumper wing = root.AddComponent<TicWingBumper>();
        wing.mainBody = main.transform;
        wing.innerBumper = inner.transform;
        wing.leftWing = left;
        wing.rightWing = right;
        wing.flapThrust = 7.5f;
        wing.bothThrustMul = 1.55f;
        wing.turnTorque = 9f;
        wing.coastDrag = 0.92f;
        wing.flapDrag = 0.988f;
        wing.maxFlightSpeed = 8f;
        wing.floatGravity = 0.3f;
        wing.bayGravity = 8f;
        wing.flipAngle = 72f;
        wing.flipUpSpeed = 720f;
        wing.flipDownSpeed = 420f;
        wing.flipAxis = Vector3.right;

        EnsureFolder("Assets/_Temp Prefabs");
        EnsureFolder("Assets/_Temp Prefabs/Bumps");
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/_Bump");
        EnsureFolder("Assets/Resources");

        PrefabUtility.SaveAsPrefabAsset(root, OutPath);
        PrefabUtility.SaveAsPrefabAsset(root, OutPathAlt);
        PrefabUtility.SaveAsPrefabAsset(root, ResourcesPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TicWing] Built prefabs at " + OutPath + ", " + OutPathAlt + ", " + ResourcesPath);
    }

    static Transform BuildWing(string name, Transform parent, Vector3 localPos, bool leftSide, Mesh mesh, Material mat)
    {
        GameObject go;
        if (mesh != null)
        {
            go = new GameObject(name);
            go.transform.SetParent(parent, false);
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            if (mat != null)
                mr.sharedMaterial = mat;
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;
            go.transform.localScale = new Vector3(
                TicWingBumper.WingScale, TicWingBumper.WingScale, TicWingBumper.WingScaleZ);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(
                TicWingBumper.WingScale * 0.35f,
                TicWingBumper.WingScale * 0.25f,
                TicWingBumper.WingScaleZ * 2f);
            Object.DestroyImmediate(go.GetComponent<Rigidbody>());
            if (mat != null)
                go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        go.transform.localPosition = localPos;
        // Mesh long-axis is Z → yaw so paddles point left (-X) / right (+X)
        go.transform.localRotation = Quaternion.Euler(0f, leftSide ? -90f : 90f, 0f);
        go.tag = "Paddle";
        go.AddComponent<PlayerGrab>().playerIndex = -1;
        return go.transform;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string[] parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
#endif
