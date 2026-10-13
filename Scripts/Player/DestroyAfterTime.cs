using UnityEngine;

public abstract class TimedDestroyBehaviour : MonoBehaviour
{
    protected abstract float DestroyDuration { get; }

    [Tooltip("When off, the timer starts only after ArmCountdown().")]
    public bool armOnStart = true;

    float lifetime;
    bool armed;

    protected virtual void Start()
    {
        if (armOnStart)
            ArmCountdown();
        else
            lifetime = float.PositiveInfinity;
    }

    /// <summary>Start (or restart) the destroy timer from now.</summary>
    public void ArmCountdown()
    {
        armed = true;
        lifetime = Time.time + DestroyDuration;
    }

    protected virtual void Update()
    {
        if (armed && lifetime < Time.time)
            Destroy(gameObject);
    }
}

/// <summary>Destroys this GameObject after <see cref="duration"/> seconds.</summary>
public class DestroyAfterTime : TimedDestroyBehaviour
{
    public float duration = 1f;

    protected override float DestroyDuration => Mathf.Max(0.01f, duration);
}
