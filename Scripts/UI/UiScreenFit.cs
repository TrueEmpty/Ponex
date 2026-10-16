using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Keeps every screen-space canvas on the same 1920x1080 layout, scaled to the display.
/// </summary>
public class UiScreenFit : MonoBehaviour
{
    public static readonly Vector2 Reference = new Vector2(1920f, 1080f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        FitAll();
        SceneManager.sceneLoaded += (_, _) => FitAll();
        if (FindAnyObjectByType<UiScreenFit>() != null)
            return;
        GameObject go = new GameObject("Ui Screen Fit");
        DontDestroyOnLoad(go);
        go.AddComponent<UiScreenFit>();
    }

    void LateUpdate()
    {
        FitAll();
    }

    public static void Fit(Canvas canvas)
    {
        if (canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)
            return;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
            && scaler.referenceResolution == Reference
            && scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight
            && Mathf.Approximately(scaler.matchWidthOrHeight, 0.5f))
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = Reference;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    public static void FitAll()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        for (int i = 0; i < canvases.Length; i++)
            Fit(canvases[i]);
    }
}
