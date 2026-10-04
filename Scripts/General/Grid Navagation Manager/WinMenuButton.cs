using UnityEngine;

/// <summary>
/// Selector / SendMessage click target for Winners menu options.
/// </summary>
public class WinMenuButton : MonoBehaviour
{
    public string buttonPressed = "Main Menu";

    void OnClick(int player)
    {
        if (Database.instance == null)
            return;

        Database.instance.WinButtonPressed(buttonPressed);
    }
}
