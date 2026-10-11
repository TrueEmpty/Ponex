using UnityEngine;

/// <summary>
/// Persistent gameplay options (PlayerPrefs). Hazards off = only walls / borders spawn.
/// </summary>
public static class GameSettings
{
    const string PrefHazards = "Ponex.HazardsEnabled";

    static bool loaded;
    static bool hazardsEnabled = true;

    /// <summary>Match-only hazards switch. Null uses the saved option.</summary>
    public static bool? HazardOverride;

    public static void ClearHazardOverride()
    {
        HazardOverride = null;
    }

    public static bool HazardsEnabled
    {
        get
        {
            EnsureLoaded();
            if (HazardOverride.HasValue)
                return HazardOverride.Value;
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
