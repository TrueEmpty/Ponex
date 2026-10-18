using System.Collections.Generic;
using UnityEngine;

public class Bahrue : MonoBehaviour
{
    Rigidbody rb;
    Database db;
    PlayerGrab pg;
    public List<string> hitTags = new List<string>();

    [SerializeField]
    List<FollowPlayer> bahrueObjs = new List<FollowPlayer>();
    int lastHealth = -1;

    public GameObject bahrueObj;
    public GameObject bahrueBump;

    [SerializeField]
    public List<Vector2Int> amountPerRow = new List<Vector2Int>();

    public Vector3 offsetAmounts = Vector3.zero;
    public Transform holder;

    public Transform token;
    public Renderer ring;
    public Renderer emblem;
    Material ringMaterial;
    Material emblemMaterial;

    public float bumpCooldown = 2;
    float timeTillReset = 0;
    float lateralScale = 1f;
    bool bumpWasHeld;
    readonly List<GameObject> travelingBumps = new List<GameObject>();
    public int healthGain = 4;
    [Tooltip("Super charge lost when hit while charging (cost is 1).")]
    public float chargeLossOnHit = 0.45f;

    #region Dash
    int canDash = 0;
    float dashEnd = 0;
    float speedIncrease = 1;
    public float speedMultiplyer = 4;
    float doubleClickTime = .3f;
    public float dashTime = .3f;
    #endregion

    [SerializeField]
    Thought thought = Thought.Nothing;
    ComputerBrain brain;
    PaddleWall.LaneLock lane;
    bool ignoredHomeWall;
    float ignoreHomeWallUntil;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;
        if (ring != null)
            ringMaterial = ring.material;
        if (emblem != null)
            emblemMaterial = emblem.material;
        brain = ComputerBrain.Ensure(gameObject, ComputerBrain.Mode.LanePaddle);
        if (brain != null)
        {
            brain.hitTags = hitTags;
            brain.wallStopDistance = 1f + PaddleWall.LineSideClearance;
        }

