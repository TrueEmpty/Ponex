using UnityEngine;

public abstract class TimedDestroyBehaviour : MonoBehaviour
{
    protected abstract float DestroyDuration { get; }

    float lifetime;

    protected virtual void Start()
    {
        lifetime = Time.time + DestroyDuration;
    }

    protected virtual void Update()
    {
        if (lifetime < Time.time)
            Destroy(gameObject);
    }
}

/// <summary>Destroys this GameObject after <see cref="duration"/> seconds.</summary>
public class DestroyAfterTime : TimedDestroyBehaviour
{
    public float duration = 1f;

    protected override float DestroyDuration => Mathf.Max(0.01f, duration);
}
