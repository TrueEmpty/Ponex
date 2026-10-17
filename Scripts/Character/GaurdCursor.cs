using UnityEngine;

public class GaurdCursor : MonoBehaviour
{
    public Renderer aura;
    public Light glow;
    static Bounds field;
    static float nextScan;

    public void SetTint(Color color)
    {
        if (aura == null)
            aura = GetComponentInChildren<Renderer>();
        if (aura != null)
        {
            Material mat = aura.material;
            Color c = color;
            c.a = 0.85f;
            mat.color = c;
        }
        if (glow == null)
            glow = GetComponentInChildren<Light>();
        if (glow != null)
            glow.color = color;
    }

    public static Vector3 ClampToField(Vector3 point, float radius)
    {
        if (!TryBounds(out Bounds box))
            return point;
        point.x = Mathf.Clamp(point.x, box.min.x + radius, box.max.x - radius);
        point.y = Mathf.Clamp(point.y, box.min.y + radius, box.max.y - radius);
        point.z = box.center.z;
        return point;
    }

    public static bool TryBounds(out Bounds box)
    {
        if (Time.unscaledTime < nextScan && field.size.sqrMagnitude > 0.01f)
        {
            box = field;
            return true;
        }
        nextScan = Time.unscaledTime + 0.5f;
        bool any = false;
        box = new Bounds(Vector3.zero, Vector3.one);
        Consider("Walls", ref box, ref any);
        Consider("Wall", ref box, ref any);
        if (!any)
            return false;
        Bounds shrunk = box;
        shrunk.Expand(new Vector3(-1.1f, -1.1f, 0f));
        field = shrunk.size.x > 1f && shrunk.size.y > 1f ? shrunk : box;
        box = field;
        return true;
    }

    static void Consider(string tag, ref Bounds box, ref bool any)
    {
        GameObject[] found;
        try
        {
            found = GameObject.FindGameObjectsWithTag(tag);
        }
        catch
        {
            return;
        }
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] == null)
                continue;
            Collider col = found[i].GetComponent<Collider>();
            Renderer ren = found[i].GetComponent<Renderer>();
            Bounds b = col != null ? col.bounds : (ren != null ? ren.bounds : new Bounds(found[i].transform.position, Vector3.one));
            if (!any)
            {
                box = b;
                any = true;
            }
            else
            {
                box.Encapsulate(b);
            }
        }
    }
}
