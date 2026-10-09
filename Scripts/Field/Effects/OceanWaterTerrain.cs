using UnityEngine;

/// <summary>
/// Procedural wavy water plane behind the playfield (Deep Harbor).
/// Clears reliance on a flat blue background quad.
/// </summary>
public class OceanWaterTerrain : MonoBehaviour
{
    public int resolution = 48;
    public float size = 40f;
    public float waveHeight = 0.35f;
    public float waveSpeed = 1.1f;
    public float waveScale = 0.18f;

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
        water.size = Mathf.Max(24f, playHalf * 2.4f);
        water.Build();
        return water;
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

        float half = size * 0.5f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                int i = y * res + x;
                float px = Mathf.Lerp(-half, half, x / (float)(res - 1));
                float py = Mathf.Lerp(-half, half, y / (float)(res - 1));
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
        mf.sharedMesh = mesh;

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.08f, 0.35f, 0.55f, 0.92f);
        mat.SetFloat("_Metallic", 0.15f);
        mat.SetFloat("_Glossiness", 0.85f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.02f, 0.12f, 0.22f));
        // Soft transparency
        mat.SetFloat("_Mode", 3f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 2900;
        mr.sharedMaterial = mat;
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
