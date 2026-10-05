using UnityEngine;

/// <summary>
/// Restores / tunes Iyolit flame visuals: billboard flame textures, play on spawn,
/// upward flame motion. Used on the character body, candle flames, and bump embers.
/// </summary>
public class IyolitFlameFx : MonoBehaviour
{
    public enum Style
    {
        Auto = 0,
        BodyCore = 1,   // small flame sitting on the wick
        BodyAura = 2,   // particles coming off the body
        Candle = 3,     // flames on burning candles
        BumpEmber = 4,  // small round ember sparks from bump projectiles
    }

    public Style style = Style.Auto;

    static Texture2D texA;
    static Texture2D texB;
    static Texture2D texC;
    static Texture2D texEmber;
    static bool texturesLoaded;

    void Awake()
    {
        Apply();
    }

    void OnEnable()
    {
        // Prefab has playOnAwake off — ensure systems run when enabled/spawned
        PlayAll();
    }

    public static void Ensure(GameObject root, Style preferred = Style.Auto)
    {
        if (root == null)
            return;

        IyolitFlameFx existing = root.GetComponent<IyolitFlameFx>();
        if (existing == null)
        {
            existing = root.AddComponent<IyolitFlameFx>();
            existing.style = preferred;
        }
        else if (preferred != Style.Auto)
        {
            existing.style = preferred;
        }
        existing.Apply();
    }

    public void Apply()
    {
        LoadTextures();
        Style resolved = ResolveStyle();

        if (resolved == Style.Candle)
            LayoutCandleFlames(transform);

        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            if (ps == null)
                continue;
            // Keep dedicated ember child config from EnsureEmberChild
            if (ps.gameObject.name == "FlameEmbers")
                continue;

            Style layer = resolved;
            if (resolved == Style.BumpEmber)
            {
                layer = Style.BumpEmber;
            }
            else if (systems.Length > 1)
            {
                if (ps.transform == transform)
                    layer = resolved == Style.Candle ? Style.Candle : Style.BodyAura;
                else
                    layer = resolved == Style.Candle ? Style.Candle : Style.BodyCore;
            }

            ConfigureSystem(ps, layer);
        }

