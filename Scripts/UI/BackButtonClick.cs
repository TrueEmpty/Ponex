using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ensures cursor short-click activates Unity UI Button OnClick on Back buttons
/// that don't use RunOnClicked.
/// </summary>
public class BackButtonClick : MonoBehaviour
{
    public static void EnsureAll()
    {
        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null || !IsBackNamed(go.name))
                continue;

            if (go.GetComponent<BackButtonClick>() == null)
                go.AddComponent<BackButtonClick>();

            SelectorClickable.Ensure(go, 170f);
        }
    }

    static bool IsBackNamed(string n)
    {
        if (string.IsNullOrEmpty(n))
            return false;
        n = n.Trim();
        return n.Equals("Back", System.StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("Back ", System.StringComparison.OrdinalIgnoreCase)
            || n.EndsWith(" Back", System.StringComparison.OrdinalIgnoreCase);
    }

    public void OnClick(int player)
    {
        // RunOnClicked handles its own short-click (including migrated long-press backs)
        if (GetComponent<RunOnClicked>() != null)
            return;

        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.Invoke();
    }
}
