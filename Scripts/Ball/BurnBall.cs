using UnityEngine;

[RequireComponent(typeof(BallInfo))]
public class BurnBall : MonoBehaviour
{
    Database db;
    BallInfo bI;
    Renderer ren;

    [Tooltip("Bounces to go coal -> ember.")]
    public int bouncesToEmber = 4;

    [Tooltip("Bounces to go ember -> fire.")]
    public int bouncesToFire = 4;

    [Tooltip("Bounces to go fire -> supernova.")]
    public int bouncesToNova = 5;

    [Tooltip("Bounces spent in supernova before reset.")]
    public int supernovaBounces = 2;

    public Material coalMaterial;
    public Material emberMaterial;
    public Material fireMaterial;
    public Material novaMaterial;

    int stage;
    int stageBounces;

    static readonly int[] DamageByStage = { 1, 2, 3, 4 };

    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();
        ren = GetComponent<Renderer>();
        ApplyStage(0, true);
    }

    void OnCollisionExit(Collision collision)
    {
        if (db == null || !db.gameStart || bI == null || !bI.ballReady || !bI.projectionOn)
            return;

        string tag = collision.transform.tag;
        if (tag != "Wall" && tag != "Walls" && tag != "Obstacle"
            && tag != "Paddle" && tag != "Player" && tag != "Lifeline" && tag != "Ball")
            return;

        stageBounces++;
        int need = stage switch
        {
            0 => bouncesToEmber,
            1 => bouncesToFire,
            2 => bouncesToNova,
            _ => supernovaBounces
        };

        if (stageBounces < need)
            return;

        stageBounces = 0;
        if (stage >= 3)
            ApplyStage(0, false);
        else
            ApplyStage(stage + 1, false);
    }

    void ApplyStage(int newStage, bool force)
    {
        stage = Mathf.Clamp(newStage, 0, 3);
        if (bI != null && bI.ball != null)
            bI.ball.damage = DamageByStage[stage];

        Material mat = stage switch
        {
            0 => coalMaterial,
            1 => emberMaterial,
            2 => fireMaterial,
            _ => novaMaterial
        };

        if (ren != null && mat != null)
            ren.sharedMaterial = mat;
        else if (ren != null)
        {
            // Fallback tint if materials not wired
            Color c = stage switch
            {
                0 => new Color(0.15f, 0.12f, 0.1f),
                1 => new Color(1f, 0.45f, 0.1f),
                2 => new Color(1f, 0.2f, 0.05f),
                _ => new Color(1f, 1f, 0.85f)
            };
            ren.material.color = c;
        }
    }
}
