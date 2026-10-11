using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows Forfeit in an online match and Reset Match in local play.
/// Arcade keeps Resume, Character Select, and Main Menu.
/// </summary>
public class PauseMenuMode : MonoBehaviour
{
    public GameObject resetMatchButton;
    public GameObject forfeitButton;

    readonly Dictionary<int, Vector2> homePositions = new Dictionary<int, Vector2>();

    void OnEnable()
    {
        bool arcade = Database.instance != null && Database.instance.gametype == Gametype.Arcade;
        bool online = Database.instance != null && Database.instance.onlineMatch && !arcade;

        if (resetMatchButton != null)
            resetMatchButton.SetActive(!online && !arcade);
        if (forfeitButton != null)
            forfeitButton.SetActive(online);

        PauseMenuButton[] buttons = GetComponentsInChildren<PauseMenuButton>(true);
        List<RectTransform> shown = new List<RectTransform>();
        for (int i = 0; i < buttons.Length; i++)
        {
            PauseMenuButton button = buttons[i];
            if (button == null)
                continue;

            RectTransform rt = button.GetComponent<RectTransform>();
            if (rt != null && !homePositions.ContainsKey(button.GetEntityId().GetHashCode()))
                homePositions[button.GetEntityId().GetHashCode()] = rt.anchoredPosition;

            bool show = true;
            string label = null;
            switch (button.action)
            {
                case "Reset Match":
                    show = !online && !arcade;
                    break;
                case "Forfeit":
                    show = online;
                    break;
                case "Reset Game":
                    label = arcade ? "Character Select" : "Reset Game";
                    break;
            }

            button.gameObject.SetActive(show);
            if (!string.IsNullOrEmpty(label))
            {
                Text text = button.GetComponentInChildren<Text>(true);
                if (text != null)
                    text.text = label;
            }

            if (show && rt != null)
                shown.Add(rt);
        }

        if (!arcade)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                PauseMenuButton button = buttons[i];
                if (button == null)
                    continue;
                RectTransform rt = button.GetComponent<RectTransform>();
                if (rt != null && homePositions.TryGetValue(button.GetEntityId().GetHashCode(), out Vector2 home))
                    rt.anchoredPosition = home;
            }
            return;
        }

        float span = 280f;
        float origin = -((shown.Count - 1) * span) * 0.5f;
        for (int i = 0; i < shown.Count; i++)
        {
            RectTransform rt = shown[i];
            Vector2 pos = rt.anchoredPosition;
            pos.x = origin + i * span;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(250f, 65f);
            SelectorClickable.Ensure(rt.gameObject, 200f);
        }
    }
}
