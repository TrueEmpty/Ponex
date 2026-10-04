using UnityEngine;

[CreateAssetMenu(fileName = "Character", menuName = "Ponex/Character Data", order = 0)]
public class CharacterData : ScriptableObject
{
    [Tooltip("Lower numbers appear first in Character Select.")]
    public int rosterOrder = 0;

    public string characterName = "";
    public int maxHealth = 10;

    public float movementSpeed = 5;
    public float pushBack = 0;

    public bool ignoreFacing = false;

    public Skill bump = new Skill();
    public Skill super = new Skill();
    public Skill dash = new Skill();

    public ObjectInfo character = new ObjectInfo();
    public ObjectInfo lifeline = new ObjectInfo();
    public GameObject selector;
    public GameObject playerInfo;

    public string superName;
    [TextArea(2, 5)]
    public string superDescription;

    public Color portraitColor = Color.cyan;

    public Texture portrait;
    public Texture icon;

    public bool active = true;

    public Characters ToCharacters()
    {
        Characters c = new Characters();
        c.name = characterName;
        c.maxHealth = maxHealth;
        c.movementSpeed = movementSpeed;
        c.pushBack = pushBack;
        c.ignoreFacing = ignoreFacing;
        c.bump = new Skill(bump);
        c.super = new Skill(super);
        c.dash = new Skill(dash);
        c.character = character != null ? new ObjectInfo(character) : new ObjectInfo();
        c.lifeline = lifeline != null ? new ObjectInfo(lifeline) : new ObjectInfo();
        c.selector = selector;
        c.playerInfo = playerInfo;
        c.superName = superName;
        c.superDescription = superDescription;
        c.portraitColor = portraitColor;
        c.portrait = portrait;
        c.icon = icon;
        c.active = active;
        return c;
    }
}
