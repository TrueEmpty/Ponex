using UnityEngine;

[CreateAssetMenu(fileName = "Field", menuName = "Ponex/Field Data", order = 1)]
public class FieldData : ScriptableObject
{
    [Tooltip("Lower numbers appear first in Field Select.")]
    public int rosterOrder = 0;

    public Field field = new Field(true);

    [Tooltip("Loops while this level is running. Music bus.")]
    public AudioClip levelMusic;

    [Tooltip("Replaces the generated background when set.")]
    public Material groundMaterial;

    [Tooltip("Albedo for this level's border walls.")]
    public Texture wallTexture;

    [Tooltip("Hazard placed around the level (Marajie sand dunes).")]
    public GameObject hazardPrefab;

    public string ResolvedName()
    {
        if (field != null && !string.IsNullOrEmpty(field.name))
            return field.name.Trim();
        return name;
    }

    public Field ToField()
    {
        if (field == null)
            field = new Field(true);
        if (string.IsNullOrEmpty(field.name))
            field.name = name;
        return new Field(field);
    }
}
