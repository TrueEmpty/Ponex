using UnityEngine;

/// <summary>
/// Places the playfield on Z so the top/bottom boundary walls sit flush with the
/// camera's vertical frustum. Landscape aspect then leaves left/right overhang for UI.
/// Test Zone (tiny overshoot already) is left unchanged; larger fields that show
/// outside the walls are pulled forward to match.
/// </summary>
public static class FieldCameraFit
{
    const float OvershootTolerance = 0.35f;

    /// <summary>
    /// Reframe <paramref name="fieldRoot"/> for <paramref name="cam"/>.
    /// Returns the Z depth used (also the gameplay fieldSize plane).
    /// </summary>
    public static float Apply(Transform fieldRoot, Camera cam, float fallbackSize)
    {
        if (fieldRoot == null)
            return fallbackSize;

        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            cam = Object.FindAnyObjectByType<Camera>();
        if (cam == null || cam.orthographic)
        {
            // Orthographic / missing cam: keep legacy size+10 depth
            fieldRoot.position = new Vector3(0f, 0f, fallbackSize);
            return fallbackSize;
        }

        if (!TryMeasureWallOuterHalf(fieldRoot, out float outerHalfY, out float centerHalf))
        {
            fieldRoot.position = new Vector3(0f, 0f, fallbackSize);
            return fallbackSize;
        }

        float halfFovRad = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float tan = Mathf.Tan(halfFovRad);
        if (tan < 0.0001f)
        {
            fieldRoot.position = new Vector3(0f, 0f, fallbackSize);
            return fallbackSize;
        }

        float currentZ = fallbackSize > 0.01f ? fallbackSize : fieldRoot.position.z;
        if (currentZ < 0.01f)
            currentZ = outerHalfY / tan;

        float visibleHalfAtCurrent = currentZ * tan;
        float overshoot = visibleHalfAtCurrent - outerHalfY;

        float z;
        if (overshoot <= OvershootTolerance)
        {
            // Test Zone and similarly framed fields — keep authored depth
            z = currentZ;
        }
        else
        {
            // Pull field forward so top/bottom walls meet the frustum
            z = outerHalfY / tan;
        }

        // Prefer a depth that also keeps gameplay half-extent coherent with wall centers
        float playSize = centerHalf > 0.1f ? centerHalf * 2f : outerHalfY * 2f;
        // Depth is the play plane; keep it as the fitted Z (not playSize) so framing wins
        z = Mathf.Max(1f, z);

        fieldRoot.position = new Vector3(0f, 0f, z);

        // Expose play size via return: callers that need wall-center span can use playSize,
        // but Database.fieldSize historically equals the Z plane — keep that contract.
        // Spawns raycast walls; Z plane must match fieldRoot.z.
        return z;
    }

    public static bool TryMeasureWallOuterHalf(Transform fieldRoot, out float outerHalfY, out float centerHalf)
    {
        outerHalfY = 0f;
        centerHalf = 0f;
        if (fieldRoot == null)
            return false;

        bool any = false;
        float maxOuterY = 0f;
        float maxCenter = 0f;

        Renderer[] renderers = fieldRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled)
                continue;
            if (!IsBoundary(r.gameObject))
                continue;

            Bounds b = r.bounds;
            maxOuterY = Mathf.Max(maxOuterY, Mathf.Abs(b.min.y), Mathf.Abs(b.max.y));
            Vector3 lp = fieldRoot.InverseTransformPoint(r.bounds.center);
            maxCenter = Mathf.Max(maxCenter, Mathf.Abs(lp.x), Mathf.Abs(lp.y));
            any = true;
        }

        if (!any)
        {
            Collider[] cols = fieldRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                Collider c = cols[i];
                if (c == null || c.isTrigger)
                    continue;
                if (!IsBoundary(c.gameObject))
                    continue;

                Bounds b = c.bounds;
                maxOuterY = Mathf.Max(maxOuterY, Mathf.Abs(b.min.y), Mathf.Abs(b.max.y));
                Vector3 lp = fieldRoot.InverseTransformPoint(b.center);
                maxCenter = Mathf.Max(maxCenter, Mathf.Abs(lp.x), Mathf.Abs(lp.y));
                any = true;
            }
        }

        if (!any || maxOuterY < 0.1f)
            return false;

        outerHalfY = maxOuterY;
        centerHalf = maxCenter > 0.1f ? maxCenter : maxOuterY;
        return true;
    }

    static bool IsBoundary(GameObject go)
    {
        if (go == null)
            return false;

        PartInfo pi = go.GetComponent<PartInfo>();
        if (pi != null && pi.part != null)
        {
            if (pi.part.canBeBorder)
                return true;
            string t = pi.part.type != null ? pi.part.type.ToLowerInvariant() : "";
            if (t == "wall" || t == "border" || t == "boundry" || t == "boundary")
                return true;
        }

        string tag = go.tag;
        if (string.IsNullOrEmpty(tag))
            return false;
        string tl = tag.ToLowerInvariant();
        return tl == "walls" || tl == "wall" || tl == "obstacle";
    }
}
