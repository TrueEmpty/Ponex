using UnityEngine;

[RequireComponent(typeof(BallInfo))]
public class MeteorBall : MonoBehaviour
{
    Database db;
    BallInfo bI;
    Rigidbody rb;
    MeshFilter meshFilter;
    MeshCollider meshCollider;
    SphereCollider sphereCol;

    [Tooltip("Starting uniform scale (smaller than basic 0.5).")]
    public float startScale = 0.32f;

    [Tooltip("Scale at which the meteor pops.")]
    public float popScale = 1.65f;

    [Tooltip("Scale gain per bounce.")]
    public float growPerBounce = 0.12f;

    public int shardCount = 10;
    public float shardScale = 0.18f;
    public float shardSpeed = 6f;
    public float shardLifetime = 4f;

    bool builtMesh;

    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();
        rb = GetComponent<Rigidbody>();
        BuildMeteorShape();
        transform.localScale = Vector3.one * startScale;
    }

    void BuildMeteorShape()
    {
        if (builtMesh)
            return;
        builtMesh = true;

        sphereCol = GetComponent<SphereCollider>();
        if (sphereCol != null)
            sphereCol.enabled = false;

        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        Mesh meteorMesh = CreateRockMesh();
        meshFilter.sharedMesh = meteorMesh;

        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = meteorMesh;
        meshCollider.convex = true;
    }

    Mesh CreateRockMesh()
    {
        // Icosahedron-ish with seeded noise for a rocky silhouette
        Mesh mesh = new Mesh { name = "MeteorRock" };
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        Vector3[] verts =
        {
            new Vector3(-1,  t, 0), new Vector3( 1,  t, 0), new Vector3(-1, -t, 0), new Vector3( 1, -t, 0),
            new Vector3( 0, -1,  t), new Vector3( 0,  1,  t), new Vector3( 0, -1, -t), new Vector3( 0,  1, -t),
            new Vector3( t,  0, -1), new Vector3( t,  0,  1), new Vector3(-t,  0, -1), new Vector3(-t,  0,  1)
        };

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 n = verts[i].normalized;
            float noise = 0.75f + 0.35f * Mathf.PerlinNoise(n.x * 3.1f + 10f, n.y * 3.1f + 20f);
            // Flatten Z a bit for 2D playfield silhouette
            n.z *= 0.55f;
            verts[i] = n.normalized * (0.5f * noise);
        }

        int[] tris =
        {
            0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
            1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
            4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
        };

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void OnCollisionExit(Collision collision)
    {
        if (db == null || !db.gameStart || bI == null || !bI.ballReady || !bI.projectionOn)
            return;

        string tag = collision.transform.tag;
        if (tag != "Wall" && tag != "Walls" && tag != "Obstacle"
            && tag != "Paddle" && tag != "Player" && tag != "Lifeline")
            return;

        float s = transform.localScale.x + growPerBounce;
        if (s >= popScale)
        {
            PopShards();
            transform.localScale = Vector3.one * startScale;
        }
        else
        {
            transform.localScale = Vector3.one * s;
        }
    }

    void PopShards()
    {
        int owner = -1;
        PlayerGrab grab = GetComponent<PlayerGrab>();
        if (grab != null && grab.IsLinked())
            owner = grab.playerIndex;

        Vector3 baseVel = rb != null ? rb.linearVelocity : Vector3.zero;

        for (int i = 0; i < shardCount; i++)
        {
            GameObject shard = Instantiate(gameObject, transform.position, Random.rotation);
            shard.name = "Meteor Shard";

            // Strip meteor growth behaviour; add shard behaviour
            MeteorBall mb = shard.GetComponent<MeteorBall>();
            if (mb != null)
                Destroy(mb);

            MeteorShard ms = shard.GetComponent<MeteorShard>();
            if (ms == null)
                ms = shard.AddComponent<MeteorShard>();
            ms.ownerPlayerIndex = owner;
            ms.lifetime = shardLifetime;

            shard.transform.localScale = Vector3.one * shardScale;

            BallInfo sInfo = shard.GetComponent<BallInfo>();
            if (sInfo != null)
            {
                if (bI != null && bI.ball != null)
                    sInfo.ball = new Ball(bI.ball);
                sInfo.ballReady = true;
                sInfo.projectionOn = false;
                sInfo.checkStuck = false;
                sInfo.anchor = shard;
                if (sInfo.ball != null)
                    sInfo.ball.damage = bI != null && bI.ball != null ? Mathf.Max(1, bI.ball.damage) : 1;
            }

            PlayerGrab sGrab = shard.GetComponent<PlayerGrab>();
            if (sGrab != null)
                sGrab.playerIndex = owner;

            Rigidbody sRb = shard.GetComponent<Rigidbody>();
            if (sRb != null)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                if (dir.sqrMagnitude < 0.01f)
                    dir = Vector2.right;
                sRb.linearVelocity = baseVel * 0.25f + new Vector3(dir.x, dir.y, 0f) * shardSpeed;
            }

            Destroy(shard, shardLifetime);
        }
    }
}
