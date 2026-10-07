using UnityEngine;

/// <summary>
/// Temporary meteor fragment: damages lifelines using the parent meteor's owner, then destroys on first non-ball hit.
/// </summary>
public class MeteorShard : MonoBehaviour
{
    public int ownerPlayerIndex = -1;
    public float lifetime = 4f;

    void Start()
    {
        BallInfo info = GetComponent<BallInfo>();
        if (info != null)
            info.matchSlot = -1;

        if (ownerPlayerIndex >= 0)
        {
            PlayerGrab grab = GetComponent<PlayerGrab>();
            if (grab != null)
                grab.playerIndex = ownerPlayerIndex;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;

        string tag = collision.transform.tag;
        if (tag == "Ball" || tag == "Ghost")
            return;

        // Lifeline damage is handled by DamageOnTagHit using this shard's BallInfo.damage / PlayerGrab
        Destroy(gameObject);
    }
}
