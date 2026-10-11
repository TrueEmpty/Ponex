using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Button;

public class RunOnClicked : MonoBehaviour
{
    [SerializeField]
    private ButtonClickedEvent onClickEvent = new ButtonClickedEvent();

    [SerializeField]
    private ButtonClickedEvent onLongClickEvent = new ButtonClickedEvent();

    void OnClick(int player)
    {
        if (HasCalls(onClickEvent))
        {
            if (IsBackButton())
                AudioSettings.PlayDeselected();
            onClickEvent.Invoke();
            return;
        }

        // Back buttons were historically wired to long-press — fire them on short click.
        if (IsBackButton() && HasCalls(onLongClickEvent))
        {
            AudioSettings.PlayDeselected();
            onLongClickEvent.Invoke();
            return;
        }

        Button btn = GetComponent<Button>();
        if (btn != null && HasCalls(btn.onClick))
        {
            if (IsBackButton())
                AudioSettings.PlayDeselected();
            btn.onClick.Invoke();
        }
    }

    void OnLongClick(int player)
    {
        // Back uses short click only
        if (IsBackButton())
            return;

        if (HasCalls(onLongClickEvent))
            onLongClickEvent.Invoke();
    }

    bool IsBackButton()
    {
        string n = gameObject.name;
        if (string.IsNullOrEmpty(n))
            return false;
        n = n.Trim();
        return n.Equals("Back", System.StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("Back ", System.StringComparison.OrdinalIgnoreCase)
            || n.EndsWith(" Back", System.StringComparison.OrdinalIgnoreCase);
    }

    static bool HasCalls(ButtonClickedEvent evt)
    {
        return evt != null && evt.GetPersistentEventCount() > 0;
    }
}
