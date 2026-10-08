using UnityEngine;

/// <summary>Target in the play area: when hit, drops a cannon ball from the top of the field.</summary>
public class SkyCannonTarget : MonoBehaviour
{
    public float cooldown = 1.5f;
    float lastFire = -999f;

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
            return;
        if (Time.time - lastFire < cooldown)
            return;
        GameObject other = collision.collider != null ? collision.collider.gameObject : null;
        if (other == null)
            return;
        bool isBall = other.CompareTag("Ball") || other.GetComponent<BallInfo>() != null;
        if (!isBall)
            return;

        float z = Database.instance != null ? Database.instance.FieldPlaySize : transform.position.z;
        float half = z * 0.5f;
        Vector3 drop = new Vector3(transform.position.x, half - 1.2f, z);
        Vector3 dir = new Vector3(0f, -1f, 0f);
        // Travel in the direction it fell into the field (downward then along X toward center bias)
        Vector3 toCenter = -transform.position;
        toCenter.z = 0f;
        if (toCenter.sqrMagnitude > 0.01f)
            dir = (Vector3.down + toCenter.normalized * 0.35f).normalized;

        TempCannonBall.Launch(drop, dir, z);
        lastFire = Time.time;
    }
}
