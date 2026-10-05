using UnityEngine;

/// <summary>Destroys this GameObject after <see cref="duration"/> seconds.</summary>
public class DestroyAfterTime : MonoBehaviour
{
    public float duration = 1f;
    float lifetime;

    void Start()
    {
        lifetime = Time.time + Mathf.Max(0.01f, duration);
    }

    void Update()
    {
        if (lifetime < Time.time)
            Destroy(gameObject);
    }
}
