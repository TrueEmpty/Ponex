using UnityEngine;

[RequireComponent(typeof(BallInfo))]
public class ShockBall : MonoBehaviour
{
    Database db;
    BallInfo bI;
    Rigidbody rb;
    Renderer ren;
    Material runtimeMat;
    ParticleSystem zapFx;

    [Header("Idle (non-Zap) speeds — a bit under Basic")]
    public float idleStartSpeed = 0.8f;
    public float idleMinSpeed = 0.8f;
    public float idleMaxSpeed = 3.2f;

    [Header("Zap speeds — nearly Speed Ball (3 / 8)")]
    public float zapStartSpeed = 2.75f;
    public float zapMinSpeed = 2.75f;
    public float zapMaxSpeed = 7.5f;

    public float zapDuration = 5f;
    public Vector2 idleIntervalRange = new Vector2(4f, 10f);
    public float shockDuration = 3f;
    public float shockMoveFactor = 0.5f;

    public Color idleColor = new Color(0.35f, 0.4f, 0.55f, 1f);
    public Color zapColor = new Color(1f, 0.92f, 0.25f, 1f);
    public Color zapEmission = new Color(1f, 0.85f, 0.15f, 1f);

    public Material idleMaterial;
    public Material zapMaterial;

    bool zapActive;
    float stateEndsAt = -1f;
    Color baseIdle = Color.white;

    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();
        rb = GetComponent<Rigidbody>();
        ren = GetComponent<Renderer>();

        if (ren != null)
        {
            runtimeMat = ren.material;
            baseIdle = runtimeMat.HasProperty("_Color") ? runtimeMat.color : idleColor;
        }

