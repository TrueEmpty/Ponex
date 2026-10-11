using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ensures cursor short-click activates Unity UI Button OnClick on Back buttons
/// that don't use RunOnClicked.
/// </summary>
public class BackButtonClick : MonoBehaviour
{
    static readonly System.Collections.Generic.List<Transform> scanBuffer =
        new System.Collections.Generic.List<Transform>(128);

    public static void EnsureAll()
    {
        MenuManager mm = MenuManager.instance;
        if (mm != null && mm.menus != null && mm.menus.Count > 0)
        {
            for (int i = 0; i < mm.menus.Count; i++)
            {
                MenuClass menu = mm.menus[i];
                if (menu == null || menu.holder == null)
                    continue;

                scanBuffer.Clear();
                menu.holder.GetComponentsInChildren(true, scanBuffer);
                for (int t = 0; t < scanBuffer.Count; t++)
                    Ensure(scanBuffer[t] != null ? scanBuffer[t].gameObject : null);
            }
            scanBuffer.Clear();
            return;
        }

        // Fallback for isolated scenes without a MenuManager.
        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
            Ensure(all[i]);
    }

    static void Ensure(GameObject go)
    {
        if (go == null || !IsBackNamed(go.name))
            return;

        if (go.GetComponent<BackButtonClick>() == null)
            go.AddComponent<BackButtonClick>();

        SelectorClickable.Ensure(go, 170f);
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
        {
            AudioSettings.PlayDeselected();
            btn.onClick.Invoke();
        }
    }
}
