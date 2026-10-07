using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Invisible central ball that only collides with walls. Spawns and shepherds two orbiting balls.
/// </summary>
[RequireComponent(typeof(BallInfo))]
public class OrbitBall : MonoBehaviour
{
    Database db;
    BallInfo bI;
    Rigidbody rb;
    Collider coreCol;

    public float orbitRadius = 0.85f;
    public float orbitSpeed = 90f;
    [Tooltip("Spring pull toward the moving orbit slot (acceleration).")]
    public float pullStrength = 5.5f;
    [Tooltip("Max orbit acceleration so bumps can still knock satellites away.")]
    public float maxPullAccel = 20f;
    public float respawnDelay = 1.35f;
    public float satelliteScale = 0.38f;
    public GameObject satellitePrefab;

    readonly List<OrbitSatellite> satellites = new List<OrbitSatellite>(2);
    float angle;
    float respawnTimer;
    float nextIgnoreRefresh;

    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();
        rb = GetComponent<Rigidbody>();
        coreCol = GetComponent<Collider>();

        if (bI != null && bI.ball != null)
        {
            bI.ball.damage = 0;
            bI.checkStuck = false;
            bI.ball.name = "Orbit Ball";
        }

        // Invisible core
        Renderer ren = GetComponent<Renderer>();
        if (ren != null)
            ren.enabled = false;

        IgnoreNonWallColliders();
        nextIgnoreRefresh = Time.time + 1.5f;
        EnsureSatellites();
    }

    void FixedUpdate()
    {
        if (db == null || !db.gameStart || bI == null || !bI.ballReady || !bI.projectionOn)
            return;

        // Occasional refresh only — was OverlapSphere(30) every physics tick
        if (Time.time >= nextIgnoreRefresh)
        {
            IgnoreNonWallColliders();
            nextIgnoreRefresh = Time.time + 1.5f;
        }

        angle += orbitSpeed * Time.fixedDeltaTime;

        // Prune destroyed
        for (int i = satellites.Count - 1; i >= 0; i--)
        {
            if (satellites[i] == null)
                satellites.RemoveAt(i);
        }

        if (satellites.Count < 2)
        {
            respawnTimer += Time.fixedDeltaTime;
            if (respawnTimer >= respawnDelay)
            {
                respawnTimer = 0f;
                EnsureSatellites();
            }
        }
        else
        {
            respawnTimer = 0f;
        }

        for (int i = 0; i < satellites.Count; i++)
        {
            float a = (angle + i * 180f) * Mathf.Deg2Rad;
            Vector3 target = rb.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * orbitRadius;
            target.z = rb.position.z;
            satellites[i].SetOrbitTarget(target, pullStrength, maxPullAccel);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (coreCol == null || collision.collider == null)
            return;

        string tag = collision.transform.tag;
        if (tag == "Wall" || tag == "Walls" || tag == "Obstacle")
            return;

        Physics.IgnoreCollision(coreCol, collision.collider, true);
    }

    void EnsureSatellites()
    {
        while (satellites.Count < 2)
        {
            GameObject satObj;
            if (satellitePrefab != null)
            {
                satObj = Instantiate(satellitePrefab, transform.position, Quaternion.identity);
            }
            else
            {
                satObj = CreateRuntimeSatellite();
            }

            OrbitSatellite sat = satObj.GetComponent<OrbitSatellite>();
            if (sat == null)
                sat = satObj.AddComponent<OrbitSatellite>();

            sat.Initialize(this, bI);
            satObj.transform.localScale = Vector3.one * satelliteScale;
            satellites.Add(sat);

            // Stagger initial placement
            float a = (angle + satellites.Count * 180f) * Mathf.Deg2Rad;
            Vector3 pos = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * orbitRadius;
            pos.z = transform.position.z;
            satObj.transform.position = pos;
            Rigidbody sRb = satObj.GetComponent<Rigidbody>();
            if (sRb != null && rb != null)
                sRb.linearVelocity = rb.linearVelocity;
        }
    }

    GameObject CreateRuntimeSatellite()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Orbit Satellite";
        go.tag = "Ball";
        go.layer = gameObject.layer;

        Rigidbody sRb = go.AddComponent<Rigidbody>();
        sRb.useGravity = false;
        sRb.constraints = RigidbodyConstraints.FreezePositionZ
            | RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationY
            | RigidbodyConstraints.FreezeRotationZ;
        sRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        sRb.mass = 6f;

        BallInfo sInfo = go.AddComponent<BallInfo>();
        if (bI != null && bI.ball != null)
        {
            sInfo.ball = new Ball(bI.ball);
            sInfo.ball.name = "Orbit Ball";
            sInfo.ball.damage = Mathf.Max(1, bI.ball.damage > 0 ? bI.ball.damage : 1);
            sInfo.ballReady = true;
            sInfo.projectionOn = true;
            sInfo.checkStuck = true;
            sInfo.anchor = go;
            sInfo.matchSlot = -1;
        }

        go.AddComponent<BallMovement>();
        go.AddComponent<PlayerGrab>();
        go.AddComponent<ClearAfterTheGame>();

        Renderer ren = go.GetComponent<Renderer>();
        Renderer coreRen = GetComponent<Renderer>();
        if (ren != null && coreRen != null && coreRen.sharedMaterial != null)
            ren.sharedMaterial = coreRen.sharedMaterial;

        return go;
    }

    void IgnoreNonWallColliders()
    {
        if (coreCol == null)
            return;

        Collider[] hits = Physics.OverlapSphere(transform.position, 30f);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider other = hits[i];
            if (other == null || other == coreCol)
                continue;

            string tag = other.tag;
            if (tag == "Wall" || tag == "Walls" || tag == "Obstacle")
                continue;

            Physics.IgnoreCollision(coreCol, other, true);
        }
    }

    void OnDestroy()
    {
        for (int i = 0; i < satellites.Count; i++)
        {
            if (satellites[i] != null)
                Destroy(satellites[i].gameObject);
        }
        satellites.Clear();
    }
}
