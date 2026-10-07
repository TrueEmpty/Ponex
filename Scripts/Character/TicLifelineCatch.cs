using UnityEngine;

/// <summary>
/// On Tic's lifeline: when a ball hits, teleport it into this player's plunger bay
/// (stacked above anything already loaded) and zero its velocity until launched.
/// Oversized balls (e.g. Size Ball) spawn at the bay mouth and get ejected outward.
/// </summary>
[RequireComponent(typeof(PlayerGrab))]
public class TicLifelineCatch : MonoBehaviour
{
    PlayerGrab pg;
    Database db;

    void Awake()
    {
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;
    }

    void OnCollisionEnter(Collision collision)
    {
        TryCatch(collision != null ? collision.collider : null);
    }

    void OnTriggerEnter(Collider other)
    {
        TryCatch(other);
    }

    void TryCatch(Collider other)
    {
        if (other == null || !other.CompareTag("Ball"))
            return;
        if (pg == null)
            pg = GetComponent<PlayerGrab>();
        if (pg == null || !pg.IsLinked() || pg.player == null)
            return;
        if (db == null)
            db = Database.instance;
        if (db == null || !db.gameStart)
            return;

        Tic tic = FindOwnerTic(pg.player);
        if (tic == null || tic.launchArea == null)
            return;

        Rigidbody ballRb = other.attachedRigidbody;
        if (ballRb == null)
            ballRb = other.GetComponentInParent<Rigidbody>();
        if (ballRb == null)
            return;

        float radius = EstimateBallRadius(ballRb);

        // Claim ownership for the Tic player
        PlayerGrab ballGrab = ballRb.GetComponent<PlayerGrab>();
        if (ballGrab != null)
            ballGrab.playerIndex = pg.playerIndex;

        // Too big for the plunger shaft — put at bay mouth and fling out like a plunger hit
        if (!tic.launchArea.CanFitBall(radius))
        {
            Vector3 mouth = tic.launchArea.GetLaunchMouthPosition(radius);
            ballRb.position = mouth;
            ballRb.transform.position = mouth;
            ballRb.angularVelocity = Vector3.zero;
            tic.launchArea.ReleaseBall(ballRb);
            ballRb.linearVelocity = tic.launchArea.GetEjectVelocity();
            return;
        }

        Vector3 slot = tic.launchArea.GetNextStackPosition(radius);
        ballRb.position = slot;
        ballRb.transform.position = slot;
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
        tic.launchArea.HoldBall(ballRb);
    }

    static float EstimateBallRadius(Rigidbody ballRb)
    {
        float radius = 0.4f;
        Collider ballCol = ballRb.GetComponentInChildren<Collider>();
        if (ballCol != null)
        {
            Vector3 e = ballCol.bounds.extents;
            radius = Mathf.Max(0.2f, Mathf.Max(e.x, Mathf.Max(e.y, e.z)));
        }
        else
        {
            Vector3 s = ballRb.transform.lossyScale;
            radius = Mathf.Max(0.2f, Mathf.Max(s.x, Mathf.Max(s.y, s.z)) * 0.5f);
        }
        return radius;
    }

    static Tic FindOwnerTic(Player p)
    {
        if (p == null || p.spawnedPlayer == null)
            return null;
        Tic tic = p.spawnedPlayer.GetComponent<Tic>();
        if (tic != null)
            return tic;
        return p.spawnedPlayer.GetComponentInChildren<Tic>();
    }
}
