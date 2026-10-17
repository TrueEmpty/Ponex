using System.Collections.Generic;
using UnityEngine;

public partial class Pawn
{
    public PatrolGroup patrol;
    public Vector3 home;
    public int lane;
    public float shotCooldown;
    public float shotWindow = 1f;
    public float placedAt = -1f;
    public bool PaysGold => !selectionPiece && placedAt >= 0f && Time.time >= placedAt + 3f;
    public float reflectorLife = 1f;
    public float reflectorDrain = 0.1f;
    float reflectorTick;

    Vector3 baseScale = Vector3.one;
    bool scaleReady;
    int appliedLevel = -1;
    GameObject shotPrefab;
    Vector3 dashVel;
    float dashLock;
    bool ghostReady;

    public int MaxHealth => MaxFor(pawnType, level);

    public float CooldownFill => shotWindow <= 0.01f ? 0f : Mathf.Clamp01(shotCooldown / shotWindow);

    public static int CostOf(PawnType type)
    {
        switch (type)
        {
            case PawnType.Attacker: return 2;
            case PawnType.Patroller: return 3;
            case PawnType.Ranger: return 4;
            case PawnType.Defender: return 5;
            default: return 1;
        }
    }

    public static int MaxFor(PawnType type, int lv)
    {
        switch (type)
        {
            case PawnType.Attacker: return lv >= 3 ? 2 : 1;
            case PawnType.Patroller: return lv >= 3 ? 4 : (lv >= 2 ? 2 : 1);
            case PawnType.Defender: return lv >= 3 ? 8 : (lv >= 2 ? 5 : 3);
            default: return 1;
        }
    }

    public bool TryUpgrade()
    {
        if (level >= 3)
            return false;
        if (patrol != null)
            return patrol.Upgrade();
        level++;
        if (pawnType == PawnType.Reflector)
        {
            reflectorLife = Mathf.Floor(reflectorLife) + 1f;
            reflectorDrain *= 0.5f;
            health = Mathf.CeilToInt(reflectorLife);
            ApplyBody(false);
            return true;
        }
        ApplyBody(true);
        return true;
    }

