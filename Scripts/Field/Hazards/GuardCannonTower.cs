using UnityEngine;

/// <summary>Central tower: ball hit launches a temp cannon ball along the hit direction.</summary>
public class GuardCannonTower : MonoBehaviour
{
    public float cooldown = 0.85f;
    float lastFire = -999f;

    void OnCollisionEnter(Collision collision)
    {
        if (!Database.MatchPlayActive)
            return;
        if (collision == null || collision.contactCount == 0)
            return;
        if (Time.time - lastFire < cooldown)
            return;

        GameObject other = collision.collider != null ? collision.collider.gameObject : null;
        if (other == null)
            return;
        bool isBall = other.CompareTag("Ball") || other.GetComponent<BallInfo>() != null
                      || other.GetComponentInParent<BallInfo>() != null;
        if (!isBall)
            return;

        ContactPoint cp = collision.GetContact(0);
        Vector3 dir = -cp.normal;
        dir.z = 0f;
        if (dir.sqrMagnitude < 0.01f && collision.relativeVelocity.sqrMagnitude > 0.01f)
            dir = -collision.relativeVelocity;

        float z = Database.instance != null ? Database.instance.FieldPlaySize : transform.position.z;
        TempCannonBall.Launch(transform.position, dir, z);
        lastFire = Time.time;
    }
}
