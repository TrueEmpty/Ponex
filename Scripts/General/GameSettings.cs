using UnityEngine;

/// <summary>
/// Persistent gameplay options (PlayerPrefs). Hazards off = only walls / borders spawn.
/// </summary>
public static class GameSettings
{
    const string PrefHazards = "Ponex.HazardsEnabled";

    static bool loaded;
    static bool hazardsEnabled = true;

    public static bool HazardsEnabled
    {
        get
        {
            EnsureLoaded();
            return hazardsEnabled;
        }
        set
        {
            hazardsEnabled = value;
            PlayerPrefs.SetInt(PrefHazards, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static void EnsureLoaded()
    {
        if (loaded)
            return;
        hazardsEnabled = PlayerPrefs.GetInt(PrefHazards, 1) != 0;
        loaded = true;
    }
}
