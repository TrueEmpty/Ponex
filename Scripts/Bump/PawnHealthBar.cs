using UnityEngine;
using UnityEngine.UI;

public class PawnHealthBar : MonoBehaviour
{
    public Pawn pawn;
    public Image[] bars;
    public Image cooldown;
    public Image backing;

    void Awake()
    {
        if (pawn == null)
            pawn = GetComponentInParent<Pawn>();
        if (bars == null || bars.Length == 0)
        {
            Image[] found = GetComponentsInChildren<Image>(true);
            var list = new System.Collections.Generic.List<Image>();
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] == null)
                    continue;
                if (found[i].name == "Cooldown")
                    cooldown = found[i];
                else if (found[i].name == "Backing")
                    backing = found[i];
                else if (found[i].name.StartsWith("Bar"))
                    list.Add(found[i]);
            }
            bars = list.ToArray();
        }
    }

    void LateUpdate()
    {
        if (pawn == null)
            return;

        Camera cam = Camera.main;
        if (cam != null)
            transform.rotation = cam.transform.rotation;

        if (transform.parent != null)
        {
            Vector3 parentScale = transform.parent.lossyScale;
            float sx = Mathf.Max(0.0001f, Mathf.Abs(parentScale.x));
            float sy = Mathf.Max(0.0001f, Mathf.Abs(parentScale.y));
            float sz = Mathf.Max(0.0001f, Mathf.Abs(parentScale.z));
            transform.localScale = new Vector3(0.012f / sx, 0.012f / sy, 0.012f / sz);
        }

        float lift = 0.8f;
        Renderer body = pawn.GetComponent<Renderer>();
        if (body == null)
        {
            Renderer[] renders = pawn.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renders.Length; i++)
            {
                if (renders[i] != null && !renders[i].transform.IsChildOf(transform))
                {
                    body = renders[i];
                    break;
                }
            }
        }
        if (body != null)
            lift = body.bounds.extents.y + 0.35f;
        transform.position = pawn.transform.position + Vector3.up * lift;
        if (cam != null)
            transform.position -= cam.transform.forward * 0.2f;

        bool reflector = pawn.pawnType == PawnType.Reflector;
        float life = Mathf.Max(0f, pawn.reflectorLife);
        int whole = Mathf.FloorToInt(life + 0.0001f);
        float frac = Mathf.Clamp01(life - whole);
        int max = reflector
            ? Mathf.Max(1, whole + (frac > 0.001f ? 1 : 0))
            : Mathf.Max(1, pawn.MaxHealth);
        int hp = Mathf.Clamp(pawn.health, 0, max);
        const float width = 92f;
        const float gap = 4f;
        float seg = (width - gap * (max - 1)) / max;
        float start = -width * 0.5f;

        if (backing != null)
            backing.color = Color.black;

        for (int i = 0; i < bars.Length; i++)
        {
            if (bars[i] == null)
                continue;
            bool slot = i < max;
            bars[i].gameObject.SetActive(slot);
            if (!slot)
                continue;
            RectTransform rt = bars[i].rectTransform;
            float barWidth = seg;
            bool filled = i < hp;
            if (reflector)
            {
                if (i < whole)
                    filled = true;
                else if (i == whole && frac > 0.001f)
                {
                    barWidth = seg * frac;
                    filled = true;
                }
                else
                    filled = false;
            }
            rt.sizeDelta = new Vector2(barWidth, 14f);
            rt.anchoredPosition = new Vector2(start + i * (seg + gap) + barWidth * 0.5f, 0f);
            bars[i].color = !filled
                ? Color.black
                : (pawn.PaysGold ? new Color(0.15f, 0.9f, 0.25f, 1f) : new Color(0.62f, 0.22f, 0.95f, 1f));
        }

        if (cooldown == null)
            return;

        bool ranger = pawn.pawnType == PawnType.Ranger && max == 1 && bars.Length > 0 && bars[0] != null;
        cooldown.gameObject.SetActive(ranger);
        if (!ranger)
            return;

        cooldown.color = Color.white;
        cooldown.rectTransform.sizeDelta = new Vector2(seg * pawn.CooldownFill, 14f);
        cooldown.rectTransform.anchoredPosition = bars[0].rectTransform.anchoredPosition;
    }
}
