using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nari space field: objects crossing a boundary teleport to the opposite side.
/// Soft particle + translucent glow at the warp — not a solid blue cube.
/// </summary>
public class SpacePortalBounds : MonoBehaviour
{
    public float halfExtent = 10f;
    public float margin = 0.35f;
    readonly HashSet<int> teleportCooldown = new HashSet<int>();
    ParticleSystem sharedBurst;

    void Start()
    {
        if (FieldCameraFit.TryMeasureWallOuterHalf(transform, out _, out float center))
            halfExtent = Mathf.Max(4f, center - 0.5f);
        else if (Database.instance != null)
            halfExtent = Database.instance.FieldPlaySize * 0.5f - 0.5f;

        StyleSpaceWalls();
        sharedBurst = BuildBurstSystem();

        if (GameSettings.HazardsEnabled)
            gameObject.AddComponent<SpacePlanetBackdrop>();
    }

    void StyleSpaceWalls()
    {
        Texture voidTex = FieldTextureFactory.SpaceVoidWall();
        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++)
        {
            Renderer r = rends[i];
            if (r == null)
                continue;
            PartInfo pi = r.GetComponent<PartInfo>();
            if (pi == null || pi.part == null || !pi.part.canBeBorder)
                continue;

            FieldTextureFactory.ApplyAlbedo(r, voidTex, new Color(0.75f, 0.85f, 1f), 0.15f, 0.55f);
            r.material.EnableKeyword("_EMISSION");
            r.material.SetColor("_EmissionColor", new Color(0.05f, 0.12f, 0.28f) * 0.7f);
            // Keep authored uniform scale (flat identical barrier cells)
        }

        Collider[] cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            Collider c = cols[i];
            if (c == null)
                continue;
            PartInfo pi = c.GetComponent<PartInfo>();
            if (pi == null || pi.part == null || !pi.part.canBeBorder)
                continue;
            c.isTrigger = true;
        }
    }

    ParticleSystem BuildBurstSystem()
    {
        GameObject go = new GameObject("PortalBurst");
        go.transform.SetParent(transform, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.45f;
        main.startLifetime = 0.45f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
        main.startColor = new Color(0.55f, 0.8f, 1f, 0.45f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 48;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] {
                new GradientColorKey(new Color(0.6f, 0.85f, 1f), 0f),
                new GradientColorKey(new Color(0.3f, 0.5f, 1f), 1f)
            },
            new[] {
                new GradientAlphaKey(0.5f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLife.color = g;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            Shader sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null)
                sh = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (sh == null)
                sh = Shader.Find("Standard");
            if (sh != null)
            {
                renderer.material = new Material(sh);
                renderer.material.SetTexture("_MainTex", FieldTextureFactory.PortalSoftGlow());
                renderer.material.color = new Color(0.6f, 0.85f, 1f, 0.5f);
            }
        }

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    void FixedUpdate()
    {
        float z = Database.instance != null ? Database.instance.FieldPlaySize : transform.position.z;
        float limit = halfExtent - margin;

        if (Database.instance != null && Database.instance.players != null)
        {
            for (int i = 0; i < Database.instance.players.Count; i++)
            {
                Player p = Database.instance.players[i];
                if (p == null || p.spawnedPlayer == null)
                    continue;
                TryPortal(p.spawnedPlayer, z, limit);
            }
        }

        for (int i = 0; i < LiveBallRegistry.Count; i++)
        {
            BallInfo info = LiveBallRegistry.GetAt(i);
            if (info == null || info.gameObject == null)
                continue;
            TryPortal(info.gameObject, z, limit);
        }
    }

    void TryPortal(GameObject go, float z, float limit)
    {
        if (go == null)
            return;
        int id = go.GetEntityId().GetHashCode();
        if (teleportCooldown.Contains(id))
            return;

        Vector3 p = go.transform.position;
        bool crossed = false;
        Vector3 np = p;

        if (p.x > limit) { np.x = -limit + margin; crossed = true; }
        else if (p.x < -limit) { np.x = limit - margin; crossed = true; }

        if (p.y > limit) { np.y = -limit + margin; crossed = true; }
        else if (p.y < -limit) { np.y = limit - margin; crossed = true; }

        if (!crossed)
            return;

        np.z = z;
        SpawnWarpFx(p);
        SpawnWarpFx(np);
        go.transform.position = np;

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb != null)
            rb.position = np;

        teleportCooldown.Add(id);
        StartCoroutine(ClearCooldown(id, 0.2f));
    }

    System.Collections.IEnumerator ClearCooldown(int id, float delay)
    {
        yield return new WaitForSeconds(delay);
        teleportCooldown.Remove(id);
    }

    void SpawnWarpFx(Vector3 pos)
    {
        // Soft translucent disc
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Quad);
        disc.name = "Portal Glow";
        disc.transform.position = pos;
        disc.transform.localScale = Vector3.one * 1.4f;
        Collider col = disc.GetComponent<Collider>();
        if (col != null)
            Destroy(col);
        Renderer r = disc.GetComponent<Renderer>();
        FieldTextureFactory.ApplyTransparentGlow(r, FieldTextureFactory.PortalSoftGlow(),
            new Color(0.55f, 0.8f, 1f, 0.4f));
        disc.AddComponent<PortalGlowFade>();

        if (sharedBurst != null)
        {
            sharedBurst.transform.position = pos;
            sharedBurst.Play(true);
        }
    }
}