        PlayAll();
    }

    /// <summary>
    /// Angles bump/candle side flames inward so they join the center flame,
    /// and drops the center slightly so it doesn't look split from the body.
    /// </summary>
    public static void LayoutCandleFlames(Transform root)
    {
        if (root == null)
            return;

        Transform center = null;
        Transform left = null;
        Transform right = null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c == null || !c.name.StartsWith("Iyolit"))
                continue;

            float z = c.localEulerAngles.z;
            if (z > 180f) z -= 360f;

            if (Mathf.Abs(z) < 5f && center == null)
                center = c;
            else if (z < -5f && left == null)
                left = c;
            else if (z > 5f && right == null)
                right = c;
            else if (center == null)
                center = c;
            else if (left == null)
                left = c;
            else if (right == null)
                right = c;
        }

        if (center == null && root.childCount > 0)
            center = root.GetChild(0);
        if (left == null || right == null)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform c = root.GetChild(i);
                if (c == center) continue;
                if (left == null) left = c;
                else if (right == null) right = c;
            }
        }

        const float sideAngle = 42f;
        const float centerY = -0.42f;
        const float sideY = -0.38f;
        const float sideX = 0.12f;

        if (center != null)
        {
            center.localPosition = new Vector3(0f, centerY, 0f);
            center.localRotation = Quaternion.identity;
        }
        if (left != null)
        {
            left.localPosition = new Vector3(-sideX, sideY, 0f);
            left.localRotation = Quaternion.Euler(0f, 0f, -sideAngle);
        }
        if (right != null)
        {
            right.localPosition = new Vector3(sideX, sideY, 0f);
            right.localRotation = Quaternion.Euler(0f, 0f, sideAngle);
        }
    }

    Style ResolveStyle()
    {
        if (style != Style.Auto)
            return style;

        string n = gameObject.name.ToLowerInvariant();
        if (GetComponent<IyolitBumpMovement>() != null)
            return Style.BumpEmber;
        if (n.Contains("flame") || n.Contains("candle"))
            return Style.Candle;

        if (transform.parent == null || transform.parent.GetComponent<IyolitMovement>() != null
            || GetComponent<IyolitMovement>() != null)
        {
            if (GetComponent<IyolitMovement>() != null)
                return Style.BodyAura;
            return Style.BodyCore;
        }

        return Style.BodyAura;
    }

    void PlayAll()
    {
        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null)
                continue;
            if (!systems[i].isPlaying)
                systems[i].Play(true);
        }
    }

    static void LoadTextures()
    {
        if (texturesLoaded)
            return;
        texturesLoaded = true;
        texA = Resources.Load<Texture2D>("Particles/FlameA");
        texB = Resources.Load<Texture2D>("Particles/FlameB");
        texC = Resources.Load<Texture2D>("Particles/FlameC");
        texEmber = Resources.Load<Texture2D>("Particles/FlameEmber");
    }

    static Material MakeMat(Texture2D tex)
    {
        Shader shader = Shader.Find("Particles/Additive");
        if (shader == null)
            shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            return null;

        Material mat = new Material(shader);
        if (mat.HasProperty("_TintColor"))
            mat.SetColor("_TintColor", new Color(1f, 1f, 1f, 0.9f));
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.white);
        if (tex != null)
        {
            mat.mainTexture = tex;
            mat.SetTexture("_MainTex", tex);
        }
        return mat;
    }

    static void ConfigureSystem(ParticleSystem ps, Style layer)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = true;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startRotation = 0f;

        Texture2D tex;
        Color c0;
        Color c1;

        switch (layer)
        {
            case Style.BodyCore:
                main.gravityModifier = -0.15f;
                tex = texA != null ? texA : texB;
                main.duration = 1f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
                main.maxParticles = 40;
                c0 = new Color(1f, 0.95f, 0.55f, 1f);
                c1 = new Color(1f, 0.45f, 0.08f, 0.95f);
                break;

            case Style.Candle:
                main.gravityModifier = -0.15f;
                tex = texB != null ? texB : texA;
                main.duration = 1.2f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.95f);
                main.maxParticles = 64;
                c0 = new Color(1f, 0.9f, 0.35f, 1f);
                c1 = new Color(1f, 0.25f, 0.05f, 0.9f);
                break;

            case Style.BumpEmber:
                // Short round ember sparks — plenty of them, not long streaks
                main.gravityModifier = -0.12f;
                tex = texEmber != null ? texEmber : (texC != null ? texC : texA);
                main.duration = 0.35f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.18f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.085f);
                main.maxParticles = 48;
                c0 = new Color(1f, 0.85f, 0.35f, 1f);
                c1 = new Color(1f, 0.35f, 0.05f, 0.9f);
                break;

            default: // BodyAura
                main.gravityModifier = -0.15f;
                tex = texC != null ? texC : texEmber;
                main.duration = 1.5f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.9f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
                main.maxParticles = 80;
                c0 = new Color(1f, 0.85f, 0.35f, 0.95f);
                c1 = new Color(1f, 0.35f, 0.05f, 0.75f);
                break;
        }

        main.startColor = new ParticleSystem.MinMaxGradient(c0, c1);

        var emission = ps.emission;
        emission.enabled = true;
        if (layer == Style.BumpEmber)
            emission.rateOverTime = 32f;
        else if (layer == Style.BodyAura)
            emission.rateOverTime = 28f;
        else if (layer == Style.Candle)
            emission.rateOverTime = 36f;
        else
            emission.rateOverTime = 22f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = layer == Style.BodyAura ? 25f : (layer == Style.BumpEmber ? 12f : 12f);
        shape.radius = layer == Style.BodyAura ? 0.15f : (layer == Style.BumpEmber ? 0.035f : 0.05f);
        if (layer == Style.BumpEmber)
            shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 1f, 0.85f), 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.1f), 0.45f),
                new GradientColorKey(new Color(0.55f, 0.05f, 0.02f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(0.75f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLife.color = g;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = layer == Style.BumpEmber
            ? new AnimationCurve(
                new Keyframe(0f, 0.85f),
                new Keyframe(0.2f, 1f),
                new Keyframe(0.55f, 0.55f),
                new Keyframe(1f, 0f))
            : new AnimationCurve(
                new Keyframe(0f, 0.45f),
                new Keyframe(0.2f, 1f),
                new Keyframe(0.7f, 0.75f),
                new Keyframe(1f, 0.05f));
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = layer == Style.Candle ? 1.4f : (layer == Style.BumpEmber ? 0.25f : 0.85f);
        velocity.z = 0f;
        velocity.orbitalX = 0f;
        velocity.orbitalY = 0f;
        velocity.orbitalZ = 0f;
        velocity.speedModifier = layer == Style.BumpEmber ? 0.05f : 0.15f;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = layer == Style.BumpEmber ? 0.12f : (layer == Style.BodyAura ? 0.35f : 0.22f);
        noise.frequency = 0.9f;
        noise.scrollSpeed = 0.45f;
        noise.damping = true;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.OldestInFront;
            renderer.minParticleSize = 0.01f;
            renderer.lengthScale = 1f;
            renderer.velocityScale = 0f;
            renderer.cameraVelocityScale = 0f;
            if (layer == Style.BumpEmber)
                renderer.maxParticleSize = 0.18f;
            else if (layer == Style.BodyAura)
                renderer.maxParticleSize = 0.35f;
            else
                renderer.maxParticleSize = 0.85f;
            Material mat = MakeMat(tex);
            if (mat != null)
            {
                renderer.sharedMaterial = mat;
                renderer.material = mat;
            }
        }

        // Extra ember spark layer on body aura / candle (not bump — bump IS the ember)
        if (layer == Style.BodyAura || layer == Style.Candle)
            EnsureEmberChild(ps.transform, layer);
    }

    static void EnsureEmberChild(Transform parent, Style parentStyle)
    {
        const string childName = "FlameEmbers";
        Transform existing = parent.Find(childName);
        GameObject go;
        if (existing != null)
            go = existing.gameObject;
        else
        {
            go = new GameObject(childName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.AddComponent<ParticleSystem>();
        }

        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps == null)
            return;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = true;
        main.duration = 0.4f;
        // Short-lived round sparks — denser, not long flame streaks
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.95f, 0.5f, 1f),
            new Color(1f, 0.4f, 0.05f, 0.85f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.15f;
        main.maxParticles = 56;
        main.startRotation = 0f;

        var emission = ps.emission;
        emission.rateOverTime = parentStyle == Style.Candle ? 22f : 16f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.06f;
        shape.radiusThickness = 1f;

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.5f),
                new GradientColorKey(new Color(0.4f, 0.05f, 0f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.1f),
                new GradientAlphaKey(0.6f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLife.color = g;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.9f),
            new Keyframe(0.25f, 1f),
            new Keyframe(0.7f, 0.4f),
            new Keyframe(1f, 0f)));

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = 0.2f;
        velocity.z = 0f;
        velocity.orbitalX = 0f;
        velocity.orbitalY = 0f;
        velocity.orbitalZ = 0f;
        velocity.speedModifier = 0.03f;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
        noise.frequency = 0.7f;
        noise.scrollSpeed = 0.3f;
        noise.damping = true;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.lengthScale = 1f;
            renderer.velocityScale = 0f;
            renderer.cameraVelocityScale = 0f;
            Material mat = MakeMat(texEmber != null ? texEmber : texC);
            if (mat != null)
            {
                renderer.sharedMaterial = mat;
                renderer.material = mat;
            }
            renderer.maxParticleSize = 0.16f;
        }

        ps.Play(true);
    }
}
