using UnityEngine;

[RequireComponent(typeof(BallInfo))]
public class NanoBall : MonoBehaviour
{
    Database db;
    BallInfo bI;

    [Tooltip("Starting scale (larger than typical 0.5 balls).")]
    public float startScale = 1.15f;

    [Tooltip("Minimum scale before the finishing hit.")]
    public float minScale = 0.05f;

    [Tooltip("Scale multiplier applied on each wall hit (closer to 1 = slower shrink).")]
    public float shrinkFactor = 0.94f;

    bool pendingDestroy;

    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();
        transform.localScale = Vector3.one * startScale;

        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc != null)
            sc.radius = 0.5f;
    }

    void OnCollisionExit(Collision collision)
    {
        if (db == null || !db.gameStart || bI == null || !bI.ballReady || !bI.projectionOn)
            return;

        string tag = collision.transform.tag;
        if (tag != "Wall" && tag != "Walls" && tag != "Obstacle")
            return;

        float s = transform.localScale.x;

        if (pendingDestroy || s <= minScale + 0.0005f)
        {
            Destroy(gameObject);
            return;
        }

        s *= shrinkFactor;
        if (s <= minScale)
        {
            s = minScale;
            pendingDestroy = true;
        }

        transform.localScale = Vector3.one * s;
    }
}
