using UnityEngine;

public class GaurdShot : MonoBehaviour
{
    public int ownerIndex = -1;
    Vector3 velocity;
    float life = 1.6f;

    public void Launch(Vector3 direction, int index)
    {
        ownerIndex = index;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.up;
        direction.z = 0f;
        velocity = direction.normalized * 14f;
    }

    void Update()
    {
        transform.position += velocity * Time.deltaTime;
        life -= Time.deltaTime;
        if (life <= 0f)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null || !other.CompareTag("Ball"))
            return;
        PlayerGrab grab = other.GetComponent<PlayerGrab>();
        if (grab != null)
            grab.playerIndex = ownerIndex;
        Destroy(gameObject);
    }
}