        EnsureZapParticles();
        // Begin ready to enter Zap soon so the match sees it early
        zapActive = false;
        ApplyVisuals(false);
        ApplySpeeds(false);
        stateEndsAt = Time.time + Random.Range(1.5f, 3.5f);
    }

    void Update()
    {
        if (db == null || !db.gameStart || bI == null || !bI.ballReady || !bI.projectionOn)
            return;

        if (Time.time < stateEndsAt)
            return;

        if (zapActive)
            EndZap();
        else
            BeginZap();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!zapActive || db == null || !db.gameStart || bI == null || !bI.ballReady || !bI.projectionOn)
            return;

        string tag = collision.transform.tag;
        if (tag != "Paddle" && tag != "Player")
            return;

        PlayerGrab hitGrab = collision.gameObject.GetComponent<PlayerGrab>();
        if (hitGrab == null)
            hitGrab = collision.gameObject.GetComponentInParent<PlayerGrab>();
        if (hitGrab == null || !hitGrab.IsLinked() || hitGrab.player == null)
            return;

        // Shock hits any paddle — owner or opponent
        Player victim = hitGrab.player;
        victim.AddConstraint(gameObject, shockDuration, PlayerConstraint.Dash);
        victim.AddConstraint(gameObject, shockDuration, PlayerConstraint.Super);
        victim.AddMoveSpeedFactor(gameObject, shockDuration, shockMoveFactor);

        EndZap();
    }

    void BeginZap()
    {
        zapActive = true;
        stateEndsAt = Time.time + zapDuration;
        ApplySpeeds(true);
        ApplyVisuals(true);
        ScaleVelocityToMin();
    }

    void EndZap()
    {
        zapActive = false;
        stateEndsAt = Time.time + Random.Range(idleIntervalRange.x, idleIntervalRange.y);
        ApplySpeeds(false);
        ApplyVisuals(false);
        ScaleVelocityToMin();
    }

    void ApplySpeeds(bool zap)
    {
        if (bI == null || bI.ball == null)
            return;

        if (zap)
        {
            bI.ball.startSpeed = zapStartSpeed;
            bI.ball.minSpeed = zapMinSpeed;
            bI.ball.maxSpeed = zapMaxSpeed;
        }
        else
        {
            bI.ball.startSpeed = idleStartSpeed;
            bI.ball.minSpeed = idleMinSpeed;
            bI.ball.maxSpeed = idleMaxSpeed;
        }
    }

    void ScaleVelocityToMin()
    {
        if (rb == null || bI == null || bI.ball == null)
            return;

        Vector3 v = rb.linearVelocity;
        v.z = 0f;
        float min = Mathf.Max(0.01f, bI.ball.minSpeed);
        float max = Mathf.Max(min, bI.ball.BumpSpeedCap);
        float speed = v.magnitude;
        if (speed < 0.0001f)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.01f)
                dir = Vector2.right;
            rb.linearVelocity = new Vector3(dir.x, dir.y, 0f) * min;
            return;
        }

        if (speed < min)
            rb.linearVelocity = v.normalized * min;
        else if (speed > max)
            rb.linearVelocity = v.normalized * max;
    }

    void ApplyVisuals(bool zap)
    {
        if (ren != null)
        {
            if (zap && zapMaterial != null)
                ren.sharedMaterial = zapMaterial;
            else if (!zap && idleMaterial != null)
                ren.sharedMaterial = idleMaterial;

            // Instance glow so emission pulses in Zap without mutating shared assets permanently
            runtimeMat = ren.material;
            if (zap)
            {
                if (runtimeMat.HasProperty("_Color"))
                    runtimeMat.color = zapColor;
                if (runtimeMat.HasProperty("_EmissionColor"))
                {
                    runtimeMat.EnableKeyword("_EMISSION");
                    runtimeMat.SetColor("_EmissionColor", zapEmission * 2.2f);
                }
            }
            else
            {
                if (runtimeMat.HasProperty("_Color"))
                    runtimeMat.color = idleMaterial != null ? baseIdle : idleColor;
                if (runtimeMat.HasProperty("_EmissionColor"))
                {
                    runtimeMat.SetColor("_EmissionColor", Color.black);
                    runtimeMat.DisableKeyword("_EMISSION");
                }
            }
        }

        if (zapFx != null)
        {
            var emission = zapFx.emission;
            emission.enabled = zap;
            if (zap && !zapFx.isPlaying)
                zapFx.Play();
            else if (!zap && zapFx.isPlaying)
                zapFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void EnsureZapParticles()
    {
        Transform existing = transform.Find("ZapFX");
        GameObject fxObj = existing != null ? existing.gameObject : new GameObject("ZapFX");
        if (existing == null)
            fxObj.transform.SetParent(transform, false);
        fxObj.transform.localPosition = Vector3.zero;

        zapFx = fxObj.GetComponent<ParticleSystem>();
        if (zapFx == null)
            zapFx = fxObj.AddComponent<ParticleSystem>();

        var main = zapFx.main;
        main.loop = true;
        main.startLifetime = 0.28f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 0.55f, 1f),
            new Color(0.55f, 0.85f, 1f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 28;
        main.gravityModifier = 0f;

        var emission = zapFx.emission;
        emission.rateOverTime = 22f;
        emission.enabled = false;

        var shape = zapFx.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.28f;

        var trails = zapFx.trails;
        trails.enabled = false;

        var colorOver = zapFx.colorOverLifetime;
        colorOver.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 1f, 0.7f), 0f),
                new GradientColorKey(new Color(0.4f, 0.7f, 1f), 0.55f),
                new GradientColorKey(new Color(0.2f, 0.3f, 0.8f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.7f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOver.color = g;

        var renderer = fxObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.trailMaterial = renderer.sharedMaterial;
        Shader sh = Shader.Find("Particles/Standard Unlit");
        if (sh != null)
        {
            Material mat = new Material(sh);
            mat.SetColor("_Color", new Color(1f, 0.95f, 0.4f, 1f));
            renderer.sharedMaterial = mat;
            renderer.trailMaterial = mat;
        }

        zapFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
