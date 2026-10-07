using UnityEngine;

[RequireComponent(typeof(BallInfo))]
public class ExplosionBall : MonoBehaviour
{
    Database db;
    BallInfo bI;
    PlayerGrab grab;
    Renderer rootRen;

    [Range(0f, 1f)]
    public float lightChanceOnWall = 0.1f;
    public float explodeRadius = 3.75f;
    public float pushForce = 6.5f;
    public float smokeScale = 2.4f;
    public Material bodyMaterial;
    public Material fuseMaterial;
    public Material bandMaterial;
    public GameObject smokePoof;

    int detonationFrame = -1;
    public bool DetonationActive => Time.frameCount == detonationFrame;
    public bool FuseIsLit => fuseLit;

    bool fuseLit;
    bool visualsReady;
    Transform fuseTip;
    ParticleSystem fuseFx;
    Material tipMatInstance;

    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();
        grab = GetComponent<PlayerGrab>();
        rootRen = GetComponent<Renderer>();

        // Projection ghosts clone this ball at high speed — never build visuals / logic on them
        if (CompareTag("Ghost") || (bI != null && bI.anchor != null && bI.anchor != gameObject))
        {
            enabled = false;
            return;
        }

        if (smokePoof != null)
            BallBlast.SmokePoofPrefab = smokePoof;

        if (!visualsReady)
        {
            BuildDynamiteBundle();
            visualsReady = true;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (db == null || !db.gameStart || bI == null || !bI.ballReady || !bI.projectionOn)
            return;
        if (collision == null)
            return;

        string tag = collision.transform.tag;

        if (fuseLit)
        {
            if (tag == "Ball")
                return;

            Detonate();
            return;
        }

        if (tag == "Wall" || tag == "Walls" || tag == "Obstacle")
        {
            if (Random.value <= lightChanceOnWall)
                LightFuse();
        }
    }

    void LightFuse()
    {
        if (fuseLit)
            return;
        fuseLit = true;
        SetFuseParticles(true);
    }

    void ClearFuse()
    {
        fuseLit = false;
        SetFuseParticles(false);
    }

    void Detonate()
    {
        detonationFrame = Time.frameCount;
        BallBlast.Detonate(
            transform.position,
            explodeRadius,
            gameObject,
            grab,
            true,
            pushForce,
            smokePoof,
            smokeScale);
        ClearFuse();
    }

    void BuildDynamiteBundle()
    {
        // Root is an invisible collider shell — long on X
        if (rootRen != null)
            rootRen.enabled = false;

        SphereCollider sphere = GetComponent<SphereCollider>();
        PhysicsMaterial phys = null;
        if (sphere != null)
        {
            phys = sphere.sharedMaterial;
            Destroy(sphere);
        }

        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(1f, 0.72f, 0.72f);
        box.center = Vector3.zero;
        if (phys != null)
            box.sharedMaterial = phys;

        transform.localScale = new Vector3(0.92f, 0.42f, 0.42f);

        // Bundle of sticks (cylinders along X)
        Vector2[] offsets =
        {
            new Vector2(-0.18f, -0.16f),
            new Vector2(0.18f, -0.16f),
            new Vector2(-0.12f, 0.14f),
            new Vector2(0.16f, 0.12f),
            new Vector2(0.02f, -0.02f)
        };

        for (int i = 0; i < offsets.Length; i++)
            CreateStick("Stick" + i, offsets[i], bodyMaterial);

        // Binding bands around the middle of the bundle (thin cubes on YZ plane)
        CreateBand("BandMid", Vector3.zero, new Vector3(0.12f, 0.95f, 0.95f), bandMaterial != null ? bandMaterial : bodyMaterial);
        CreateBand("BandLeft", new Vector3(-0.28f, 0f, 0f), new Vector3(0.1f, 0.9f, 0.9f), bandMaterial != null ? bandMaterial : bodyMaterial);
        CreateBand("BandRight", new Vector3(0.28f, 0f, 0f), new Vector3(0.1f, 0.9f, 0.9f), bandMaterial != null ? bandMaterial : bodyMaterial);

        // Fuse sticking out the +X end (no collider)
        GameObject fuse = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fuse.name = "DynamiteFuse";
        fuse.transform.SetParent(transform, false);
        fuse.transform.localPosition = new Vector3(0.62f, 0.2f, 0.05f);
        fuse.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        fuse.transform.localScale = new Vector3(0.1f, 0.2f, 0.1f);
        Destroy(fuse.GetComponent<Collider>());
        Renderer fuseRen = fuse.GetComponent<Renderer>();
        if (fuseRen != null)
            fuseRen.sharedMaterial = fuseMaterial != null ? fuseMaterial : bodyMaterial;

        GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tip.name = "FuseTip";
        tip.transform.SetParent(fuse.transform, false);
        tip.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        tip.transform.localScale = Vector3.one * 0.55f;
        Destroy(tip.GetComponent<Collider>());
        Renderer tipRen = tip.GetComponent<Renderer>();
        if (tipRen != null)
        {
            tipRen.sharedMaterial = fuseMaterial != null ? fuseMaterial : bodyMaterial;
            tipMatInstance = tipRen.material;
        }
        fuseTip = tip.transform;

        AttachFuseParticles(fuseTip);
        SetFuseParticles(false);
    }

