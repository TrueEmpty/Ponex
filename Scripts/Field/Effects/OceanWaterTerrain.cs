using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Procedural wavy water plane behind the playfield (Deep Harbor).
/// Sized to the camera so it fills past the left/right walls, with a dark blue
/// solid backdrop behind the waves.
/// </summary>
public class OceanWaterTerrain : MonoBehaviour
{
    public int resolution = 56;
    public float width = 80f;
    public float height = 40f;
    public float waveHeight = 0.35f;
    public float waveSpeed = 1.1f;
    public float waveScale = 0.18f;

    static readonly Color DeepNavy = new Color(0.02f, 0.06f, 0.16f, 1f);

    Mesh mesh;
    Vector3[] baseVerts;
    Vector3[] verts;
    MeshFilter mf;
    MeshRenderer mr;
    float nextMeshUpdateTime;
    const float MeshUpdateInterval = 1f / 30f;

    public static OceanWaterTerrain Create(Transform fieldRoot, float playHalf)
    {
        GameObject go = new GameObject("Ocean Water Terrain");
        go.transform.SetParent(fieldRoot, false);
        go.transform.localPosition = new Vector3(0f, 0f, 1.2f);
        go.transform.localRotation = Quaternion.identity;

        OceanWaterTerrain water = go.AddComponent<OceanWaterTerrain>();
        water.FitToCameraAndPlayfield(playHalf);
        water.BuildBackdrop();
        water.Build();
        return water;
    }

    void FitToCameraAndPlayfield(float playHalf)
    {
        float minHalf = Mathf.Max(12f, playHalf);
        height = minHalf * 2.6f;
        width = minHalf * 5.5f;

        Camera cam = Camera.main;
        if (cam == null)
            cam = FindAnyObjectByType<Camera>();
        if (cam == null)
            return;

        float dist = Mathf.Abs(transform.position.z - cam.transform.position.z);
        if (dist < 1f)
            dist = 20f;

        float halfH;
        float halfW;
        if (cam.orthographic)
        {
            halfH = cam.orthographicSize;
            halfW = halfH * cam.aspect;
        }
        else
        {
            halfH = dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            halfW = halfH * cam.aspect;
        }

        // Cover the full view, then push further past the side walls / screen edges
        height = Mathf.Max(height, halfH * 2.25f);
        width = Mathf.Max(width, halfW * 2.45f);
    }

    void BuildBackdrop()
    {
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "Ocean Backdrop";
        bg.transform.SetParent(transform, false);
        bg.transform.localPosition = new Vector3(0f, 0f, 1.1f);
        bg.transform.localRotation = Quaternion.identity;
        bg.transform.localScale = new Vector3(width * 1.5f, height * 1.5f, 1f);

        Collider col = bg.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        Renderer r = bg.GetComponent<Renderer>();
        if (r != null)
        {
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", DeepNavy);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", DeepNavy);
            mat.color = DeepNavy;
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

    }

    void Build()
    {
        mf = gameObject.AddComponent<MeshFilter>();
        mr = gameObject.AddComponent<MeshRenderer>();

        mesh = new Mesh { name = "OceanWaves" };
        mesh.MarkDynamic();
        int res = Mathf.Clamp(resolution, 16, 96);
        int count = res * res;
        baseVerts = new Vector3[count];
        verts = new Vector3[count];
        Vector2[] uvs = new Vector2[count];
        int[] tris = new int[(res - 1) * (res - 1) * 6];

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                int i = y * res + x;
                float px = Mathf.Lerp(-halfW, halfW, x / (float)(res - 1));
                float py = Mathf.Lerp(-halfH, halfH, y / (float)(res - 1));
                baseVerts[i] = new Vector3(px, py, 0f);
                verts[i] = baseVerts[i];
                uvs[i] = new Vector2(x / (float)(res - 1), y / (float)(res - 1));
            }
        }

        int t = 0;
        for (int y = 0; y < res - 1; y++)
        {
            for (int x = 0; x < res - 1; x++)
            {
                int i = y * res + x;
                tris[t++] = i;
                tris[t++] = i + res;
                tris[t++] = i + 1;
                tris[t++] = i + 1;
                tris[t++] = i + res;
                tris[t++] = i + res + 1;
            }
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.08f, 0.35f, 0.55f, 0.88f);
        mat.SetFloat("_Metallic", 0.15f);
        mat.SetFloat("_Glossiness", 0.85f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.02f, 0.12f, 0.22f));
        mat.SetFloat("_Mode", 3f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 2900;
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    void Update()
    {
        if (mesh == null || baseVerts == null)
            return;

        float now = Time.time;
        if (now < nextMeshUpdateTime)
            return;
        nextMeshUpdateTime = now + MeshUpdateInterval;

        float t = now * waveSpeed;
        for (int i = 0; i < baseVerts.Length; i++)
        {
            Vector3 b = baseVerts[i];
            float h =
                Mathf.Sin(b.x * waveScale * 6.2f + t) * waveHeight * 0.55f
                + Mathf.Cos(b.y * waveScale * 5.1f + t * 0.85f) * waveHeight * 0.4f
                + Mathf.PerlinNoise(b.x * waveScale + t * 0.15f, b.y * waveScale) * waveHeight;
            verts[i] = new Vector3(b.x, b.y, h);
        }
        mesh.vertices = verts;
        mesh.RecalculateNormals();
    }
}
