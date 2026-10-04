using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Celarus : MonoBehaviour
{
    Rigidbody rb;
    Database db;
    PlayerGrab pg;

    Transform spinPoint;

    public bool day = false;
    public float dayCycle = 20;
    public float spinSpeed = 50;
    [SerializeField]
    float cycle = 0;   

    DamageOnTagHit sunHit;
    PullObjectIn sunGravity;

    DamageOnTagHit moonHit;
    PullObjectIn moonGravity;

    [SerializeField]
    bool moving = false;

    public Vector3 leavePoint = new Vector3(0,-1,-4.5f);
    public float leaveSpeed = 50;

    public Vector3 solarFlareSpeed;
    public GameObject solarFlare;
    public float sunRight = 0;
    public float sunMiddle = 0;
    public float sunLeft = 0;

    public Vector3 sFsideDir = new Vector3(3.5f,3.5f);
    public float sFResetCount = 2;
    public float sFoffset = 4;
    public float sFoffsetAngle = 35;
    [Tooltip("Legacy field — moon skate uses moonPullAccel instead.")]
    public float moonGravityMultiplier = .5f;

    [Header("Moon Skate")]
    public float moonPullAccel = 55f;
    public float skateAccel = 70f;
    public float airSteerAccel = 21f;
    public float maxSkateSpeed = 40f;
    public float starRadius = 0.05f;
    public float surfaceRide = 0f;
    public float spinDuration = 0.35f;
    public float slamSpeed = 48f;
    public float bounceRestitution = 0.9f;
    public float maxBounceBoost = 1.55f;
    public float slamBounceBonus = 1.25f;
    public float groundFriction = 0.15f;
    public float launchAssist = 1.15f;

    bool onMoon;
    bool slamming;
    float spinLeft;
    float spinCooldown;
    float slamCooldown;
    Transform visual;

    // Buffered from Update so FixedUpdate never misses press edges
    int pendingMoveDir;
    bool pendingSpin;
    bool pendingSlam;

    #region AI
    bool thinking = false;
    public float thinkTime = .5f;

    [SerializeField]
    Thought thought = Thought.Nothing;

    //Starting Chance Weight
    public float chanceToDoNothing = 50; //Do Nothing
    public float chanceToMove = 100; //Move Left
    public float chanceToBump = 0; //Move Bump
    public float chanceToSuper = 0; //Move Super

    enum Thought
    {
        Nothing,
        MoveLeft,
        MoveRight,
        MoveUp,
        MoveDown
    }
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;

        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        // Massive authored mass fights fluid skate forces — keep Acceleration-friendly
        if (rb.mass > 50f)
            rb.mass = 5f;

        if (transform.childCount > 0)
            visual = transform.GetChild(0);
    }

    // Update is called once per frame
    void Update()
    {
        if (db == null || pg == null || pg.player == null)
            return;

        if (db.gameStart && pg.player.currentHealth > 0)
        {
            if (spinPoint != null && sunHit != null && sunGravity != null && moonHit != null && moonGravity != null)
            {
                if (pg.player.computer)
                {
                    thinking = false;
                    ComputerAI.Decision d = ComputerAI.Evaluate(transform, pg.player, null, 1.5f);
                    if (d.wantBump) thought = Thought.MoveUp;
                    else if (d.wantSuper) thought = Thought.MoveDown;
                    else if (d.moveDir > 0) thought = Thought.MoveRight;
                    else if (d.moveDir < 0) thought = Thought.MoveLeft;
                    else thought = Thought.Nothing;
                }

                UpdateDayAndNight();
                PlanetsUpdate();
                SunAttacks();
                BufferMoonInput();
                TickSpin();
                if (spinCooldown > 0f)
                    spinCooldown -= Time.deltaTime;
                if (slamCooldown > 0f)
                    slamCooldown -= Time.deltaTime;
            }
            else
            {
                GetLifelineObjs();
            }
        }
    }

    void FixedUpdate()
    {
        if (db == null || pg == null || pg.player == null)
            return;
        if (!db.gameStart || pg.player.currentHealth <= 0)
            return;
        if (spinPoint == null || moonGravity == null)
            return;

        CelarusMove();
    }

    void BufferMoonInput()
    {
        if (day || moving)
        {
            pendingMoveDir = 0;
            return;
        }

        Player p = pg.player;
        int moveDir = 0;
        if (p.CanMove)
        {
            if (pg.inp.right || thought == Thought.MoveRight)
                moveDir += 1;
            if (pg.inp.left || thought == Thought.MoveLeft)
                moveDir -= 1;

            switch (p.facing)
            {
                case Facing.Down:
                case Facing.Right:
                    moveDir *= -1;
                    break;
            }
        }
        pendingMoveDir = moveDir;

        // Edges + held Jump/Interact so FixedUpdate never misses
        if (p.CanBump && (pg.inp.tf_bump || thought == Thought.MoveUp || JumpPressed()))
            pendingSpin = true;
        if (p.CanSuper && (pg.inp.tf_super || thought == Thought.MoveDown || InteractPressed()))
            pendingSlam = true;
    }

    bool JumpPressed()
    {
        ControllerLink cL = pg.player != null ? pg.player.cLink : null;
        if (cL == null)
            return false;
        ControllerButtons b = cL["Jump"];
        return b != null && b.wasPressedThisFrame;
    }

    bool InteractPressed()
    {
        ControllerLink cL = pg.player != null ? pg.player.cLink : null;
        if (cL == null)
            return false;
        ControllerButtons b = cL["Interact"];
        return b != null && b.wasPressedThisFrame;
    }

    IEnumerator AI()
    {
        //Inital Info
        Facing f = pg.player.facing;
        string thoughtString = "";

        //Decisions
        float dNull = chanceToDoNothing; //Do Nothing
        float dML = chanceToMove; //Move Left
        float dMR = chanceToMove; //Move Right
        float dMB = chanceToBump; //Move Bump
        float dMS = chanceToSuper; //Move Super

        //Get Ball Hit locations that Will are set to hit the wall behind him
        GameObject[] allBalls = GameObject.FindGameObjectsWithTag("Ball");
        List<Vector3> importantCollisions = new List<Vector3>();
        Vector3 curPos = transform.position;

        if (allBalls.Length > 0)
        {
            for (int i = 0; i < allBalls.Length; i++)
            {
                BallInfo bI = allBalls[i].GetComponent<BallInfo>();

                if (bI != null)
                {
                    int fcpc = bI.futureColisionPoints.Count;

                    if (fcpc > 0)
                    {
                        for (int c = 0; c < fcpc; c++)
                        {
                            Vector3 cp = bI.futureColisionPoints[c];

                            switch (f)
                            {
                                case Facing.Up:
                                    if (cp.y <= curPos.y)
                                    {
                                        importantCollisions.Add(cp);
                                    }
                                    break;
                                case Facing.Down:
                                    if (cp.y >= curPos.y)
                                    {
                                        importantCollisions.Add(cp);
                                    }
                                    break;
                                case Facing.Left:
                                    if (cp.x >= curPos.x)
                                    {
                                        importantCollisions.Add(cp);
                                    }
                                    break;
                                case Facing.Right:
                                    if (cp.x <= curPos.x)
                                    {
                                        importantCollisions.Add(cp);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        //Go through important collisions and use that to help determine the next action
        if (importantCollisions.Count > 0)
        {
            float iCo = importantCollisions.Count;

            for (int c = 0; c < iCo; c++)
            {
                Vector3 v3 = importantCollisions[c];
                float disRight = 0;

                switch (f)
                {
                    case Facing.Up:
                        disRight = v3.x - curPos.x;
                        break;
                    case Facing.Down:
                        disRight = v3.x - curPos.x;
                        disRight *= -1;
                        break;
                    case Facing.Left:
                        disRight = v3.y - curPos.y;
                        break;
                    case Facing.Right:
                        disRight = v3.y - curPos.y;
                        disRight *= -1;
                        break;
                }

                if (Mathf.Abs(disRight) <= 1) //In line with Collision
                {
                    disRight = 0;
                    dNull += 10;

                    if (pg.player.bump.Enough())
                    {
                        dMB += 5 / iCo;
                    }

                    if (pg.player.super.Enough())
                    {
                        dMS += 5 / iCo;
                    }
                }

                dMR -= disRight;
                dML += disRight;

                if (Mathf.Abs(disRight) >= 3) //At least 2 lengths away
                {
                    if (pg.player.super.Enough())
                    {
                        dMS += 10 / iCo;
                    }
                }
            }
        }

        thoughtString += "Ball Count: " + allBalls.Length + "/" + importantCollisions.Count + "\n";

        //Choose an action
        if (dNull < 0)
        {
            dNull = 0;
        }

        if (dMR < 0)
        {
            dMR = 0;
        }

        if (dML < 0)
        {
            dML = 0;
        }

        if (dMB < 0)
        {
            dMB = 0;
        }

        if (dMS < 0)
        {
            dMS = 0;
        }

        float rNull = dNull;
        float rMR = dNull + dMR;
        float rML = rMR + dML;
        float rMB = rML + dMB;
        float rMS = rMB + dMS;

        float rN = Random.Range(0f, rMS);

        if (rN <= rNull)
        {
            thought = Thought.Nothing;
            thoughtString += "Choice: Do Nothing";
        }
        else if (rN <= rMR)
        {
            thought = Thought.MoveRight;
            thoughtString += "Choice: Move Right";
        }
        else if (rN <= rML)
        {
            thought = Thought.MoveLeft;
            thoughtString += "Choice: Move Left";
        }
        else if (rN <= rMB)
        {
            thought = Thought.MoveUp;
            thoughtString += "Choice: Bump";
        }
        else if (rN <= rMS)
        {
            thought = Thought.MoveDown;
            thoughtString += "Choice: Super";
        }

        thoughtString += "\n";
        thoughtString += "Random Number: " + rN + "\n";
        thoughtString += "Nothing: " + rNull + "\n";
        thoughtString += "Move Right: " + rMR + "\n";
        thoughtString += "Move Left: " + rML + "\n";
        thoughtString += "Use Bump: " + rMB + "\n";
        thoughtString += "Use Super: " + rMS;

        //Debug.Log(thoughtString);
        yield return null;

        //Wait until thinking again
        yield return new WaitForSeconds(thinkTime);

        thinking = false;
        yield return null;
    }

    void CelarusMove()
    {
        if (moving || day || moonGravity == null || rb == null)
            return;

        Vector3 moonPos = moonGravity.transform.position;
        moonPos.z = transform.position.z;

        float moonRadius = GetMoonRadius();
        float rideRadius = moonRadius + starRadius + surfaceRide;

        Vector3 toStar = transform.position - moonPos;
        toStar.z = 0f;
        float dist = toStar.magnitude;
        if (dist < 0.0001f)
        {
            toStar = Vector3.up;
            dist = 0.0001f;
        }

        Vector3 radialOut = toStar / dist;
        Vector3 radialIn = -radialOut;
        Vector3 tangentRight = Vector3.Cross(radialOut, Vector3.forward).normalized;

        // Hard surface: never allow the star inside the moon
        if (dist < rideRadius)
        {
            Vector3 fixedPos = moonPos + radialOut * rideRadius;
            fixedPos.z = transform.position.z;
            rb.position = fixedPos;
            transform.position = fixedPos;
            dist = rideRadius;
            toStar = radialOut * rideRadius;
        }

        float clearance = dist - rideRadius;
        onMoon = clearance <= 0.12f;

        // Bump spin locks the star in place (ground or air)
        TryStartSpin();
        if (spinLeft > 0f)
        {
            pendingSlam = false;
            rb.linearVelocity = Vector3.zero;
            // Stay glued to current orbit radius while spinning
            Vector3 holdPos = moonPos + radialOut * Mathf.Max(dist, rideRadius);
            holdPos.z = transform.position.z;
            rb.position = holdPos;
            transform.position = holdPos;
            UpdateMoonVisual(tangentRight, 0f);
            return;
        }

        Vector3 v = rb.linearVelocity;
        v.z = 0f;

        // Kill inward velocity while riding so gravity can't push through the surface
        if (onMoon)
        {
            float inward = Vector3.Dot(v, radialIn);
            if (inward > 0f)
                v -= radialIn * inward;
        }

        int moveDir = pendingMoveDir;
        float tanSpeed = Vector3.Dot(v, tangentRight);
        float airSpeedCap = maxSkateSpeed * 0.5f;

        // Skate / air steer along the moon tangent (air is half speed)
        if (moveDir != 0)
        {
            float accel = onMoon ? skateAccel : airSteerAccel;
            tanSpeed += moveDir * accel * Time.fixedDeltaTime;
        }
        else if (onMoon && !slamming)
        {
            tanSpeed = Mathf.MoveTowards(tanSpeed, 0f, groundFriction * maxSkateSpeed * Time.fixedDeltaTime);
        }

        float tanCap = onMoon ? maxSkateSpeed : airSpeedCap;
        tanSpeed = Mathf.Clamp(tanSpeed, -tanCap, tanCap);

        // Radial velocity + gravity, with launch when skating fast enough (centrifugal)
        float radSpeed = Vector3.Dot(v, radialOut); // + = leaving moon
        float centripetalNeeded = (tanSpeed * tanSpeed) / Mathf.Max(0.1f, rideRadius);
        float pull = slamming ? moonPullAccel * 3f : moonPullAccel;

        if (onMoon && !slamming && centripetalNeeded > pull * launchAssist)
        {
            // Fast enough to ramp off — lift instead of glue
            radSpeed += (centripetalNeeded - pull) * Time.fixedDeltaTime;
            onMoon = false;
        }
        else
        {
            radSpeed -= pull * Time.fixedDeltaTime;
            if (onMoon && radSpeed < 0f)
                radSpeed = 0f;
        }

        // Super slam only in the air
        if (!onMoon)
        {
            ApplyAirSlam(ref radSpeed);
        }
        else
        {
            slamming = false;
            pendingSlam = false;
        }

        Vector3 newVel = tangentRight * tanSpeed + radialOut * radSpeed;
        float speedCap = onMoon ? maxSkateSpeed * 1.35f : airSpeedCap * 1.35f;
        if (newVel.magnitude > speedCap)
            newVel = newVel.normalized * speedCap;
        newVel.z = 0f;
        rb.linearVelocity = newVel;

        UpdateMoonVisual(tangentRight, tanSpeed);
    }

    void TryStartSpin()
    {
        if (!pendingSpin)
            return;

        pendingSpin = false;
        if (spinLeft > 0f || spinCooldown > 0f)
            return;

        spinLeft = spinDuration;
        spinCooldown = spinDuration + 0.05f;
        rb.linearVelocity = Vector3.zero;
    }

    void ApplyAirSlam(ref float radSpeed)
    {
        if (pendingSlam && slamCooldown <= 0f)
        {
            slamming = true;
            slamCooldown = 0.25f;
            radSpeed = -slamSpeed;
            pg.player.RecordUltUsed();
        }
        pendingSlam = false;

        if (slamming)
            radSpeed = Mathf.Min(radSpeed, -slamSpeed * 0.85f);
    }

    void TickSpin()
    {
        if (spinLeft <= 0f)
            return;

        float step = 360f * (Time.deltaTime / Mathf.Max(0.01f, spinDuration));
        if (visual != null)
            visual.Rotate(0f, 0f, step, Space.Self);
        else
            transform.Rotate(0f, 0f, step, Space.Self);

        spinLeft -= Time.deltaTime;
        if (spinLeft <= 0f && visual != null)
            visual.localRotation = Quaternion.Euler(0f, 180f, 0f);
    }

    void UpdateMoonVisual(Vector3 tangentRight, float tanSpeed)
    {
        if (visual == null || spinLeft > 0f)
            return;

        float lean = Mathf.Clamp(tanSpeed * 1.8f, -60f, 60f);
        visual.localRotation = Quaternion.Euler(0f, 180f, lean);
    }

    float GetMoonRadius()
    {
        if (moonGravity == null)
            return 3f;

        SphereCollider sc = moonGravity.GetComponent<SphereCollider>();
        if (sc != null)
        {
            float s = Mathf.Max(moonGravity.transform.lossyScale.x, moonGravity.transform.lossyScale.y);
            return sc.radius * s;
        }

        return 0.5f * Mathf.Max(moonGravity.transform.lossyScale.x, 3f);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (day || moving || moonGravity == null || rb == null)
            return;
        if (!IsMoonCollision(collision))
            return;

        ResolveMoonLanding(true);
    }

    void OnCollisionStay(Collision collision)
    {
        if (day || moving || moonGravity == null || rb == null)
            return;
        if (!IsMoonCollision(collision))
            return;

        // Keep from sinking while physics contacts
        Vector3 moonPos = moonGravity.transform.position;
        moonPos.z = transform.position.z;
        float rideRadius = GetMoonRadius() + starRadius + surfaceRide;
        Vector3 toStar = transform.position - moonPos;
        toStar.z = 0f;
        if (toStar.magnitude < rideRadius)
        {
            Vector3 pos = moonPos + toStar.normalized * rideRadius;
            pos.z = transform.position.z;
            rb.position = pos;
        }
    }

    void ResolveMoonLanding(bool fromImpact)
    {
        Vector3 moonPos = moonGravity.transform.position;
        moonPos.z = transform.position.z;
        Vector3 radialOut = transform.position - moonPos;
        radialOut.z = 0f;
        if (radialOut.sqrMagnitude < 0.0001f)
            radialOut = Vector3.up;
        radialOut.Normalize();

        float rideRadius = GetMoonRadius() + starRadius + surfaceRide;
        Vector3 pos = moonPos + radialOut * rideRadius;
        pos.z = transform.position.z;
        rb.position = pos;

        Vector3 v = rb.linearVelocity;
        v.z = 0f;
        Vector3 tangentRight = Vector3.Cross(radialOut, Vector3.forward).normalized;
        Vector3 vTan = tangentRight * Vector3.Dot(v, tangentRight);
        float intoMoon = Mathf.Max(0f, Vector3.Dot(v, -radialOut));

        if (!fromImpact && intoMoon < 1f && !slamming)
        {
            rb.linearVelocity = vTan;
            slamming = false;
            onMoon = true;
            return;
        }

        float tanSpeed = vTan.magnitude;
        float speed = v.magnitude + 0.001f;
        float skimQuality = Mathf.Clamp01(tanSpeed / speed);
        float boost = Mathf.Lerp(1f, maxBounceBoost, skimQuality);
        if (slamming)
            boost *= slamBounceBonus;

        Vector3 bounced = vTan + radialOut * (intoMoon * bounceRestitution);
        if (bounced.sqrMagnitude < 0.01f)
            bounced = (tanSpeed > 0.1f ? vTan : tangentRight * 8f);

        bounced *= boost;
        float cap = maxSkateSpeed * (slamming ? 1.4f : 1.25f);
        if (bounced.magnitude > cap)
            bounced = bounced.normalized * cap;

        rb.linearVelocity = bounced;
        slamming = false;
        onMoon = true;
    }

    bool IsMoonCollision(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return false;

        Transform t = collision.collider.transform;
        if (moonGravity != null && (t == moonGravity.transform || t.IsChildOf(moonGravity.transform)))
            return true;

        string n = t.name.ToLowerInvariant();
        return n.Contains("moon");
    }

    void SunAttacks()
    {
        Player p = pg.player;

        if (p.CanBump && day)
        {
            Vector3 spPo = sunGravity.transform.position + (spinPoint.parent.transform.up * sFoffset);

            if (sunLeft >= 0)
            {
                //Spawn Ready Bubbles

                //Send Solar Flare
                if ( pg.inp.tf_left || thought == Thought.MoveLeft)
                {
                    GameObject ls = Instantiate(solarFlare, spPo + (-spinPoint.parent.transform.right * sFoffset), spinPoint.parent.rotation);

                    Vector3 ro = ls.transform.rotation.eulerAngles;
                    ro.z += sFoffsetAngle;
                    ls.transform.rotation = Quaternion.Euler(ro);

                    PlayerGrab lsPG = ls.GetComponent<PlayerGrab>();

                    if(lsPG != null)
                    {
                        lsPG.playerIndex = pg.playerIndex;
                    }

                    ConstantSpin lsCS = ls.transform.GetChild(0).GetComponent<ConstantSpin>();

                    if (lsCS != null)
                    {
                        lsCS.spinSpeed = Random.Range(solarFlareSpeed.x,solarFlareSpeed.y);
                    }

                    Rigidbody lsRB = ls.GetComponent<Rigidbody>();

                    if(lsRB != null)
                    {
                        lsRB.AddForce(new Vector3(sFsideDir.x, sFsideDir.y, sFsideDir.z) * solarFlareSpeed.z, ForceMode.Force);
                    }

                    ResetFlares lsRF = ls.transform.GetChild(0).GetComponent<ResetFlares>();

                    if (lsRF != null)
                    {
                        lsRF.script = this;
                    }

                   sunLeft = -sFResetCount;
                }
            }

            if (sunMiddle >= 0)
            {
                //Spawn Ready Bubbles

                //Send Solar Flare
                if (pg.inp.tf_up || thought == Thought.MoveUp)
                {
                    GameObject ls = Instantiate(solarFlare, spPo, spinPoint.parent.rotation);

                    PlayerGrab lsPG = ls.GetComponent<PlayerGrab>();

                    if (lsPG != null)
                    {
                        lsPG.playerIndex = pg.playerIndex;
                    }

                    ConstantSpin lsCS = ls.GetComponent<ConstantSpin>();

                    if (lsCS != null)
                    {
                        lsCS.spinSpeed = Random.Range(solarFlareSpeed.x, solarFlareSpeed.y);
                    }

                    Rigidbody lsRB = ls.GetComponent<Rigidbody>();

                    if (lsRB != null)
                    {
                        lsRB.AddForce(new Vector3(sFsideDir.x, sFsideDir.y, sFsideDir.z) * solarFlareSpeed.z, ForceMode.Force);
                    }

                    sunMiddle = -sFResetCount;
                }
            }

            if (sunRight >= 0)
            {
                //Spawn Ready Bubbles

                //Send Solar Flare
                if (pg.inp.tf_right || thought == Thought.MoveRight)
                {
                    GameObject ls = Instantiate(solarFlare, spPo + (spinPoint.parent.transform.right * sFoffset), spinPoint.parent.rotation);

                    Vector3 ro = ls.transform.rotation.eulerAngles;
                    ro.z -= sFoffsetAngle;
                    ls.transform.rotation = Quaternion.Euler(ro);

                    PlayerGrab lsPG = ls.GetComponent<PlayerGrab>();

                    if (lsPG != null)
                    {
                        lsPG.playerIndex = pg.playerIndex;
                    }

                    ConstantSpin lsCS = ls.GetComponent<ConstantSpin>();

                    if (lsCS != null)
                    {
                        lsCS.spinSpeed = Random.Range(solarFlareSpeed.x, solarFlareSpeed.y);
                    }

                    Rigidbody lsRB = ls.GetComponent<Rigidbody>();

                    if (lsRB != null)
                    {
                        lsRB.AddForce(new Vector3(sFsideDir.x, sFsideDir.y, sFsideDir.z) * solarFlareSpeed.z,ForceMode.Force);
                    }

                    sunRight = -sFResetCount;
                }
            }

            sunLeft += Time.deltaTime;
            sunMiddle += Time.deltaTime;
            sunRight += Time.deltaTime;
        }
    }

    void PlanetsUpdate()
    {
        sunHit.enabled = (!moving && day);
        sunGravity.active = (!moving && day);

        moonHit.enabled = (!moving && !day);
        moonGravity.active = (!moving && !day);
    }

    void UpdateDayAndNight()
    {
        if(!moving)
        {
            if (cycle >= dayCycle)
            {
                day = !day;
                moving = true;
                StartCoroutine(SwapCycle());
            }

            cycle += Time.deltaTime;
        }
    }

    IEnumerator SwapCycle()
    {
        float rot = 0;
        int dir = 1;

        //Start Celarus Leaving
        if (day)
        {
            rot = 180;
            dir = -1;

            //Raise Celarus
            float leaveTime = 0;
            float timeframe = .75f;

            while(leaveTime < 1)
            {
                transform.position += transform.up * Time.deltaTime;

                leaveTime += (1/ timeframe) * Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            //Make Celarus Spin
        }

        Vector3 curRot = spinPoint.transform.localRotation.eulerAngles;

        while(Mathf.Abs(curRot.z - rot) > 1)
        {
            spinPoint.transform.localRotation = Quaternion.Euler(0, 0, curRot.z + (dir * spinSpeed * Time.deltaTime));

            //Make Celarus Leave/Return
            transform.position = (moonGravity.transform.position + leavePoint);
            yield return new WaitForEndOfFrame();
            curRot = spinPoint.transform.localRotation.eulerAngles;
        }

        spinPoint.transform.localRotation = Quaternion.Euler(0, 0, rot);
        rb.linearVelocity = Vector3.zero;
        yield return null;

        //Finish Celarus Return
        if (!day)
        {
            transform.position = (moonGravity.transform.position + (moonGravity.transform.up * 3));
            yield return null;
        }

        cycle = 0;
        moving = false;
        yield return null;
    }

    void GetLifelineObjs()
    {
        Player p = pg.player;

        if (p != null)
        {
            GameObject sll = p.spawnedLifeline;

            if (sll != null)
            {
                if(sll.transform.childCount >= 1)
                {
                    spinPoint = sll.transform.GetChild(0);

                    if(spinPoint != null)
                    {
                        if(spinPoint.childCount > 0)
                        {
                            for(int i = 0; i < spinPoint.childCount; i++)
                            {
                                Transform spc = spinPoint.GetChild(i);

                                if(spc != null)
                                {
                                    if(spc.name.ToLower().Trim() == "moon")
                                    {
                                        moonHit = spc.GetComponent<DamageOnTagHit>();
                                        moonGravity = spc.GetComponent<PullObjectIn>();
                                        // Keep moon from being shoved by the skating star
                                        Rigidbody moonRb = spc.GetComponent<Rigidbody>();
                                        if (moonRb != null)
                                        {
                                            moonRb.isKinematic = true;
                                            moonRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                                        }
                                    }
                                    else if(spc.name.ToLower().Trim() == "sun")
                                    {
                                        sunHit = spc.GetComponent<DamageOnTagHit>();
                                        sunGravity = spc.GetComponent<PullObjectIn>();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
