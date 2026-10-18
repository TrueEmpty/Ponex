using UnityEngine;

/// <summary>Temporary shock applied by Test's bump. Same zap look and paddle shock as Shock Ball.</summary>
public class ShockedBall : MonoBehaviour
{
    public float shockDuration = 3f;
    public float shockMoveFactor = 0.5f;

    static readonly Color ZapColor = new Color(1f, 0.92f, 0.25f, 1f);
    static readonly Color ZapEmission = new Color(1f, 0.85f, 0.15f, 1f);

    float endsAt;
    ParticleSystem zapFx;
    Renderer[] renderers;
    Material[] savedMaterials;
    bool lookApplied;

    public void Refresh(float seconds)
    {
        endsAt = Time.time + seconds;
        ShockBall ownShock = GetComponent<ShockBall>();
        if (ownShock != null)
            ownShock.ForceZap(seconds);

        ApplyZapLook();

        if (zapFx == null)
            zapFx = ShockBall.CreateZapFx(transform, "BumpZapFX");
        var main = zapFx.main;
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 3.4f);
        main.maxParticles = 80;
        var emission = zapFx.emission;
        emission.rateOverTime = 70f;
        emission.enabled = true;
        var shape = zapFx.shape;
        shape.radius = 0.45f;
        if (!zapFx.isPlaying)
            zapFx.Play();
    }

    void ApplyZapLook()
    {
        if (lookApplied)
            return;

        Renderer[] found = GetComponentsInChildren<Renderer>(true);
        int count = 0;
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] == null || found[i] is ParticleSystemRenderer)
                continue;
            count++;
        }
        if (count == 0)
            return;

        renderers = new Renderer[count];
        savedMaterials = new Material[count];
        int n = 0;
        for (int i = 0; i < found.Length; i++)
        {
            Renderer ren = found[i];
            if (ren == null || ren is ParticleSystemRenderer)
                continue;
            renderers[n] = ren;
            savedMaterials[n] = ren.sharedMaterial;
            Material mat = ren.material;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", ZapColor);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", ZapColor);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", ZapEmission * 2.2f);
            }
            n++;
        }
        lookApplied = true;
    }

    void RestoreLook()
    {
        if (!lookApplied || renderers == null)
            return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            renderers[i].sharedMaterial = savedMaterials[i];
        }
        lookApplied = false;
    }

    void Update()
    {
        if (Time.time < endsAt)
            return;
        if (zapFx != null)
        {
            ParticleSystem.EmissionModule emission = zapFx.emission;
            emission.enabled = false;
            zapFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        RestoreLook();
        Destroy(this);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (Time.time > endsAt || collision == null)
            return;

        string tag = collision.transform.tag;
        if (tag != "Paddle" && tag != "Player")
            return;

        PlayerGrab hitGrab = collision.gameObject.GetComponent<PlayerGrab>();
        if (hitGrab == null)
            hitGrab = collision.gameObject.GetComponentInParent<PlayerGrab>();
        if (hitGrab == null || !hitGrab.IsLinked() || hitGrab.player == null)
            return;

        Player victim = hitGrab.player;
        victim.AddConstraint(gameObject, shockDuration, PlayerConstraint.Dash);
        victim.AddConstraint(gameObject, shockDuration, PlayerConstraint.Super);
        victim.AddMoveSpeedFactor(gameObject, shockDuration, shockMoveFactor);
    }
}
