using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Temporary star residue that can bump the ball and claim ownership.
/// Collider matches the star; mesh is hidden; tiny star particles provide the VFX.
/// Optional pull toward a fixed world point (drop trail).
/// </summary>
public class CelarusResidue : MonoBehaviour
{
    public Vector3 pullTarget;
    public bool pullToTarget;
    public float pullStrength = 2200f;
    public float pullRadius = 4f;

    PlayerGrab grab;

    static Texture2D starCutoutTex;
    static Texture2D starTwinkleTex;
    static bool texturesResolved;

    static readonly Color[] StarPalette =
    {
        new Color(1f, 0.92f, 0.2f, 1f),   // yellow
        new Color(0.72f, 0.35f, 1f, 1f),  // purple
        new Color(0.25f, 0.45f, 1f, 1f),  // blue
        new Color(0.2f, 0.95f, 1f, 1f),   // cyan
        new Color(1f, 1f, 1f, 1f),        // white
    };

    public static CelarusResidue Spawn(
        Vector3 position,
        int playerIndex,
        float duration,
        float radius,
        Color color,
        bool pullToTarget = false,
        Vector3 pullTarget = default,
        float pullStrength = 2200f,
        float pullRadius = 4f)
    {
        float r = Mathf.Max(0.08f, radius);

        GameObject go = new GameObject(pullToTarget ? "CelarusDropTrail" : "CelarusSpinResidue");
        go.tag = "Paddle";
        go.layer = 3;
        go.transform.position = position;

        // Collider root is scaled to star size; keep FX unscaled so textures stay crisp
        SphereCollider col = go.AddComponent<SphereCollider>();
        col.isTrigger = false;
        col.radius = r;

        Rigidbody body = go.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        PlayerGrab pg = go.AddComponent<PlayerGrab>();
        pg.playerIndex = playerIndex;

        SetBallOwnerOnTagHit owner = go.AddComponent<SetBallOwnerOnTagHit>();
        owner.delay = 0f;
        if (owner.targetTags == null)
            owner.targetTags = new List<string>();
        if (!owner.targetTags.Exists(t => t != null && t.Equals("Ball", System.StringComparison.OrdinalIgnoreCase)))
            owner.targetTags.Add("Ball");

        AttachStarParticles(go, r, duration);

        DestroyAfterTime life = go.AddComponent<DestroyAfterTime>();
        life.duration = Mathf.Max(0.05f, duration);

        CelarusResidue residue = go.AddComponent<CelarusResidue>();
        residue.grab = pg;
        residue.pullToTarget = pullToTarget;
        residue.pullTarget = pullTarget;
        residue.pullStrength = pullStrength;
        residue.pullRadius = Mathf.Max(r * 2f, pullRadius);

        IgnoreNonBallColliders(col, playerIndex);
        return residue;
    }

    static void ResolveParticleTextures()
    {
        if (texturesResolved)
            return;
        texturesResolved = true;

        starCutoutTex = Resources.Load<Texture2D>("Particles/StarCutout");
        starTwinkleTex = Resources.Load<Texture2D>("Particles/StarTwinkle");

        // Fallback: Texture (some importers don't expose Texture2D via that path)
        if (starCutoutTex == null)
            starCutoutTex = Resources.Load<Texture>("Particles/StarCutout") as Texture2D;
        if (starTwinkleTex == null)
            starTwinkleTex = Resources.Load<Texture>("Particles/StarTwinkle") as Texture2D;

        if (starCutoutTex == null)
            Debug.LogWarning("CelarusResidue: missing Resources/Particles/StarCutout");
        if (starTwinkleTex == null)
            Debug.LogWarning("CelarusResidue: missing Resources/Particles/StarTwinkle");
    }

