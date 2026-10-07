using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared AOE blast helpers for Mine / Explosion balls.
/// Explosion damage is always capped at 1 HP per character (no Nari sever-from-segment).
/// </summary>
public static class BallBlast
{
    public const int MaxExplosionDamage = 1;

    /// <summary>Ghost Ball smoke poof — set by GhostBall / ExplosionBall on Start.</summary>
    public static GameObject SmokePoofPrefab;

    static readonly Collider[] overlapBuffer = new Collider[64];
    static readonly HashSet<int> hitPlayers = new HashSet<int>();

    public static bool TreatCollisionAsExplosion(GameObject ball)
    {
        if (ball == null)
            return false;
        if (ball.GetComponent<DroppedMine>() != null)
            return true;
        ExplosionBall boom = ball.GetComponent<ExplosionBall>();
        return boom != null && (boom.DetonationActive || boom.FuseIsLit);
    }

    public static int DamagePlayer(Player victim, Player owner, int blastId, int amount = MaxExplosionDamage)
    {
        if (victim == null)
            return 0;

        int dealt = Mathf.Clamp(amount, 0, MaxExplosionDamage);
        if (dealt <= 0)
            return 0;

        int lost = victim.ApplyGoalDamage(dealt, blastId);
        if (lost > 0 && owner != null)
            owner.RecordDamageDealt(lost);
        return lost;
    }

    public static void Detonate(
        Vector3 center,
        float radius,
        GameObject source,
        PlayerGrab sourceGrab,
        bool pushBalls,
        float pushForce,
        GameObject smokePoof = null,
        float smokeScale = 2.2f)
    {
        if (source == null)
            return;

        Database db = Database.instance;
        int ownerIndex = sourceGrab != null && sourceGrab.IsLinked() ? sourceGrab.playerIndex : -1;
        int ownerTeam = -999;
        Player ownerPlayer = null;
        if (db != null && db.players != null && ownerIndex >= 0 && ownerIndex < db.players.Count)
        {
            ownerPlayer = db.players[ownerIndex];
            if (ownerPlayer != null)
                ownerTeam = ownerPlayer.team;
        }

        int blastId = source.GetEntityId().GetHashCode();
        hitPlayers.Clear();

        int count = Physics.OverlapSphereNonAlloc(center, radius, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapBuffer[i];
            if (hit == null)
                continue;

            if (pushBalls && hit.CompareTag("Ball") && hit.gameObject != source)
            {
                Rigidbody otherRb = hit.attachedRigidbody;
                if (otherRb != null && otherRb.gameObject != source)
                {
                    Vector3 away = otherRb.worldCenterOfMass - center;
                    away.z = 0f;
                    if (away.sqrMagnitude < 0.0001f)
                        away = Random.insideUnitCircle;
                    away.Normalize();
                    otherRb.linearVelocity += away * pushForce;
                }
            }

            bool isLifeline = hit.CompareTag("Lifeline")
                || (hit.transform.parent != null && hit.transform.parent.CompareTag("Lifeline"))
                || hit.GetComponent<DamageOnTagHit>() != null
                || hit.GetComponent<NariTail>() != null;
            if (!isLifeline)
            {
                // Cheap parent check only when needed
                if (hit.GetComponentInParent<DamageOnTagHit>() == null
                    && hit.GetComponentInParent<NariTail>() == null)
                    continue;
            }

            PlayerGrab victimGrab = hit.GetComponent<PlayerGrab>();
            if (victimGrab == null)
                victimGrab = hit.GetComponentInParent<PlayerGrab>();
            if (victimGrab == null || !victimGrab.IsLinked() || victimGrab.player == null)
                continue;

            if (ownerIndex >= 0 && victimGrab.playerIndex == ownerIndex)
                continue;
            if (ownerTeam != -999 && victimGrab.player.team == ownerTeam)
                continue;
            if (!hitPlayers.Add(victimGrab.playerIndex))
                continue;

            DamagePlayer(victimGrab.player, ownerPlayer, blastId, MaxExplosionDamage);
        }

        SpawnSmokeFire(center, smokePoof != null ? smokePoof : SmokePoofPrefab, smokeScale);
    }

    public static void SpawnSmokeFire(Vector3 center, GameObject smokePoof, float scale)
    {
        if (smokePoof != null)
        {
            // Layered smoke / fire poofs for a bigger blast look
            float[] scales = { scale, scale * 0.7f, scale * 1.15f };
            Vector3[] offsets =
            {
                Vector3.zero,
                new Vector3(0.15f, 0.1f, 0f),
                new Vector3(-0.12f, -0.08f, 0f)
            };
            for (int i = 0; i < scales.Length; i++)
            {
                GameObject fx = Object.Instantiate(smokePoof, center + offsets[i], Quaternion.identity);
                fx.transform.localScale = Vector3.one * scales[i];
                // Warm tint toward fire on first burst
                if (i == 0)
                    TintParticles(fx, new Color(1f, 0.45f, 0.12f, 0.85f));
                else if (i == 1)
                    TintParticles(fx, new Color(0.35f, 0.35f, 0.35f, 0.7f));
            }
            return;
        }

        // Fallback tiny burst if prefab missing
        GameObject fallback = new GameObject("BallBlastFX");
        fallback.transform.position = center;
        ParticleSystem ps = fallback.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.duration = 0.45f;
        main.startLifetime = 0.7f;
        main.startSpeed = 2.5f;
        main.startSize = 0.35f;
        main.startColor = new Color(1f, 0.4f, 0.1f, 0.8f);
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });
        Object.Destroy(fallback, 1.5f);
    }

    static void TintParticles(GameObject fx, Color color)
    {
        if (fx == null)
            return;
        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        if (ps == null)
            return;
        var main = ps.main;
        main.startColor = color;
    }
}
