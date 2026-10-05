using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class QuitGameButton : MonoBehaviour
{
    public void OnLongClick(int player)
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