    static Material MakeParticleMaterial(Texture2D tex)
    {
        Shader shader = Shader.Find("Particles/Additive");
        if (shader == null)
            shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            return null;

        Material mat = new Material(shader);
        // White tint so particle startColor / palette drives the hue
        if (mat.HasProperty("_TintColor"))
            mat.SetColor("_TintColor", new Color(1f, 1f, 1f, 0.85f));
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.white);
        if (tex != null)
        {
            mat.mainTexture = tex;
            mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
        }
        return mat;
    }

    static void AttachStarParticles(GameObject host, float radius, float duration)
    {
        ResolveParticleTextures();

        // Main star cutouts — each burst picks a palette color
        BuildParticleLayer(
            host,
            "StarDust",
            starCutoutTex,
            duration,
            rate: 14f,
            burstMin: 8,
            burstMax: 14,
            sizeMin: radius * 0.22f,
            sizeMax: radius * 0.42f,
            speedMin: 0.04f,
            speedMax: 0.35f,
            maxParticles: 40,
            shapeRadius: radius * 0.85f,
            orbit: 0.75f,
            twinklePulse: false);

        // Smaller twinkle sparks
        BuildParticleLayer(
            host,
            "StarTwinkle",
            starTwinkleTex,
            duration,
            rate: 22f,
            burstMin: 12,
            burstMax: 20,
            sizeMin: radius * 0.06f,
            sizeMax: radius * 0.14f,
            speedMin: 0.02f,
            speedMax: 0.55f,
            maxParticles: 56,
            shapeRadius: radius * 1.05f,
            orbit: 1.35f,
            twinklePulse: true);
    }

    static void BuildParticleLayer(
        GameObject host,
        string name,
        Texture2D tex,
        float duration,
        float rate,
        short burstMin,
        short burstMax,
        float sizeMin,
        float sizeMax,
        float speedMin,
        float speedMax,
        int maxParticles,
        float shapeRadius,
        float orbit,
        bool twinklePulse)
    {
        GameObject fx = new GameObject(name);
        fx.transform.SetParent(host.transform, false);
        fx.transform.localPosition = Vector3.zero;
        fx.transform.localScale = Vector3.one;

        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        // AddComponent starts playing when playOnAwake is default-true — stop before editing duration
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = Mathf.Max(0.1f, duration);
        main.startLifetime = twinklePulse
            ? new ParticleSystem.MinMaxCurve(duration * 0.15f, duration * 0.45f)
            : new ParticleSystem.MinMaxCurve(duration * 0.45f, duration * 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        // Random palette per particle (yellow / purple / blue / cyan / white)
        main.startColor = BuildPaletteStartColor();
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = twinklePulse ? -0.03f : -0.012f;
        main.maxParticles = maxParticles;
        main.scalingMode = ParticleSystemScalingMode.Local;

        var emission = ps.emission;
        emission.rateOverTime = rate;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstMin, burstMax) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(0.05f, shapeRadius);
        shape.radiusThickness = 1f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        if (twinklePulse)
        {
            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.08f),
                    new GradientAlphaKey(0.2f, 0.35f),
                    new GradientAlphaKey(1f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
        }
        else
        {
            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.1f),
                    new GradientAlphaKey(0.7f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
        }
        colorOverLifetime.color = fade;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = twinklePulse
            ? new AnimationCurve(
                new Keyframe(0f, 0.15f),
                new Keyframe(0.15f, 1.1f),
                new Keyframe(0.4f, 0.2f),
                new Keyframe(0.65f, 1f),
                new Keyframe(1f, 0f))
            : new AnimationCurve(
                new Keyframe(0f, 0.4f),
                new Keyframe(0.2f, 1f),
                new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        // Keep orbital curves in matching Constant mode (avoids mode-mismatch errors / bad AABB)
        float orbitAmt = Mathf.Clamp(orbit, 0f, 3f);
        velocity.orbitalX = 0f;
        velocity.orbitalY = 0f;
        velocity.orbitalZ = orbitAmt;
        velocity.speedModifier = twinklePulse ? 0.25f : 0.12f;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = twinklePulse ? 0.2f : 0.1f;
        noise.frequency = twinklePulse ? 1.4f : 0.75f;
        noise.scrollSpeed = 0.3f;
        noise.damping = true;

        var renderer = fx.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            Material mat = MakeParticleMaterial(tex);
            if (mat != null)
            {
                renderer.sharedMaterial = mat;
                renderer.material = mat;
            }
            renderer.maxParticleSize = 0.5f;
            renderer.minParticleSize = 0.001f;
        }

        ps.Play(true);
    }

    static ParticleSystem.MinMaxGradient BuildPaletteStartColor()
    {
        Gradient g = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[StarPalette.Length];
        for (int i = 0; i < StarPalette.Length; i++)
        {
            float t = StarPalette.Length <= 1 ? 0f : i / (float)(StarPalette.Length - 1);
            colorKeys[i] = new GradientColorKey(StarPalette[i], t);
        }
        g.SetKeys(
            colorKeys,
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });

        ParticleSystem.MinMaxGradient mm = new ParticleSystem.MinMaxGradient(g);
        mm.mode = ParticleSystemGradientMode.RandomColor;
        return mm;
    }

    static void IgnoreNonBallColliders(Collider self, int playerIndex)
    {
        if (self == null)
            return;

        Collider[] nearby = Physics.OverlapSphere(self.bounds.center, self.bounds.extents.magnitude + 2f);
        for (int i = 0; i < nearby.Length; i++)
        {
            Collider other = nearby[i];
            if (other == null || other == self)
                continue;
            if (other.CompareTag("Ball"))
                continue;
            Physics.IgnoreCollision(self, other, true);
        }

        PlayerGrab[] grabs = Object.FindObjectsByType<PlayerGrab>(FindObjectsInactive.Exclude);
        for (int i = 0; i < grabs.Length; i++)
        {
            PlayerGrab g = grabs[i];
            if (g == null || g.playerIndex != playerIndex)
                continue;
            Collider[] cols = g.GetComponentsInChildren<Collider>();
            for (int c = 0; c < cols.Length; c++)
            {
                if (cols[c] != null && cols[c] != self)
                    Physics.IgnoreCollision(self, cols[c], true);
            }
        }
    }

    public void SetPullTarget(Vector3 target)
    {
        pullTarget = target;
        pullToTarget = true;
    }

    void FixedUpdate()
    {
        if (!pullToTarget)
            return;

        Collider[] hits = Physics.OverlapSphere(transform.position, pullRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null || !hit.CompareTag("Ball"))
                continue;

            Rigidbody ballRb = hit.attachedRigidbody != null ? hit.attachedRigidbody : hit.GetComponent<Rigidbody>();
            if (ballRb == null)
                continue;

            Vector3 toTarget = pullTarget - ballRb.position;
            toTarget.z = 0f;
            float dist = toTarget.magnitude;
            if (dist < 0.01f)
                continue;

            float falloff = 1f - Mathf.Clamp01(dist / Mathf.Max(0.1f, pullRadius));
            ballRb.AddForce(toTarget.normalized * (pullStrength * (0.35f + 0.65f * falloff)) * Time.fixedDeltaTime, ForceMode.Acceleration);

            PlayerGrab ballGrab = hit.GetComponent<PlayerGrab>();
            if (ballGrab != null && grab != null)
                ballGrab.playerIndex = grab.playerIndex;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;
        if (!collision.collider.CompareTag("Ball"))
            return;

        PlayerGrab ballGrab = collision.collider.GetComponent<PlayerGrab>();
        if (ballGrab != null && grab != null)
            ballGrab.playerIndex = grab.playerIndex;
    }
}
