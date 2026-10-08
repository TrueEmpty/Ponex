using UnityEngine;

/// <summary>Scrolls / pulses a water-like material on the field floor effect part.</summary>
[RequireComponent(typeof(Renderer))]
public class AnimatedWater : MonoBehaviour
{
    public Vector2 scrollSpeed = new Vector2(0.04f, 0.02f);
    public float pulseAmp = 0.08f;
    public float pulseSpeed = 1.4f;
    Renderer ren;
    Vector2 baseScale = Vector2.one;
    Color baseColor;

    void Start()
    {
        ren = GetComponent<Renderer>();
        if (ren != null && ren.material != null)
        {
            baseColor = ren.material.color;
            if (ren.material.HasProperty("_MainTex"))
                baseScale = ren.material.mainTextureScale;
        }

        // Soft particle splash ring on nearby wall hits is handled by WaterSplashBridge
        if (GetComponent<WaterSplashBridge>() == null)
            gameObject.AddComponent<WaterSplashBridge>();
    }

    void Update()
    {
        if (ren == null || ren.material == null)
            return;

        Vector2 offset = scrollSpeed * Time.time;
        if (ren.material.HasProperty("_MainTex"))
            ren.material.mainTextureOffset = offset;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmp;
        Color c = baseColor;
        c.r = Mathf.Clamp01(baseColor.r * pulse);
        c.g = Mathf.Clamp01(baseColor.g * (0.9f + pulseAmp * Mathf.Sin(Time.time * pulseSpeed * 0.7f)));
        ren.material.color = c;
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
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Splash";
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.35f;
        Collider c = go.GetComponent<Collider>();
        if (c != null)
            Destroy(c);
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Standard"));
            r.material.color = new Color(0.55f, 0.8f, 1f, 0.55f);
        }
        go.AddComponent<SplashFade>();
    }
}

public class SplashFade : MonoBehaviour
{
    float age;
    Vector3 startScale;

    void Start()
    {
        startScale = transform.localScale;
    }

    void Update()
    {
        age += Time.deltaTime;
        transform.localScale = startScale * (1f + age * 3f);
        Renderer r = GetComponent<Renderer>();
        if (r != null && r.material != null)
        {
            Color c = r.material.color;
            c.a = Mathf.Clamp01(1f - age * 2.5f);
            r.material.color = c;
        }
        if (age > 0.45f)
            Destroy(gameObject);
    }
}
