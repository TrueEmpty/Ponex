#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Restores Gameplay.unity from Unity's Temp scene backup (or a copied .backup).
/// </summary>
public static class RestoreSceneBackup
{
    const string Gameplay = "Assets/Scenes/Gameplay.unity";
    const string SafetyCopy = "Assets/Scenes/Gameplay.before-restore.unity";
    const string PreservedBackup = "Tools/scene-backups/Gameplay.pre-1-46.backup";
    const string TempBackup = "Temp/__Backupscenes/0.backup";

    [MenuItem("Tools/Ponex/Restore Gameplay From Temp Backup")]
    public static void Restore()
    {
        string root = Directory.GetCurrentDirectory();
        string preserved = Path.Combine(root, PreservedBackup.Replace('/', Path.DirectorySeparatorChar));
        string temp = Path.Combine(root, TempBackup.Replace('/', Path.DirectorySeparatorChar));
        string backupPath = File.Exists(preserved) ? preserved : temp;

        if (!File.Exists(backupPath))
        {
            EditorUtility.DisplayDialog("Restore", "No backup file found at:\n" + backupPath, "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Restore Gameplay scene?",
                "This replaces Assets/Scenes/Gameplay.unity with the Unity Temp backup from ~1:47 AM.\n\n" +
                "A safety copy will be saved as Gameplay.before-restore.unity first.\n\n" +
                "Close any unsaved Gameplay work first. Continue?",
                "Restore", "Cancel"))
            return;

        // Safety copy of current
        File.Copy(Gameplay, SafetyCopy, true);

        // Close gameplay if open
        var scene = EditorSceneManager.GetSceneByPath(Gameplay);
        if (scene.IsValid() && scene.isLoaded)
            EditorSceneManager.CloseScene(scene, true);

        File.Copy(backupPath, Gameplay, true);
        AssetDatabase.Refresh();

        EditorSceneManager.OpenScene(Gameplay, OpenSceneMode.Single);
        EditorUtility.DisplayDialog(
            "Restore",
            "Restored Gameplay from backup.\n\n" +
            "If the scene fails to load (binary vs text), use the safety copy:\n" +
            SafetyCopy,
            "OK");
    }
}
#endif
