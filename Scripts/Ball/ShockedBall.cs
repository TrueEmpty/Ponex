using UnityEngine;

/// <summary>Temporary shock applied by Test's bump. Same zap particles and paddle shock as Shock Ball.</summary>
public class ShockedBall : MonoBehaviour
{
    public float shockDuration = 3f;
    public float shockMoveFactor = 0.5f;

    float endsAt;
    ParticleSystem zapFx;

    public void Refresh(float seconds)
    {
        endsAt = Time.time + seconds;
        if (zapFx == null)
            zapFx = ShockBall.CreateZapFx(transform, "BumpZapFX");
        ParticleSystem.EmissionModule emission = zapFx.emission;
        emission.enabled = true;
        if (!zapFx.isPlaying)
            zapFx.Play();
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
