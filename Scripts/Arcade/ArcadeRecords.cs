using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Who has cleared arcade, including coop pairs and the computer difficulty used.
/// Stored in PlayerPrefs and loaded when the game opens.
/// </summary>
public static class ArcadeRecords
{
    const string PrefClears = "Ponex.Arcade.Clears";
    const string PrefDifficulty = "Ponex.Arcade.Difficulty";

    static bool loaded;
    static readonly List<ArcadeClear> clears = new List<ArcadeClear>();
    static ComputerAI.CpuDifficulty selected = ComputerAI.CpuDifficulty.Normal;

    public static IReadOnlyList<ArcadeClear> Clears
    {
        get
        {
            EnsureLoaded();
            return clears;
        }
    }

    public static ComputerAI.CpuDifficulty SelectedDifficulty
    {
        get
        {
            EnsureLoaded();
            return selected;
        }
        set
        {
            EnsureLoaded();
            if (value == ComputerAI.CpuDifficulty.Training)
                value = ComputerAI.CpuDifficulty.Easy;
            selected = value;
            PlayerPrefs.SetInt(PrefDifficulty, (int)selected);
            PlayerPrefs.Save();
        }
    }

    public static void EnsureLoaded()
    {
        if (loaded)
            return;
        loaded = true;
        clears.Clear();

        int diff = PlayerPrefs.GetInt(PrefDifficulty, (int)ComputerAI.CpuDifficulty.Normal);
        if (diff <= (int)ComputerAI.CpuDifficulty.Training || diff > (int)ComputerAI.CpuDifficulty.Impossible)
            diff = (int)ComputerAI.CpuDifficulty.Normal;
        selected = (ComputerAI.CpuDifficulty)diff;

        string raw = PlayerPrefs.GetString(PrefClears, "");
        if (string.IsNullOrEmpty(raw))
            return;

        string[] lines = raw.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            ArcadeClear clear = ArcadeClear.Parse(lines[i]);
            if (clear != null)
                clears.Add(clear);
        }
    }

    public static void RecordClear(bool coop, string characterA, string characterB, ComputerAI.CpuDifficulty difficulty)
    {
        EnsureLoaded();
        ArcadeClear entry = ArcadeClear.Make(coop, characterA, characterB, difficulty);
        for (int i = 0; i < clears.Count; i++)
        {
            if (clears[i].Same(entry))
                return;
        }

        clears.Add(entry);
        Save();
    }

    public static bool HasClear(bool coop, string characterA, string characterB, ComputerAI.CpuDifficulty difficulty)
    {
        EnsureLoaded();
        ArcadeClear entry = ArcadeClear.Make(coop, characterA, characterB, difficulty);
        for (int i = 0; i < clears.Count; i++)
        {
            if (clears[i].Same(entry))
                return true;
        }
        return false;
    }

    static void Save()
    {
        string raw = "";
        for (int i = 0; i < clears.Count; i++)
        {
            if (i > 0)
                raw += "\n";
            raw += clears[i].Write();
        }
        PlayerPrefs.SetString(PrefClears, raw);
        PlayerPrefs.Save();
    }
}

public class ArcadeClear
{
    public bool coop;
    public string characterA = "";
    public string characterB = "";
    public int difficulty;

    public static ArcadeClear Make(bool coop, string a, string b, ComputerAI.CpuDifficulty difficulty)
    {
        a = a ?? "";
        b = b ?? "";
        if (coop && string.Compare(a, b, StringComparison.OrdinalIgnoreCase) > 0)
        {
            string swap = a;
            a = b;
            b = swap;
        }
        if (!coop)
            b = "";

        return new ArcadeClear
        {
            coop = coop,
            characterA = a,
            characterB = b,
            difficulty = (int)difficulty
        };
    }

    public bool Same(ArcadeClear other)
    {
        if (other == null)
            return false;
        return coop == other.coop
            && difficulty == other.difficulty
            && string.Equals(characterA, other.characterA, StringComparison.OrdinalIgnoreCase)
            && string.Equals(characterB, other.characterB, StringComparison.OrdinalIgnoreCase);
    }

    public string Write()
    {
        return (coop ? "1" : "0") + "|" + difficulty + "|" + characterA + "|" + characterB;
    }

    public static ArcadeClear Parse(string line)
    {
        if (string.IsNullOrEmpty(line))
            return null;
        string[] parts = line.Split('|');
        if (parts.Length < 3)
            return null;
        if (!int.TryParse(parts[1], out int diff))
            return null;

        return new ArcadeClear
        {
            coop = parts[0] == "1",
            difficulty = diff,
            characterA = parts[2],
            characterB = parts.Length > 3 ? parts[3] : ""
        };
    }
}
