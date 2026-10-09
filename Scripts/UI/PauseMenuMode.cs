using UnityEngine;

/// <summary>
/// Shows Forfeit in an online match and Reset Match in local play.
/// Both buttons already exist on the Pause canvas.
/// </summary>
public class PauseMenuMode : MonoBehaviour
{
    public GameObject resetMatchButton;
    public GameObject forfeitButton;

    void OnEnable()
    {
        bool online = Database.instance != null && Database.instance.onlineMatch;
        if (resetMatchButton != null)
            resetMatchButton.SetActive(!online);
        if (forfeitButton != null)
            forfeitButton.SetActive(online);
    }
}
