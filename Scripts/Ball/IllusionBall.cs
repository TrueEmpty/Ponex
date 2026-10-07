using UnityEngine;

[RequireComponent(typeof(BallInfo))]
public class IllusionBall : MonoBehaviour
{
    static int creatingIllusion;

    Database db;
    BallInfo bI;
    Rigidbody rb;
    Renderer ren;

    [Tooltip("Random wait (min/max seconds) before spawning a new illusion.")]
    public Vector2 spawnIntervalRange = new Vector2(1.25f, 5.5f);

    [Tooltip("Random lifetime (min/max seconds) for each illusion copy.")]
    public Vector2 lifetimeRange = new Vector2(2f, 7.5f);

    [Tooltip("Spawn distance from the real ball.")]
    public float spawnOffset = 0.9f;

    [Range(0.05f, 1f)]
    public float illusionAlpha = 0.45f;

    [HideInInspector]
    public float illusionLifetime = 6f;

    GameObject illusion;
    float nextSpawnTime;
    bool waitingToSpawn = true;
    bool isCopy;

    void Awake()
    {
        if (creatingIllusion > 0)
            isCopy = true;
    }

    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();
        rb = GetComponent<Rigidbody>();
        ren = GetComponent<Renderer>();

        if (!isCopy)
        {
            ScheduleNextSpawn();
            return;
        }

        if (bI != null && bI.ball != null)
        {
            bI.ball = new Ball(bI.ball);
            bI.ball.damage = 0;
            bI.ball.name = "Illusion Copy";
            bI.ballReady = true;
            bI.projectionOn = false;
            bI.checkStuck = true;
            bI.matchSlot = -1;
        }

        if (ren != null && ren.material != null)
        {
            Color c = ren.material.color;
            c.a = illusionAlpha;
            ren.material.color = c;
        }

        Destroy(gameObject, Mathf.Max(0.25f, illusionLifetime));
    }

    void Update()
    {
        if (isCopy || db == null || !db.gameStart || bI == null || !bI.ballReady || !bI.projectionOn)
            return;

        if (illusion != null)
            return;

        if (!waitingToSpawn)
        {
            ScheduleNextSpawn();
            waitingToSpawn = true;
        }

        if (Time.time < nextSpawnTime)
            return;

        SpawnIllusion();
        waitingToSpawn = false;
    }

    void OnDestroy()
    {
        // Skip projection ghosts (projectionOn false) so they don't wipe the real decoy
        if (!isCopy && bI != null && bI.projectionOn && illusion != null)
            Destroy(illusion);
    }

    void ScheduleNextSpawn()
    {
        float min = Mathf.Min(spawnIntervalRange.x, spawnIntervalRange.y);
        float max = Mathf.Max(spawnIntervalRange.x, spawnIntervalRange.y);
        nextSpawnTime = Time.time + Random.Range(min, max);
    }

    void SpawnIllusion()
    {
        if (bI == null || bI.ball == null || bI.ball.prefab == null || rb == null)
            return;

        Vector2 dir = Random.insideUnitCircle;
        if (dir.sqrMagnitude < 0.01f)
            dir = Vector2.right;
        dir.Normalize();

        Vector3 realPos = rb.position;
        Vector3 realVel = rb.linearVelocity;
        realPos.z = transform.position.z;

        Vector3 decoyPos = realPos + new Vector3(dir.x, dir.y, 0f) * spawnOffset;
        decoyPos.z = realPos.z;

        Vector3 decoyVel = realVel;
        float angle = Random.Range(-40f, 40f) * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        decoyVel = new Vector3(realVel.x * cos - realVel.y * sin, realVel.x * sin + realVel.y * cos, 0f);
        if (decoyVel.sqrMagnitude < 0.01f)
            decoyVel = new Vector3(dir.x, dir.y, 0f) * Mathf.Max(bI.ball.minSpeed, 1f);

        creatingIllusion++;
        GameObject copy;
        try
        {
            copy = Instantiate(bI.ball.prefab, decoyPos, transform.rotation);
        }
        finally
        {
            creatingIllusion--;
        }

        if (copy == null)
            return;

        copy.name = "Illusion Copy";
        copy.transform.localScale = transform.localScale;

        Rigidbody copyRb = copy.GetComponent<Rigidbody>();
        IllusionBall copyScript = copy.GetComponent<IllusionBall>();
        if (copyScript != null)
        {
            copyScript.illusionAlpha = illusionAlpha;
            float lifeMin = Mathf.Min(lifetimeRange.x, lifetimeRange.y);
            float lifeMax = Mathf.Max(lifetimeRange.x, lifetimeRange.y);
            copyScript.illusionLifetime = Random.Range(lifeMin, lifeMax);
        }

        PlayerGrab copyGrab = copy.GetComponent<PlayerGrab>();
        PlayerGrab ownGrab = GetComponent<PlayerGrab>();
        if (copyGrab != null && ownGrab != null && ownGrab.IsLinked())
            copyGrab.playerIndex = ownGrab.playerIndex;

        // Swap so the path that kept the original momentum is not always the real ball
        if (copyRb != null)
        {
            copyRb.position = realPos;
            copy.transform.position = realPos;
            copyRb.linearVelocity = realVel;

            rb.position = decoyPos;
            transform.position = decoyPos;
            rb.linearVelocity = decoyVel;
        }

        illusion = copy;
    }
}
