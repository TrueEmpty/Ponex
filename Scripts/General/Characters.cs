using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Characters
{
    public string name = "";
    public int maxHealth = 10;

    public float movementSpeed = 50;
    public float pushBack = 0;

    public Skill bump;
    public Skill super;
    public Skill dash;

    public ObjectInfo character = null;

    public ObjectInfo lifeline = null;
    public GameObject selector = null;
    public GameObject playerInfo = null;

    public string superName;
    public string superDescription;

    public Color portraitColor = Color.cyan;

    public Texture portrait;
    public Texture icon;

    public bool active = false;

    public Characters()
    {

    }

    public Characters(Characters p)
    {
        name = p.name;

        bump = new Skill(p.bump);
        super = new Skill(p.super);
        dash = new Skill(p.dash);

        character = new ObjectInfo(p.character);

        lifeline = new ObjectInfo(p.lifeline);
        selector = p.selector;
        playerInfo = p.playerInfo;

        superName = p.superName;
        superDescription = p.superDescription;

        portraitColor = p.portraitColor;

        portrait = p.portrait;
        icon = p.icon;

        active = p.active;
    }
}