    void BeginRole()
    {
        placedAt = Time.time;
        if (pawnType == PawnType.Patroller && patrol == null)
            patrol = new PatrolGroup(this);
        ApplyBody(true);
        shotPrefab = Resources.Load<GameObject>("Gaurd/Ranger Shot");
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void ApplyBody(bool refill)
    {
        if (!scaleReady)
        {
            baseScale = transform.localScale;
            scaleReady = true;
        }
        bool levelChanged = appliedLevel != level;
        appliedLevel = level;
        if (refill && levelChanged)
            health = MaxHealth;
        if (pawnType == PawnType.Reflector)
        {
            float s = level >= 3 ? 2f : (level >= 2 ? 1.4f : 1f);
            transform.localScale = baseScale * s;
        }
    }

    void TickRole()
    {
        if (db == null || !db.gameStart)
            return;
        switch (pawnType)
        {
            case PawnType.Reflector: TickReflector(); break;
            case PawnType.Attacker: TickAttacker(); break;
            case PawnType.Patroller: TickPatroller(); break;
            case PawnType.Ranger: TickRanger(); break;
            case PawnType.Defender: TickDefender(); break;
        }
    }

    void TickReflector()
    {
        reflectorTick += Time.deltaTime;
        if (reflectorTick < 1f)
            return;
        int steps = Mathf.FloorToInt(reflectorTick);
        reflectorTick -= steps;
        reflectorLife = Mathf.Max(0f, reflectorLife - reflectorDrain * steps);
        health = Mathf.CeilToInt(reflectorLife);
        if (reflectorLife <= 0f)
            Destroy(gameObject);
    }

    void TickAttacker()
    {
        if (level < 2)
            return;

        dashLock -= Time.deltaTime;
        BallInfo ball = ClosestBall(level >= 3 ? 80f : 4.5f);
        if (level >= 3 && ball != null)
        {
            Vector3 to = ball.transform.position - transform.position;
            to.z = 0f;
            if (to.sqrMagnitude > 0.04f)
                dashVel = to.normalized * 6.5f;
        }
        else if (level == 2 && ball != null && dashLock <= 0f && dashVel.sqrMagnitude < 0.2f)
        {
            Vector3 to = ball.transform.position - transform.position;
            to.z = 0f;
            if (to.sqrMagnitude > 0.04f)
            {
                dashVel = to.normalized * 10f;
                dashLock = 0.85f;
            }
        }

        if (dashVel.sqrMagnitude < 0.01f)
            return;

        if (level == 2)
        {
            dashVel = Vector3.Lerp(dashVel, Vector3.zero, Time.deltaTime * 0.35f);
            if (dashVel.magnitude < 1.2f)
                dashVel = Vector3.zero;
        }
    }

    void FixedUpdate()
    {
        if (selectionPiece || pawnType != PawnType.Attacker || dashVel.sqrMagnitude < 0.01f)
            return;
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            return;
        Vector3 next = body.position + dashVel * Time.fixedDeltaTime;
        next.z = home.z;
        body.MovePosition(GaurdCursor.ClampToField(next, 0.45f));
    }

    void TickPatroller()
    {
        if (patrol == null)
            return;
        float dist = level >= 2 ? 3.2f : 1.6f;
        float speed = 2.6f;
        Vector3 axis = patrol.Axis(lane);
        float t = Mathf.PingPong(Time.time * speed, dist);
        if (level <= 1)
            transform.position = home + axis * (t - dist * 0.5f);
        else
            transform.position = home + axis.normalized * t * Mathf.Sign(lane == 0 || lane == 2 ? -1f : 1f);
        Vector3 p = transform.position;
        p.z = home.z;
        transform.position = GaurdCursor.ClampToField(p, 0.4f);
    }

    void TickRanger()
    {
        shotCooldown -= Time.deltaTime;
        float range = level >= 3 ? 10f : (level >= 2 ? 5.5f : 3.2f);
        float window = level >= 3 ? 0.75f : (level >= 2 ? 1.35f : 2.6f);
        shotWindow = window;
        BallInfo ball = ClosestBall(range);
        if (ball == null)
            return;
        Vector3 to = ball.transform.position - transform.position;
        to.z = 0f;
        if (to.sqrMagnitude < 0.04f)
            return;
        transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.back);
        if (shotCooldown > 0f || shotPrefab == null)
            return;
        GameObject shot = Instantiate(shotPrefab, transform.position, Quaternion.identity);
        GaurdShot bullet = shot.GetComponent<GaurdShot>();
        if (bullet != null)
            bullet.Launch(to, pg.playerIndex);
        shotCooldown = window;
    }

    void TickDefender()
    {
        if (level < 2)
            return;
        BallInfo ball = ClosestBall(6.5f);
        if (ball == null || pg == null || pg.player == null || pg.player.spawnedPlayer == null)
            return;
        Transform guard = pg.player.spawnedPlayer.transform;
        Vector3 axis = guard.right;
        axis.z = 0f;
        if (axis.sqrMagnitude < 0.001f)
            axis = Vector3.right;
        axis.Normalize();
        float along = Vector3.Dot(ball.transform.position - transform.position, axis);
        float speed = level >= 3 ? 11f : 6.5f;
        float step = Mathf.Clamp(along, -speed * Time.deltaTime, speed * Time.deltaTime);
        Vector3 next = transform.position + axis * step;
        next.z = home.z;
        transform.position = GaurdCursor.ClampToField(next, 0.5f);
    }

