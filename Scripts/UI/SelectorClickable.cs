using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ensures a UI object can be hit by PlayerSelectorObj + UniversalCollisionDetector.
/// </summary>
public static class SelectorClickable
{
    public static void Ensure(GameObject go, float fallbackSize = 80f)
    {
        if (go == null)
            return;

        BoxCollider col = go.GetComponent<BoxCollider>();
        if (col == null)
            col = go.AddComponent<BoxCollider>();

        col.isTrigger = true;
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            bool point = rt.anchorMin == rt.anchorMax;
            float w = point ? Mathf.Abs(rt.sizeDelta.x) : rt.rect.width;
            float h = point ? Mathf.Abs(rt.sizeDelta.y) : rt.rect.height;
            if (w < 1f) w = rt.rect.width > 1f ? rt.rect.width : fallbackSize;
            if (h < 1f) h = rt.rect.height > 1f ? rt.rect.height : fallbackSize;
            col.size = new Vector3(w, h, 1f);
            col.center = new Vector3((0.5f - rt.pivot.x) * w, (0.5f - rt.pivot.y) * h, 0f);
        }
        else
        {
            col.size = new Vector3(fallbackSize, fallbackSize, 1f);
            col.center = Vector3.zero;
        }

        if (go.GetComponent<ButtonInteraction>() == null)
            go.AddComponent<ButtonInteraction>();

        if (go.GetComponent<Outline>() == null && go.GetComponent<Image>() != null)
        {
            Outline ol = go.AddComponent<Outline>();
            ol.effectColor = new Color(0.1f, 0.35f, 0.95f, 1f);
            ol.effectDistance = new Vector2(3f, 3f);
        }
    }
}
