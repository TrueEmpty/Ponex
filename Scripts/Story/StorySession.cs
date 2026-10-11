using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StorySession
{
    public const string SceneName = "Story";

    public static string character = "Test";
    public static int chapter;
    public static bool loadSavedTransform;
    public static bool active;

    static readonly List<Camera> hiddenCameras = new List<Camera>();

    public static void Begin(string characterName, int chapterIndex, bool useSavePosition)
    {
        character = string.IsNullOrEmpty(characterName) ? "Test" : characterName;
        chapter = chapterIndex;
        loadSavedTransform = useSavePosition;
        active = true;

        Database db = Database.instance;
        if (db != null)
        {
            db.storyPlaying = true;
            db.storyPauseOpen = false;
            db.storySharedCursor = false;
        }

        HideMenus();
        StashCameras();

        if (!Application.CanStreamedLevelBeLoaded(SceneName))
        {
            Debug.LogWarning("Story scene is not in the build settings. Add Assets/Scenes/Story.unity.");
            return;
        }

        Scene existing = SceneManager.GetSceneByName(SceneName);
        if (existing.IsValid() && existing.isLoaded)
        {
            SceneManager.SetActiveScene(existing);
            StoryWorld.BeginIfReady();
            return;
        }

        SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive).completed += _ =>
        {
            Scene scene = SceneManager.GetSceneByName(SceneName);
            if (scene.IsValid())
                SceneManager.SetActiveScene(scene);
            StoryWorld.BeginIfReady();
        };
    }

    public static void ExitToMenu()
    {
        StoryPlayer player = Object.FindAnyObjectByType<StoryPlayer>();
        if (player != null)
        {
            StoryProgress.WritePositionSave(character, chapter, player.transform.position, player.transform.rotation);
        }

        StoryPauseMenu.Close();
        active = false;
        Database db = Database.instance;
        if (db != null)
        {
            db.storyPlaying = false;
            db.storyPauseOpen = false;
            db.storySharedCursor = false;
        }

        Scene scene = SceneManager.GetSceneByName(SceneName);
        if (scene.IsValid() && scene.isLoaded)
            SceneManager.UnloadSceneAsync(scene);

        RestoreCameras();
        MenuManager mm = MenuManager.instance;
        if (mm != null)
        {
            mm.openMenu.Clear();
            mm.OpenMenu("Main Menu");
        }
    }

    static void HideMenus()
    {
        MenuManager mm = MenuManager.instance;
        if (mm == null || mm.menus == null)
            return;
        mm.openMenu.Clear();
        for (int i = 0; i < mm.menus.Count; i++)
        {
            MenuClass menu = mm.menus[i];
            if (menu != null && menu.holder != null)
                menu.holder.SetActive(false);
        }
    }

    static void StashCameras()
    {
        hiddenCameras.Clear();
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null || !cam.enabled)
                continue;
            if (cam.GetComponent<StoryWorld>() != null || cam.GetComponentInParent<StoryWorld>() != null)
                continue;
            cam.enabled = false;
            hiddenCameras.Add(cam);
        }
    }

    static void RestoreCameras()
    {
        for (int i = 0; i < hiddenCameras.Count; i++)
        {
            if (hiddenCameras[i] != null)
                hiddenCameras[i].enabled = true;
        }
        hiddenCameras.Clear();
    }
}
