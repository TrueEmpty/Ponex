using UnityEngine;

/// <summary>
/// Bump ember projectile: rises briefly then falls toward the owner's wall (not world down),
/// and despawns shortly after floor contact (~0.5s).
/// </summary>
[RequireComponent(typeof(PlayerGrab))]
public class IyolitBumpMovement : MonoBehaviour
{
    PlayerGrab pG;
    float timeBeforeFall;
    float rightAmount;
    float timer;
    bool falling;
    bool killing;
    bool dirsLocked;
    Vector3 fallDir = Vector3.down;
    Vector3 sideDir = Vector3.right;

    [Tooltip("Seconds floating before falling toward the owner's wall.")]
    public float riseTimeMin = 0.05f;
    public float riseTimeMax = 0.28f;
    public float riseSpeed = 3.2f;
    public float fallSpeed = 3.0f;
    public float sideDrift = 0.7f;
    [Tooltip("Hard lifetime so embers never hang around.")]
    public float maxLife = 2.2f;
    [Tooltip("How long after hitting her floor before the ember despawns.")]
    public float floorKillDelay = 0.5f;

    void Start()
    {
        pG = GetComponent<PlayerGrab>();
        timeBeforeFall = Random.Range(riseTimeMin, riseTimeMax);
        rightAmount = Random.Range(-1f, 1f);
        ResolveDirections();
        StyleAsEmber();
        // Wall despawn is handled here (~0.5s) — don't let DestroyObjectOnContact wipe on Wall instantly
        DestroyObjectOnContact doc = GetComponent<DestroyObjectOnContact>();
        if (doc != null && doc.tagHit != null)
        {
            doc.tagHit.RemoveAll(t =>
                t != null && t.Trim().Equals("Wall", System.StringComparison.OrdinalIgnoreCase));
        }
        Destroy(gameObject, maxLife);
    }

    void ResolveDirections()
    {
        if (dirsLocked)
            return;

        Transform owner = null;
        if (pG != null && pG.IsLinked() && pG.player != null && pG.player.spawnedPlayer != null)
            owner = pG.player.spawnedPlayer.transform;

        if (owner != null)
        {
            fallDir = (-owner.up).normalized;
            sideDir = owner.right.normalized;
            transform.rotation = owner.rotation;
            dirsLocked = true;
        }
        else
        {
            fallDir = (-transform.up).normalized;
            sideDir = transform.right.normalized;
        }
    }

    void StyleAsEmber()
    {
        IyolitFlameFx.Ensure(gameObject, IyolitFlameFx.Style.BumpEmber);
    }

    void Update()
    {
        if (killing)
            return;

        ResolveDirections();

        timer += Time.deltaTime;
        if (!falling && timer >= timeBeforeFall)
            falling = true;

        Vector3 move = Vector3.zero;
        if (falling)
            move += fallDir * fallSpeed;
        else
            move += -fallDir * riseSpeed;

        move += sideDir * (rightAmount + Random.Range(-0.35f, 0.35f)) * sideDrift;
        transform.position += move * Time.deltaTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (killing || collision == null)
            return;

        string tag = collision.transform.tag != null ? collision.transform.tag.ToLowerInvariant().Trim() : "";
        if (tag == "wall" || tag == "floor" || tag == "ground")
            BeginFloorKill();
    }

    void OnTriggerEnter(Collider other)
    {
        if (killing || other == null)
            return;
        string tag = other.tag != null ? other.tag.ToLowerInvariant().Trim() : "";
        if (tag == "wall" || tag == "floor" || tag == "ground")
            BeginFloorKill();
    }

    void BeginFloorKill()
    {
        if (killing)
            return;
        killing = true;

        // Stop creating new sparks but keep the current ones visible until destroy
        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null)
                continue;
            systems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        Destroy(gameObject, Mathf.Clamp(floorKillDelay, 0.05f, 0.5f));
    }
}
