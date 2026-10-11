using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StoryChapter
{
    public string character;
    public int index;
    public string title;
}

public static class StoryProgress
{
    const string InitKey = "Ponex.Story.Initialized";
    const string SkipKey = "Ponex.Story.SkipCutscenes";
    const string ChapterKeyPrefix = "Ponex.Story.Chapter.";
    const string SaveKeyPrefix = "Ponex.Story.Save.";

    public static void EnsureLoaded(Database db)
    {
        if (!PlayerPrefs.HasKey(InitKey))
        {
            PlayerPrefs.SetInt(InitKey, 1);
            PlayerPrefs.SetInt(ChapterPref("Test", 0), 1);
            PlayerPrefs.Save();
        }

        if (db != null)
            db.skipStoryCutscenes = PlayerPrefs.GetInt(SkipKey, 0) == 1;
    }

    public static void SetSkipCutscenes(bool skip)
    {
        if (Database.instance != null)
            Database.instance.skipStoryCutscenes = skip;
        PlayerPrefs.SetInt(SkipKey, skip ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool ChapterUnlocked(string character, int index)
    {
        if (string.IsNullOrEmpty(character))
            return false;
        return PlayerPrefs.GetInt(ChapterPref(character, index), 0) == 1;
    }

    public static void UnlockChapter(string character, int index)
    {
        if (string.IsNullOrEmpty(character) || index < 0)
            return;
        PlayerPrefs.SetInt(ChapterPref(character, index), 1);
        PlayerPrefs.Save();
    }

    public static List<StoryChapter> ChaptersFor(string character)
    {
        List<StoryChapter> list = new List<StoryChapter>();
        if (character == "Test")
        {
            list.Add(new StoryChapter { character = character, index = 0, title = "Awake" });
            list.Add(new StoryChapter { character = character, index = 1, title = "The Hall" });
            return list;
        }

        list.Add(new StoryChapter { character = character, index = 0, title = "Beginning" });
        list.Add(new StoryChapter { character = character, index = 1, title = "Chapter 2" });
        return list;
    }

    public static List<string> PlayableCharacters(Database db)
    {
        List<string> names = new List<string>();
        if (db == null || db.characters == null)
            return names;

        for (int i = 0; i < db.characters.Count; i++)
        {
            Characters c = db.characters[i];
            if (c == null || !c.active || string.IsNullOrEmpty(c.name))
                continue;
            if (!ChapterUnlocked(c.name, 0))
                continue;
            names.Add(c.name);
        }
        return names;
    }

    public static bool HasSave(string character)
    {
        return PlayerPrefs.HasKey(SavePref(character));
    }

    public static int SavedChapter(string character)
    {
        string raw = PlayerPrefs.GetString(SavePref(character), "");
        if (string.IsNullOrEmpty(raw))
            return 0;
        StorySaveData data = JsonUtility.FromJson<StorySaveData>(raw);
        return data != null ? data.chapter : 0;
    }

    public static bool TryGetSave(string character, out StorySaveData data)
    {
        data = null;
        string raw = PlayerPrefs.GetString(SavePref(character), "");
        if (string.IsNullOrEmpty(raw))
            return false;
        data = JsonUtility.FromJson<StorySaveData>(raw);
        return data != null;
    }

    public static void WriteChapterSave(string character, int chapter)
    {
        StorySaveData data = new StorySaveData
        {
            character = character,
            chapter = chapter,
            hasTransform = false
        };
        PlayerPrefs.SetString(SavePref(character), JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static void WritePositionSave(string character, int chapter, Vector3 position, Quaternion rotation)
    {
        StorySaveData data = new StorySaveData
        {
            character = character,
            chapter = chapter,
            hasTransform = true,
            px = position.x,
            py = position.y,
            pz = position.z,
            rx = rotation.x,
            ry = rotation.y,
            rz = rotation.z,
            rw = rotation.w
        };
        PlayerPrefs.SetString(SavePref(character), JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static bool ShouldSkipMenus(Database db)
    {
        List<string> playable = PlayableCharacters(db);
        if (playable.Count != 1 || playable[0] != "Test")
            return false;
        int chapter = HasSave("Test") ? SavedChapter("Test") : 0;
        return chapter <= 0;
    }

    static string ChapterPref(string character, int index)
    {
        return ChapterKeyPrefix + character + "." + index;
    }

    static string SavePref(string character)
    {
        return SaveKeyPrefix + character;
    }
}

[Serializable]
public class StorySaveData
{
    public string character;
    public int chapter;
    public bool hasTransform;
    public float px, py, pz;
    public float rx, ry, rz, rw;

    public Vector3 Position => new Vector3(px, py, pz);
    public Quaternion Rotation => new Quaternion(rx, ry, rz, rw);
}