public class PortalGlowFade : MonoBehaviour
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
        float t = age / 0.55f;
        transform.localScale = startScale * (1f + t * 0.8f);
        Renderer r = GetComponent<Renderer>();
        if (r != null && r.material != null)
        {
            Color c = r.material.color;
            c.a = Mathf.Clamp01(0.4f * (1f - t));
            r.material.color = c;
        }
        if (age > 0.55f)
            Destroy(gameObject);
    }
}

/// <summary>Decorative planets drifting behind the play plane.</summary>
public class SpacePlanetBackdrop : MonoBehaviour
{
    class Planet
    {
        public Transform t;
        public Vector3 vel;
        public float life;
    }

    readonly List<Planet> planets = new List<Planet>();
    float spawnTimer;

    void Update()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = Random.Range(2.5f, 5f);
            Spawn();
        }

        float z = Database.instance != null ? Database.instance.FieldPlaySize : transform.position.z;
        for (int i = planets.Count - 1; i >= 0; i--)
        {
            Planet p = planets[i];
            if (p.t == null) { planets.RemoveAt(i); continue; }
            p.t.position += p.vel * Time.deltaTime;
            p.life -= Time.deltaTime;
            if (p.life <= 0f || Mathf.Abs(p.t.position.x) > 40f || Mathf.Abs(p.t.position.y) > 40f)
            {
                Destroy(p.t.gameObject);
                planets.RemoveAt(i);
            }
            else
            {
                Vector3 pos = p.t.position;
                pos.z = z + 2.5f;
                p.t.position = pos;
            }
        }
    }

    void Spawn()
    {
        float z = Database.instance != null ? Database.instance.FieldPlaySize : 20f;
        float half = 16f;
        Vector3 start = new Vector3(
            Random.value > 0.5f ? -half : half,
            Random.Range(-half, half),
            z + 2.5f);
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Planet";
        go.transform.SetParent(transform, true);
        go.transform.position = start;
        go.transform.localScale = Vector3.one * Random.Range(1.2f, 3.5f);
        Collider c = go.GetComponent<Collider>();
        if (c != null)
            Destroy(c);
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Standard"));
            r.material.color = Color.HSVToRGB(Random.value, 0.35f, Random.Range(0.35f, 0.7f));
        }
        planets.Add(new Planet
        {
            t = go.transform,
            vel = new Vector3(-Mathf.Sign(start.x) * Random.Range(1.5f, 3.5f), Random.Range(-0.6f, 0.6f), 0f),
            life = Random.Range(8f, 16f)
        });
    }
}
