using UnityEngine;

/// <summary>
/// Temporary projectile: damages lifelines, bounces off paddles/balls, destroys on
/// wall / structure / lifeline contact after dealing damage.
/// </summary>
public class TempCannonBall : MonoBehaviour
{
    public float speed = 18f;
    public int lifelineDamage = 1;
    public float lifeSeconds = 8f;
    Rigidbody rb;
    float spawnTime;

    public static TempCannonBall Launch(Vector3 origin, Vector3 dir, float zPlane)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Cannon Ball";
        go.tag = "Ball";
        go.transform.position = new Vector3(origin.x, origin.y, zPlane);
        go.transform.localScale = Vector3.one * 0.55f;

        Rigidbody body = go.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        SphereCollider col = go.GetComponent<SphereCollider>();
        if (col != null)
            col.sharedMaterial = CreateBounceMat();

        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Standard"));
            r.material.color = new Color(0.15f, 0.15f, 0.18f, 1f);
            r.material.SetFloat("_Metallic", 0.7f);
            r.material.SetFloat("_Glossiness", 0.55f);
        }

        TempCannonBall ball = go.AddComponent<TempCannonBall>();
        Vector3 flat = new Vector3(dir.x, dir.y, 0f);
        if (flat.sqrMagnitude < 0.0001f)
            flat = Vector3.up;
        flat.Normalize();
        body.linearVelocity = flat * ball.speed;
        return ball;
    }

    static PhysicsMaterial CreateBounceMat()
    {
        PhysicsMaterial mat = new PhysicsMaterial("CannonBounce")
        {
            bounciness = 0.95f,
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };
        return mat;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spawnTime = Time.time;
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;
        // Lock to play plane Z
        float z = Database.instance != null ? Database.instance.FieldPlaySize : transform.position.z;
        Vector3 p = rb.position;
        p.z = z;
        rb.position = p;
        Vector3 v = rb.linearVelocity;
        v.z = 0f;
        if (v.sqrMagnitude > 0.01f)
            rb.linearVelocity = v.normalized * speed;

        if (Time.time - spawnTime > lifeSeconds)
            Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;
        GameObject other = collision.collider.gameObject;
        string tag = other.tag != null ? other.tag : "";

        if (tag == "Lifeline" || (other.GetComponentInParent<PlayerGrab>() != null && other.CompareTag("Lifeline")))
        {
            PlayerGrab pg = other.GetComponentInParent<PlayerGrab>();
            if (pg != null && pg.player != null)
            {
                int lost = pg.player.ApplyGoalDamage(lifelineDamage, gameObject.GetEntityId().GetHashCode());
                if (lost > 0)
                {
                    pg.player.RecordGoalConceded(lost);
                    pg.player.RecordHazardDamageTaken(lost);
                }
            }
            Destroy(gameObject);
            return;
        }

        if (tag == "Walls" || tag == "Wall" || tag == "Obstacle")
        {
            Destroy(gameObject);
            return;
        }

        if (other.GetComponent<BreakableHazard>() != null
            || other.GetComponentInParent<BreakableHazard>() != null
            || other.GetComponent<GuardCannonTower>() != null)
        {
            Destroy(gameObject);
        }
        // Paddles / balls — bounce via physic material, keep living
    }
}
