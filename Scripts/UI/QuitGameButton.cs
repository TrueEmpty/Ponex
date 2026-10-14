using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class QuitGameButton : MonoBehaviour
{
    public void OnLongClick(int player)
    {
        if (OnlineMode.InLobby && OnlineMode.instance != null)
        {
            OnlineMode.instance.StartCoroutine(OnlineMode.instance.LeaveAndQuit());
            return;
        }
        QuitNow();
    }

    void QuitNow()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
