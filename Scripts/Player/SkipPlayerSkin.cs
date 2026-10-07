using UnityEngine;

/// <summary>
/// Mark a renderer / hierarchy so <see cref="PlayerSkin"/> leaves authored colors alone.
/// Put on Tic's lifeline (or any part) when skin tint must not overwrite art.
/// </summary>
public class SkipPlayerSkin : MonoBehaviour
{
    [Tooltip("When true, this object and all children skip skinning.")]
    public bool includeChildren = true;

    public static bool ShouldSkipRenderer(Renderer ren)
    {
        if (ren == null)
            return false;

        SkipPlayerSkin skip = ren.GetComponentInParent<SkipPlayerSkin>();
        if (skip == null)
            return false;

        if (skip.includeChildren)
            return true;

        return skip.gameObject == ren.gameObject;
    }

    /// <summary>True when the whole root should keep authored colors (component on root/any child with includeChildren).</summary>
    public static bool ShouldSkipRoot(GameObject root)
    {
        if (root == null)
            return false;

        SkipPlayerSkin[] skips = root.GetComponentsInChildren<SkipPlayerSkin>(true);
        for (int i = 0; i < skips.Length; i++)
        {
            SkipPlayerSkin s = skips[i];
            if (s == null)
                continue;
            if (s.gameObject == root)
                return true;
            if (s.includeChildren && s.transform.IsChildOf(root.transform))
            {
                // Component covering the full tree (on root or only child markers) —
                // treat as full skip when every renderer under root is covered.
                Renderer[] rends = root.GetComponentsInChildren<Renderer>(true);
                if (rends.Length == 0)
                    return true;
                bool allCovered = true;
                for (int r = 0; r < rends.Length; r++)
                {
                    if (rends[r] is ParticleSystemRenderer)
                        continue;
                    if (!ShouldSkipRenderer(rends[r]))
                    {
                        allCovered = false;
                        break;
                    }
                }
                if (allCovered)
                    return true;
            }
        }

        return false;
    }
}
