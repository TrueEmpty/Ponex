using UnityEngine;

[RequireComponent(typeof(BallInfo))]
public class MineBall : MonoBehaviour
{
    Database db;
    BallInfo bI;
    Rigidbody rb;
    PlayerGrab ownGrab;

    public Vector2 dropIntervalRange = new Vector2(2.5f, 5.5f);
    public float mineScale = 0.42f;
    public GameObject minePrefab;
    public Material mineBodyMaterial;
    public Material mineFuseMaterial;
    public Material bombBodyMaterial;
    public Material bombFuseMaterial;

    float nextDropAt;
    bool visualsReady;

    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();
        rb = GetComponent<Rigidbody>();
        ownGrab = GetComponent<PlayerGrab>();

        if (!visualsReady)
        {
            MineBombVisual.Apply(gameObject, bombBodyMaterial, bombFuseMaterial, false, 1f);
            visualsReady = true;
        }

        ScheduleDrop();
    }

    void Update()
    {
        if (db == null || !db.gameStart || bI == null || !bI.ballReady || !bI.projectionOn)
            return;

        if (Time.time < nextDropAt)
            return;

        DropMine();
        ScheduleDrop();
    }

    void ScheduleDrop()
    {
        nextDropAt = Time.time + Random.Range(dropIntervalRange.x, dropIntervalRange.y);
    }

    void DropMine()
    {
        GameObject mine;
        if (minePrefab != null)
            mine = Instantiate(minePrefab, transform.position, transform.rotation);
        else
            mine = CreateRuntimeMine();

        mine.name = "Dropped Mine";
        mine.transform.localScale = Vector3.one * mineScale;
        mine.tag = "Ball";
        mine.layer = gameObject.layer;

        BallInfo mInfo = mine.GetComponent<BallInfo>();
        if (mInfo == null)
            mInfo = mine.AddComponent<BallInfo>();
        if (bI != null && bI.ball != null)
            mInfo.ball = new Ball(bI.ball);
        mInfo.ballReady = true;
        mInfo.projectionOn = false;
        mInfo.checkStuck = true;
        mInfo.matchSlot = -1;
        mInfo.anchor = mine;
        if (mInfo.ball != null)
        {
            mInfo.ball.name = "Dropped Mine";
            mInfo.ball.damage = Mathf.Max(1, bI != null && bI.ball != null ? bI.ball.damage : 1);
        }

        if (mine.GetComponent<BallMovement>() == null)
            mine.AddComponent<BallMovement>();
        if (mine.GetComponent<ClearAfterTheGame>() == null)
            mine.AddComponent<ClearAfterTheGame>();

        PlayerGrab mGrab = mine.GetComponent<PlayerGrab>();
        if (mGrab == null)
            mGrab = mine.AddComponent<PlayerGrab>();
        if (ownGrab != null && ownGrab.IsLinked())
            mGrab.playerIndex = ownGrab.playerIndex;

        Rigidbody mRb = mine.GetComponent<Rigidbody>();
        if (mRb == null)
        {
            mRb = mine.AddComponent<Rigidbody>();
            mRb.useGravity = false;
            mRb.constraints = RigidbodyConstraints.FreezePositionZ
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationY
                | RigidbodyConstraints.FreezeRotationZ;
            mRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            mRb.mass = 8f;
        }

        if (rb != null)
            mRb.linearVelocity = rb.linearVelocity * 0.35f;

        DroppedMine dropped = mine.GetComponent<DroppedMine>();
        if (dropped == null)
            dropped = mine.AddComponent<DroppedMine>();
        dropped.Setup(gameObject, mineBodyMaterial, mineFuseMaterial);

        Collider parentCol = GetComponent<Collider>();
        Collider mineCol = mine.GetComponent<Collider>();
        if (parentCol != null && mineCol != null)
            Physics.IgnoreCollision(parentCol, mineCol, true);
    }

    GameObject CreateRuntimeMine()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        SphereCollider sc = go.GetComponent<SphereCollider>();
        SphereCollider parentSc = GetComponent<SphereCollider>();
        if (sc != null && parentSc != null)
            sc.sharedMaterial = parentSc.sharedMaterial;
        return go;
    }
}
