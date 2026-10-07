#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Autosaves Character Creation working draft when leaving Play Mode.
/// </summary>
[InitializeOnLoad]
public static class CharacterCreationDraftPersistence
{
    static CharacterCreationDraftPersistence()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            if (CharacterCreationManager.instance != null && CharacterCreationManager.IsActive)
                CharacterCreationManager.instance.PersistWorkingDraftForEditor();
        }
    }
}
#endif
