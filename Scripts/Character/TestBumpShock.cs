using UnityEngine;

public class TestBumpShock : MonoBehaviour
{
    public float shockSeconds = 2f;

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
            return;
        TryShock(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        TryShock(other);
    }

    void TryShock(Collider other)
    {
        if (other == null)
            return;
        bool ball = other.CompareTag("Ball")
            || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Ball"));
        if (!ball)
            return;

        GameObject ballObject = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        ShockedBall shock = ballObject.GetComponent<ShockedBall>();
        if (shock == null)
            shock = ballObject.AddComponent<ShockedBall>();
        shock.Refresh(shockSeconds);
    }
}
