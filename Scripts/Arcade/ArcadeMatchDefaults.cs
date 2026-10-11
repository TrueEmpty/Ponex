using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Premade arcade rounds, levels 1 through 10, for solo and coop.
/// Coop rosters are the solo fight with the opponent count doubled.
/// </summary>
public static class ArcadeMatchDefaults
{
    public static List<ArcadeMatch> CreateAll()
    {
        List<ArcadeMatch> list = new List<ArcadeMatch>(20);
        AddSolo(list);
        AddCoop(list);
        return list;
    }

    public static List<ArcadeMatch> Matching(bool coop, int level)
    {
        ArcadeMatch[] assets = Resources.LoadAll<ArcadeMatch>("Arcade");
        List<ArcadeMatch> found = new List<ArcadeMatch>();
        if (assets != null)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                ArcadeMatch match = assets[i];
                if (match != null && match.coop == coop && match.challengeLevel == level)
                    found.Add(match);
            }
        }

        if (found.Count > 0)
            return found;

        List<ArcadeMatch> made = CreateAll();
        for (int i = 0; i < made.Count; i++)
        {
            if (made[i].coop == coop && made[i].challengeLevel == level)
                found.Add(made[i]);
        }
        return found;
    }

    static void AddSolo(List<ArcadeMatch> list)
    {
        list.Add(Make("First Serve", 1, false, "Test Zone", Balls("Basic Ball"), true, 1f, 1f, false, false, 0f, false,
            HumanBottom(),
            Foe("Test", Facing.Down, 2, 0f)));

        list.Add(Make("Harbor Wall", 2, false, "Deep Harbor", Balls("Basic Ball"), true, 1f, 1f, false, false, 0f, false,
            HumanBottom(),
            Foe("Garmen", Facing.Down, 2, 0f)));

        list.Add(Make("Crossfire", 3, false, "Kingdom of Nuoryn", Balls("Basic Ball"), true, 1f, 1f, false, false, 0f, false,
            HumanBottom(),
            Foe("Trigger", Facing.Right, 3, 0f),
            Foe("Gaurd", Facing.Left, 4, 0f)));

        list.Add(Make("Dune Line", 4, false, "Marajie Dessert", Balls("Basic Ball"), true, 1f, 1f, false, false, 0f, false,
            HumanBottom(),
            Foe("Uwrenmaw", Facing.Down, 2, -2.4f),
            Foe("Tic", Facing.Down, 2, 2.4f)));

        list.Add(Make("Jungle Split", 5, false, "Deep Jungle", Balls("Basic Ball", "Mine Ball"), true, 1f, 1f, false, false, 0f, false,
            HumanBottom(),
            Foe("Muri", Facing.Right, 3, 0f),
            Foe("Iyolit", Facing.Left, 4, 0f)));

        list.Add(Make("Heavy Hands", 6, false, "Test Zone", Balls("Size Ball"), true, 2f, 1.15f, false, false, 0f, false,
            HumanBottom(),
            Foe("Bahrue", Facing.Down, 2, -2.2f),
            Foe("Yuotay", Facing.Down, 2, 2.2f)));

        list.Add(Make("Moon Orbit", 7, false, "Nari's Void Orbit", Balls("Orbit Ball"), true, 1f, 1f, false, false, 0f, false,
            HumanBottom(),
            Foe("Celarus", Facing.Down, 2, -2.2f),
            Foe("Moonlight Celarus", Facing.Down, 2, 2.2f)));

        list.Add(Make("Second Wind", 8, false, "Deep Harbor", Balls("Basic Ball", "Frost Ball"), true, 1f, 1f, true, false, 0f, false,
            HumanBottom(),
            Foe("Garmen", Facing.Down, 2, -3.2f),
            Foe("Master Pong", Facing.Down, 2, 0f),
            Foe("Trigger", Facing.Down, 2, 3.2f)));

        list.Add(Make("Nari Burst", 9, false, "Nari's Void Orbit", Balls("Clone Ball"), true, 1f, 1.2f, false, false, 0f, false,
            HumanBottom(),
            Line("Nari", 3, Facing.Right, 3, 3.2f)));

        list.Add(Make("Unbroken", 10, false, "Kingdom of Nuoryn", Balls("Speed Ball"), true, 1.5f, 1f, false, true, 40f, false,
            HumanBottom(),
            Foe("Gaurd", Facing.Down, 2, -3.2f),
            Foe("Tic", Facing.Down, 2, 0f),
            Foe("Uwrenmaw", Facing.Down, 2, 3.2f)));
    }

    static void AddCoop(List<ArcadeMatch> list)
    {
        list.Add(Make("First Serve", 1, true, "Test Zone", Balls("Basic Ball"), true, 1f, 1f, false, false, 0f, false,
            HumansTogether(),
            Line("Test", 2, Facing.Down, 2, 2.6f)));

        list.Add(Make("Harbor Wall", 2, true, "Deep Harbor", Balls("Basic Ball"), true, 1f, 1f, false, false, 0f, false,
            HumansTogether(),
            Line("Garmen", 2, Facing.Down, 2, 2.6f)));

        list.Add(Make("Crossfire", 3, true, "Kingdom of Nuoryn", Balls("Basic Ball"), true, 1f, 1f, false, false, 0f, true,
            HumansSplit(),
            Foe("Gaurd", Facing.Left, 3, -2.2f),
            Foe("Muri", Facing.Left, 3, 2.2f),
            Foe("Trigger", Facing.Right, 4, -2.2f),
            Foe("Yuotay", Facing.Right, 4, 2.2f)));

        list.Add(Make("Dune Line", 4, true, "Marajie Dessert", Balls("Basic Ball", "Mine Ball"), true, 1f, 1f, false, false, 0f, false,
            HumansTogether(),
            Foe("Uwrenmaw", Facing.Down, 2, -3.6f),
            Foe("Uwrenmaw", Facing.Down, 2, -1.2f),
            Foe("Tic", Facing.Down, 2, 1.2f),
            Foe("Tic", Facing.Down, 2, 3.6f)));

        list.Add(Make("Jungle Split", 5, true, "Deep Jungle", Balls("Basic Ball", "Mine Ball"), true, 1f, 1f, false, false, 0f, false,
            HumansTogether(),
            Foe("Muri", Facing.Right, 3, -2.2f),
            Foe("Muri", Facing.Right, 3, 2.2f),
            Foe("Iyolit", Facing.Left, 4, -2.2f),
            Foe("Iyolit", Facing.Left, 4, 2.2f)));

        list.Add(Make("Heavy Hands", 6, true, "Test Zone", Balls("Size Ball"), true, 2f, 1.15f, false, false, 0f, false,
            HumansTogether(),
            Foe("Bahrue", Facing.Down, 2, -3.6f),
            Foe("Bahrue", Facing.Down, 2, -1.2f),
            Foe("Yuotay", Facing.Down, 2, 1.2f),
            Foe("Yuotay", Facing.Down, 2, 3.6f)));

        list.Add(Make("Moon Orbit", 7, true, "Nari's Void Orbit", Balls("Orbit Ball"), true, 1f, 1f, false, false, 0f, false,
            HumansTogether(),
            Foe("Celarus", Facing.Down, 2, -3.6f),
            Foe("Moonlight Celarus", Facing.Down, 2, -1.2f),
            Foe("Sunshine Celarus", Facing.Down, 2, 1.2f),
            Foe("Celarus", Facing.Down, 2, 3.6f)));

        list.Add(Make("Second Wind", 8, true, "Deep Harbor", Balls("Basic Ball", "Frost Ball"), true, 1f, 1f, true, false, 0f, false,
            HumansTogether(),
            Line(new[] { "Garmen", "Master Pong", "Trigger", "Gaurd", "Tic", "Bahrue" }, Facing.Down, 2, 1.7f)));

        list.Add(Make("Nari Burst", 9, true, "Nari's Void Orbit", Balls("Clone Ball"), true, 1f, 1.25f, false, false, 0f, false,
            HumansTogether(),
            Line("Nari", 6, Facing.Right, 3, 1.8f)));

        list.Add(Make("Unbroken", 10, true, "Kingdom of Nuoryn", Balls("Speed Ball"), true, 1.5f, 1f, false, true, 50f, false,
            HumansTogether(),
            Line(new[] { "Gaurd", "Tic", "Uwrenmaw", "Master Pong", "Bahrue", "Trigger" }, Facing.Down, 2, 1.7f)));
    }

    static ArcadeSeat HumanBottom()
    {
        return ArcadeSeat.Human(0, Facing.Up, 1, 0f);
    }

    static ArcadeSeat[] HumansTogether()
    {
        return new[]
        {
            ArcadeSeat.Human(0, Facing.Up, 1, -1.7f),
            ArcadeSeat.Human(1, Facing.Up, 1, 1.7f)
        };
    }

    static ArcadeSeat[] HumansSplit()
    {
        return new[]
        {
            ArcadeSeat.Human(0, Facing.Up, 1, 0f),
            ArcadeSeat.Human(1, Facing.Down, 2, 0f)
        };
    }

    static ArcadeSeat Foe(string name, Facing facing, int team, float offset)
    {
        return ArcadeSeat.Enemy(name, facing, team, offset);
    }

    static ArcadeSeat[] Line(string name, int count, Facing facing, int team, float gap)
    {
        string[] names = new string[count];
        for (int i = 0; i < count; i++)
            names[i] = name;
        return Line(names, facing, team, gap);
    }

    static ArcadeSeat[] Line(string[] names, Facing facing, int team, float gap)
    {
        ArcadeSeat[] seats = new ArcadeSeat[names.Length];
        float start = -(names.Length - 1) * gap * 0.5f;
        for (int i = 0; i < names.Length; i++)
            seats[i] = ArcadeSeat.Enemy(names[i], facing, team, start + i * gap);
        return seats;
    }

    static string[] Balls(params string[] names)
    {
        return names;
    }

    static ArcadeMatch Make(
        string matchName,
        int level,
        bool coop,
        string field,
        string[] balls,
        bool hazards,
        float damage,
        float size,
        bool regen,
        bool infinite,
        float survive,
        bool split,
        params object[] seatGroups)
    {
        ArcadeMatch match = ScriptableObject.CreateInstance<ArcadeMatch>();
        match.name = (coop ? "Coop " : "Solo ") + level + " " + matchName;
        match.matchName = matchName;
        match.challengeLevel = level;
        match.coop = coop;
        match.fieldName = field;
        match.ballNames = balls;
        match.hazards = hazards;
        match.ballDamageScale = damage;
        match.ballSizeScale = size;
        match.enemiesRegenerate = regen;
        match.regenInterval = 3.5f;
        match.regenAmount = 1;
        match.enemiesInfiniteHealth = infinite;
        match.surviveSeconds = survive;
        match.coopSplit = split;

        List<ArcadeSeat> seats = new List<ArcadeSeat>();
        for (int i = 0; i < seatGroups.Length; i++)
        {
            if (seatGroups[i] is ArcadeSeat one)
                seats.Add(one);
            else if (seatGroups[i] is ArcadeSeat[] many)
                seats.AddRange(many);
        }
        match.seats = seats.ToArray();
        return match;
    }
}
