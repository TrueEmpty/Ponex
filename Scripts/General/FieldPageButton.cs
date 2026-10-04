using UnityEngine;

/// <summary>Page turner for Field Select (Prev / Next).</summary>
public class FieldPageButton : MonoBehaviour
{
    public int direction = 1;

    public void OnClick(int player)
    {
        if (FieldSelect.instance != null)
            FieldSelect.instance.PageChange(direction);
    }
}
