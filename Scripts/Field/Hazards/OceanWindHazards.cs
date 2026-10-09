using UnityEngine;

/// <summary>
/// Ocean field director: rotating wind that drifts unlocked hazards, periodically
/// spawns floating logs / icebergs.
/// </summary>
public class OceanWindHazards : MonoBehaviour
{
    public static Vector2 CurrentWind { get; private set; }

    public float windSpeed = 1.6f;
    public float windRotateDegreesPerSecond = 8f;
    public float spawnInterval = 4.5f;

    float windAngle;
    float spawnTimer;
    float half;

    void Start()
    {
        half = EstimateHalf();
        windAngle = Random.Range(0f, 360f);
        spawnTimer = 1.5f;
    }

    void Update()
    {
        windAngle += windRotateDegreesPerSecond * Time.deltaTime;
        float rad = windAngle * Mathf.Deg2Rad;
        CurrentWind = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * windSpeed;

        if (!GameSettings.HazardsEnabled)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
            return;
        spawnTimer = spawnInterval + Random.Range(-1f, 1.5f);
        SpawnRandom();
    }

    void SpawnRandom()
    {
        half = EstimateHalf();
        float z = Database.instance != null ? Database.instance.FieldPlaySize : transform.position.z;
        Vector3 pos = new Vector3(
            Random.Range(-half + 2f, half - 2f),
            Random.Range(-half + 2f, half - 2f),
            z);

        int roll = Random.Range(0, 2);
        if (roll == 0)
        {
            GameObject log = FieldPartFactory.MakePrimitivePart("Drift Log", PrimitiveType.Cylinder,
                pos, new Vector3(1.6f, 0.45f, 0.45f),
                new Color(0.45f, 0.32f, 0.14f), "Obstacle");
            log.transform.SetParent(transform, true);
            log.AddComponent<DriftingLogHazard>();
        }
        else
        {
            GameObject ice = FieldPartFactory.MakePrimitivePart("Iceberg", PrimitiveType.Cube,
                pos, new Vector3(1.8f, 1.4f, 1f),
                new Color(0.75f, 0.9f, 0.95f), "Obstacle");
            ice.transform.SetParent(transform, true);
            BreakableHazard b = ice.AddComponent<BreakableHazard>();
            b.hitsToBreak = 6;
            ice.AddComponent<DriftingLogHazard>().surfaceSeconds = 10f;
        }
    }

    float EstimateHalf()
    {
        if (FieldCameraFit.TryMeasureWallOuterHalf(transform, out float outer, out float center))
            return Mathf.Max(4f, center - 1f);
        if (Database.instance != null && Database.instance.FieldPlaySize > 0f)
            return Database.instance.FieldPlaySize * 0.5f - 1f;
        return 8f;
    }

    void OnDestroy()
    {
        CurrentWind = Vector2.zero;
    }
}
