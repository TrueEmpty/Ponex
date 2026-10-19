using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Two Tics on the same team and wall become one machine.
/// The back seat keeps the machine. The forward seat is the wing bumper.
/// Both share a doubled health pool.
/// </summary>
public static class TicCoopLayout
{

    public static bool IsTicPlayer(Player p)
    {
        if (p == null)
            return false;
        if (!string.IsNullOrEmpty(p.name)
            && p.name.Equals("Tic", System.StringComparison.OrdinalIgnoreCase))
            return true;
        return PrefabHas<Tic>(p.character != null ? p.character.prefabs : null);
    }

    static bool PrefabHas<T>(GameObject prefab) where T : Component
    {
        if (prefab == null)
            return false;
        try
        {
            return prefab.GetComponent<T>() != null;
        }
        catch (MissingReferenceException)
        {
            return false;
        }
    }

    /// <summary>Call once before the StartGame spawn loop.</summary>
    public static void Prepare(List<Player> players, float fieldPlaySize)
    {
        if (players == null)
            return;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == null)
                continue;
            players[i].ticWingForm = false;
            players[i].ticHostFused = false;
            players[i].ticPartnerIndex = -1;
        }

        for (int f = 0; f < 4; f++)
        {
            Facing facing = (Facing)f;
            List<int> teams = new List<int>(4);
            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];
                if (p == null || p.arcadeOut || p.facing != facing || !IsTicPlayer(p))
                    continue;
                if (!teams.Contains(p.team))
                    teams.Add(p.team);
            }

            for (int t = 0; t < teams.Count; t++)
                FuseGroup(PlayersInGroup(players, facing, teams[t]));
        }
    }

    static List<Player> PlayersInGroup(List<Player> players, Facing facing, int team)
    {
        List<Player> group = new List<Player>(2);
        for (int i = 0; i < players.Count; i++)
        {
            Player p = players[i];
            if (p == null || p.arcadeOut || p.facing != facing || p.team != team || !IsTicPlayer(p))
                continue;
            group.Add(p);
        }
        return group;
    }

    /// <summary>Back seat (lower position) stays the machine. Forward seats become wing bumpers.</summary>
    static void FuseGroup(List<Player> group)
    {
        if (group == null || group.Count < 2)
            return;

        group.Sort((a, b) =>
        {
            int c = a.position.CompareTo(b.position);
            return c != 0 ? c : a.index.CompareTo(b.index);
        });

        Player host = group[0];
        host.ticHostFused = true;
        host.ticWingForm = false;
        host.ticWallSideOffset = 0f;
        host.ticPartnerIndex = group[1].index;
        DoublePool(host);

        for (int i = 1; i < group.Count; i++)
        {
            Player wing = group[i];
            wing.ticWingForm = true;
            wing.ticHostFused = false;
            wing.ticWallSideOffset = 0f;
            wing.ticPartnerIndex = host.index;
            DoublePool(wing);
            wing.currentHealth = host.currentHealth;
            wing.maxHealth = host.maxHealth;
        }
    }

    static void DoublePool(Player p)
    {
        if (p == null || p.sharedHealthDoubled)
            return;
        p.maxHealth = Mathf.Max(1, p.maxHealth * 2);
        p.currentHealth = p.maxHealth;
        p.sharedHealthDoubled = true;
    }

    /// <summary>Seats are final. Fuse same-team Tics on each wall.</summary>
    public static void KeepTicsApart(List<Player> players, float fieldPlaySize)
    {
        Prepare(players, fieldPlaySize);
    }
}
