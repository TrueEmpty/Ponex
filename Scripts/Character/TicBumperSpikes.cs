using UnityEngine;

/// <summary>
/// Four piston-colored capsules that rest hidden inside the bumper body,
/// then snap out to the four sides and retract (wing bump + frozen basic bumpers).
/// </summary>
public class TicBumperSpikes : MonoBehaviour
{
    [Tooltip("How far from center each spike travels (world/local units).")]
    public float outDistance = 0.32f;
    public float outTime = 0.05f;
    public float inTime = 0.1f;
    public float ballKnockSpeed = 22f;

    // Match Tic bumper body silhouette (flat capsule), slightly smaller
    public Vector3 spikeScale = new Vector3(0.28f, 0.09f, 0.28f);

    Transform[] spikes = new Transform[4];
    Vector3[] restPos = new Vector3[4];
    Vector3[] outPos = new Vector3[4];
    float animT = -1f;
    bool retracting;
    bool built;
    Material pistonMat;

    public bool IsAnimating => animT >= 0f;

    void Awake()
    {
        EnsureBuilt();
    }

    Material ResolvePistonMat()
    {
        if (pistonMat != null)
            return pistonMat;

#if UNITY_EDITOR
        pistonMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Materials/Colors/Characters/Tic Piston.mat");
#endif
        if (pistonMat == null)
        {
            // Runtime: steal from any child already using piston, or invent a yellow
            Renderer[] rends = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null || rends[i].sharedMaterial == null)
                    continue;
                string n = rends[i].sharedMaterial.name;
                if (n.IndexOf("Piston", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("piston", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    pistonMat = rends[i].sharedMaterial;
                    break;
                }
            }
        }
        if (pistonMat == null)
        {
            pistonMat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard"));
            if (pistonMat != null)
            {
                if (pistonMat.HasProperty("_BaseColor"))
                    pistonMat.SetColor("_BaseColor", new Color(0.95f, 0.82f, 0.15f, 1f));
                if (pistonMat.HasProperty("_Color"))
                    pistonMat.SetColor("_Color", new Color(0.95f, 0.82f, 0.15f, 1f));
            }
        }
        return pistonMat;
    }

    public void EnsureBuilt()
    {
        if (built && spikes[0] != null)
            return;
        built = true;

        Material mat = ResolvePistonMat();

        Transform holder = transform.Find("BumpSpikes");
        if (holder == null)
        {
            GameObject h = new GameObject("BumpSpikes");
            h.transform.SetParent(transform, false);
            holder = h.transform;
        }

        // Local play-plane directions
        Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.up, Vector3.down };
        string[] names = { "SpikeRight", "SpikeLeft", "SpikeUp", "SpikeDown" };

        for (int i = 0; i < 4; i++)
        {
            Transform existing = holder.Find(names[i]);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                // Capsule like the Tic bumper body (not a tall cylinder)
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = names[i];
                go.transform.SetParent(holder, false);
                Collider col = go.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);
                Rigidbody rb = go.GetComponent<Rigidbody>();
                if (rb != null)
                    Destroy(rb);
            }

            if (mat != null)
            {
                Renderer r = go.GetComponent<Renderer>();
                if (r != null)
                    r.sharedMaterial = mat;
            }

            Vector3 dir = dirs[i];
            // Flat capsule in XY like the normal Tic "Bump" mesh
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = spikeScale;
            // Fully inside the body when at rest
            restPos[i] = Vector3.zero;
            outPos[i] = dir * outDistance;
            go.transform.localPosition = restPos[i];
            go.SetActive(false);
            spikes[i] = go.transform;
        }
    }

    void Update()
    {
        if (animT < 0f)
            return;

        if (!retracting)
        {
            animT += Time.deltaTime / Mathf.Max(0.01f, outTime);
            float u = Mathf.Clamp01(animT);
            float e = 1f - (1f - u) * (1f - u);
            for (int i = 0; i < 4; i++)
            {
                if (spikes[i] == null) continue;
                spikes[i].localPosition = Vector3.Lerp(restPos[i], outPos[i], e);
            }
            if (u >= 1f)
            {
                retracting = true;
                animT = 0f;
            }
        }
        else
        {
            animT += Time.deltaTime / Mathf.Max(0.01f, inTime);
            float u = Mathf.Clamp01(animT);
            float e = u * u;
            for (int i = 0; i < 4; i++)
            {
                if (spikes[i] == null) continue;
                spikes[i].localPosition = Vector3.Lerp(outPos[i], restPos[i], e);
            }
            if (u >= 1f)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (spikes[i] == null) continue;
                    spikes[i].gameObject.SetActive(false);
                    spikes[i].localPosition = restPos[i];
                }
                animT = -1f;
                retracting = false;
            }
        }
    }

    public void Pop()
    {
        EnsureBuilt();
        animT = 0f;
        retracting = false;
        for (int i = 0; i < 4; i++)
        {
            if (spikes[i] == null) continue;
            spikes[i].gameObject.SetActive(true);
            spikes[i].localScale = spikeScale;
            spikes[i].localPosition = restPos[i];
        }
    }

    public void PopAndKnockBall(Rigidbody ballRb, Vector3 hitPoint)
    {
        float prevOut = outTime;
        float prevIn = inTime;
        outTime = 0.03f;
        inTime = 0.06f;
        Pop();
        outTime = prevOut;
        inTime = prevIn;

        if (ballRb == null)
            return;

        Vector3 away = ballRb.position - transform.position;
        away.z = 0f;
        if (away.sqrMagnitude < 0.0001f)
            away = ballRb.position - hitPoint;
        away.z = 0f;
        if (away.sqrMagnitude < 0.0001f)
            away = Vector3.up;
        away.Normalize();

        ballRb.linearVelocity = away * ballKnockSpeed;
        ballRb.angularVelocity = Vector3.zero;
    }
}
