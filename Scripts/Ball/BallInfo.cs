using System.Collections.Generic;
using UnityEngine;

public class BallInfo : MonoBehaviour
{
    public Ball ball;

    Database db;
    public GameObject anchor;
    [SerializeField]
    public List<Collision> futureColisions = new List<Collision>();
    public List<Vector3> futureColisionPoints = new List<Vector3>();
    [HideInInspector()]
    public bool documentColisions = false;

    Rigidbody rb;

    Vector3 lPos = Vector3.zero;
    [Tooltip("How little X or Y can change before that axis counts as stuck.")]
    public float tolorance = 0.5f;
    [Tooltip("Seconds stuck on the same X or same Y before the ball is destroyed.")]
    public float timeTillReset = 8f;
    public float ttR = 0;

    float stuckTimerX = 0f;
    float stuckTimerY = 0f;

    public bool ballReady = false;
    public bool checkStuck = true;
    public bool speedCap = true;
    public bool projectionOn = true;
    public bool showProjection = false;

    /// <summary>
    /// Match loadout slot (0..ballCount-1). -1 = ability/extra ball (clone, orbit satellite, etc.).
    /// </summary>
    public int matchSlot = -1;

    public bool showFutureCollisions = false;

    [Tooltip("How often to refresh analytic bounce prediction for AI.")]
    public float predictionInterval = 0.12f;

    float nextPredictionAt;
    float cachedRadius = 0.25f;

    PlayerGrab ownerGrab;
    int ownershipPlayerIndex = -1;
    float ownershipTimer = 0f;
    int lastHitterIndex = -1;
    int uniqueHitterMask;
    int hitsSinceLifeline;
    int wallBouncesSinceHit;

    void OnEnable()
    {
        LiveBallRegistry.Register(this);
    }

    void OnDisable()
    {
        LiveBallRegistry.Unregister(this);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        db = Database.instance;
        lPos = transform.position;
        ownerGrab = GetComponent<PlayerGrab>();
        LiveBallRegistry.Register(this);

        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc != null)
            cachedRadius = sc.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        else
        {
            BoxCollider bc = GetComponent<BoxCollider>();
            if (bc != null)
            {
                Vector3 s = Vector3.Scale(bc.size, transform.lossyScale);
                cachedRadius = Mathf.Max(s.x, s.y) * 0.5f;
            }
        }
    }

    void FixedUpdate()
    {
        LockToPlayfield();
    }

    void LockToPlayfield()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (db == null)
            db = Database.instance;
        if (rb == null || db == null || db.FieldPlaySize <= 0.01f)
            return;

        float z = db.FieldPlaySize;
        Vector3 pos = rb.position;
        if (Mathf.Abs(pos.z - z) > 0.0001f)
        {
            pos.z = z;
            rb.position = pos;
            transform.position = pos;
        }

        if (!rb.isKinematic)
        {
            Vector3 vel = rb.linearVelocity;
            if (Mathf.Abs(vel.z) > 0.0001f)
            {
                vel.z = 0f;
                rb.linearVelocity = vel;
            }
        }

        if ((rb.constraints & RigidbodyConstraints.FreezePositionZ) == 0)
            rb.constraints |= RigidbodyConstraints.FreezePositionZ;
    }

    void Update()
    {
        if (db == null || !db.gameStart)
            return;

        if (anchor == null)
        {
            Destroy(gameObject);
            return;
        }

        if (!ballReady)
            return;

        TrackBallOwnership();

        if (checkStuck)
            StuckInAxis();

        // Cheap analytic bounce path for AI (replaces Instantiate ghost projection)
        if (projectionOn && Time.time >= nextPredictionAt && rb != null)
        {
            nextPredictionAt = Time.time + Mathf.Max(0.05f, predictionInterval);
            BallTrajectory.Predict(
                rb.position,
                rb.linearVelocity,
                cachedRadius,
                futureColisionPoints,
                5,
                48f);
        }
    }

    void StuckInAxis()
    {
        // Prefabs previously used 0.05 which never stayed "stuck" due to physics jitter
        float tol = Mathf.Max(tolorance, 0.35f);
        Vector3 pos = transform.position;

        if (Mathf.Abs(pos.x - lPos.x) <= tol)
        {
            stuckTimerX += Time.deltaTime;
        }
        else
        {
            stuckTimerX = 0f;
            lPos.x = pos.x;
        }

        if (Mathf.Abs(pos.y - lPos.y) <= tol)
        {
            stuckTimerY += Time.deltaTime;
        }
        else
        {
            stuckTimerY = 0f;
            lPos.y = pos.y;
        }

        ttR = Mathf.Max(stuckTimerX, stuckTimerY);

        if (stuckTimerX > timeTillReset || stuckTimerY > timeTillReset)
        {
            Destroy(gameObject);
        }
    }

    void TrackBallOwnership()
    {
        if (ownerGrab == null || db == null || db.players == null)
            return;

        int idx = ownerGrab.playerIndex;
        if (idx < 0 || idx >= db.players.Count)
        {
            ownershipPlayerIndex = -1;
            ownershipTimer = 0f;
            return;
        }

        bool newPossession = idx != ownershipPlayerIndex;
        if (newPossession)
        {
            ownershipPlayerIndex = idx;
            ownershipTimer = 0f;
        }

        ownershipTimer += Time.deltaTime;
        Player owner = db.players[idx];
        if (owner == null)
            return;
        owner.RecordBallOwnershipSeconds(Mathf.FloorToInt(ownershipTimer));
        owner.RecordBallOwnershipTick(Time.deltaTime, newPossession);
    }

    public void NotePlayerHit(int playerIndex)
    {
        if (playerIndex < 0)
            return;
        lastHitterIndex = playerIndex;
        hitsSinceLifeline++;
        if (playerIndex < 32)
            uniqueHitterMask |= 1 << playerIndex;
        wallBouncesSinceHit = 0;
    }

    public void NoteWallBounce()
    {
        if (lastHitterIndex < 0 || db == null || db.players == null)
            return;
        Player p = db.players.Find(x => x != null && x.index == lastHitterIndex);
        if (p == null)
            return;
        p.RecordWallBounceAfterHit();
        wallBouncesSinceHit++;
    }

    public bool ConsumeAceIfSoloHitter(int scorerIndex)
    {
        bool ace = lastHitterIndex == scorerIndex
            && uniqueHitterMask != 0
            && (uniqueHitterMask & (uniqueHitterMask - 1)) == 0;
        ResetRallyTracking();
        return ace;
    }

    public bool ConsumeVolley()
    {
        return wallBouncesSinceHit == 0 && hitsSinceLifeline > 0;
    }

    public void ResetRallyTracking()
    {
        uniqueHitterMask = 0;
        hitsSinceLifeline = 0;
        wallBouncesSinceHit = 0;
        lastHitterIndex = -1;
    }
}
