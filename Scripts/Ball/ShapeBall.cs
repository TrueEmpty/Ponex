using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Voxel morphing ball: cubes stay parented and lerp between fixed shape layouts.
/// Compound box colliders move with the cubes so bounce shape matches the visuals.
/// </summary>
[RequireComponent(typeof(BallInfo))]
public class ShapeBall : MonoBehaviour
{
    Database db;
    BallInfo bI;
    Renderer rootRenderer;
    SphereCollider rootSphere;

    [Tooltip("How many voxels around the silhouette.")]
    public int voxelCount = 24;

    [Tooltip("Overall radius of the shape in local units (before parent scale).")]
    public float shapeRadius = 0.48f;

    [Tooltip("Visual cube size.")]
    public float voxelSize = 0.16f;

    public float morphDuration = 1.35f;
    public Vector2 morphIntervalRange = new Vector2(3.5f, 8f);

    readonly List<Transform> voxels = new List<Transform>();
    readonly List<BoxCollider> voxelCols = new List<BoxCollider>();
    readonly List<Vector3> fromPos = new List<Vector3>();
    readonly List<Vector3> toPos = new List<Vector3>();
    readonly List<Vector3[]> shapes = new List<Vector3[]>();

    int shapeIndex;
    float morphT = 1f;
    float nextMorphAt;
    bool built;

    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();
        rootRenderer = GetComponent<Renderer>();
        rootSphere = GetComponent<SphereCollider>();

        // Ghost projection clones explode the voxel setup — keep Shape Ball self-contained
        if (bI != null)
            bI.projectionOn = false;

