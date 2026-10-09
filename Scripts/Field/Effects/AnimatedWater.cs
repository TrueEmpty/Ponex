using System.Collections.Generic;
using UnityEngine;

/// <summary>Scrolls / pulses a water-like material on the field floor effect part.</summary>
[RequireComponent(typeof(Renderer))]
public class AnimatedWater : MonoBehaviour
{
    public Vector2 scrollSpeed = new Vector2(0.04f, 0.02f);
    public float pulseAmp = 0.08f;
    public float pulseSpeed = 1.4f;
    Renderer ren;
    Material material;
    Vector2 baseScale = Vector2.one;
    Color baseColor;
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    void Start()
    {
        ren = GetComponent<Renderer>();
        if (ren != null)
        {
            material = ren.material;
            if (material != null)
            {
                baseColor = material.color;
                if (material.HasProperty(MainTexId))
                    baseScale = material.mainTextureScale;
            }
        }

        // Soft particle splash ring on nearby wall hits is handled by WaterSplashBridge
        if (GetComponent<WaterSplashBridge>() == null)
            gameObject.AddComponent<WaterSplashBridge>();
    }

    void Update()
    {
        if (material == null)
            return;

        Vector2 offset = scrollSpeed * Time.time;
        if (material.HasProperty(MainTexId))
            material.mainTextureOffset = offset;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmp;
        Color c = baseColor;
        c.r = Mathf.Clamp01(baseColor.r * pulse);
        c.g = Mathf.Clamp01(baseColor.g * (0.9f + pulseAmp * Mathf.Sin(Time.time * pulseSpeed * 0.7f)));
        material.color = c;
    }
}

/// <summary>Spawns brief splash bursts when balls hit Obstacle/Walls on ocean fields.</summary>
public class WaterSplashBridge : MonoBehaviour
{
    float cooldown;

    void Update()
    {
        cooldown -= Time.deltaTime;
    }

    void OnEnable()
    {
        // Listen via a lightweight scene probe on the field root
        Transform root = transform.root;
        if (root != null && root.GetComponent<WaterSplashListener>() == null)
            root.gameObject.AddComponent<WaterSplashListener>();
    }
}

public class WaterSplashListener : MonoBehaviour
{
    float cooldown;

    void OnCollisionEnter(Collision collision)
    {
        // Field root rarely collides — attach helpers to border parts instead
    }

    void Start()
    {
        // Tag border parts with splash responders
        Collider[] cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null)
                continue;
            string tag = cols[i].tag;
            if (tag != "Walls" && tag != "Wall" && tag != "Obstacle")
                continue;
            if (cols[i].GetComponent<WaterSplashOnHit>() == null)
                cols[i].gameObject.AddComponent<WaterSplashOnHit>();
        }
    }
}

public class WaterSplashOnHit : MonoBehaviour
{
    static float globalCooldown;
    static readonly Stack<SplashFade> pool = new Stack<SplashFade>();
    static Material splashMaterial;

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;
        if (Time.time < globalCooldown)
            return;
        GameObject other = collision.collider.gameObject;
        if (!other.CompareTag("Ball") && other.GetComponent<BallInfo>() == null)
            return;

        globalCooldown = Time.time + 0.08f;
        ContactPoint cp = collision.GetContact(0);
        SpawnSplash(cp.point);
    }

    static void SpawnSplash(Vector3 pos)
    {
        SplashFade splash = null;
        while (pool.Count > 0 && splash == null)
            splash = pool.Pop();

        if (splash != null)
        {
            splash.gameObject.SetActive(true);
            splash.Restart(pos);
            return;
        }

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Splash";
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.35f;
        Collider c = go.GetComponent<Collider>();
        if (c != null)
        {
            c.enabled = false;
            Destroy(c);
        }
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            if (splashMaterial == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader != null)
                {
                    splashMaterial = new Material(shader);
                    splashMaterial.color = new Color(0.55f, 0.8f, 1f, 0.55f);
                }
            }
            r.sharedMaterial = splashMaterial;
        }
        splash = go.AddComponent<SplashFade>();
        splash.Restart(pos);
    }

    public static void Recycle(SplashFade splash)
    {
        if (splash == null)
            return;
        splash.gameObject.SetActive(false);
        pool.Push(splash);
    }
}

public class SplashFade : MonoBehaviour
{
    float age;
    Vector3 startScale;
    Renderer cachedRenderer;
    MaterialPropertyBlock properties;
    static readonly int ColorId = Shader.PropertyToID("_Color");

    public void Restart(Vector3 position)
    {
        if (cachedRenderer == null)
            cachedRenderer = GetComponent<Renderer>();
        if (properties == null)
            properties = new MaterialPropertyBlock();

        age = 0f;
        startScale = Vector3.one * 0.35f;
        transform.position = position;
        transform.localScale = startScale;
        SetAlpha(0.55f);
    }

    void Update()
    {
        age += Time.deltaTime;
        transform.localScale = startScale * (1f + age * 3f);
        SetAlpha(Mathf.Clamp01(1f - age * 2.5f));
        if (age > 0.45f)
            WaterSplashOnHit.Recycle(this);
    }

    void SetAlpha(float alpha)
    {
        if (cachedRenderer == null)
            return;
        cachedRenderer.GetPropertyBlock(properties);
        properties.SetColor(ColorId, new Color(0.55f, 0.8f, 1f, alpha));
        cachedRenderer.SetPropertyBlock(properties);
    }
}
