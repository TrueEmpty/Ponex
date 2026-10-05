using UnityEngine;

/// <summary>
/// Legacy UI Leave button — disconnect is now a hold input on the controller
/// (Select / Backspace / Delete via the "Leave" action). This script only cleans
/// up any leftover UI button if one still exists in the scene.
/// </summary>
public class DisconnectPlayerButton : MonoBehaviour
{
    const string ButtonName = "Leave";

    /// <summary>Destroys any leftover Leave UI under parent — do not recreate it.</summary>
    public static void EnsureExists(Transform parent)
    {
        DestroyLeaveUnder(parent);
        if (parent != null)
            DestroyLeaveUnder(parent.parent);
    }

    static void DestroyLeaveUnder(Transform parent)
    {
        if (parent == null)
            return;
        Transform existing = parent.Find(ButtonName);
        if (existing != null)
            Object.Destroy(existing.gameObject);
    }

    void Awake()
    {
        // If an old Leave UI somehow still has this component, remove the object
        Destroy(gameObject);
    }
}