    BallInfo ClosestBall(float range)
    {
        BallInfo[] balls = FindObjectsByType<BallInfo>(FindObjectsInactive.Exclude);
        BallInfo best = null;
        float bestD = range * range;
        for (int i = 0; i < balls.Length; i++)
        {
            if (balls[i] == null)
                continue;
            Vector3 d = balls[i].transform.position - transform.position;
            d.z = 0f;
            float sq = d.sqrMagnitude;
            if (sq < bestD)
            {
                bestD = sq;
                best = balls[i];
            }
        }
        return best;
    }

    bool AlreadyOurBall(PlayerGrab ballGrab)
    {
        if (pg == null || ballGrab == null || ballGrab.playerIndex != pg.playerIndex)
            return false;
        return true;
    }

    int lastBallHitId;
    int lastBallHitFrame;

    public void TakeBallHit(GameObject ball)
    {
        if (selectionPiece || ball == null)
            return;
        int id = ball.GetEntityId().GetHashCode();
        if (lastBallHitId == id && lastBallHitFrame == Time.frameCount)
            return;
        lastBallHitId = id;
        lastBallHitFrame = Time.frameCount;

        if (pg == null)
            pg = GetComponent<PlayerGrab>();
        int amount = 1;
        BallInfo info = ball.GetComponent<BallInfo>();
        if (info != null && info.ball != null)
            amount = Mathf.Max(1, info.ball.damage);
        ApplyHit(amount, ball);
        Claim(ball);
    }

    void ApplyHit(int amount, GameObject ball)
    {
        if (amount < 0)
            amount = 0;
        if (pawnType == PawnType.Reflector)
        {
            reflectorLife = Mathf.Max(0f, reflectorLife - amount);
            health = Mathf.CeilToInt(reflectorLife);
            if (reflectorLife <= 0f)
                Die(ball);
            return;
        }
        if (patrol != null)
        {
            patrol.Hit(amount, ball);
            return;
        }
        health -= amount;
        if (health <= 0)
            Die(ball);
    }

    void Die(GameObject ball)
    {
        if (ball != null)
            Claim(ball);
        if (patrol != null)
        {
            patrol.Kill(ball);
            return;
        }
        Destroy(gameObject);
    }

    void Claim(GameObject ball)
    {
        if (ball == null)
            return;
        if (pg == null)
            pg = GetComponent<PlayerGrab>();
        if (pg == null || pg.playerIndex < 0)
            return;
        PlayerGrab[] grabs = ball.GetComponentsInParent<PlayerGrab>(true);
        if (grabs == null || grabs.Length == 0)
            grabs = ball.GetComponentsInChildren<PlayerGrab>(true);
        for (int i = 0; i < grabs.Length; i++)
        {
            if (grabs[i] != null)
                grabs[i].playerIndex = pg.playerIndex;
        }
    }

    public Gaurd Owner()
    {
        if (pg == null || !pg.IsLinked() || pg.player == null || pg.player.spawnedPlayer == null)
            return null;
        return pg.player.spawnedPlayer.GetComponent<Gaurd>();
    }

    void Bounce(Collision collision)
    {
        if (dashVel.sqrMagnitude < 0.05f || collision.contactCount == 0)
            return;
        Vector3 n = collision.GetContact(0).normal;
        n.z = 0f;
        if (n.sqrMagnitude < 0.001f)
            return;
        dashVel = Vector3.Reflect(dashVel, n.normalized);
    }

    void PassThrough(Collider other)
    {
        if (other == null)
            return;
        Collider[] mine = GetComponents<Collider>();
        for (int i = 0; i < mine.Length; i++)
        {
            if (mine[i] != null && mine[i].enabled)
                Physics.IgnoreCollision(mine[i], other, true);
        }
    }

    static bool IsBall(Collider other)
    {
        return other != null && other.CompareTag("Ball");
    }

    static bool IsStructure(Collider other)
    {
        if (other == null)
            return false;
        return other.CompareTag("Wall") || other.CompareTag("Walls") || other.CompareTag("Obstacle");
    }

