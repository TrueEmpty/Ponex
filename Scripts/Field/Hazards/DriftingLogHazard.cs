using UnityEngine;

/// <summary>Log that rises into the play plane, drifts with wind, then sinks and despawns.</summary>
public class DriftingLogHazard : MonoBehaviour
{
    public float riseSeconds = 1.2f;
    public float surfaceSeconds = 6f;
    public float sinkSeconds = 1.2f;
    public bool affectedByWind = true;

    Vector3 surfacePos;
    Vector3 deepPos;
    float age;
    enum Phase { Rise, Surface, Sink }
    Phase phase = Phase.Rise;

    void Start()
    {
        surfacePos = transform.position;
        deepPos = surfacePos + Vector3.forward * 4f;
        transform.position = deepPos;
        age = 0f;
    }

    void Update()
    {
        if (!Database.MatchPlayActive)
            return;

        age += Time.deltaTime;
        float z = Database.instance != null ? Database.instance.FieldPlaySize : surfacePos.z;
        surfacePos.z = z;
        deepPos = surfacePos + Vector3.forward * 4f;

        if (affectedByWind)
        {
            Vector2 wind = OceanWindHazards.CurrentWind;
            surfacePos.x += wind.x * Time.deltaTime;
            surfacePos.y += wind.y * Time.deltaTime;
            deepPos.x = surfacePos.x;
            deepPos.y = surfacePos.y;
        }

        switch (phase)
        {
            case Phase.Rise:
                transform.position = Vector3.Lerp(deepPos, surfacePos, Mathf.Clamp01(age / riseSeconds));
                if (age >= riseSeconds) { age = 0f; phase = Phase.Surface; }
                break;
            case Phase.Surface:
                transform.position = surfacePos;
                if (age >= surfaceSeconds) { age = 0f; phase = Phase.Sink; }
                break;
            case Phase.Sink:
                transform.position = Vector3.Lerp(surfacePos, deepPos, Mathf.Clamp01(age / sinkSeconds));
                if (age >= sinkSeconds)
                    Destroy(gameObject);
                break;
        }
    }
}
