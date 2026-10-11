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
    readonly HashSet<int> suppressedPullers = new HashSet<int>();
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
            bI.checkStuck = true;
            bI.ball.name = "Orbit Ball";
            if (bI.anchor == null)
                bI.anchor = gameObject;
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
            target.z = PlayZ();
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
        PullObjectIn puller = collision.collider.GetComponent<PullObjectIn>()
            ?? collision.collider.GetComponentInParent<PullObjectIn>();
        if (puller != null)
            SuppressCelestialPull(puller, true);
    }

    /// <summary>
    /// Celarus moon/sun gravity: once this invisible core touches the body,
    /// ignore that well until we fully leave its range so we cannot sit locked inside.
    /// </summary>
    public bool IsCelestialPullBlocked(PullObjectIn puller)
    {
        if (puller == null)
            return false;

        int id = puller.GetEntityId().GetHashCode();
        float range = Mathf.Max(0.01f, puller.distance * Mathf.Max(0.1f, puller.disScale));
        float dist = Vector3.Distance(transform.position, puller.transform.position);

        if (suppressedPullers.Contains(id))
        {
            if (dist > range + 0.2f)
                suppressedPullers.Remove(id);
            else
                return true;
        }

        if (OverlapsCelestial(puller, dist))
        {
            SuppressCelestialPull(puller, false);
            return true;
        }

        return false;
    }

    void SuppressCelestialPull(PullObjectIn puller, bool fromCollision)
    {
        if (puller == null)
            return;

        int id = puller.GetEntityId().GetHashCode();
        bool first = suppressedPullers.Add(id);
        if (!first && !fromCollision)
            return;

        NudgeOutOfWell(puller);
    }

    bool OverlapsCelestial(PullObjectIn puller, float dist)
    {
        float bodyR = CelestialRadius(puller);
        float ballR = 0.2f;
        if (coreCol != null)
            ballR = Mathf.Max(0.08f, Mathf.Max(coreCol.bounds.extents.x, coreCol.bounds.extents.y));
        return dist <= bodyR + ballR * 0.35f;
    }

    static float CelestialRadius(PullObjectIn puller)
    {
        SphereCollider sc = puller.GetComponent<SphereCollider>();
        if (sc != null)
        {
            float s = Mathf.Max(puller.transform.lossyScale.x, puller.transform.lossyScale.y);
            return Mathf.Max(0.35f, sc.radius * s);
        }

        Collider col = puller.GetComponent<Collider>();
        if (col != null)
            return Mathf.Max(0.35f, Mathf.Max(col.bounds.extents.x, col.bounds.extents.y));

        return 1.25f;
    }

    void NudgeOutOfWell(PullObjectIn puller)
    {
        if (rb == null || puller == null)
            return;

        Vector3 away = rb.position - puller.transform.position;
        away.z = 0f;
        if (away.sqrMagnitude < 0.0001f)
        {
            Vector3 v = rb.linearVelocity;
            v.z = 0f;
            away = v.sqrMagnitude > 0.01f ? v : Vector3.up;
        }
        away.Normalize();

        Vector3 vel = rb.linearVelocity;
        vel.z = 0f;
        float inward = Vector3.Dot(vel, -away);
        if (inward > 0f)
            vel += away * inward;
        rb.linearVelocity = vel;
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
            pos.z = PlayZ();
            satObj.transform.position = pos;
            Rigidbody sRb = satObj.GetComponent<Rigidbody>();
            if (sRb != null)
            {
                sRb.position = pos;
                if (rb != null && !sRb.isKinematic)
                    sRb.linearVelocity = rb.linearVelocity;
            }
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
        Vector3 spawn = transform.position;
        spawn.z = PlayZ();
        go.transform.position = spawn;
        sRb.position = spawn;
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
            sInfo.checkStuck = false;
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

    float PlayZ()
    {
        if (db != null && db.FieldPlaySize > 0.01f)
            return db.FieldPlaySize;
        return rb != null ? rb.position.z : transform.position.z;
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
