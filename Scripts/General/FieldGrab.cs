using UnityEngine;
using UnityEngine.UI;

public class FieldGrab : MonoBehaviour
{
    Database db;
    public Field field;
    public int fieldIndex = -1;

    Image background;
    Outline outline;
    RawImage icon;
    Text nameText;
    ButtonInteraction bi;
    bool setup;

    void Start()
    {
        db = Database.instance;
        Setup();
        if (field != null)
            UpdateLook();
    }

    void Update()
    {
        if (field != null)
        {
            if (!setup)
                UpdateLook();

            UpdateSelection();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Setup()
    {
        background = GetComponent<Image>();
        outline = GetComponent<Outline>();
        bi = GetComponent<ButtonInteraction>();
        if (transform.childCount > 0)
            icon = transform.GetChild(0).GetComponent<RawImage>();
        if (transform.childCount > 1)
            nameText = transform.GetChild(1).GetComponent<Text>();
    }

    public void Bind(Field f, int index)
    {
        field = f;
        fieldIndex = index;
        setup = false;
        if (background == null)
            Setup();
        UpdateLook();
    }

    void UpdateLook()
    {
        if (field == null)
            return;

        if (background == null)
            Setup();

        if (background != null)
            background.color = Color.white;

        if (icon != null)
        {
            icon.texture = field.icon;
            icon.color = field.active ? Color.white : Color.black;
        }

        if (nameText != null)
        {
            nameText.text = field.name;
            nameText.color = Color.white;
        }

        if (bi != null)
        {
            if (field.active) bi.Activate();
            else bi.Deactivate();
        }

        setup = true;
        UpdateSelection();
    }

    void UpdateSelection()
    {
        RefreshSelectedLook();
    }

    public void RefreshSelectedLook()
    {
        if (db == null) db = Database.instance;
        if (background == null || field == null || !field.active)
            return;

        bool selected = fieldIndex >= 0 && db != null && db.selectedField == fieldIndex;
        background.color = selected ? new Color(0.2f, 0.85f, 0.35f, 1f) : Color.white;
    }

    public void OnClick(int player)
    {
        if (field == null || !field.active || fieldIndex < 0)
            return;

        if (db == null) db = Database.instance;
        db.selectedField = fieldIndex;

        FieldSelect fs = FieldSelect.instance;
        if (fs != null)
        {
            fs.RefreshAllSelectionLooks();
            fs.UpdateSelectedPortrait();
        }
    }
}
