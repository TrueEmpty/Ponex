using System.Collections.Generic;
using UnityEngine;

public class Gaurd : MonoBehaviour
{
    /// <summary>Gap from the wall face to Gaurd's back — one default ball (0.5) plus a little slack.</summary>
    public const float BallBehindGap = 0.62f;

    Rigidbody rb;
    Database db;
    PlayerGrab pg;

    public List<string> hitTags = new List<string>();

    public GameObject defenders;
    public GameObject reflectors;
    public GameObject attackers;

    public List<Pawn> pawns = new List<Pawn>();
    bool savedByPawns = false;

    [SerializeField]
#pragma warning disable CS0414 // Inspector AI-thought debug
    Thought thought = Thought.Nothing;
#pragma warning restore CS0414

    bool ignoredHomeWall;
    bool pinned;
    Vector3 pinPos;
    Quaternion pinRot;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        MakeImmovable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;
        MakeImmovable();

        DamageOnTagHit dmg = GetComponent<DamageOnTagHit>();
        if (dmg == null)
            dmg = gameObject.AddComponent<DamageOnTagHit>();
        dmg.tagHit = "Ball";
        dmg.damageEvenIfOwnBall = true;

        if (GetComponent<DestroyOnDeath>() == null)
            gameObject.AddComponent<DestroyOnDeath>();

        PinCurrentPose();
    }

    void Update()
    {
        pawns.RemoveAll(pawn => pawn == null);

        if (!ignoredHomeWall && db != null && db.gameStart)
        {
            PaddleWall.IgnoreHomeWallCollisions(transform, hitTags, null);
            ignoredHomeWall = true;
        }

        HoldStill();

        if (pg == null || !pg.IsLinked())
            return;

        Player p = pg.player;
        if (db == null || !db.gameStart)
            return;

        if (savedByPawns && pawns.Count == 0)
        {
            if (p.currentHealth <= 1)
                p.currentHealth = 0;
            savedByPawns = false;
        }

        if (p.currentHealth <= 0 && !CharacterCreationManager.IsActive)
            DespawnSelfAndSpawns();
    }

    void FixedUpdate()
    {
        HoldStill();
    }

    void LateUpdate()
    {
        HoldStill();
    }

    void MakeImmovable()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void HoldStill()
    {
        if (!pinned)
            PinCurrentPose();
        else if (pg != null && pg.player != null)
            pinRot = WallFacingRotation(pg.player);

        if (!pinned)
            return;

        MakeImmovable();
        transform.SetPositionAndRotation(pinPos, pinRot);
        if (rb == null)
            return;

        rb.position = pinPos;
        rb.rotation = pinRot;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void PinCurrentPose()
    {
        if (pinned)
            return;

        Physics.SyncTransforms();
        pinPos = transform.position;
        if (db != null && db.FieldPlaySize > 0.01f)
            pinPos.z = db.FieldPlaySize;
        pinRot = WallFacingRotation(pg != null ? pg.player : null);
        pinned = true;
        MakeImmovable();
    }

    static Quaternion WallFacingRotation(Player p)
    {
        Vector3 into = p != null ? PaddleWall.IntoField(p.facing) : Vector3.up;
        if (into.sqrMagnitude < 0.0001f)
            into = Vector3.up;
        Quaternion rot = Quaternion.LookRotation(Vector3.forward, into.normalized);
        if (p != null && p.character != null)
            rot *= Quaternion.Euler(p.character.rotationOffset);
        return rot;
    }

    void OnCollisionEnter(Collision collision)
    {
        HoldStill();
        if (collision != null)
            TryTakeBallHit(collision.gameObject);
    }

    void OnCollisionStay(Collision collision)
    {
        HoldStill();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other != null)
            TryTakeBallHit(other.gameObject);
    }

    void TryTakeBallHit(GameObject hit)
    {
        if (hit == null || !hit.CompareTag("Ball"))
            return;
        if (pg == null || !pg.IsLinked() || pg.player == null)
            return;

        PlayerGrab tpG = hit.GetComponent<PlayerGrab>();
        BallInfo tbI = hit.GetComponent<BallInfo>();

        int baseDamage = 1;
        if (tbI != null && tbI.ball != null)
            baseDamage = Mathf.Max(1, tbI.ball.damage);

        int lost = pg.player.ApplyGoalDamage(baseDamage, hit.GetEntityId().GetHashCode());
        if (lost > 0 && tpG != null && tpG.IsLinked() && tpG.player != null
            && tpG.playerIndex != pg.playerIndex)
            tpG.player.RecordDamageDealt(lost);

        pawns.RemoveAll(pawn => pawn == null);

        if (pg.player.currentHealth <= 1 && CharacterCreationManager.IsActive)
            return;

        if (pg.player.currentHealth <= 0)
        {
            if (pawns.Count > 0)
            {
                pg.player.currentHealth = 1;
                savedByPawns = true;
            }
            else
            {
                pg.player.currentHealth = 0;
                savedByPawns = false;
                if (!CharacterCreationManager.IsActive)
                    DespawnSelfAndSpawns();
            }
        }
    }

    public void RegisterPawn(Pawn pawn)
    {
        if (pawn == null || pawns.Contains(pawn))
            return;
        pawns.Add(pawn);
    }

    void DespawnSelfAndSpawns()
    {
        DestroyOwnedSpawns();
        if (gameObject != null)
            Destroy(gameObject);
    }

    void DestroyOwnedSpawns()
    {
        pawns.RemoveAll(pawn => pawn == null);
        for (int i = 0; i < pawns.Count; i++)
        {
            if (pawns[i] != null)
                Destroy(pawns[i].gameObject);
        }
        pawns.Clear();

        int index = pg != null ? pg.playerIndex : -1;
        Pawn[] allPawns = FindObjectsByType<Pawn>(FindObjectsInactive.Exclude);
        for (int i = 0; i < allPawns.Length; i++)
        {
            Pawn pawn = allPawns[i];
            if (pawn == null)
                continue;
            PlayerGrab grab = pawn.GetComponent<PlayerGrab>();
            if (index >= 0 && grab != null && grab.playerIndex == index)
                Destroy(pawn.gameObject);
        }

        RepeatSpawn[] spawners = GetComponentsInChildren<RepeatSpawn>(true);
        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] != null)
                spawners[i].DestroySpawned();
        }
    }

    void OnDestroy()
    {
        DestroyOwnedSpawns();
    }
}
