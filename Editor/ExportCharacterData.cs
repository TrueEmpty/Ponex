#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Tools/Ponex/Export Characters To ScriptableObjects
/// Copies Database.characters into Assets/Resources/Characters as CharacterData assets.
/// </summary>
public static class ExportCharacterData
{
    const string OutFolder = "Assets/Resources/Characters";

    [MenuItem("Tools/Ponex/Export Characters To ScriptableObjects")]
    public static void Export()
    {
        Database db = Object.FindAnyObjectByType<Database>();
        if (db == null)
        {
            EditorUtility.DisplayDialog("Export Characters", "No Database found in the open scene.", "OK");
            return;
        }

        if (db.characters == null || db.characters.Count == 0)
        {
            EditorUtility.DisplayDialog("Export Characters", "Database.characters is empty.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(OutFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "Characters");

        int written = 0;
        for (int i = 0; i < db.characters.Count; i++)
        {
            Characters src = db.characters[i];
            if (src == null || string.IsNullOrEmpty(src.name))
                continue;

            string safe = src.name;
            foreach (char c in Path.GetInvalidFileNameChars())
                safe = safe.Replace(c, '_');

            string path = $"{OutFolder}/{(i + 1):00}_{safe}.asset";
            CharacterData asset = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CharacterData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.rosterOrder = i + 1;
            asset.characterName = src.name;
            asset.maxHealth = src.maxHealth;
            asset.movementSpeed = src.movementSpeed;
            asset.pushBack = src.pushBack;
            asset.ignoreFacing = src.ignoreFacing;
            asset.bump = new Skill(src.bump);
            asset.super = new Skill(src.super);
            asset.dash = new Skill(src.dash);
            asset.character = src.character != null ? new ObjectInfo(src.character) : new ObjectInfo();
            asset.lifeline = src.lifeline != null ? new ObjectInfo(src.lifeline) : new ObjectInfo();
            asset.selector = src.selector;
            asset.playerInfo = src.playerInfo;
            asset.superName = src.superName;
            asset.superDescription = src.superDescription;
            asset.portraitColor = src.portraitColor;
            asset.portrait = src.portrait;
            asset.icon = src.icon;
            asset.active = src.active;

            EditorUtility.SetDirty(asset);
            written++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Export Characters", $"Wrote/updated {written} CharacterData assets in {OutFolder}.", "OK");
    }
}
#endif