        BuildVoxels();
        ScheduleNext();
    }

    void Update()
    {
        if (!built)
            return;

        if (morphT < 1f)
        {
            morphT = Mathf.Clamp01(morphT + Time.deltaTime / Mathf.Max(0.05f, morphDuration));
            float s = morphT * morphT * (3f - 2f * morphT);
            for (int i = 0; i < voxels.Count; i++)
            {
                if (voxels[i] == null)
                    continue;
                voxels[i].localPosition = Vector3.Lerp(fromPos[i], toPos[i], s);
            }
        }

        if (db == null || !db.gameStart || bI == null || !bI.ballReady)
            return;

        if (morphT < 1f || Time.time < nextMorphAt)
            return;

        int next = Random.Range(0, shapes.Count);
        if (next == shapeIndex && shapes.Count > 1)
            next = (next + 1) % shapes.Count;
        BeginMorph(next);
        ScheduleNext();
    }

    void ScheduleNext()
    {
        nextMorphAt = Time.time + Random.Range(morphIntervalRange.x, morphIntervalRange.y);
    }

    void OnDestroy()
    {
        for (int i = 0; i < voxels.Count; i++)
        {
            if (voxels[i] != null)
                Destroy(voxels[i].gameObject);
        }
        voxels.Clear();
        voxelCols.Clear();
        if (rootRenderer != null)
            rootRenderer.enabled = true;
        if (rootSphere != null)
            rootSphere.enabled = true;
    }

    void BuildVoxels()
    {
        if (built)
            return;
        built = true;

        if (rootRenderer != null)
            rootRenderer.enabled = false;
        if (rootSphere != null)
            rootSphere.enabled = false;

        Material mat = rootRenderer != null ? rootRenderer.sharedMaterial : null;
        int n = Mathf.Max(12, voxelCount);

        for (int i = 0; i < n; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "ShapeVoxel";
            cube.transform.SetParent(transform, false);
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = Vector3.one * voxelSize;
            cube.layer = gameObject.layer;

            // Keep BoxCollider for compound collision with parent Rigidbody
            BoxCollider box = cube.GetComponent<BoxCollider>();
            if (box != null)
            {
                box.isTrigger = false;
                voxelCols.Add(box);
            }

            if (mat != null)
            {
                Renderer r = cube.GetComponent<Renderer>();
                if (r != null)
                    r.sharedMaterial = mat;
            }

            voxels.Add(cube.transform);
            fromPos.Add(Vector3.zero);
            toPos.Add(Vector3.zero);
        }

        // Deterministic layouts — same index always maps to the same angular slot
        shapes.Add(BuildCircle(n));
        shapes.Add(BuildSquare(n));
        shapes.Add(BuildTriangle(n));
        shapes.Add(BuildDiamond(n));
        shapes.Add(BuildCross(n));
        shapes.Add(BuildStar(n));

        ApplyImmediate(0);
    }

    void BeginMorph(int index)
    {
        shapeIndex = index;
        Vector3[] target = shapes[shapeIndex];
        for (int i = 0; i < voxels.Count; i++)
        {
            fromPos[i] = voxels[i].localPosition;
            toPos[i] = target[i];
        }
        morphT = 0f;
    }

    void ApplyImmediate(int index)
    {
        shapeIndex = index;
        Vector3[] target = shapes[index];
        for (int i = 0; i < voxels.Count; i++)
        {
            Vector3 p = target[i];
            voxels[i].localPosition = p;
            fromPos[i] = p;
            toPos[i] = p;
        }
        morphT = 1f;
    }

    Vector3 Slot(float x, float y)
    {
        return new Vector3(x, y, 0f);
    }

    Vector3[] BuildCircle(int n)
    {
        Vector3[] pts = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n * Mathf.PI * 2f;
            // Outer ring + a few inner points for fill (every 4th)
            float rad = (i % 4 == 0) ? shapeRadius * 0.45f : shapeRadius;
            pts[i] = Slot(Mathf.Cos(t) * rad, Mathf.Sin(t) * rad);
        }
        return pts;
    }

    Vector3[] BuildSquare(int n)
    {
        Vector3[] pts = new Vector3[n];
        float half = shapeRadius;
        for (int i = 0; i < n; i++)
        {
            float u = i / (float)n;
            float edgeF = u * 4f;
            int edge = Mathf.Min(3, Mathf.FloorToInt(edgeF));
            float t = edgeF - edge;
            float x, y;
            switch (edge)
            {
                case 0: x = Mathf.Lerp(-half, half, t); y = half; break;
                case 1: x = half; y = Mathf.Lerp(half, -half, t); break;
                case 2: x = Mathf.Lerp(half, -half, t); y = -half; break;
                default: x = -half; y = Mathf.Lerp(-half, half, t); break;
            }
            // Pull every 5th voxel inward for thickness
            if (i % 5 == 0)
            {
                x *= 0.4f;
                y *= 0.4f;
            }
            pts[i] = Slot(x, y);
        }
        return pts;
    }

    Vector3[] BuildTriangle(int n)
    {
        Vector3[] pts = new Vector3[n];
        Vector3 a = Slot(0f, shapeRadius);
        Vector3 b = Slot(-shapeRadius * 0.9f, -shapeRadius * 0.65f);
        Vector3 c = Slot(shapeRadius * 0.9f, -shapeRadius * 0.65f);
        Vector3[] corners = { a, b, c };

        for (int i = 0; i < n; i++)
        {
            float u = i / (float)n;
            float edgeF = u * 3f;
            int edge = Mathf.Min(2, Mathf.FloorToInt(edgeF));
            float t = edgeF - edge;
            Vector3 p0 = corners[edge];
            Vector3 p1 = corners[(edge + 1) % 3];
            Vector3 p = Vector3.Lerp(p0, p1, t);
            if (i % 5 == 0)
                p *= 0.35f;
            pts[i] = p;
        }
        return pts;
    }

    Vector3[] BuildDiamond(int n)
    {
        Vector3[] pts = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n * Mathf.PI * 2f;
            float x = Mathf.Cos(t);
            float y = Mathf.Sin(t);
            float m = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
            Vector3 p = Slot(x, y) / Mathf.Max(0.01f, m) * shapeRadius;
            // Rotate 45°
            float rx = (p.x - p.y) * 0.7071f;
            float ry = (p.x + p.y) * 0.7071f;
            if (i % 5 == 0)
            {
                rx *= 0.4f;
                ry *= 0.4f;
            }
            pts[i] = Slot(rx, ry);
        }
        return pts;
    }

    Vector3[] BuildCross(int n)
    {
        Vector3[] pts = new Vector3[n];
        float arm = shapeRadius;
        float thick = shapeRadius * 0.22f;
        for (int i = 0; i < n; i++)
        {
            bool horiz = i % 2 == 0;
            float t = (i / 2) / Mathf.Max(1f, (n / 2f) - 1f);
            if (horiz)
                pts[i] = Slot(Mathf.Lerp(-arm, arm, t), ((i % 4) < 2 ? -thick : thick) * 0.5f);
            else
                pts[i] = Slot(((i % 4) < 2 ? -thick : thick) * 0.5f, Mathf.Lerp(-arm, arm, t));
        }
        return pts;
    }

    Vector3[] BuildStar(int n)
    {
        Vector3[] pts = new Vector3[n];
        const int tips = 5;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n * Mathf.PI * 2f;
            // 5-point star radius modulation
            float tip = (Mathf.Cos(t * tips) + 1f) * 0.5f;
            float rad = Mathf.Lerp(shapeRadius * 0.38f, shapeRadius, tip);
            if (i % 6 == 0)
                rad *= 0.35f;
            pts[i] = Slot(Mathf.Cos(t) * rad, Mathf.Sin(t) * rad);
        }
        return pts;
    }
}
