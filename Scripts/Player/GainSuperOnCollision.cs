using UnityEngine;

public class GainSuperOnCollision : MonoBehaviour
{
    PlayerGrab pG;
    public float amount = 1;

    void Start()
    {
        pG = GetComponent<PlayerGrab>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.tag.ToLower().Trim() != "ball")
            return;

        if (pG == null || pG.player == null || pG.player.super == null)
            return;

        // Only fill the meter — never activate supers.
        // (readyPercent is Drive-on for Garmen; setting it here auto-cast Drive.)
        pG.player.super.Gain(amount);
    }
}
