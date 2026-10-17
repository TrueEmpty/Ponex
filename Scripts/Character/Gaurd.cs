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

    public GameObject reflectors;
    public GameObject attackers;
    public GameObject patrollers;
    public GameObject rangers;
    public GameObject defenders;

    public List<Pawn> pawns = new List<Pawn>();
    public Pawn hovering = null;

    bool savedByPawns = false;
    GaurdCursor cursor;
    Pawn ghost;
    int ghostShown = -1;
    float goldTimer;
    bool superWasHeld;
    GameObject ringPrefab;

    [SerializeField]
    Thought thought = Thought.Nothing;
    ComputerBrain brain;

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
        dmg.damageEvenIfOwnBall = false;

        if (GetComponent<DestroyOnDeath>() == null)
            gameObject.AddComponent<DestroyOnDeath>();

        PinCurrentPose();
        pg.player.movementSpeed = 0;
        if (pg.player.super != null)
        {
            pg.player.super.max = Mathf.Max(pg.player.super.max, 999f);
            pg.player.super.amount = Mathf.Floor(pg.player.super.amount);
        }
        reflectors = reflectors != null ? reflectors : Resources.Load<GameObject>("Gaurd/Reflector");
        attackers = attackers != null ? attackers : Resources.Load<GameObject>("Gaurd/Attacker");
        patrollers = patrollers != null ? patrollers : Resources.Load<GameObject>("Gaurd/Patroller");
        rangers = rangers != null ? rangers : Resources.Load<GameObject>("Gaurd/Ranger");
        defenders = defenders != null ? defenders : Resources.Load<GameObject>("Gaurd/Defender");
        ringPrefab = Resources.Load<GameObject>("Gaurd/Placement Ring");
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
        PlayerInfoNavigation();
        TickGold();
        ShowTroopGhost();
        BuyTroop();

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

    void PlayerInfoNavigation()
    {
        if (pg.player.CanMove)
        {
            int moveDir = 0;

            if (pg.player.computer)
            {
                ComputerAI.Decision d = brain != null
                    ? brain.Lane
                    : ComputerAI.Evaluate(transform, pg.player, hitTags, PaddleWall.LateralStopDistance(transform));
                moveDir = d.moveDir;
                thought = brain != null
                    ? brain.Thought
                    : (d.wantBump ? Thought.MoveUp : (d.wantSuper ? Thought.MoveDown : Thought.Nothing));
            }
            else
            {
                thought = Thought.Nothing;
                if (pg.inp.tf_dashRight)
                    moveDir += 1;
                if (pg.inp.tf_dashLeft)
                    moveDir -= 1;
            }

            pg.player.movementSpeed += moveDir;

            if (pg.player.movementSpeed < 0)
            {
                pg.player.movementSpeed = 4;
            }
            else if (pg.player.movementSpeed > 4)
            {
                pg.player.movementSpeed = 0;
            }
        }
    }

    void TickGold()
    {
        if (db == null || !db.gameStart || pg == null || !pg.IsLinked())
            return;
        if (pg.player.super != null)
        {
            pg.player.super.max = Mathf.Max(pg.player.super.max, 999f);
            pg.player.super.amount = Mathf.Floor(pg.player.super.amount);
        }
        goldTimer += Time.deltaTime;
        if (goldTimer < 6f)
            return;
        goldTimer -= 3f;
        AddGold(1);
    }

    public int Gold => pg != null && pg.player != null && pg.player.super != null
        ? Mathf.FloorToInt(pg.player.super.amount) : 0;

    public void AddGold(int amount)
    {
        if (amount == 0 || pg == null || pg.player == null || pg.player.super == null)
            return;
        Skill skill = pg.player.super;
        skill.max = Mathf.Max(skill.max, 999f);
        skill.amount = Mathf.Clamp(Mathf.Floor(skill.amount) + amount, 0f, skill.max);
    }

    public bool SpendGold(int amount)
    {
        if (amount < 0 || Gold < amount)
            return false;
        pg.player.super.amount = Gold - amount;
        return true;
    }

    public void PayDamageGold(int damage)
    {
        if (damage > 0)
            AddGold(damage * 5);
    }

    int SelectedUnit()
    {
        if (pg == null || pg.player == null)
            return 0;
        return Mathf.Clamp(Mathf.RoundToInt(pg.player.movementSpeed), 0, 4);
    }

    GameObject PrefabFor(int index)
    {
        switch (index)
        {
            case 1: return attackers;
            case 2: return patrollers;
            case 3: return rangers;
            case 4: return defenders;
            default: return reflectors;
        }
    }

    static PawnType TypeFor(int index)
    {
        switch (index)
        {
            case 1: return PawnType.Attacker;
            case 2: return PawnType.Patroller;
            case 3: return PawnType.Ranger;
            case 4: return PawnType.Defender;
            default: return PawnType.Reflector;
        }
    }

    void EnsureCursor()
    {
        if (cursor != null || ringPrefab == null)
            return;
        GameObject go = Instantiate(ringPrefab);
        cursor = go.GetComponent<GaurdCursor>();
        if (cursor == null)
            cursor = go.AddComponent<GaurdCursor>();
        Vector3 start = transform.position + transform.up * 3f;
        go.transform.position = GaurdCursor.ClampToField(start, 0.9f);
    }

    void MoveCursor()
    {
        if (cursor == null || pg == null || pg.player == null)
            return;

        float x = 0f;
        float y = 0f;
        const float dead = 0.35f;
        System.Collections.Generic.List<ControllerLink> links = new System.Collections.Generic.List<ControllerLink>();
        if (pg.player.inputLinks != null)
        {
            for (int i = 0; i < pg.player.inputLinks.Count; i++)
            {
                if (pg.player.inputLinks[i] != null && !links.Contains(pg.player.inputLinks[i]))
                    links.Add(pg.player.inputLinks[i]);
            }
        }
        if (pg.player.cLink != null && !links.Contains(pg.player.cLink))
            links.Add(pg.player.cLink);
        for (int i = 0; i < links.Count; i++)
        {
            ControllerButtons move = links[i]["Move"];
            if (move == null)
                continue;
            if (move.value.x > dead) x = 1f;
            else if (move.value.x < -dead) x = -1f;
            if (move.value.y > dead) y = 1f;
            else if (move.value.y < -dead) y = -1f;
        }

        bool into, back, right, left;
        switch (pg.player.facing)
        {
            case Facing.Down:
                into = y < 0f; back = y > 0f; right = x > 0f; left = x < 0f;
                break;
            case Facing.Left:
                into = x > 0f; back = x < 0f; right = y < 0f; left = y > 0f;
                break;
            case Facing.Right:
                into = x < 0f; back = x > 0f; right = y > 0f; left = y < 0f;
                break;
            default:
                into = y > 0f; back = y < 0f; right = x > 0f; left = x < 0f;
                break;
        }

        Vector3 step = Vector3.zero;
        if (into) step += transform.up;
        if (back) step -= transform.up;
        if (right) step += transform.right;
        if (left) step -= transform.right;
        step.z = 0f;
        if (step.sqrMagnitude > 1f)
            step.Normalize();
        Vector3 next = cursor.transform.position + step * 8f * Time.deltaTime;
        cursor.transform.position = GaurdCursor.ClampToField(next, 0.9f);
    }

    Pawn PawnUnderCursor()
    {
        if (cursor == null)
            return null;
        Collider[] hits = Physics.OverlapSphere(cursor.transform.position, 0.7f, ~0, QueryTriggerInteraction.Ignore);
        Pawn best = null;
        float bestD = 999f;
        for (int i = 0; i < hits.Length; i++)
        {
            Pawn pawn = hits[i].GetComponentInParent<Pawn>();
            if (pawn == null || pawn.selectionPiece || !pawns.Contains(pawn))
                continue;
            float d = Vector3.Distance(pawn.transform.position, cursor.transform.position);
            if (d < bestD)
            {
                bestD = d;
                best = pawn;
            }
        }
        hovering = best;
        return best;
    }

    bool SpotBlocked(Vector3 pos, Pawn ignore)
    {
        Collider[] hits = Physics.OverlapSphere(pos, 0.45f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null || hit.isTrigger)
                continue;
            if (ghost != null && (hit.transform == ghost.transform || hit.transform.IsChildOf(ghost.transform)))
                continue;
            if (hit.GetComponentInParent<GaurdCursor>() != null)
                continue;
            Pawn pawn = hit.GetComponentInParent<Pawn>();
            if (pawn != null && (pawn.selectionPiece || pawn == ignore))
                continue;
            if (hit.GetComponentInParent<ParticleSystem>() != null)
                continue;
            if (!CountsAsPlacementBlock(hit))
                continue;
            return true;
        }
        return false;
    }

    static bool CountsAsPlacementBlock(Collider hit)
    {
        string tag = hit.tag;
        if (tag == "Wall" || tag == "Walls")
            return false;
        if (tag == "Ball" || tag == "Player" || tag == "Paddle" || tag == "Hazard" || tag == "Obstacle")
            return true;
        Vector3 size = hit.bounds.size;
        if (size.x > 6f && size.y > 6f)
            return false;
        return false;
    }

    void ShowTroopGhost()
    {
        if (pg == null || !pg.IsLinked() || db == null || !db.gameStart)
        {
            if (cursor != null)
                cursor.gameObject.SetActive(false);
            if (ghost != null)
                ghost.gameObject.SetActive(false);
            return;
        }

        EnsureCursor();
        if (cursor == null)
            return;
        cursor.gameObject.SetActive(true);
        MoveCursor();

        Pawn under = PawnUnderCursor();
        int selected = SelectedUnit();
        int cost = Pawn.CostOf(TypeFor(selected));
        bool upgrading = under != null && under.level < 3;
        bool canAfford = upgrading
            ? Gold >= (under.level + 1) * Pawn.CostOf(under.pawnType)
            : Gold >= cost;
        bool blocked = !upgrading && SpotBlocked(cursor.transform.position, null);
        Color tint = Color.white;
        if (upgrading)
            tint = canAfford ? new Color(1f, 0.92f, 0.15f, 1f) : Color.red;
        else if (!canAfford || blocked)
            tint = Color.red;
        cursor.SetTint(tint);

        bool showGhost = under == null;
        if (!showGhost)
        {
            if (ghost != null)
                ghost.gameObject.SetActive(false);
            return;
        }

        if (ghost == null || ghostShown != selected)
        {
            if (ghost != null)
                Destroy(ghost.gameObject);
            GameObject prefab = PrefabFor(selected);
            if (prefab == null)
                return;
            GameObject body = Instantiate(prefab);
            ghost = body.GetComponent<Pawn>();
            if (ghost == null)
                return;
            ghost.selectionPiece = true;
            PlayerGrab grab = body.GetComponent<PlayerGrab>();
            if (grab != null)
                grab.playerIndex = pg.playerIndex;
            ghostShown = selected;
        }

        ghost.gameObject.SetActive(true);
        ghost.transform.position = cursor.transform.position;
        ghost.transform.rotation = Quaternion.identity;
    }

    void BuyTroop()
    {
        if (cursor == null || pg == null || !pg.IsLinked() || db == null || !db.gameStart)
            return;

        Pawn under = hovering;
        if (SuperPressed() && under != null && under.level < 3)
        {
            int price = (under.level + 1) * Pawn.CostOf(under.pawnType);
            if (SpendGold(price) && under.TryUpgrade())
                PlayUpgradeBurst(under.transform.position);
            return;
        }

        if (!pg.inp.tf_bump || under != null)
            return;
        int selected = SelectedUnit();
        int cost = Pawn.CostOf(TypeFor(selected));
        if (Gold < cost || SpotBlocked(cursor.transform.position, null))
            return;
        GameObject prefab = PrefabFor(selected);
        if (prefab == null || !SpendGold(cost))
            return;
        GameObject body = Instantiate(prefab, cursor.transform.position, Quaternion.identity);
        PlayerGrab grab = body.GetComponent<PlayerGrab>();
        if (grab != null)
            grab.playerIndex = pg.playerIndex;
        Pawn pawn = body.GetComponent<Pawn>();
        if (pawn != null)
            pawn.home = cursor.transform.position;
    }

    bool SuperPressed()
    {
        bool held = false;
        if (pg != null && pg.player != null)
        {
            if (pg.player.inputLinks != null)
            {
                for (int i = 0; i < pg.player.inputLinks.Count; i++)
                {
                    ControllerLink link = pg.player.inputLinks[i];
                    if (link != null && link["Interact"] != null && link["Interact"].isPressed)
                        held = true;
                }
            }
            if (pg.player.cLink != null && pg.player.cLink["Interact"] != null && pg.player.cLink["Interact"].isPressed)
                held = true;
        }
        bool pressed = held && !superWasHeld;
        superWasHeld = held;
        return pressed;
    }

    static void PlayUpgradeBurst(Vector3 pos)
    {
        GameObject go = new GameObject("Gaurd Upgrade");
        go.transform.position = pos;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.duration = 0.35f;
        main.startLifetime = 0.4f;
        main.startSpeed = 2.2f;
        main.startSize = 0.18f;
        main.startColor = new Color(1f, 0.86f, 0.15f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 16) });
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;
        ps.Play();
        Object.Destroy(go, 0.8f);
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
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.constraints = RigidbodyConstraints.FreezeAll;
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
        if (tpG != null && tpG.playerIndex == pg.playerIndex)
            return;
        BallInfo tbI = hit.GetComponent<BallInfo>();

        int baseDamage = 1;
        if (tbI != null && tbI.ball != null)
            baseDamage = Mathf.Max(1, tbI.ball.damage);

        int lost = pg.player.ApplyGoalDamage(baseDamage, hit.GetEntityId().GetHashCode());
        if (lost > 0)
            pg.player.RecordGoalConceded(lost);
        if (lost > 0 && tpG != null && tpG.IsLinked() && tpG.player != null
            && tpG.playerIndex != pg.playerIndex)
        {
            tpG.player.RecordDamageDealt(lost, pg.player);
            tpG.player.RecordGoalScored(lost, pg.player);
        }

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
        if (cursor != null)
            Destroy(cursor.gameObject);
        if (ghost != null)
            Destroy(ghost.gameObject);
        DestroyOwnedSpawns();
    }
}
