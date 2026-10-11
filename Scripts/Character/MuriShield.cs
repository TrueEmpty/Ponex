using UnityEngine;

/// <summary>
/// Timed shield driven by the bump meter. Full charge lasts 10s and drains in real time.
/// A kunai-to-ball hit adds up to 3s. Any absorbed ball hit dumps the remaining charge.
/// </summary>
public class MuriShield : MonoBehaviour
{
    public Transform visual;
    public float fullCharge = 10f;
    public float kunaiCharge = 3f;
    public float blinkBelow = 3f;

    public bool HasShield => ChargeRemaining() > 0.001f;

    PlayerGrab pg;
    Renderer visRend;
    MaterialPropertyBlock block;
    Color baseColor = new Color(0.45f, 0.95f, 1f, 0.16f);
    Color emitColor = new Color(0.05f, 0.22f, 0.28f, 1f);
    bool meterReady;
    bool wasShielded;
    static Mesh shardMesh;
    static Material shardMat;

    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        pg = GetComponent<PlayerGrab>();
        if (visual == null)
        {
            Transform child = transform.Find("Shield");
            if (child != null)
                visual = child;
        }
        if (visual != null)
        {
            visRend = visual.GetComponent<Renderer>();
            if (visual.GetComponent<SkipPlayerSkin>() == null)
            {
                SkipPlayerSkin skip = visual.gameObject.AddComponent<SkipPlayerSkin>();
                skip.includeChildren = true;
            }
            if (visRend != null && visRend.sharedMaterial != null)
            {
                if (visRend.sharedMaterial.HasProperty("_Color"))
                    baseColor = visRend.sharedMaterial.GetColor("_Color");
                if (visRend.sharedMaterial.HasProperty("_EmissionColor"))
                    emitColor = visRend.sharedMaterial.GetColor("_EmissionColor");
            }
        }
        block = new MaterialPropertyBlock();
        SetVisible(false);
        if (visRend != null)
            visRend.enabled = true;
    }

    void Start()
    {
        SyncMeter();
        wasShielded = HasShield;
        SetVisible(HasShield);
        ApplyPulse(0.12f);
    }

    public bool TryAbsorb()
    {
        if (!HasShield)
            return false;
        BreakShield();
        return true;
    }

    public void AddCharge(float seconds)
    {
        SyncMeter();
        Skill bump = Bump();
        if (bump == null)
            return;

        float add = Mathf.Min(Mathf.Max(0f, seconds), kunaiCharge);
        if (add <= 0f)
            return;

        bump.Gain(add);
        SetVisible(true);
        if (visRend != null)
            visRend.enabled = true;
        wasShielded = true;
    }

    void Update()
    {
        SyncMeter();
        Skill bump = Bump();
        if (bump == null)
            return;

        Database db = Database.instance;
        bool live = db != null && db.gameStart
            && pg != null && pg.player != null && pg.player.currentHealth > 0;

        if (live && bump.amount > 0f)
        {
            bump.amount -= Time.deltaTime;
            if (bump.amount < 0f)
                bump.amount = 0f;
            if (bump.amount <= 0.001f)
            {
                bump.amount = 0f;
                BreakShield();
                return;
            }
        }

        bool shielded = bump.amount > 0.001f;
        if (shielded != wasShielded)
        {
            SetVisible(shielded);
            wasShielded = shielded;
        }
        if (!shielded || visRend == null)
            return;

        float remain = bump.amount;
        if (remain > blinkBelow)
        {
            visRend.enabled = true;
            float pulse = 0.08f + 0.05f * Mathf.Abs(Mathf.Sin(Time.time * 3.4f));
            ApplyPulse(pulse);
            return;
        }

        float urgency = 1f - Mathf.Clamp01(remain / Mathf.Max(0.01f, blinkBelow));
        float hz = Mathf.Lerp(1.6f, 12f, urgency);
        bool on = Mathf.Repeat(Time.time * hz, 1f) < 0.55f;
        visRend.enabled = on;
        if (on)
        {
            float pulse = 0.10f + 0.08f * urgency;
            ApplyPulse(pulse);
        }
    }

    void BreakShield()
    {
        Skill bump = Bump();
        if (bump != null)
            bump.amount = 0f;

        bool playFx = wasShielded || (visual != null && visual.gameObject.activeSelf);
        wasShielded = false;
        if (visRend != null)
            visRend.enabled = true;
        SetVisible(false);
        if (playFx)
            PlayBreakFx();
    }

    float ChargeRemaining()
    {
        Skill bump = Bump();
        return bump != null ? bump.amount : 0f;
    }

    Skill Bump()
    {
        if (pg == null)
            pg = GetComponent<PlayerGrab>();
        if (pg == null || pg.player == null)
            return null;
        return pg.player.bump;
    }

    void SyncMeter()
    {
        Skill bump = Bump();
        if (bump == null)
            return;

        bump.max = fullCharge;
        bump.cost = 999f;
        if (!meterReady && bump.amount <= 0.001f)
            bump.amount = fullCharge;
        if (bump.amount > bump.max)
            bump.amount = bump.max;
        meterReady = true;
    }

    void ApplyPulse(float alpha)
    {
        if (visRend == null)
            return;
        Color c = baseColor;
        c.a = alpha;
        visRend.GetPropertyBlock(block);
        block.SetColor(ColorId, c);
        block.SetColor(EmissionId, emitColor * (0.35f + alpha));
        visRend.SetPropertyBlock(block);
    }

    void SetVisible(bool on)
    {
        if (visual != null)
            visual.gameObject.SetActive(on);
    }

    void PlayBreakFx()
    {
        Vector3 pos = visual != null ? visual.position : transform.position;
        float radius = 0.35f;
        if (visual != null)
        {
            Vector3 s = visual.lossyScale;
            radius = Mathf.Max(0.12f, Mathf.Max(s.x, Mathf.Max(s.y, s.z)) * 0.5f);
        }

        Color shardColor = baseColor;
        shardColor.a = 0.55f;

        GameObject fx = new GameObject("MuriShieldBreak");
        fx.transform.position = pos;
        SkipPlayerSkin skip = fx.AddComponent<SkipPlayerSkin>();
        skip.includeChildren = true;

        ParticleSystem ps = ParticleEdit.AddStopped(fx);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.25f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 4.2f);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(0.04f, 0.11f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startSizeZ = new ParticleSystem.MinMaxCurve(0.006f, 0.018f);
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = shardColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.85f;
        main.maxParticles = 28;
        main.scalingMode = ParticleSystemScalingMode.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16, 22) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
        shape.radiusThickness = 0.35f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.35f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = fade;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.55f, 0.85f),
            new Keyframe(1f, 0.05f)));

        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.separateAxes = true;
        rotation.x = new ParticleSystem.MinMaxCurve(-6f, 6f);
        rotation.y = new ParticleSystem.MinMaxCurve(-6f, 6f);
        rotation.z = new ParticleSystem.MinMaxCurve(-8f, 8f);

        var renderer = fx.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = ShardMesh();
            renderer.sharedMaterial = ShardMaterial();
            renderer.minParticleSize = 0.001f;
            renderer.maxParticleSize = 0.35f;
        }

        ps.Play(true);
        Destroy(fx, 1.1f);
    }

    static Mesh ShardMesh()
    {
        if (shardMesh != null)
            return shardMesh;
        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tmp.hideFlags = HideFlags.HideAndDontSave;
        shardMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(tmp);
        return shardMesh;
    }

    static Material ShardMaterial()
    {
        if (shardMat != null)
            return shardMat;

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null)
            shader = Shader.Find("Particles/Alpha Blended");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            return null;

        shardMat = new Material(shader);
        shardMat.name = "MuriShieldShard";
        if (shardMat.HasProperty("_Mode"))
        {
            shardMat.SetFloat("_Mode", 2f);
            shardMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            shardMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            shardMat.SetInt("_ZWrite", 0);
            shardMat.DisableKeyword("_ALPHATEST_ON");
            shardMat.EnableKeyword("_ALPHABLEND_ON");
            shardMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            shardMat.renderQueue = 3000;
        }
        if (shardMat.HasProperty("_Color"))
            shardMat.SetColor("_Color", Color.white);
        if (shardMat.HasProperty("_TintColor"))
            shardMat.SetColor("_TintColor", new Color(1f, 1f, 1f, 0.7f));
        shardMat.SetFloat("_Glossiness", 0.65f);
        return shardMat;
    }
}
