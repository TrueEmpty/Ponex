public class DestroyAfterLifetime : TimedDestroyBehaviour
{
    public float duration = .5f;

    protected override float DestroyDuration => duration;
}
