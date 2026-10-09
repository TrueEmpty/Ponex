using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene button on the Pause canvas. Controller clicks and mouse clicks both land here.
/// </summary>
public class PauseMenuButton : MonoBehaviour
{
    public string action = "Resume";
    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(Press);
    }

    void OnClick(int player)
    {
        Press();
    }

    void Press()
    {
        if (Database.instance != null)
            Database.instance.PauseButtonPressed(action);
    }
}
