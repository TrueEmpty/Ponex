using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coop same-wall Celarus family: half HP when she blocks a teammate's wall
/// lifeline, and one shared moon/sun for two Celarus (or Moon/Sun variants).
/// </summary>
public static class CelarusCoopLayout
{
    public static bool IsCelarusPlayer(Player p)
    {
        if (p == null)
            return false;
        if (IsCelarusName(p.name))
            return true;
        if (p.character != null && p.character.prefabs != null
            && p.character.prefabs.GetComponent<Celarus>() != null)
            return true;
        return false;
    }

    public static bool IsCelarusName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        return name.Equals("Celarus", System.StringComparison.OrdinalIgnoreCase)
            || name.Equals("Sunshine Celarus", System.StringComparison.OrdinalIgnoreCase)
            || name.Equals("Moonlight Celarus", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Muri / Nari sit in the field — Celarus does not cover their goal.</summary>
    public static bool IsUnblockedTeammate(Player p)
    {
        if (p == null)
            return true;
        if (IsCelarusPlayer(p))
            return true;

        if (!string.IsNullOrEmpty(p.name))
        {
            if (p.name.Equals("Muri", System.StringComparison.OrdinalIgnoreCase)
                || p.name.Equals("Nari", System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (p.character != null && p.character.prefabs != null)
        {
            if (p.character.prefabs.GetComponent<MuriMove>() != null)
                return true;
            if (p.character.prefabs.GetComponent<NariMove>() != null)
                return true;
        }

        return false;
    }

    public static CelarusPhaseLock ReadPhaseLock(Player p)
    {
        if (p != null && p.spawnedPlayer != null)
        {
            Celarus live = p.spawnedPlayer.GetComponent<Celarus>();
            if (live != null)
                return live.phaseLock;
        }

        if (p != null && p.character != null && p.character.prefabs != null)
        {
            Celarus prefab = p.character.prefabs.GetComponent<Celarus>();
            if (prefab != null)
                return prefab.phaseLock;
        }

        if (p != null && !string.IsNullOrEmpty(p.name))
        {
            if (p.name.IndexOf("Sunshine", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return CelarusPhaseLock.ForceSun;
            if (p.name.IndexOf("Moonlight", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return CelarusPhaseLock.ForceMoon;
        }

        return CelarusPhaseLock.Cycle;
    }

    /// <summary>Call once after roster bind, before the StartGame spawn loop.</summary>
    public static void Prepare(List<Player> players, Gametype gametype)
    {
        if (players == null)
            return;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == null)
                continue;
            players[i].celarusShareGuest = false;
            players[i].celarusShareHost = false;
            players[i].celarusShareHostIndex = -1;
        }

        if (gametype != Gametype.Coop)
            return;

        for (int f = 0; f < 4; f++)
        {
            Facing facing = (Facing)f;
            List<Player> wall = PlayersOnWall(players, facing);
            List<Player> celarus = new List<Player>(2);
            bool blocksTeammate = false;

            for (int i = 0; i < wall.Count; i++)
            {
                Player p = wall[i];
                if (IsCelarusPlayer(p))
                    celarus.Add(p);
                else if (!IsUnblockedTeammate(p))
                    blocksTeammate = true;
            }

            if (blocksTeammate)
            {
                for (int i = 0; i < celarus.Count; i++)
                    ApplyHalfHealth(celarus[i]);
            }

            if (celarus.Count < 2)
                continue;

            celarus.Sort((a, b) =>
            {
                int ha = HostPriority(ReadPhaseLock(a));
                int hb = HostPriority(ReadPhaseLock(b));
                if (ha != hb)
                    return ha.CompareTo(hb);
                int c = a.position.CompareTo(b.position);
                return c != 0 ? c : a.index.CompareTo(b.index);
            });

            Player host = celarus[0];
            host.celarusShareHost = true;
            host.celarusShareGuest = false;
            host.celarusShareHostIndex = host.index;

            for (int i = 1; i < celarus.Count; i++)
            {
                celarus[i].celarusShareHost = false;
                celarus[i].celarusShareGuest = true;
                celarus[i].celarusShareHostIndex = host.index;
            }
        }
    }

    /// <summary>After every Celarus on the wall has spawned, point guests at the host lifeline.</summary>
    public static void BindAfterSpawn(List<Player> players)
    {
        if (players == null)
            return;

        for (int i = 0; i < players.Count; i++)
        {
            Player p = players[i];
            if (p == null || !p.celarusShareHost || p.spawnedLifeline == null)
                continue;

            List<int> members = LivingShareIndexes(players, p);
            WireSharedDamage(p.spawnedLifeline, p.index, members);

            DestroyOnDeath death = p.spawnedLifeline.GetComponent<DestroyOnDeath>();
            if (death != null)
                death.enabled = false;

            for (int m = 0; m < members.Count; m++)
            {
                Player mate = FindByIndex(players, members[m]);
                if (mate == null || mate.spawnedPlayer == null)
                    continue;
                Celarus c = mate.spawnedPlayer.GetComponent<Celarus>();
                if (c != null)
                    c.BindSharedLifeline(p.spawnedLifeline, members);
            }
        }
    }

    public static void RefreshSharedDamage(Player host)
    {
        if (host == null || host.spawnedLifeline == null)
            return;
        Database db = Database.instance;
        List<Player> players = db != null ? db.players : null;
        WireSharedDamage(host.spawnedLifeline, host.index, LivingShareIndexes(players, host));
    }

    public static void OnShareMemberDied(Player dead)
    {
        if (dead == null)
            return;

        Database db = Database.instance;
        if (db == null || db.players == null)
            return;

        Player host = FindByIndex(db.players, dead.celarusShareHostIndex >= 0
            ? dead.celarusShareHostIndex
            : dead.index);
        if (host == null)
            host = dead;

        List<Player> living = new List<Player>(2);
        CollectLivingShare(db.players, host, living);
        if (dead.currentHealth > 0 && !living.Contains(dead))
            living.Add(dead);
        living.RemoveAll(p => p == null || p.currentHealth <= 0 || p == dead);

        GameObject lifeline = host.spawnedLifeline;
        if (lifeline == null)
        {
            for (int i = 0; i < db.players.Count; i++)
            {
                Player p = db.players[i];
                if (p != null && p.spawnedLifeline != null && SameShareGroup(p, host))
                {
                    lifeline = p.spawnedLifeline;
                    break;
                }
            }
        }

        if (living.Count == 0)
        {
            if (lifeline != null)
                Object.Destroy(lifeline);
            if (host != null)
                host.spawnedLifeline = null;
            return;
        }

        Player newHost = living[0];
        for (int i = 0; i < living.Count; i++)
        {
            if (HostPriority(ReadPhaseLock(living[i])) < HostPriority(ReadPhaseLock(newHost)))
                newHost = living[i];
        }

        if (lifeline != null)
        {
            if (host != newHost)
            {
                host.spawnedLifeline = null;
                newHost.spawnedLifeline = lifeline;
            }

            newHost.celarusShareHost = true;
            newHost.celarusShareGuest = false;
            newHost.celarusShareHostIndex = newHost.index;

            PlayerGrab rootGrab = lifeline.GetComponent<PlayerGrab>();
            if (rootGrab != null)
                rootGrab.playerIndex = newHost.index;

            PlayerGrab[] grabs = lifeline.GetComponentsInChildren<PlayerGrab>(true);
            for (int g = 0; g < grabs.Length; g++)
            {
                if (grabs[g] != null)
                    grabs[g].playerIndex = newHost.index;
            }
        }

        List<int> members = new List<int>(living.Count);
        for (int i = 0; i < living.Count; i++)
        {
            living[i].celarusShareHostIndex = newHost.index;
            living[i].celarusShareHost = living[i] == newHost;
            living[i].celarusShareGuest = living[i] != newHost;
            members.Add(living[i].index);
        }

        if (lifeline != null)
            WireSharedDamage(lifeline, newHost.index, members);

        bool sunLeft = AnyPhase(living, CelarusPhaseLock.ForceSun);
        bool moonLeft = AnyPhase(living, CelarusPhaseLock.ForceMoon);
        bool cycleLeft = AnyPhase(living, CelarusPhaseLock.Cycle);

        for (int i = 0; i < living.Count; i++)
        {
            if (living[i].spawnedPlayer == null)
                continue;
            Celarus c = living[i].spawnedPlayer.GetComponent<Celarus>();
            if (c == null)
                continue;
            c.BindSharedLifeline(lifeline, members);
            if (!cycleLeft && moonLeft && !sunLeft)
                c.LockShareToMoon();
        }
    }

    static int HostPriority(CelarusPhaseLock lockMode)
    {
        if (lockMode == CelarusPhaseLock.ForceSun)
            return 0;
        if (lockMode == CelarusPhaseLock.Cycle)
            return 1;
        return 2;
    }

    static void ApplyHalfHealth(Player p)
    {
        if (p == null || p.maxHealth <= 1)
            return;
        int half = Mathf.Max(1, Mathf.CeilToInt(p.maxHealth / 2f));
        p.maxHealth = half;
        p.currentHealth = half;
    }

    static List<Player> PlayersOnWall(List<Player> players, Facing facing)
    {
        List<Player> wall = new List<Player>(4);
        for (int i = 0; i < players.Count; i++)
        {
            Player p = players[i];
            if (p == null || p.facing != facing)
                continue;
            wall.Add(p);
        }
        return wall;
    }

    static List<int> LivingShareIndexes(List<Player> players, Player host)
    {
        List<int> ids = new List<int>(2);
        if (players == null || host == null)
            return ids;
        for (int i = 0; i < players.Count; i++)
        {
            Player p = players[i];
            if (p == null || p.currentHealth <= 0)
                continue;
            if (!SameShareGroup(p, host))
                continue;
            ids.Add(p.index);
        }
        if (!ids.Contains(host.index) && host.currentHealth > 0)
            ids.Add(host.index);
        return ids;
    }

    static void CollectLivingShare(List<Player> players, Player host, List<Player> into)
    {
        if (players == null || host == null)
            return;
        for (int i = 0; i < players.Count; i++)
        {
            Player p = players[i];
            if (p == null || p.currentHealth <= 0)
                continue;
            if (!SameShareGroup(p, host))
                continue;
            if (!into.Contains(p))
                into.Add(p);
        }
    }

    static bool SameShareGroup(Player a, Player b)
    {
        if (a == null || b == null)
            return false;
        if (a.facing != b.facing)
            return false;
        if (!IsCelarusPlayer(a) || !IsCelarusPlayer(b))
            return false;
        int ha = a.celarusShareHostIndex >= 0 ? a.celarusShareHostIndex : a.index;
        int hb = b.celarusShareHostIndex >= 0 ? b.celarusShareHostIndex : b.index;
        return ha == hb || a.index == hb || b.index == ha;
    }

    static bool AnyPhase(List<Player> living, CelarusPhaseLock lockMode)
    {
        for (int i = 0; i < living.Count; i++)
        {
            if (ReadPhaseLock(living[i]) == lockMode)
                return true;
        }
        return false;
    }

    static Player FindByIndex(List<Player> players, int index)
    {
        if (players == null || index < 0)
            return null;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && players[i].index == index)
                return players[i];
        }
        return null;
    }

    static void WireSharedDamage(GameObject lifeline, int primaryIndex, List<int> members)
    {
        if (lifeline == null)
            return;

        List<int> extras = new List<int>(2);
        if (members != null)
        {
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i] != primaryIndex)
                    extras.Add(members[i]);
            }
        }

        DamageOnTagHit[] hits = lifeline.GetComponentsInChildren<DamageOnTagHit>(true);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
                continue;
            hits[i].alsoDamagePlayerIndexes = extras.Count > 0 ? extras.ToArray() : null;
            PlayerGrab grab = hits[i].GetComponent<PlayerGrab>();
            if (grab != null)
                grab.playerIndex = primaryIndex;
        }

        PlayerGrab[] grabs = lifeline.GetComponentsInChildren<PlayerGrab>(true);
        for (int i = 0; i < grabs.Length; i++)
        {
            if (grabs[i] != null)
                grabs[i].playerIndex = primaryIndex;
        }
    }
}
