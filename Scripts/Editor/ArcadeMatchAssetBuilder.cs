#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Writes the premade arcade rounds into Resources so they can be edited as assets.
/// </summary>
[InitializeOnLoad]
static class ArcadeMatchAssetBuilder
{
    static ArcadeMatchAssetBuilder()
    {
        EditorApplication.delayCall += Ensure;
    }

    static void Ensure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Arcade"))
            AssetDatabase.CreateFolder("Assets/Resources", "Arcade");

        string[] existing = AssetDatabase.FindAssets("t:ArcadeMatch", new[] { "Assets/Resources/Arcade" });
        if (existing != null && existing.Length >= 20)
            return;

        List<ArcadeMatch> matches = ArcadeMatchDefaults.CreateAll();
        for (int i = 0; i < matches.Count; i++)
        {
            ArcadeMatch match = matches[i];
            string file = Sanitize(match.name) + ".asset";
            string path = "Assets/Resources/Arcade/" + file;
            if (AssetDatabase.LoadAssetAtPath<ArcadeMatch>(path) != null)
            {
                Object.DestroyImmediate(match);
                continue;
            }

            AssetDatabase.CreateAsset(match, path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static string Sanitize(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Arcade Match";
        char[] chars = name.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            if (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_')
                continue;
            chars[i] = '_';
        }
        return new string(chars);
    }
}
#endif
