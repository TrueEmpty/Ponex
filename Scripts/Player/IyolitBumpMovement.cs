using UnityEngine;

/// <summary>
/// Bump ember projectile: rises briefly then falls toward the owner's wall (not world down),
/// and despawns quickly on contact so particles don't linger on the floor.
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
    public float riseTimeMax = 0.35f;
    public float riseSpeed = 4.2f;
    public float fallSpeed = 7.5f;
    public float sideDrift = 0.9f;
    [Tooltip("Hard lifetime so embers never hang around.")]
    public float maxLife = 1.1f;
    [Tooltip("How quickly to fade/destroy after hitting the owner's floor/wall.")]
    public float floorKillDelay = 0.05f;

    void Start()
    {
        pG = GetComponent<PlayerGrab>();
        timeBeforeFall = Random.Range(riseTimeMin, riseTimeMax);
        rightAmount = Random.Range(-1f, 1f);
        ResolveDirections();
        StyleAsEmber();
        Destroy(gameObject, maxLife);
    }

    void ResolveDirections()
    {
        if (dirsLocked)
            return;

        // Prefer the owner's transform so fall always goes to THEIR wall/floor
        Transform owner = null;
        if (pG != null && pG.IsLinked() && pG.player != null && pG.player.spawnedPlayer != null)
            owner = pG.player.spawnedPlayer.transform;

        if (owner != null)
        {
            // Away from wall = owner.up; toward wall/floor = -owner.up
            fallDir = (-owner.up).normalized;
            sideDir = owner.right.normalized;
            // Keep projectile facing with owner so particle cones stay oriented
            transform.rotation = owner.rotation;
            dirsLocked = true;
        }
        else
        {
            // Flame root is already oriented to her wall — use that until owner links
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
            move += -fallDir * riseSpeed; // rise away from wall

        move += sideDir * (rightAmount + Random.Range(-0.35f, 0.35f)) * sideDrift;
        transform.position += move * Time.deltaTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (killing || collision == null)
            return;

        string tag = collision.transform.tag != null ? collision.transform.tag.ToLowerInvariant().Trim() : "";
        // Floor for Iyolit is the wall behind her — also accept ground/floor tags if present
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

        // Stop leaving a trail of particles on the floor
        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null)
                continue;
            systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        Destroy(gameObject, Mathf.Max(0.01f, floorKillDelay));
    }
}
