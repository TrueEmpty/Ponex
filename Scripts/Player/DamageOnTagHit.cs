using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageOnTagHit : MonoBehaviour
{
    PlayerGrab pg;
    Database db;

    public string tagHit = "Ball";
    public int damageIncrease = 0;

    [Tooltip("If true, still apply goal damage when the ball is already owned by this player (e.g. mit catch).")]
    public bool damageEvenIfOwnBall = false;

    [Tooltip("If > 0, clamp damage dealt by this collider to this value (Yuotay mit = 1).")]
    public int maxDamagePerHit = 0;

    /// <summary>Shared Celarus moon/sun: also apply the same goal hit to these player indexes.</summary>
    [System.NonSerialized] public int[] alsoDamagePlayerIndexes;

    void Start()
    {
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.tag.ToLower().Trim() != tagHit.ToLower().Trim())
            return;

        if (pg == null || !pg.IsLinked() || pg.player == null)
            return;

        PlayerGrab tpG = collision.gameObject.GetComponent<PlayerGrab>();
        BallInfo tbI = collision.gameObject.GetComponent<BallInfo>();
        bool pass = true;

        bool ownBall = false;
        if (tpG != null && tpG.IsLinked())
        {
            if (tpG.playerIndex == pg.playerIndex)
            {
                ownBall = true;
                if (!damageEvenIfOwnBall)
                    pass = false;
            }
            else if (db != null && db.players != null
                && tpG.playerIndex >= 0 && tpG.playerIndex < db.players.Count
                && pg.playerIndex >= 0 && pg.playerIndex < db.players.Count)
            {
                Player tp = db.players[tpG.playerIndex];
                Player yp = db.players[pg.playerIndex];
                if (tp != null && yp != null && tp.team == yp.team && !damageEvenIfOwnBall)
                    pass = false;
            }
        }

        if (!pass)
            return;

        int baseDamage = 0;
        if (tbI != null && tbI.ball != null)
            baseDamage = tbI.ball.damage;

        int dealt = baseDamage + damageIncrease;
        if (maxDamagePerHit > 0)
            dealt = Mathf.Min(dealt, maxDamagePerHit);
        if (dealt <= 0)
            return;

        // Actual HP lost (deduped if multiple DamageOnTagHit colliders hit the same ball this frame)
        int ballId = collision.gameObject.GetEntityId().GetHashCode();
        int lost = pg.player.ApplyGoalDamage(dealt, ballId);
        lost += ApplySharedCelarusDamage(dealt, ballId);

        // Credit ball owner for the HP actually removed — keeps Dealt/Taken in sync
        if (lost > 0 && tpG != null && tpG.IsLinked() && tpG.player != null && !ownBall)
        {
            tpG.player.RecordDamageDealt(lost);

            if (tpG.player.computer)
                ComputerAI.OnPaddleHitBall(tpG.player);
        }

        if (pg.player.computer && lost > 0)
            ComputerAI.OnTookGoalDamage(pg.player, lost);
    }

    int ApplySharedCelarusDamage(int dealt, int ballId)
    {
        if (alsoDamagePlayerIndexes == null || alsoDamagePlayerIndexes.Length == 0)
            return 0;
        if (db == null || db.players == null)
            return 0;

        int extraLost = 0;
        for (int i = 0; i < alsoDamagePlayerIndexes.Length; i++)
        {
            int idx = alsoDamagePlayerIndexes[i];
            if (pg != null && idx == pg.playerIndex)
                continue;
            Player other = null;
            for (int p = 0; p < db.players.Count; p++)
            {
                if (db.players[p] != null && db.players[p].index == idx)
                {
                    other = db.players[p];
                    break;
                }
            }
            if (other == null || other.currentHealth <= 0)
                continue;
            extraLost += other.ApplyGoalDamage(dealt, ballId);
        }
        return extraLost;
    }
}
