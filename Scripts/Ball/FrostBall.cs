using UnityEngine;

[RequireComponent(typeof(BallInfo))]
public class FrostBall : MonoBehaviour
{
    Database db;
    BallInfo bI;
    PlayerGrab ownGrab;

    [Tooltip("How long the hit player stays frozen.")]
    public float freezeDuration = 1.5f;

    [Tooltip("Minimum time between freeze applications from this ball.")]
    public float freezeCooldown = 0.35f;

    float nextFreezeTime;

    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();
        ownGrab = GetComponent<PlayerGrab>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (db == null || !db.gameStart || bI == null || !bI.ballReady || !bI.projectionOn)
            return;
        if (Time.time < nextFreezeTime)
            return;

        string tag = collision.transform.tag;
        if (tag != "Paddle" && tag != "Player" && tag != "Lifeline")
            return;

        PlayerGrab hitGrab = collision.gameObject.GetComponent<PlayerGrab>();
        if (hitGrab == null)
            hitGrab = collision.gameObject.GetComponentInParent<PlayerGrab>();
        if (hitGrab == null || !hitGrab.IsLinked() || hitGrab.player == null)
            return;

        if (ownGrab != null && ownGrab.IsLinked() && ownGrab.playerIndex == hitGrab.playerIndex)
            return;

        hitGrab.player.AddConstraint(gameObject, freezeDuration, PlayerConstraint.Move);
        hitGrab.player.AddConstraint(gameObject, freezeDuration, PlayerConstraint.Bump);
        nextFreezeTime = Time.time + freezeCooldown;
    }
}
