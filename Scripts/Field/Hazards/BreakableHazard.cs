using UnityEngine;

/// <summary>Structure that crumbles after enough ball hits, removing its colliders.</summary>
public class BreakableHazard : MonoBehaviour
{
    public int hitsToBreak = 5;
    public string ballTag = "Ball";
    int hits;

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;
        if (!IsBall(collision.collider.gameObject))
            return;
        RegisterHit();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;
        if (!IsBall(other.gameObject))
            return;
        RegisterHit();
    }

    bool IsBall(GameObject go)
    {
        if (go == null)
            return false;
        if (!string.IsNullOrEmpty(ballTag) && go.CompareTag(ballTag))
            return true;
        return go.GetComponent<BallInfo>() != null || go.GetComponentInParent<BallInfo>() != null;
    }

    void RegisterHit()
    {
        hits++;
        // Visual feedback — brief shrink pulse
        transform.localScale *= 0.97f;
        if (hits < hitsToBreak)
            return;

        Collider[] cols = GetComponentsInChildren<Collider>();
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null)
                cols[i].enabled = false;
        }

        Renderer[] rends = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < rends.Length; i++)
        {
            if (rends[i] != null)
                rends[i].enabled = false;
        }

        Destroy(gameObject, 0.05f);
    }
}
