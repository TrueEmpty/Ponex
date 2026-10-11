using System;
using UnityEngine;

/// <summary>
/// One premade arcade round. Challenge level 1 is the first match and 10 is the last.
/// Solo and coop each have their own set. Coop rounds are written with the larger roster.
/// </summary>
[CreateAssetMenu(fileName = "Arcade Match", menuName = "Ponex/Arcade Match", order = 20)]
public class ArcadeMatch : ScriptableObject
{
    public string matchName = "Match";
    public int challengeLevel = 1;
    public bool coop;

    public string fieldName = "Test Zone";
    public string[] ballNames = { "Basic Ball" };

    public ArcadeSeat[] seats;

    [Header("Modifiers")]
    public float ballDamageScale = 1f;
    public float ballSizeScale = 1f;
    public bool hazards = true;
    public bool enemiesRegenerate;
    public float regenInterval = 4f;
    public int regenAmount = 1;
    public bool enemiesInfiniteHealth;
    public float surviveSeconds;
    /// <summary>Coop humans stand on opposite sides and different teams. Either one can finish the round, and if both are the last ones standing they win without fighting each other.</summary>
    public bool coopSplit;
}

[Serializable]
public class ArcadeSeat
{
    public string characterName;
    /// <summary>0 or 1 for a human seat. -1 is an enemy.</summary>
    public int humanSlot = -1;
    public Facing facing = Facing.Down;
    public int team = 2;
    public float wallOffset;

    public bool IsHuman => humanSlot >= 0;

    public static ArcadeSeat Human(int slot, Facing facing, int team, float wallOffset)
    {
        return new ArcadeSeat
        {
            characterName = "",
            humanSlot = slot,
            facing = facing,
            team = team,
            wallOffset = wallOffset
        };
    }

    public static ArcadeSeat Enemy(string characterName, Facing facing, int team, float wallOffset)
    {
        return new ArcadeSeat
        {
            characterName = characterName,
            humanSlot = -1,
            facing = facing,
            team = team,
            wallOffset = wallOffset
        };
    }
}
