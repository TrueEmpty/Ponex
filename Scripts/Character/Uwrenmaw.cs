using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uwrenmaw circles his pillars, then flies off like a ball with a trailing sand tail.
/// Head is about half of Nari's head mesh (1.3 → 0.65). Health is 3 per finished pillar.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Uwrenmaw : MonoBehaviour
{
    const int SegmentCount = 8;
    const float HeadScale = 0.65f;
    const float SegmentScale = 0.5f;
    const float SegmentSpacing = 0.36f;
    const float OrbitRadius = 1.45f;
    const float CatchPadding = 1.25f;
    const float OrbitSpeedScale = 1f / 3f;
    const float AirSpeedScale = 1.18f;
    const float PillarWidth = 0.84f;
    const float PillarDepth = 3.4f;
    const int LapsPerTick = 5;

    Rigidbody rb;
    PlayerGrab pg;
    Database db;

    readonly List<Transform> segments = new List<Transform>();
    readonly List<Vector3> trail = new List<Vector3>(96);
    readonly List<UwrenmawPillar> pillars = new List<UwrenmawPillar>();

    UwrenmawPillar orbit;
    UwrenmawPillar growing;
    float circleSign = 1f;
    float lapAngle;
    bool flying = true;
    bool dashing;
    float dashUntil;
    float playZ;
    UwrenmawPillar ignorePillar;
    bool leftIgnoreRange;
    bool placed;
    float flySpeed;
    Vector3 flyDir = Vector3.up;

    public float circleDashMultiply = 2.15f;
    public float airDashMultiply = 2.6f;
    public float circleDashTime = 1.15f;
    public float airDashTime = 0.28f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;

        transform.localScale = Vector3.one * HeadScale;
        rb.useGravity = false;
        rb.mass = 1.2f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere == null)
            sphere = gameObject.AddComponent<SphereCollider>();
        sphere.radius = 0.5f;
        ApplyBounce(sphere);

        if (GetComponent<ClearAfterTheGame>() == null)
            gameObject.AddComponent<ClearAfterTheGame>();

        if (GetComponent<MeshFilter>() == null)
        {
            GameObject view = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            view.name = "Head";
            view.transform.SetParent(transform, false);
            view.transform.localPosition = Vector3.zero;
            view.transform.localScale = Vector3.one;
            Collider extra = view.GetComponent<Collider>();
            if (extra != null)
                Destroy(extra);
            Paint(view.GetComponent<Renderer>(), UwrenmawArt.Body());
        }

        BuildTail();
    }

    void Start()
    {
        if (db == null)
            db = Database.instance;
        playZ = transform.position.z;
        flySpeed = pg != null && pg.player != null ? pg.player.EffectiveMovementSpeed : 6f;
        if (flySpeed < 1f)
            flySpeed = 6f;
    }

    void Update()
    {
        if (pg == null || pg.player == null || db == null)
            return;

        if (!placed)
            PlaceOpening();

        if (!db.gameStart || pg.player.currentHealth <= 0)
            return;

        // Jump, Super, and the other face buttons. Dash is handled on its own.
        bool action = pg.inp.tf_up || pg.inp.tf_down || pg.inp.tf_left || pg.inp.tf_right || pg.inp.tf_super;
        bool dash = pg.inp.tf_dashLeft || pg.inp.tf_dashRight;
        if (pg.player.computer)
            Think(ref action, ref dash);

        if (dash && pg.player.CanDash && pg.player.dash != null && pg.player.dash.Enough() && !dashing)
            StartDash();

        if (action && !dashing)
        {
            if (orbit != null)
                Release(false);
            else if (pg.player.super != null && pg.player.super.Enough())
                BeginGrow();
        }

        if (orbit == null && pg.player.super != null)
            pg.player.super.Charge();

        if (pg.player.dash != null)
            pg.player.dash.Charge();

        if (dashing && Time.time >= dashUntil)
            EndDash();
    }

    void FixedUpdate()
    {
        if (pg == null || pg.player == null || db == null || !placed || pg.player.currentHealth <= 0)
            return;
        if (!db.gameStart && orbit == null)
            return;

        Vector3 pos = rb.position;
        pos.z = playZ;

        if (orbit != null)
        {
            float angSpeed = (flySpeed / 1.05f) * OrbitSpeedScale * (dashing ? circleDashMultiply : 1f);
            float speed = angSpeed * OrbitRadius;
            Vector3 center = orbit.transform.position;
            center.z = playZ;
            Vector3 radial = pos - center;
            radial.z = 0f;
            if (radial.sqrMagnitude < 0.0001f)
                radial = Vector3.right;
            radial = radial.normalized * OrbitRadius;
            Vector3 tangent = new Vector3(-radial.y, radial.x, 0f) * circleSign;
            float ang = angSpeed * Time.fixedDeltaTime;
            radial = Quaternion.AngleAxis(ang * Mathf.Rad2Deg * circleSign, Vector3.forward) * radial;
            Vector3 next = center + radial;
            next.z = playZ;
            rb.MovePosition(next);
            rb.linearVelocity = tangent.normalized * speed;
            flyDir = tangent.normalized;
            transform.rotation = Quaternion.LookRotation(Vector3.forward, flyDir);

            lapAngle += Mathf.Abs(ang);
            if (lapAngle >= Mathf.PI * 2f * LapsPerTick)
            {
                lapAngle -= Mathf.PI * 2f * LapsPerTick;
                OnFiveLaps();
            }

            if (growing != null)
                growing.SetGrow(Mathf.Clamp01(lapAngle / (Mathf.PI * 2f * LapsPerTick) + (growing.complete ? 1f : 0f)));
        }
        else
        {
            float speed = flySpeed * AirSpeedScale * (dashing ? airDashMultiply : 1f);
            if (flyDir.sqrMagnitude < 0.0001f)
                flyDir = Vector3.up;
            flyDir.z = 0f;
            flyDir.Normalize();
            Vector3 locked = rb.position;
            locked.z = playZ;
            flyDir = SandDuneDeflect.Steer(locked, flyDir, speed);
            Vector3 vel = flyDir * speed;
            vel.z = 0f;
            rb.linearVelocity = vel;
            transform.rotation = Quaternion.LookRotation(Vector3.forward, flyDir);
            rb.MovePosition(locked);

            UwrenmawPillar near = NearestPillar(locked, OrbitRadius + CatchPadding);
            if (ignorePillar != null && !leftIgnoreRange)
            {
                Vector3 away = locked - ignorePillar.transform.position;
                away.z = 0f;
                if (away.magnitude > OrbitRadius + CatchPadding + 0.45f)
                    leftIgnoreRange = true;
                else if (near == ignorePillar)
                    near = null;
            }
            if (near != null && near.complete)
                EnterOrbit(near, locked);
        }

        RecordTrail();
        PlaceTail();
    }

    void LateUpdate()
    {
        if (pg == null || pg.player == null)
            return;
        Color scheme = PlayerSkin.GetColor(pg.player, db);
        Color tail = Color.Lerp(Color.white, scheme, 0.82f);
        tail.a = 1f;
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] == null)
                continue;
            TintRenderer(segments[i].GetComponent<Renderer>(), tail);
        }
        for (int i = 0; i < pillars.Count; i++)
        {
            if (pillars[i] != null)
                pillars[i].SetTint(scheme);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;
        if (collision.collider.GetComponentInParent<Uwrenmaw>() == this)
            return;
        UwrenmawPillar hitPillar = collision.collider.GetComponentInParent<UwrenmawPillar>();
        if (hitPillar != null && orbit != null && hitPillar == orbit)
            return;

        bool ball = collision.collider.CompareTag("Ball");
        bool tail = false;
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] != null && collision.collider.transform == segments[i])
            {
                tail = true;
                break;
            }
        }
        if (orbit != null)
        {
            if (!ball)
                circleSign *= -1f;
            return;
        }

        Vector3 incoming = flyDir.sqrMagnitude > 0.01f ? flyDir : Vector3.up;
        Vector3 normal = Vector3.zero;
        float best = 0.05f;
        int contacts = collision.contactCount;
        for (int i = 0; i < contacts; i++)
        {
            Vector3 n = collision.GetContact(i).normal;
            n.z = 0f;
            if (n.sqrMagnitude < 0.0001f)
                continue;
            n.Normalize();
            float oppose = -Vector3.Dot(incoming, n);
            if (oppose > best)
            {
                best = oppose;
                normal = n;
            }
        }
        if (normal.sqrMagnitude < 0.0001f)
            return;

        Vector3 reflected = Vector3.Reflect(incoming, normal);
        reflected.z = 0f;
        if (reflected.sqrMagnitude < 0.0001f)
            reflected = normal;
        reflected.Normalize();

        Vector3 tailPush = Vector3.zero;
        Vector3 pos = transform.position;
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] == null)
                continue;
            Vector3 fromSeg = pos - segments[i].position;
            fromSeg.z = 0f;
            float dist = fromSeg.magnitude;
            if (dist > 1.05f || dist < 0.0001f)
                continue;
            if (Vector3.Dot(reflected, fromSeg) > 0.2f)
                continue;
            tailPush += fromSeg.normalized * (1.05f - dist);
        }
        if (tail || tailPush.sqrMagnitude > 0.0001f)
        {
            if (tailPush.sqrMagnitude < 0.0001f)
                tailPush = normal;
            reflected = Vector3.Slerp(reflected, tailPush.normalized, 0.7f);
            reflected.z = 0f;
            reflected.Normalize();
        }

        flyDir = reflected;
        float keep = flySpeed * AirSpeedScale * (dashing ? airDashMultiply : 1f);
        rb.linearVelocity = flyDir * keep;
        Vector3 outPos = rb.position + normal * 0.06f;
        outPos.z = playZ;
        rb.position = outPos;
    }

    void PlaceOpening()
    {
        placed = true;
        playZ = db != null ? db.FieldPlaySize : transform.position.z;
        Vector3 into = IntoField();
        Vector3 wallSpot = transform.position;
        wallSpot.z = playZ;

        // Just enough room for someone to slip between him and the wall.
        const float passGap = 0.95f;
        if (TryHomeWall(into, out Vector3 wallPoint))
            wallSpot = wallPoint + into * passGap;
        wallSpot.z = playZ;

        Vector3 along = new Vector3(-into.y, into.x, 0f);
        if (SpotTaken(wallSpot) || SpotTaken(wallSpot + into * OrbitRadius))
        {
            bool found = false;
            for (int step = 1; step <= 10 && !found; step++)
            {
                for (int sign = -1; sign <= 1 && !found; sign += 2)
                {
                    Vector3 candidate = wallSpot + along * (sign * step * 1.15f);
                    candidate.z = playZ;
                    if (SpotTaken(candidate) || SpotTaken(candidate + into * OrbitRadius))
                        continue;
                    wallSpot = candidate;
                    found = true;
                }
            }
        }

        Vector3 pillarSpot = wallSpot + into * OrbitRadius;
        wallSpot.z = playZ;
        pillarSpot.z = playZ;
        rb.position = wallSpot;
        transform.position = wallSpot;

        UwrenmawPillar first = SpawnPillar(pillarSpot, true);
        SyncHealth();
        EnterOrbit(first, wallSpot);
        trail.Clear();
        RecordTrail();
    }

    bool TryHomeWall(Vector3 into, out Vector3 wallPoint)
    {
        wallPoint = transform.position;
        Vector3 towardHome = -into;
        if (towardHome.sqrMagnitude < 0.0001f)
            return false;
        towardHome.Normalize();
        Vector3 origin = transform.position + into * 8f;
        origin.z = playZ;
        RaycastHit[] hits = Physics.RaycastAll(origin, towardHome, 80f, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return false;
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        Vector3 fieldCenter = new Vector3(0f, 0f, playZ);
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitT = hits[i].transform;
            if (hitT == null || hitT == transform || hitT.IsChildOf(transform))
                continue;
            string tag = hitT.tag;
            if (tag != "Walls" && tag != "Wall" && tag != "Obstacle")
                continue;
            Vector3 candidate = hits[i].point;
            candidate.z = playZ;
            if (Vector3.Dot(candidate - fieldCenter, towardHome) < 0.35f)
                continue;
            wallPoint = candidate;
            return true;
        }
        return false;
    }

    Vector3 IntoField()
    {
        if (pg == null || pg.player == null)
            return Vector3.up;
        return PaddleWall.IntoField(pg.player.facing);
    }

    bool SpotTaken(Vector3 spot)
    {
        Collider[] hits = Physics.OverlapSphere(spot, 0.7f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;
            string tag = hit.tag;
            if (tag == "Walls" || tag == "Wall")
                continue;
            if (hit.GetComponent<SandDuneDeflect>() != null || hit.GetComponentInParent<SandDuneDeflect>() != null)
                return true;
            if (tag == "Hazard" || tag == "Obstacle")
                return true;
            Uwrenmaw other = hit.GetComponentInParent<Uwrenmaw>();
            if (other != null && other != this)
                return true;
            if (tag == "Player" && other == null)
                return true;
        }
        return false;
    }

    void EnterOrbit(UwrenmawPillar pillar, Vector3 from)
    {
        if (pillar == null || !pillar.complete)
            return;
        if (growing != null && pillar != growing)
            CancelGrow();

        orbit = pillar;
        flying = false;
        lapAngle = 0f;
        Vector3 radial = from - pillar.transform.position;
        radial.z = 0f;
        Vector3 vel = rb.linearVelocity;
        vel.z = 0f;
        float side = Vector3.Cross(radial.sqrMagnitude > 0.0001f ? radial.normalized : Vector3.right, vel.sqrMagnitude > 0.05f ? vel : flyDir).z;
        circleSign = side >= 0f ? 1f : -1f;
    }

    void Release(bool keepDash)
    {
        if (orbit == null)
            return;
        Vector3 radial = transform.position - orbit.transform.position;
        radial.z = 0f;
        if (radial.sqrMagnitude < 0.0001f)
            radial = Vector3.right;
        flyDir = new Vector3(-radial.y, radial.x, 0f).normalized * circleSign;
        ignorePillar = orbit;
        leftIgnoreRange = false;
        orbit = null;
        flying = true;
        if (growing != null)
            CancelGrow();
        if (!keepDash)
            EndDash();
        rb.linearVelocity = flyDir * flySpeed;
    }

    void BeginGrow()
    {
        if (growing != null || orbit != null)
            return;
        pg.player.super.Spend();
        pg.player.RecordUltUsed();
        Vector3 spot = transform.position;
        spot.z = playZ;
        growing = SpawnPillar(spot, false);
        growing.BeginGrowDust();
        orbit = growing;
        flying = false;
        lapAngle = 0f;
        Vector3 radial = flyDir.sqrMagnitude > 0.01f ? Vector3.Cross(Vector3.forward, flyDir) : Vector3.right;
        circleSign = 1f;
        if (Vector3.Dot(radial, Vector3.right) < 0f)
            circleSign = -1f;
    }

    void OnFiveLaps()
    {
        if (growing != null && orbit == growing)
        {
            growing.StopGrowDust();
            growing.Finish();
            growing = null;
            SyncHealth();
            return;
        }

        if (orbit != null && orbit.complete && orbit.hp < UwrenmawPillar.MaxHp)
        {
            orbit.hp++;
            orbit.RefreshCracks();
            SyncHealth();
            PlayAura(orbit.transform.position);
        }
    }

    void CancelGrow()
    {
        if (growing == null)
            return;
        UwrenmawPillar dead = growing;
        growing = null;
        if (orbit == dead)
            orbit = null;
        pillars.Remove(dead);
        if (dead != null)
            Destroy(dead.gameObject);
        flying = orbit == null;
    }

    public void DamagePillar(UwrenmawPillar pillar, GameObject ball)
    {
        if (pillar == null || !pillar.complete || pillar.hp <= 0 || pg == null || pg.player == null)
            return;

        PlayerGrab other = ball != null ? ball.GetComponent<PlayerGrab>() : null;
        if (other == null && ball != null)
            other = ball.GetComponentInParent<PlayerGrab>();
        if (other != null && other.IsLinked() && other.player != null && pg.player != null)
        {
            if (other.playerIndex == pg.playerIndex)
                return;
            if (other.player.team == pg.player.team && other.playerIndex != pg.playerIndex)
                return;
        }

        int ballId = ball != null ? ball.GetEntityId().GetHashCode() : 0;
        pillar.hp--;
        pillar.Puff();
        if (pillar.hp <= 0)
        {
            if (orbit == pillar)
                Release(true);
            pillars.Remove(pillar);
            Destroy(pillar.gameObject);
        }
        else
        {
            pillar.RefreshCracks();
        }

        int lost = pg.player.ApplyGoalDamage(1, ballId);
        SyncHealth();
        if (lost > 0)
        {
            pg.player.RecordGoalConceded(lost);
            if (other != null && other.IsLinked() && other.player != null)
            {
                other.player.RecordDamageDealt(lost, pg.player);
                other.player.RecordGoalScored(lost, pg.player);
            }
            if (pg.player.computer)
                ComputerAI.OnTookGoalDamage(pg.player, lost);
        }
    }

    void SyncHealth()
    {
        if (pg == null || pg.player == null)
            return;
        int sum = 0;
        int count = 0;
        for (int i = pillars.Count - 1; i >= 0; i--)
        {
            UwrenmawPillar p = pillars[i];
            if (p == null || !p.complete)
                continue;
            count++;
            sum += Mathf.Clamp(p.hp, 0, UwrenmawPillar.MaxHp);
        }
        pg.player.maxHealth = count * UwrenmawPillar.MaxHp;
        pg.player.currentHealth = sum;
    }

    UwrenmawPillar NearestPillar(Vector3 pos, float range)
    {
        UwrenmawPillar best = null;
        float bestD = range * range;
        for (int i = 0; i < pillars.Count; i++)
        {
            UwrenmawPillar p = pillars[i];
            if (p == null || !p.complete)
                continue;
            Vector3 d = p.transform.position - pos;
            d.z = 0f;
            float sq = d.sqrMagnitude;
            if (sq <= bestD)
            {
                bestD = sq;
                best = p;
            }
        }
        return best;
    }

    UwrenmawPillar SpawnPillar(Vector3 spot, bool finished)
    {
        GameObject go = new GameObject("Uwrenmaw Pillar");
        go.tag = "Lifeline";
        go.transform.position = new Vector3(spot.x, spot.y, playZ);
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(PillarWidth, PillarWidth, 0.35f);
        Rigidbody body = go.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        body.constraints = RigidbodyConstraints.FreezeAll;

        GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vis.name = "Visual";
        Collider visCol = vis.GetComponent<Collider>();
        if (visCol != null)
            Destroy(visCol);
        vis.transform.SetParent(go.transform, false);
        vis.transform.localScale = new Vector3(PillarWidth, PillarWidth, PillarDepth);
        Paint(vis.GetComponent<Renderer>(), UwrenmawArt.Cracks(0));

        UwrenmawPillar pillar = go.AddComponent<UwrenmawPillar>();
        if (go.GetComponent<ClearAfterTheGame>() == null)
            go.AddComponent<ClearAfterTheGame>();
        pillar.Setup(this, finished);
        pillars.Add(pillar);
        if (pg != null && pg.player != null && pg.player.spawnedLifeline == null && finished)
            pg.player.spawnedLifeline = go;
        return pillar;
    }

    void BuildTail()
    {
        PhysicsMaterial bounce = BounceMaterial();
        for (int i = 0; i < SegmentCount; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Uwrenmaw Body " + (i + 1);
            go.tag = "Player";
            go.transform.localScale = Vector3.one * (SegmentScale - i * 0.02f);
            go.transform.position = transform.position - transform.up * SegmentSpacing * (i + 1);
            Rigidbody body = go.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                col.material = bounce;
                SphereCollider sphere = col as SphereCollider;
                if (sphere != null)
                    sphere.radius = 0.42f;
                // The first links overlap the head. Later links can block him and change a bounce.
                if (i < 2)
                    Physics.IgnoreCollision(col, GetComponent<Collider>(), true);
                for (int p = 0; p < segments.Count; p++)
                {
                    Collider other = segments[p].GetComponent<Collider>();
                    if (other != null)
                        Physics.IgnoreCollision(col, other, true);
                }
            }
            Paint(go.GetComponent<Renderer>(), UwrenmawArt.Body());
            if (go.GetComponent<ClearAfterTheGame>() == null)
                go.AddComponent<ClearAfterTheGame>();
            segments.Add(go.transform);
        }
    }

    void RecordTrail()
    {
        Vector3 p = transform.position;
        p.z = playZ;
        if (trail.Count == 0 || (trail[0] - p).sqrMagnitude > 0.0008f)
            trail.Insert(0, p);
        float keep = SegmentSpacing * (SegmentCount + 3);
        float walked = 0f;
        for (int i = 1; i < trail.Count; i++)
        {
            walked += Vector3.Distance(trail[i - 1], trail[i]);
            if (walked > keep)
            {
                trail.RemoveRange(i, trail.Count - i);
                break;
            }
        }
    }

    void PlaceTail()
    {
        bool wave = orbit == null && !dashing;
        for (int i = 0; i < segments.Count; i++)
        {
            Transform seg = segments[i];
            if (seg == null)
                continue;
            float dist = SegmentSpacing * (i + 1);
            Vector3 along = PointBehind(dist, out Vector3 tangent);
            if (wave)
            {
                Vector3 side = new Vector3(-tangent.y, tangent.x, 0f);
                float amp = 0.07f * (1f - i / (float)(SegmentCount + 1));
                along += side * Mathf.Sin(Time.time * 5.5f + i * 0.85f) * amp;
            }
            along.z = playZ;
            seg.position = along;
            if (tangent.sqrMagnitude > 0.0001f)
                seg.rotation = Quaternion.LookRotation(Vector3.forward, tangent);
        }
    }

    Vector3 PointBehind(float distance, out Vector3 tangent)
    {
        tangent = flyDir.sqrMagnitude > 0.01f ? flyDir : Vector3.up;
        if (trail.Count == 0)
            return transform.position - tangent * distance;
        float left = distance;
        for (int i = 0; i < trail.Count - 1; i++)
        {
            Vector3 a = trail[i];
            Vector3 b = trail[i + 1];
            float seg = Vector3.Distance(a, b);
            if (seg < 0.0001f)
                continue;
            if (left <= seg)
            {
                float t = left / seg;
                tangent = (a - b).normalized;
                return Vector3.Lerp(a, b, t);
            }
            left -= seg;
        }
        Vector3 last = trail[trail.Count - 1];
        if (trail.Count > 1)
            tangent = (trail[trail.Count - 2] - last).normalized;
        return last - tangent * left;
    }

    void StartDash()
    {
        Skill dash = pg.player.dash;
        dash.Spend();
        pg.player.RecordDash();
        dashing = true;
        dashUntil = Time.time + (orbit != null ? circleDashTime : airDashTime);
        BeginSandBurst();
    }

    void EndDash()
    {
        dashing = false;
    }

    void BeginSandBurst()
    {
        GameObject go = new GameObject("Uwrenmaw Dash");
        go.transform.SetParent(transform, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.duration = Mathf.Max(airDashTime, 0.45f);
        main.startLifetime = 0.45f;
        main.startSpeed = 2.4f;
        main.startSize = 0.22f;
        main.startColor = PaletteParticle(0.95f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;
        var emission = ps.emission;
        emission.rateOverTime = 90f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.28f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = ParticleMaterial();
        }
        ps.Play();
        Destroy(go, Mathf.Max(circleDashTime, airDashTime) + 0.6f);
    }

    Color PaletteParticle(float alpha)
    {
        Color color = pg != null && pg.player != null ? PlayerSkin.GetColor(pg.player, db) : Color.white;
        color.a = alpha;
        return color;
    }

    static Material ParticleMaterial()
    {
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        mat.mainTexture = UwrenmawArt.Soft();
        mat.color = Color.white;
        return mat;
    }

    static void TintRenderer(Renderer rend, Color color)
    {
        if (rend == null)
            return;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        rend.GetPropertyBlock(block);
        block.SetColor("_Color", color);
        block.SetColor("_BaseColor", color);
        rend.SetPropertyBlock(block);
        if (rend.material != null)
            rend.material.color = color;
    }

    void PlayAura(Vector3 pos)
    {
        GameObject go = new GameObject("Uwrenmaw Aura");
        go.transform.position = pos;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.duration = 0.6f;
        main.startLifetime = 0.7f;
        main.startSpeed = 0.6f;
        main.startSize = 0.28f;
        main.startColor = PaletteParticle(0.9f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;
        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 18) });
        emission.rateOverTime = 0f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.45f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.material = ParticleMaterial();
        }
        ps.Play();
        Destroy(go, 1.4f);
    }

    void Think(ref bool action, ref bool dash)
    {
        action = false;
        dash = false;
        BallInfo ball = null;
        float best = float.PositiveInfinity;
        if (db != null)
        {
            BallInfo[] balls = FindObjectsByType<BallInfo>(FindObjectsInactive.Exclude);
            for (int i = 0; i < balls.Length; i++)
            {
                if (balls[i] == null)
                    continue;
                float d = (balls[i].transform.position - transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    ball = balls[i];
                }
            }
        }

        if (orbit != null)
        {
            if (ball != null && best > 16f && lapAngle > 1.2f)
                action = true;
            return;
        }

        if (pg.player.super != null && pg.player.super.Enough() && ball != null && best < 25f)
            action = true;
        if (pg.player.dash != null && pg.player.dash.Enough() && best > 20f)
            dash = true;
    }

    void OnDestroy()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] != null)
                Destroy(segments[i].gameObject);
        }
        for (int i = 0; i < pillars.Count; i++)
        {
            if (pillars[i] != null)
                Destroy(pillars[i].gameObject);
        }
    }

    static void Paint(Renderer rend, Texture tex)
    {
        if (rend == null)
            return;
        Shader shader = Shader.Find("Standard");
        if (shader == null)
            return;
        Material mat = new Material(shader);
        mat.mainTexture = tex;
        mat.color = Color.white;
        rend.material = mat;
    }

    static PhysicsMaterial BounceMaterial()
    {
        return new PhysicsMaterial("Uwrenmaw Bounce")
        {
            bounciness = 0.95f,
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };
    }

    static void ApplyBounce(Collider col)
    {
        if (col != null)
            col.material = BounceMaterial();
    }
}
