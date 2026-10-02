using UnityEngine;
using static UnityEngine.UI.Button;

public class RunOnClicked : MonoBehaviour
{
    [SerializeField]
    private ButtonClickedEvent onClickEvent = new ButtonClickedEvent();

    [SerializeField]
    private ButtonClickedEvent onLongClickEvent = new ButtonClickedEvent();


    void OnClick(int player)
    {
        Debug.Log("Running on Clicked");
        onClickEvent.Invoke();
    }

    void OnLongClick(int player)
    {
        Debug.Log("Running on Clicked");
        onLongClickEvent.Invoke();
    }
}
