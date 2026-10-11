using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// When two Tics share a wall: side-by-side if the field is wide enough,
/// otherwise fuse into one double-HP machine + a wing-bumper second player.
/// </summary>
public static class TicCoopLayout
{
    /// <summary>World units of wall length needed for two full Tic machines.</summary>
    public const float MinWallForSideBySide = 22f;
    /// <summary>Lateral offset from wall center for each Tic when side-by-side.</summary>
    public const float SideBySideHalfGap = 5.5f;

    public static bool IsTicPlayer(Player p)
    {
        if (p == null)
            return false;
        if (!string.IsNullOrEmpty(p.name)
            && p.name.Equals("Tic", System.StringComparison.OrdinalIgnoreCase))
            return true;
        if (p.character != null && p.character.prefabs != null
            && p.character.prefabs.GetComponent<Tic>() != null)
            return true;
        return false;
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
            players[i].ticWallSideOffset = 0f;
            players[i].ticPartnerIndex = -1;
        }

        // Facing → Tic players on that wall
        for (int f = 0; f < 4; f++)
        {
            Facing facing = (Facing)f;
            List<Player> group = new List<Player>(2);
            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];
                if (p == null || p.facing != facing)
                    continue;
                if (p.arcadeOut || !IsTicPlayer(p))
                    continue;
                group.Add(p);
            }

            if (group.Count < 2)
                continue;

            // Sort: position 0 first, then by index
            group.Sort((a, b) =>
            {
                int c = a.position.CompareTo(b.position);
                return c != 0 ? c : a.index.CompareTo(b.index);
            });

            Player a = group[0];
            Player b = group[1];

            if (fieldPlaySize >= MinWallForSideBySide)
            {
                a.ticWallSideOffset = -SideBySideHalfGap;
                b.ticWallSideOffset = SideBySideHalfGap;
                a.ticPartnerIndex = b.index;
                b.ticPartnerIndex = a.index;
            }
            else
            {
                // Fuse: first keeps the machine (double HP), second becomes wing bumper
                a.ticHostFused = true;
                a.maxHealth = Mathf.Max(1, a.maxHealth * 2);
                a.currentHealth = a.maxHealth;
                a.ticPartnerIndex = b.index;

                b.ticWingForm = true;
                b.ticPartnerIndex = a.index;
                b.ticHostFused = false;
            }

            // Extra Tics beyond 2 on same wall → also wing forms onto host
            for (int i = 2; i < group.Count; i++)
            {
                group[i].ticWingForm = true;
                group[i].ticPartnerIndex = a.index;
            }
        }
    }

    /// <summary>
    /// Arcade seats write a wall offset after Prepare. Two full machines on one wall
    /// must still be side-by-side or fused, never stacked on the same spot.
    /// Solo Tics keep the seat offset already assigned.
    /// </summary>
    public static void KeepTicsApart(List<Player> players, float fieldPlaySize)
    {
        if (players == null)
            return;

        for (int f = 0; f < 4; f++)
        {
            Facing facing = (Facing)f;
            List<Player> group = new List<Player>(2);
            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];
                if (p == null || p.arcadeOut || p.facing != facing || !IsTicPlayer(p))
                    continue;
                group.Add(p);
            }

            if (group.Count < 2)
                continue;

            group.Sort((a, b) =>
            {
                int c = a.ticWallSideOffset.CompareTo(b.ticWallSideOffset);
                return c != 0 ? c : a.index.CompareTo(b.index);
            });

            Player host = group[0];
            Player mate = group[1];
            bool alreadyFused = host.ticHostFused;
            for (int i = 0; i < group.Count; i++)
            {
                group[i].ticWingForm = false;
                group[i].ticHostFused = false;
                group[i].ticPartnerIndex = -1;
            }
            if (fieldPlaySize >= MinWallForSideBySide)
            {
                host.ticWallSideOffset = -SideBySideHalfGap;
                mate.ticWallSideOffset = SideBySideHalfGap;
                host.ticPartnerIndex = mate.index;
                mate.ticPartnerIndex = host.index;
            }
            else
            {
                host.ticHostFused = true;
                if (!alreadyFused)
                {
                    host.maxHealth = Mathf.Max(1, host.maxHealth * 2);
                    host.currentHealth = host.maxHealth;
                }
                host.ticPartnerIndex = mate.index;
                mate.ticWingForm = true;
                mate.ticPartnerIndex = host.index;
            }

            for (int i = 2; i < group.Count; i++)
            {
                group[i].ticWingForm = true;
                group[i].ticPartnerIndex = host.index;
            }
        }
    }
}