        rb.useGravity = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (db.gameStart && pg.player.currentHealth > 0)
        {
            BahrueUpkeep();

            // Super first so Move constraint applies before movement this frame
            if (pg.player.CanSuper)
            {
                ActivateSuper();
            }

            bool chargingSuper = pg.player.super != null && pg.player.super.amount > 0f
                && (pg.inp.super || thought == Thought.MoveDown);

            if (chargingSuper || !pg.player.CanMove)
            {
                rb.linearVelocity = Vector3.zero;
                canDash = 0;
                speedIncrease = 1;
                dashEnd = 0f;
            }
            else if (pg.player.CanMove)
            {
                OnMove();
                if (pg.player.CanDash)
                    OnDash();
            }

            ActivateBump(pg.player.CanBump && !chargingSuper);

            if (dashEnd > 0)
                dashEnd -= Time.deltaTime;
            else
            {
                canDash = 0;
                speedIncrease = 1;
            }

            if (pg.player.dash != null)
                pg.player.dash.Charge();

            if (pg.player != null)
                lane.Ensure(rb, pg.player.facing);
            if (!ignoredHomeWall || Time.time < ignoreHomeWallUntil)
            {
                PaddleWall.IgnoreHomeWallCollisions(transform, hitTags, null);
                if (!ignoredHomeWall)
                    ignoreHomeWallUntil = Time.time + 1.25f;
                ignoredHomeWall = true;
            }
            // Dash/contact can overshoot soft wall stop — push out so leave-input always works
            PaddleWall.Unstick(rb, transform, hitTags);
            lane.Clamp(rb);
        }
    }

    void FixedUpdate()
    {
        lane.Clamp(rb);
    }

    void OnMove()
    {
        int moveDir = 0;

        if (pg.player.computer)
        {
            ComputerAI.Decision d = brain != null
                ? brain.Lane
                : ComputerAI.Evaluate(transform, pg.player, hitTags, 1f);
            moveDir = d.moveDir;
            thought = brain != null
                ? brain.Thought
                : (d.wantBump ? Thought.MoveUp : (d.wantSuper ? Thought.MoveDown : Thought.Nothing));
        }
        else
        {
            if (pg.inp.right)
                moveDir += 1;
            if (pg.inp.left)
                moveDir -= 1;
        }

        // Block only into-wall travel; moving away from a wall is always allowed
        if (moveDir != 0 && WallInDirection(moveDir))
            moveDir = 0;

        Vector3 rotDir = PaddleWall.AbsAxes(transform.right);
        rb.linearVelocity = rotDir * moveDir * speedIncrease * pg.player.EffectiveMovementSpeed;
    }

    void OnDash()
    {
        Skill d = pg.player.dash;
        if (d == null || d.max <= 0 || d.cost <= 0)
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

    bool WallInDirection(int dir)
    {
        float wallDis = 1f;
        if (amountPerRow.Count >= 1)
        {
            float halfDis = (((float)amountPerRow[0].x / 2) * (offsetAmounts.x * transform.localScale.x));
            wallDis = halfDis;
        }

        wallDis *= lateralScale;
        wallDis += PaddleWall.LineSideClearance;
        Transform wallOrigin = holder != null ? holder : transform;
        return PaddleWall.WallInDirection(wallOrigin, dir, hitTags, wallDis);
    }

    void BahrueUpkeep()
    {
        if (pg.player.currentHealth > 0)
        {
            // Hit while charging super — dump a large chunk of charge
            if (lastHealth >= 0 && pg.player.currentHealth < lastHealth
                && pg.player.super != null && pg.player.super.amount > 0f)
            {
                pg.player.super.amount = Mathf.Max(0f, pg.player.super.amount - chargeLossOnHit);
            }

            for (int i = bahrueObjs.Count - 1; i >= 0; i--)
            {
                if (bahrueObjs[i] == null)
                    bahrueObjs.RemoveAt(i);
            }

            if (lastHealth != pg.player.currentHealth || bahrueObjs.Count != pg.player.currentHealth)
                UpdateBahrue();
            else if (bahrueObjs.Count > 0)
                ApplyStackLayout();

            // More blocks = slower, but never so slow that he can't slide
            float scaled = (1.5f * (50f / Mathf.Max(1, pg.player.currentHealth))) / 4f;
            pg.player.movementSpeed = Mathf.Max(1.6f, scaled);
        }
    }

    void UpdateBahrue()
    {
        //Remove Extras
        if (bahrueObjs.Count > pg.player.currentHealth)
        {
            for (int i = bahrueObjs.Count - 1; i >= pg.player.currentHealth; i--)
            {
                //Run Animation at position

                //Remove
                Destroy(bahrueObjs[i].gameObject);
                bahrueObjs.RemoveAt(i);
            }
        }

        //Add Missing
        if (bahrueObjs.Count < pg.player.currentHealth)
        {
            for (int i = bahrueObjs.Count; i < pg.player.currentHealth; i++)
            {
                //Run Animation at position

                if (bahrueObj == null || holder == null)
                    break;

                //Add
                GameObject go = Instantiate(bahrueObj, holder.position + transform.up * -2.5f, transform.rotation);
                go.transform.SetParent(holder, true);

                PlayerGrab cpg = go.GetComponent<PlayerGrab>();
                FollowPlayer fp = go.GetComponent<FollowPlayer>();
                if (fp == null)
                    fp = go.AddComponent<FollowPlayer>();

                if (cpg != null)
                    cpg.playerIndex = pg.playerIndex;

                fp.playerTransform = holder;
                fp.offset = new Vector3(0, -3, 0);
                fp.useStackLerp = true;
                bahrueObjs.Add(fp);
            }
        }

        ApplyStackLayout();
        lastHealth = pg.player.currentHealth;
    }

    void ApplyStackLayout()
    {
        if (bahrueObjs.Count <= 0)
            return;

        amountPerRow = AmountPerRow();
        UpdateLateralScale();
        for (int i = 0; i < bahrueObjs.Count; i++)
        {
            if (bahrueObjs[i] == null)
                continue;

            Vector3 getPosOff = GetPosition(i);
            if (getPosOff != bahrueObjs[i].offset)
                bahrueObjs[i].LerpOffset(getPosOff, 5.5f);
        }
    }

    void ActivateSuper()
    {
        Vector3 pP = Vector3.zero;
        pP.y = GetTopPoint() + 1;
        token.localPosition = pP;

        // Hold-to-charge is intentional — use superHeld, not Enough()-alone activation
        if (pg.inp.super || thought == Thought.MoveDown)
        {
            pg.player.AddConstraint(gameObject, -1, PlayerConstraint.Move);
            pg.player.AddConstraint(gameObject, -1, PlayerConstraint.Bump);
            rb.linearVelocity = Vector3.zero;

            float superGainSpeed = .9f * (50 / (float)Mathf.Max(1, pg.player.currentHealth));
            pg.player.super.amount += ((Time.deltaTime * superGainSpeed) / 100);

            if (pg.player.super.amount >= pg.player.super.cost)
            {
                pg.player.maxHealth += healthGain;
                pg.player.Heal(healthGain);
                pg.player.super.amount = 0;
                pg.player.RecordUltUsed();
                pg.player.RemoveConstraint(gameObject, PlayerConstraint.Move);
                pg.player.RemoveConstraint(gameObject, PlayerConstraint.Bump);
                //Play Shine Animation/Sound/Particle
            }
        }
        else
        {
            pg.player.RemoveConstraint(gameObject, PlayerConstraint.Move);
            pg.player.RemoveConstraint(gameObject, PlayerConstraint.Bump);
            pg.player.super.amount = 0;
        }

        ring.gameObject.SetActive(pg.player.super.amount != 0);
        emblem.gameObject.SetActive(pg.player.super.amount != 0);

        if (ringMaterial != null && emblemMaterial != null)
        {
            Color rC = ringMaterial.color;
            Color eC = emblemMaterial.color;

            rC.a = pg.player.super.amount;
            eC.a = pg.player.super.amount;

            ringMaterial.color = rC;
            emblemMaterial.color = eC;
        }

        pg.player.maxHealth = pg.player.currentHealth;
    }

    void ActivateBump(bool allowFire)
    {
        float bP = timeTillReset / bumpCooldown;

        if (bP > 1)
        {
            bP = 1;
        }
        else if (bP < 0)
        {
            bP = 0;
        }

        pg.player.bump.readyPercent = bP;

        bool bumpHeld = pg.inp.up || pg.inp.bump || thought == Thought.MoveUp;
        bool bumpPressed = bumpHeld && !bumpWasHeld;
        bumpWasHeld = bumpHeld;

        PruneTravelingBumps();
        bool stoppedByPress = false;
        if (travelingBumps.Count > 0 && (bumpPressed || timeTillReset >= bumpCooldown))
        {
            stoppedByPress = bumpPressed;
            HaltTravelingBumps();
        }

        // Holding bump still refires once the cooldown ends.
        // The press that stops a bump already in flight does not also launch a new one.
        if (allowFire && bumpHeld && !stoppedByPress && timeTillReset >= bumpCooldown && pg.player.CanBump)
        {
            if (bahrueObjs.Count > 1)
            {
                if (amountPerRow.Count > 1)
                {
                    int cILR = amountPerRow[^1].x;

                    if (bahrueObjs.Count > cILR)
                    {
                        for (int i = 0; i < cILR; i++)
                        {
                            GameObject bo = bahrueObjs[^1].gameObject;

                            Vector3 spawnPos = bo.transform.position;
                            GameObject go = Instantiate(bahrueBump, spawnPos, transform.rotation) as GameObject;

                            PlayerGrab bpg = go.GetComponent<PlayerGrab>();

                            if (bpg != null && pg != null)
                                bpg.playerIndex = pg.playerIndex;

                            travelingBumps.Add(go);

                            pg.player.currentHealth--;
                            pg.player.maxHealth = pg.player.currentHealth;
                            Destroy(bo);
                            bahrueObjs.RemoveAt(bahrueObjs.Count - 1);
                        }

                        timeTillReset = 0;
                    }
                }
            }
        }

        timeTillReset += Time.deltaTime;
    }

    void PruneTravelingBumps()
    {
        for (int i = travelingBumps.Count - 1; i >= 0; i--)
        {
            if (travelingBumps[i] == null)
                travelingBumps.RemoveAt(i);
        }
    }

    void HaltTravelingBumps()
    {
        for (int i = travelingBumps.Count - 1; i >= 0; i--)
        {
            GameObject go = travelingBumps[i];
            if (go == null)
                continue;

            ConstantForward flight = go.GetComponent<ConstantForward>();
            if (flight != null)
                flight.Halt();

            // Linger for the normal lifetime, starting when travel stops
            DestroyAfterLifetime linger = go.GetComponent<DestroyAfterLifetime>();
            if (linger != null)
                linger.ArmCountdown();

            travelingBumps.RemoveAt(i);
        }
    }

    Vector3 GetPosition(int index)
    {
        Vector3 result = Vector3.zero;
        int totalCount = 0;
        int row = -1;
        int col = -1;

        if (amountPerRow.Count > 0)
        {
            for (int i = 0; i < amountPerRow.Count; i++)
            {
                Vector2Int apr = amountPerRow[i];
                if (index < totalCount + apr.x)
                {
                    row = i;
                    col = totalCount - index;
                    break;
                }

                totalCount += apr.x;
            }

            if (row >= 0 && row < amountPerRow.Count)
            {
                Vector2Int apr = amountPerRow[row];
                float step = offsetAmounts.x * lateralScale;
                float maxDis = (apr.x * step) * .5f;
                result.x = (maxDis + (col * step)) - (step / 2);
                result.y = row * offsetAmounts.y;
            }
        }

        return result;
    }

    static readonly RaycastHit[] wallHits = new RaycastHit[16];

    void UpdateLateralScale()
    {
        lateralScale = 1f;
        if (amountPerRow == null || amountPerRow.Count == 0 || offsetAmounts.x <= 0.01f)
            return;

        int widest = 1;
        for (int i = 0; i < amountPerRow.Count; i++)
            widest = Mathf.Max(widest, amountPerRow[i].x);

        float halfReach = (widest * offsetAmounts.x) * 0.5f;
        if (halfReach <= 0.01f)
            return;

        if (!WallDistance(1, out float right) || !WallDistance(-1, out float left))
            return;

        float scaleX = Mathf.Abs(transform.lossyScale.x);
        if (scaleX < 0.001f)
            scaleX = 1f;
        left /= scaleX;
        right /= scaleX;

        // Full spacing until each end is against a wall. Then stop extending.
        if (halfReach < left || halfReach < right)
            return;

        float span = left + right;
        float natural = halfReach * 2f;
        if (natural > span && span > 0.01f)
            lateralScale = span / natural;
    }

    bool WallDistance(int dir, out float distance)
    {
        distance = 0f;
        Transform originT = holder != null ? holder : transform;
        Vector3 direction = PaddleWall.AbsAxes(originT.right) * Mathf.Sign(dir);
        if (direction.sqrMagnitude < 0.0001f)
            return false;

        int count = Physics.RaycastNonAlloc(originT.position, direction, wallHits, 80f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            Transform t = wallHits[i].transform;
            if (t == null || t == originT || t.IsChildOf(originT) || originT.IsChildOf(t))
                continue;
            if (!PaddleWall.MatchesWallTag(t.tag, hitTags))
                continue;
            if (wallHits[i].distance < best)
            {
                best = wallHits[i].distance;
                found = true;
            }
        }

        if (!found)
            return false;
        distance = best;
        return true;
    }

    List<Vector2Int> AmountPerRow()
    {
        List<Vector2Int> result = new List<Vector2Int>();
        //How Many in that row,Position Until Up = > 2

        if (bahrueObjs.Count > 0)
        {
            result.Add(new Vector2Int(0, 0));

            for (int i = 0; i < bahrueObjs.Count; i++)
            {
                for (int r = 0; r < result.Count; r++)
                {
                    Vector2Int v2I = result[r];
                    if (v2I.y < 2)
                    {
                        v2I.x++;
                        v2I.y++;
                        result[r] = v2I;
                        break;
                    }
                    else
                    {
                        if (result.Count <= r + 1)
                        {
                            for (int n = r; n >= 0; n--)
                            {
                                Vector2Int n2I = result[n];
                                n2I.y = 1;
                                result[n] = n2I;
                            }

                            result.Add(new Vector2Int(1, 1));
                            break;
                        }
                    }
                }
            }
        }

        return result;
    }

    float GetTopPoint()
    {
        return amountPerRow.Count * offsetAmounts.y;
    }
}
