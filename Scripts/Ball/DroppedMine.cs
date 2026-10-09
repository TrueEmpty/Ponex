using UnityEngine;

/// <summary>
/// Armed mine: ignores its dropper until clear, then after first real hit starts a 5s fuse
/// with accelerating pulse glow, exploding on lifeline contact or timeout.
/// </summary>
public class DroppedMine : MonoBehaviour
{
    Database db;
    BallInfo bI;
    PlayerGrab grab;
    Rigidbody rb;
    Renderer ren;
    Collider selfCol;
    GameObject dropper;
    Collider dropperCol;

    public float fuseDuration = 5f;
    public float explodeRadius = 1.35f;
    public float clearDistance = 0.9f;
    public Color pulseColor = new Color(1f, 0.35f, 1f, 1f);

    bool ignoreCleared;
    bool fuseLit;
    bool exploded;
    float fuseStart = -1f;
    Color baseColor = Color.white;
    Material pulseMat;

    public void Setup(GameObject parentBall, Material bodyMat, Material fuseMat)
    {
        dropper = parentBall;
        if (dropper != null)
            dropperCol = dropper.GetComponent<Collider>();

        MineBombVisual.Apply(gameObject, bodyMat, fuseMat, true, 1.1f);
    }

    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();
        grab = GetComponent<PlayerGrab>();
        rb = GetComponent<Rigidbody>();
        ren = GetComponent<Renderer>();
        selfCol = GetComponent<Collider>();

        if (ren != null)
        {
            pulseMat = ren.material;
            baseColor = pulseMat.HasProperty("_Color") ? pulseMat.color : Color.white;
        }

        if (bI != null)
        {
            bI.matchSlot = -1;
            // Contact + blast both stay at 1; Nari sever is redirected via BallBlast
            if (bI.ball != null)
                bI.ball.damage = 1;
        }
    }

    float nextPulseVisual = -1f;

    void Update()
    {
        if (exploded)
            return;

        if (!ignoreCleared)
            TryClearDropperIgnore();

        if (!fuseLit)
            return;

        if (Time.time - fuseStart >= fuseDuration)
        {
            Explode();
            return;
        }

        // Throttle glow updates — was rewriting material every frame
        if (Time.time < nextPulseVisual)
            return;
        nextPulseVisual = Time.time + 0.05f;

        float t = (Time.time - fuseStart) / Mathf.Max(0.01f, fuseDuration);
        float pulseHz = Mathf.Lerp(2f, 14f, Mathf.Clamp01(t));
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseHz * Mathf.PI * 2f);
        if (pulseMat != null)
        {
            pulseMat.color = Color.Lerp(baseColor, pulseColor, 0.35f + pulse * 0.65f);
            if (pulseMat.HasProperty("_EmissionColor"))
            {
                pulseMat.SetColor("_EmissionColor", pulseColor * (0.4f + pulse * 2.2f));
                pulseMat.EnableKeyword("_EMISSION");
            }
        }
    }

    void TryClearDropperIgnore()
    {
        if (dropper == null || dropperCol == null || selfCol == null)
        {
            ignoreCleared = true;
            return;
        }

        // Distance-only clear — ComputePenetration every frame was expensive
        float distSq = (transform.position - dropper.transform.position).sqrMagnitude;
        float need = clearDistance * clearDistance;
        if (distSq >= need)
        {
            Physics.IgnoreCollision(selfCol, dropperCol, false);
            ignoreCleared = true;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (exploded || collision == null)
            return;

        if (collision.gameObject == dropper)
            return;

        string tag = collision.transform.tag;
        // Ownership is independent of fuse state: every new player-owned hit
        // replaces the previous owner, including hits after the mine is armed.
        if (tag == "Paddle" || tag == "Player" || tag == "Ball")
            ClaimOwnerFrom(collision.collider);

        if (tag == "Lifeline")
        {
            Explode();
            return;
        }

        if (fuseLit)
            return;

        if (tag != "Paddle" && tag != "Player" && tag != "Wall" && tag != "Walls"
            && tag != "Obstacle" && tag != "Ball")
            return;

        ArmFuse();
    }

    void ClaimOwnerFrom(Collider hitter)
    {
        if (grab == null || hitter == null)
            return;

        PlayerGrab hitterGrab = BallBlast.ResolveLinkedPlayerGrab(hitter);
        if (hitterGrab != null)
            grab.playerIndex = hitterGrab.playerIndex;
    }

    void ArmFuse()
    {
        if (fuseLit || exploded)
            return;
        fuseLit = true;
        fuseStart = Time.time;
    }

    void Explode()
    {
        if (exploded)
            return;
        exploded = true;

        BallBlast.Detonate(
            transform.position,
            explodeRadius,
            gameObject,
            grab,
            false,
            0f,
            BallBlast.SmokePoofPrefab,
            explodeRadius * 0.65f);

        Destroy(gameObject);
    }
}
