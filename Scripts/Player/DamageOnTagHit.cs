using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageOnTagHit : MonoBehaviour
{
    PlayerGrab pg;
    Database db;

    public string tagHit = "Ball";
    public int damageIncrease = 0;

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

        if (tpG != null && tpG.IsLinked())
        {
            if (tpG.playerIndex == pg.playerIndex)
            {
                pass = false;
            }
            else if (db != null && db.players != null
                && tpG.playerIndex >= 0 && tpG.playerIndex < db.players.Count
                && pg.playerIndex >= 0 && pg.playerIndex < db.players.Count)
            {
                Player tp = db.players[tpG.playerIndex];
                Player yp = db.players[pg.playerIndex];
                if (tp != null && yp != null && tp.team == yp.team)
                    pass = false;
            }
        }

        if (!pass)
            return;

        int baseDamage = 0;
        if (tbI != null && tbI.ball != null)
            baseDamage = tbI.ball.damage;

        int dealt = baseDamage + damageIncrease;
        if (dealt <= 0)
            return;

        // Actual HP lost (deduped if multiple DamageOnTagHit colliders hit the same ball this frame)
        int lost = pg.player.ApplyGoalDamage(dealt, collision.gameObject.GetInstanceID());

        // Credit ball owner for the HP actually removed — keeps Dealt/Taken in sync
        if (lost > 0 && tpG != null && tpG.IsLinked() && tpG.player != null)
        {
            tpG.player.RecordDamageDealt(lost);

            if (tpG.player.computer)
                ComputerAI.OnPaddleHitBall(tpG.player);
        }

        if (pg.player.computer && lost > 0)
            ComputerAI.OnTookGoalDamage(pg.player, lost);
    }
}