    void PrepareGhost()
    {
        if (ghostReady)
            return;
        ghostReady = true;
        Collider[] cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].enabled = true;
            cols[i].isTrigger = true;
        }
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = true;
        }
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] == null || scripts[i] == this)
                continue;
            scripts[i].enabled = false;
        }
        PawnHealthBar bar = GetComponentInChildren<PawnHealthBar>(true);
        if (bar != null)
            bar.gameObject.SetActive(false);
    }

    void PaintGhost()
    {
        if (ren == null)
            ren = GetComponentInChildren<Renderer>();
        if (ren == null || pg == null || !pg.IsLinked() || pg.player == null)
            return;
        if (material == null)
            material = ren.material;
        Color c = PlayerSkin.GetColor(pg.player, db);
        c.a = 4f / 255f;
        material.color = c;
    }
}

public class PatrolGroup
{
    public int level = 1;
    public int health = 1;
    public Vector3 origin;
    public Vector3 right = Vector3.right;
    public Vector3 forward = Vector3.up;
    public List<Pawn> members = new List<Pawn>();
    bool dead;

    public PatrolGroup(Pawn first)
    {
        origin = first.home == Vector3.zero ? first.transform.position : first.home;
        Gaurd owner = first.Owner();
        if (owner != null)
        {
            right = owner.transform.right;
            forward = owner.transform.up;
            right.z = 0f;
            forward.z = 0f;
            if (right.sqrMagnitude > 0.001f) right.Normalize();
            if (forward.sqrMagnitude > 0.001f) forward.Normalize();
        }
        first.home = origin;
        first.lane = 0;
        first.patrol = this;
        members.Add(first);
        health = first.MaxHealth;
        level = first.level;
        Sync();
    }

    public Vector3 Axis(int lane)
    {
        if (lane >= 2)
            return forward;
        return right;
    }

    public void Hit(int amount, GameObject ball)
    {
        health -= amount;
        Sync();
        if (health <= 0)
            Kill(ball);
    }

    public bool Upgrade()
    {
        if (level >= 3)
            return false;
        level++;
        Pawn source = members.Count > 0 ? members[0] : null;
        if (source == null)
            return false;
        GameObject prefab = null;
        Gaurd owner = source.Owner();
        if (owner != null)
            prefab = owner.patrollers;
        int want = level >= 3 ? 4 : (level >= 2 ? 2 : 1);
        while (members.Count < want && prefab != null)
        {
            GameObject go = Object.Instantiate(prefab, origin, Quaternion.identity);
            PlayerGrab grab = go.GetComponent<PlayerGrab>();
            PlayerGrab src = source.GetComponent<PlayerGrab>();
            if (grab != null && src != null)
                grab.playerIndex = src.playerIndex;
            Pawn pawn = go.GetComponent<Pawn>();
            pawn.patrol = this;
            pawn.home = origin;
            pawn.lane = members.Count;
            pawn.level = level;
            members.Add(pawn);
        }
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] == null)
                continue;
            members[i].level = level;
            members[i].lane = i;
            members[i].home = origin;
            members[i].patrol = this;
        }
        health = Pawn.MaxFor(PawnType.Patroller, level);
        Sync();
        return true;
    }

    void Sync()
    {
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] == null)
                continue;
            members[i].health = health;
            members[i].level = level;
        }
    }

    public void Kill(GameObject ball)
    {
        if (dead)
            return;
        dead = true;
        if (ball != null)
        {
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i] != null)
                {
                    PlayerGrab grab = ball.GetComponent<PlayerGrab>();
                    PlayerGrab mine = members[i].GetComponent<PlayerGrab>();
                    if (grab != null && mine != null)
                        grab.playerIndex = mine.playerIndex;
                    break;
                }
            }
        }
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] != null)
                Object.Destroy(members[i].gameObject);
        }
        members.Clear();
    }
}
