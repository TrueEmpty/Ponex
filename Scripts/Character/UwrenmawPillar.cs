using UnityEngine;

/// <summary>One of Uwrenmaw's pillars. Three ball hits break it. Cracks show the damage.</summary>
public class UwrenmawPillar : MonoBehaviour
{
    public const int MaxHp = 3;

    public Uwrenmaw owner;
    public int hp = MaxHp;
    public bool complete = true;

    Renderer rend;
    Transform visual;
    Vector3 fullScale;
    int shownDamage = -1;
    Color tint = Color.white;
    ParticleSystem growDust;

    public void Setup(Uwrenmaw uwren, bool finished)
    {
        owner = uwren;
        complete = finished;
        hp = finished ? MaxHp : 0;
        visual = transform.Find("Visual");
        if (visual != null)
        {
            rend = visual.GetComponent<Renderer>();
            fullScale = visual.localScale;
        }
        if (!finished)
        {
            SetGrow(0f);
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
        }
        else
            PlaceVisual(fullScale.z);
        RefreshCracks();
    }

    public void SetGrow(float amount)
    {
        amount = Mathf.Clamp01(amount);
        if (visual == null)
            return;
        PlaceVisual(Mathf.Lerp(0.12f, fullScale.z, amount));
    }

    public void PlaceVisual(float depth)
    {
        if (visual == null)
            return;
        depth = Mathf.Max(0.08f, depth);
        float full = Mathf.Max(0.08f, fullScale.z);
        float stickUp = 1.45f * Mathf.Clamp01(depth / full);
        Vector3 s = fullScale;
        s.z = depth;
        visual.localScale = s;
        visual.localPosition = new Vector3(0f, 0f, -stickUp + depth * 0.5f);
    }

    public void Finish()
    {
        complete = true;
        hp = MaxHp;
        StopGrowDust();
        PlaceVisual(fullScale.z);
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = true;
        RefreshCracks();
    }

    public void SetTint(Color color)
    {
        tint = color;
        tint.a = 1f;
        ApplyTint();
        if (growDust != null)
        {
            var main = growDust.main;
            Color dust = tint;
            dust.a = 0.9f;
            main.startColor = dust;
        }
    }

    static Material ParticleMaterial()
    {
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        mat.mainTexture = UwrenmawArt.Soft();
        mat.color = Color.white;
        return mat;
    }

    public void RefreshCracks()
    {
        if (rend == null && visual != null)
            rend = visual.GetComponent<Renderer>();
        if (rend == null)
            return;
        int damage = complete ? MaxHp - Mathf.Clamp(hp, 0, MaxHp) : 0;
        if (damage != shownDamage)
        {
            shownDamage = damage;
            if (rend.material != null)
                rend.material.mainTexture = UwrenmawArt.Cracks(damage);
        }
        ApplyTint();
    }

    void ApplyTint()
    {
        if (rend == null)
            return;
        Color shade = Color.Lerp(Color.white, tint, 0.72f);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        rend.GetPropertyBlock(block);
        block.SetColor("_Color", shade);
        block.SetColor("_BaseColor", shade);
        rend.SetPropertyBlock(block);
    }

    public void BeginGrowDust()
    {
        StopGrowDust();
        GameObject go = new GameObject("Grow Dust");
        go.transform.SetParent(transform, false);
        growDust = go.AddComponent<ParticleSystem>();
        var main = growDust.main;
        main.loop = true;
        main.startLifetime = 0.7f;
        main.startSpeed = 0.9f;
        main.startSize = 0.18f;
        Color dust = tint;
        dust.a = 0.9f;
        main.startColor = dust;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;
        var emission = growDust.emission;
        emission.rateOverTime = 28f;
        var shape = growDust.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.35f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = ParticleMaterial();
        }
        growDust.Play();
    }

    public void StopGrowDust()
    {
        if (growDust == null)
            return;
        ParticleSystem dust = growDust;
        growDust = null;
        dust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(dust.gameObject, 1.2f);
    }

    public void Puff()
    {
        GameObject go = new GameObject("Pillar Smoke");
        go.transform.position = transform.position;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.duration = 0.25f;
        main.startLifetime = 0.45f;
        main.startSpeed = 1.6f;
        main.startSize = 0.35f;
        Color smoke = tint;
        smoke.a = 0.85f;
        main.startColor = smoke;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 30;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 16) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.material = ParticleMaterial();
        }
        ps.Play();
        Destroy(go, 1.2f);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!complete || owner == null || hp <= 0 || collision == null)
            return;
        if (!collision.collider.CompareTag("Ball"))
            return;
        owner.DamagePillar(this, collision.gameObject);
    }
}
