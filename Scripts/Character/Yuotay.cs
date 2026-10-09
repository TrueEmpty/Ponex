using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Yuotay : MonoBehaviour
{
    public Transform batHitPoint;
    public Transform batGrip;
    public Transform batWeight;
    Rigidbody rb;
    Database db;
    PlayerGrab pg;

    public List<string> hitTags = new List<string>();

    public float dis = 1.38f;

    int canDash = 0;
    float dashEnd = 0;
    float speedIncrease = 1;
    public float speedMultiplyer = 4;
    float doubleClickTime = .3f;
    public float dashTime = .3f;

    public float swingSpeed = 50;
    public int lastfacing = 0;
    public float facingSpeed = 5;
    public float swingingMoveSpeedOffset = 2;
    public bool swinging = false;
    public bool swung = false;
    public bool bunting = false;
    public float swingTimeDelay = .1f;
    public float swingTimer = 0;
    float basePushBack = 10;

    StickOnCollision stickOnCollision = null;
    float unstickTime = -1;
    public Vector2 unstickRange = Vector2.zero;

    [Header("Bat hit force (speed imparted to ball)")]
    public float idleHitSpeed = 4f;
    public float buntHitSpeed = 6f;
    public float swingHitSpeedMin = 12f;
    public float swingHitSpeedMax = 22f;
    float lastBatHitTime = -999f;
    const float BatHitCooldown = 0.05f;

    [SerializeField]
    public List<Vector4> swingPoints = new List<Vector4>();
    int lastMoveDir = 0;
    int lastNonZeroMoveDir = 0;

    [SerializeField]
    Thought thought = Thought.Nothing;

    PaddleWall.LaneLock lane;
    bool wallsIgnored;
    bool mitIgnored;
    Collider[] batColliders;
    Collider[] bodyColliders;
    Collider[] allColliders;
    ComputerBrain brain;
    float laneMin;
    float laneMax;
    float laneRefresh;
    static readonly List<string> ObstacleTags = new List<string> { "Obstacle" };

    public static bool IsYuotayPlayer(Player p)
    {
        if (p == null)
            return false;
        if (!string.IsNullOrEmpty(p.name)
            && p.name.Equals("Yuotay", System.StringComparison.OrdinalIgnoreCase))
            return true;
        return p.character != null && p.character.prefabs != null
            && p.character.prefabs.GetComponent<Yuotay>() != null;
    }

    /// <summary>Two Yuotays on one wall sit side-by-side so a bat cannot spawn on top of the other and get shoved to the far wall.</summary>
    public static void PrepareSameWallSeats(List<Player> players, float fieldPlaySize)
    {
        if (players == null)
            return;

        float halfGap = Mathf.Clamp(fieldPlaySize * 0.18f, 2.8f, 5f);

        for (int f = 0; f < 4; f++)
        {
            Facing facing = (Facing)f;
            List<Player> group = new List<Player>(2);
            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];
                if (p == null || p.facing != facing || !IsYuotayPlayer(p))
                    continue;
                group.Add(p);
            }
            if (group.Count < 2)
                continue;

            group.Sort((a, b) =>
            {
                int c = a.position.CompareTo(b.position);
                return c != 0 ? c : a.index.CompareTo(b.index);
            });

            for (int i = 0; i < group.Count; i++)
            {
                if (Mathf.Abs(group[i].ticWallSideOffset) > 0.01f)
                    continue;
                float t = group.Count == 1 ? 0.5f : i / (float)(group.Count - 1);
                group[i].ticWallSideOffset = Mathf.Lerp(-halfGap, halfGap, t);
            }
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;
        brain = ComputerBrain.Ensure(gameObject, ComputerBrain.Mode.LanePaddle);
        if (brain != null)
        {
            // Don't treat stadium segments as AI blockers — clamp handles the real ends
            brain.hitTags = ObstacleTags;
            brain.wallStopDistance = dis;
        }

        if (pg != null && pg.player != null)
            lane.Ensure(rb, pg.player.facing);

        rb.useGravity = false;
        if (GetComponent<DestroyOnDeath>() == null)
            gameObject.AddComponent<DestroyOnDeath>();

        // Child colliders except the root body — bat mesh pushes into walls during swings
        List<Collider> bats = new List<Collider>();
        List<Collider> body = new List<Collider>();
        allColliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < allColliders.Length; i++)
        {
            Collider c = allColliders[i];
            if (c == null)
                continue;
            if (c.transform == transform)
                body.Add(c);
            else
                bats.Add(c);
        }
        bodyColliders = body.ToArray();
        batColliders = bats.ToArray();
        IgnoreStadiumAndMit();
        StartCoroutine(IgnoreStadiumAfterWallsSpawn());
    }

    void Update()
    {
        if (db == null || pg == null || pg.player == null)
            return;

        Player p = pg.player;
        lane.Ensure(rb, p.facing);

        if (p.currentHealth <= 0)
        {
            DespawnWithMit();
            return;
        }

        if (db.gameStart && p.currentHealth > 0)
        {
            IgnoreStadiumAndMit();
            Unstick();

            if (p.CanMove)
            {
                if (!swinging && p.CanDash)
                    OnDash();

                OnMove();
            }
            else
            {
                rb.linearVelocity = Vector3.zero;
            }

            PaddleWall.Unstick(rb, transform, ObstacleTags, bodyColliders);
            ClampToSide();

            if (p.CanBump)
                OnBump();

            if (p.CanSuper && !swinging)
                OnSuper();

            if (dashEnd > 0)
                dashEnd -= Time.deltaTime;
            else
            {
                canDash = 0;
                speedIncrease = 1;
            }

            p.dash.Charge();
            p.bump.Charge();

            bool wantBunt = p.computer
                ? thought == Thought.MoveDown
                : pg.inp.super;

            if (bunting && !(wantBunt && p.CanSuper))
            {
                p.RemoveConstraint(gameObject);

                if (!swinging)
                    p.pushBack = basePushBack;

                bunting = false;
            }

            if (batGrip != null && batHitPoint != null)
                batGrip.LookAt(batHitPoint);

            ClampToSide();
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
            ClampToSide();
    }

    void ClampToSide()
    {
        lane.Clamp(rb);
        if (pg != null && pg.player != null)
            PaddleWall.ClampToLaneLimits(rb, transform, pg.player.facing, ref laneMin, ref laneMax, ref laneRefresh);
    }

    void DespawnWithMit()
    {
        Player p = pg != null ? pg.player : null;
        if (p != null && p.spawnedLifeline != null)
        {
            Destroy(p.spawnedLifeline);
            p.spawnedLifeline = null;
        }
        Destroy(gameObject);
    }

    IEnumerator IgnoreStadiumAfterWallsSpawn()
    {
        yield return new WaitForEndOfFrame();
        wallsIgnored = false;
        mitIgnored = false;
        IgnoreStadiumAndMit();
    }

    void IgnoreStadiumAndMit()
    {
        if (allColliders == null)
            allColliders = GetComponentsInChildren<Collider>(true);

        if (!wallsIgnored)
        {
            PaddleWall.IgnoreBoundaryWallCollisions(transform, allColliders);
            wallsIgnored = true;
        }

        if (mitIgnored || pg == null || pg.player == null || pg.player.spawnedLifeline == null)
            return;

        Collider[] mitCols = pg.player.spawnedLifeline.GetComponentsInChildren<Collider>(true);
        PaddleWall.IgnoreColliderSets(allColliders, mitCols);
        mitIgnored = true;
    }

    void Unstick()
    {
        Player p = pg.player;
        if (p == null)
            return;

        if (stickOnCollision == null)
        {
            if (p.spawnedLifeline != null)
                stickOnCollision = p.spawnedLifeline.GetComponent<StickOnCollision>();
        }
        else if (stickOnCollision.stuckObjects.Count > 0)
        {
            if (unstickTime <= -1)
            {
                unstickTime = Time.time + Random.Range(unstickRange.x, unstickRange.y);
            }
            else if (unstickTime <= Time.time)
            {
                stickOnCollision.Unstick();
                unstickTime = -1;
            }
        }
    }

    void OnMove()
    {
        int moveDir = 0;

        if (pg.player.computer)
        {
            ComputerAI.Decision d = brain != null
                ? brain.Lane
                : ComputerAI.Evaluate(transform, pg.player, hitTags, dis);
            moveDir = d.moveDir;
            if (brain != null)
            {
                thought = brain.Thought;
                if (thought == Thought.Nothing)
                {
                    if (d.moveDir > 0) thought = Thought.MoveRight;
                    else if (d.moveDir < 0) thought = Thought.MoveLeft;
                }
            }
            else if (d.wantBump) thought = Thought.MoveUp;
            else if (d.wantSuper) thought = Thought.MoveDown;
            else if (d.moveDir > 0) thought = Thought.MoveRight;
            else if (d.moveDir < 0) thought = Thought.MoveLeft;
            else thought = Thought.Nothing;
        }
        else
        {
            thought = Thought.Nothing;
            if (pg.inp.right)
                moveDir += 1;
            if (pg.inp.left)
                moveDir -= 1;
        }

        if (moveDir != 0 && WallInDirection(moveDir))
            moveDir = 0;

        if (moveDir > 0)
        {
            lastNonZeroMoveDir = 1;
            lastfacing = 0;
            swingTimer = Time.time;
            swung = false;
        }
        else if (moveDir < 0)
        {
            lastNonZeroMoveDir = -1;
            lastfacing = 1;
            swingTimer = Time.time;
            swung = false;
        }

        Vector3 rotDir = PaddleWall.AbsAxes(transform.right);
        float swingSlow = swinging ? swingingMoveSpeedOffset : 1f;
        rb.linearVelocity = rotDir * moveDir * (speedIncrease / swingSlow) * pg.player.EffectiveMovementSpeed;

        if (!swinging && batHitPoint != null)
        {
            // lastNonZeroMoveDir is screen/AbsAxes space; bat poses are local — convert for top (Z=180) etc.
            int swingLocal = MoveDirToLocalBatX(lastNonZeroMoveDir);
            // Wind up opposite the swing arc (local +X during SwingBat)
            Vector3 gotoPos = new Vector3(-swingLocal * 2f, -2f, -1.5f);
            batHitPoint.localPosition = Vector3.Lerp(batHitPoint.localPosition, gotoPos, facingSpeed * Time.deltaTime);

            if (moveDir == 0 && swingTimer + swingTimeDelay < Time.time && !swung)
            {
                swinging = true;
                swung = true;
                StartCoroutine(SwingBat(swingLocal));
            }
        }

        lastMoveDir = moveDir;
    }

    void OnDash()
    {
        Skill d = pg.player.dash;
        if (d == null)
            return;

        if (pg.player.computer)
        {
            ComputerAI.Decision ai = ComputerAI.GetLastDecision(pg.playerIndex);
            if (ai.wantDash && ai.moveDir != 0 && d.amount >= d.cost && Mathf.Abs(canDash) != 2)
                StartDash(ai.moveDir > 0 ? 1 : -1, d);
            return;
        }

        if (pg.inp.tf_dashRight && d.amount >= d.cost && Mathf.Abs(canDash) != 2)
        {
            StartDash(1, d);
            return;
        }
        if (pg.inp.tf_dashLeft && d.amount >= d.cost && Mathf.Abs(canDash) != 2)
        {
            StartDash(-1, d);
            return;
        }

        if (pg.inp.tf_right && d.amount >= d.cost)
        {
            if (Mathf.Abs(canDash) == 2) { }
            else if (canDash == 1 && dashEnd > 0)
                StartDash(1, d);
            else
            {
                canDash = 1;
                dashEnd = doubleClickTime;
            }
        }

        if (pg.inp.tf_left && d.amount >= d.cost)
        {
            if (Mathf.Abs(canDash) == 2) { }
            else if (canDash == -1 && dashEnd > 0)
                StartDash(-1, d);
            else
            {
                canDash = -1;
                dashEnd = doubleClickTime;
            }
        }
    }

    void StartDash(int dir, Skill d)
    {
        speedIncrease = speedMultiplyer;
        dashEnd = dashTime;
        d.readyPercent = 0;
        d.Spend();
        canDash = dir > 0 ? 2 : -2;
        pg.player.RecordDash();
    }

    void OnBump()
    {
        Skill b = pg.player.bump;
        if (b == null)
            return;

        bool wantBump = pg.player.computer
            ? thought == Thought.MoveUp
            : pg.inp.tf_bump;

        if (wantBump && b.Enough())
        {
            b.Spend();
            thought = Thought.Nothing;
        }
    }

    void OnSuper()
    {
        Player p = pg.player;
        if (p == null || batHitPoint == null)
            return;

        bool wantBunt = p.computer
            ? thought == Thought.MoveDown
            : pg.inp.super;

        if (!wantBunt)
            return;

        p.AddConstraint(gameObject);
        p.pushBack = 100;
        Vector3 nP = new Vector3(MoveDirToLocalBatX(lastNonZeroMoveDir) * 2f, 0f, 0f);
        batHitPoint.localPosition = Vector3.Lerp(batHitPoint.localPosition, nP, p.super.speed * Time.deltaTime);

        bunting = true;
        swung = true;
    }

    bool WallInDirection(int dir)
    {
        if (pg == null || pg.player == null)
            return false;
        return PaddleWall.LaneEndInDirection(
            transform, pg.player.facing, dir, ref laneMin, ref laneMax, ref laneRefresh);
    }

    /// <summary>
    /// Movement ±1 follows AbsAxes (screen-consistent). Bat local +X follows transform.right,
    /// which is flipped on top (Facing.Down / Z=180). Convert so swing continues the move.
    /// </summary>
    int MoveDirToLocalBatX(int moveDir)
    {
        if (moveDir == 0)
            return 0;

        Vector3 localRight = transform.right;
        localRight.z = 0f;
        Vector3 screenRight = PaddleWall.AbsAxes(transform.right);
        if (localRight.sqrMagnitude < 0.0001f || screenRight.sqrMagnitude < 0.0001f)
            return moveDir;

        return Vector3.Dot(localRight.normalized, screenRight) < 0f ? -moveDir : moveDir;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.gameObject == null)
            return;
        if (!collision.gameObject.CompareTag("Ball"))
            return;
        if (Time.time - lastBatHitTime < BatHitCooldown)
            return;

        Rigidbody ballRb = collision.rigidbody;
        if (ballRb == null)
            ballRb = collision.gameObject.GetComponent<Rigidbody>();
        if (ballRb == null)
            return;

        // Away from the mit / wall = into the field along paddle forward
        Vector3 awayFromMit = transform.up;
        awayFromMit.z = 0f;
        if (awayFromMit.sqrMagnitude < 0.0001f)
            awayFromMit = Vector3.up;
        awayFromMit.Normalize();

        // Blend a little contact normal so glancing hits still look natural
        if (collision.contactCount > 0)
        {
            Vector3 n = -collision.GetContact(0).normal;
            n.z = 0f;
            if (n.sqrMagnitude > 0.0001f)
            {
                n.Normalize();
                // Prefer field direction so we don't bounce back into the mit
                if (Vector3.Dot(n, awayFromMit) < 0f)
                    n = Vector3.Reflect(n, awayFromMit);
                awayFromMit = (awayFromMit * 0.75f + n * 0.25f).normalized;
            }
        }

        float speed;
        if (bunting)
            speed = buntHitSpeed;
        else if (swinging)
        {
            // pushBack ramps 500→5000 during the sweet spot of the swing
            float t = Mathf.InverseLerp(500f, 5000f, pg.player != null ? pg.player.pushBack : 500f);
            speed = Mathf.Lerp(swingHitSpeedMin, swingHitSpeedMax, Mathf.Clamp01(t));
        }
        else
            speed = idleHitSpeed;

        Vector3 v = awayFromMit * speed;
        v.z = 0f;
        ballRb.linearVelocity = v;
        lastBatHitTime = Time.time;

        if (bunting)
            YuotaySfx.PlayBunt();
        else if (swinging)
            YuotaySfx.PlayBatHit();
        else
            YuotaySfx.PlayBunt(0.5f); // soft dribble contact
    }

    IEnumerator SwingBat(int curDir)
    {
        Player p = pg.player;
        if (p == null || batHitPoint == null)
        {
            swinging = false;
            yield break;
        }

        YuotaySfx.PlaySwingWhoosh();

        float lP = 0;
        Vector3 startPos = new Vector3(0, -2, 0);
        Vector3 endPos = new Vector3(0, 2, 0);
        Vector3 curPos = new Vector3(0, 2, 0);

        while (lP < 1)
        {
            lP += swingSpeed * Time.deltaTime;
            curPos = Vector3.Lerp(startPos, endPos, lP);
            float opMiss = (Mathf.Abs(.5f - lP) * 2);

            if (opMiss <= .8f)
                opMiss = 0;

            float missing = (1 - opMiss);
            curPos.x = curDir * (missing * 2);
            curPos.z = (opMiss * -1.5f);

            p.pushBack = 500 + (missing * 4500);

            batHitPoint.localPosition = curPos;
            ClampToSide();
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSecondsRealtime(.1f);

        p.pushBack = basePushBack;
        swinging = false;
        ClampToSide();
        yield return null;
    }
}
