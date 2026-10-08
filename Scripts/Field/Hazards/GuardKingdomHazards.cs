using UnityEngine;

/// <summary>
/// Fallback kingdom hazards when a field has no authored Guard Tower part.
/// Prefer authored Part templates (isHazard / tangible / spawnRange) on Kingdom of Nuoryn.
/// </summary>
public class GuardKingdomHazards : MonoBehaviour
{
    void Start()
    {
        if (!GameSettings.HazardsEnabled)
            return;

        float z = Database.instance != null ? Database.instance.FieldPlaySize : transform.position.z;
        float half = 10f;
        if (FieldCameraFit.TryMeasureWallOuterHalf(transform, out _, out float center))
            half = Mathf.Max(6f, center - 2.5f);

        // Fixed tower only
        GameObject tower = FieldPartFactory.MakePrimitivePart("Guard Tower", PrimitiveType.Cube,
            new Vector3(0f, 0f, z), new Vector3(2.2f, 2.2f, 2.4f),
            new Color(0.5f, 0.48f, 0.44f), "Obstacle");
        tower.transform.SetParent(transform, true);
        FieldTextureFactory.ApplyAlbedo(tower.GetComponent<Renderer>(),
            FieldTextureFactory.KingdomStone(), Color.white, 0.35f, 0.4f);
        tower.AddComponent<GuardCannonTower>();
    }
}