    void CreateStick(string name, Vector2 yz, Material mat)
    {
        GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stick.name = name;
        stick.transform.SetParent(transform, false);
        // Default cylinder is Y-long → rotate onto X
        stick.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        stick.transform.localPosition = new Vector3(0f, yz.x, yz.y);
        stick.transform.localScale = new Vector3(0.28f, 0.48f, 0.28f);
        Destroy(stick.GetComponent<Collider>());
        Renderer ren = stick.GetComponent<Renderer>();
        if (ren != null && mat != null)
            ren.sharedMaterial = mat;
    }

    void CreateBand(string name, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
        band.name = name;
        band.transform.SetParent(transform, false);
        band.transform.localPosition = localPos;
        band.transform.localScale = localScale;
        Destroy(band.GetComponent<Collider>());
        Renderer br = band.GetComponent<Renderer>();
        if (br != null && mat != null)
            br.sharedMaterial = mat;
    }

    void AttachFuseParticles(Transform tip)
    {
        GameObject fx = new GameObject("FuseSparks");
        fx.transform.SetParent(tip, false);
        fuseFx = fx.AddComponent<ParticleSystem>();

        var main = fuseFx.main;
        main.loop = true;
        main.startLifetime = 0.3f;
        main.startSpeed = 0.55f;
        main.startSize = 0.06f;
        main.startColor = new Color(1f, 0.5f, 0.1f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;
        main.gravityModifier = 0.3f;

        var emission = fuseFx.emission;
        emission.rateOverTime = 18f;
        emission.enabled = false;

        var shape = fuseFx.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.02f;

        var renderer = fx.GetComponent<ParticleSystemRenderer>();
        Shader sh = Shader.Find("Particles/Standard Unlit");
        if (sh != null)
        {
            Material mat = new Material(sh);
            mat.SetColor("_Color", new Color(1f, 0.55f, 0.15f, 1f));
            renderer.sharedMaterial = mat;
        }
    }

    void SetFuseParticles(bool on)
    {
        if (fuseFx == null)
            return;
        var emission = fuseFx.emission;
        emission.enabled = on;
        if (on)
        {
            if (!fuseFx.isPlaying)
                fuseFx.Play();
            if (tipMatInstance != null)
                tipMatInstance.color = new Color(1f, 0.4f, 0.08f, 1f);
        }
        else
        {
            fuseFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (tipMatInstance != null && fuseMaterial != null)
                tipMatInstance.color = fuseMaterial.color;
        }
    }
}
