using UnityEngine;
using UnityEngine.UI;

public class FieldGrab : MonoBehaviour
{
    Database db;
    public Field field;
    public int fieldIndex = -1;
    public bool isRandom = false;

    Image background;
    Outline outline;
    RawImage icon;
    Text nameText;
    ButtonInteraction bi;
    bool setup;

    static readonly Color SelectedGreen = new Color(0.2f, 0.85f, 0.35f, 1f);

    void Start()
    {
        db = Database.instance;
        Setup();
        if (isRandom || field != null)
            UpdateLook();
    }

    void Update()
    {
        if (isRandom)
        {
            if (!setup)
                UpdateLook();
            UpdateSelection();
            return;
        }

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
        isRandom = false;
        field = f;
        fieldIndex = index;
        setup = false;
        if (background == null)
            Setup();
        UpdateLook();
    }

    public void BindRandom()
    {
        isRandom = true;
        field = null;
        fieldIndex = -1;
        setup = false;
        if (background == null)
            Setup();
        UpdateLook();
    }

    void UpdateLook()
    {
        if (background == null)
            Setup();

        if (isRandom)
        {
            SelectorTileVisual.ApplyRandom(background, outline, icon, nameText, bi);

            setup = true;
            UpdateSelection();
            return;
        }

        if (field == null)
            return;

        SelectorTileVisual.ApplyEntry(
            background, Color.white, icon, field.icon, field.active,
            nameText, field.name, bi);

        setup = true;
        UpdateSelection();
    }

    int lastSelectedField = int.MinValue;

    void UpdateSelection()
    {
        if (db == null) db = Database.instance;
        int sel = db != null ? db.selectedField : int.MinValue;
        if (sel == lastSelectedField)
            return;
        lastSelectedField = sel;
        RefreshSelectedLook();
    }

    public void RefreshSelectedLook()
    {
        if (db == null) db = Database.instance;
        if (background == null)
            return;

        if (isRandom)
        {
            bool selected = db != null && db.selectedField < 0;
            background.color = selected ? SelectedGreen : SelectorTileVisual.RandomBackground;
            return;
        }

        if (field == null || !field.active)
            return;

        bool isSelected = fieldIndex >= 0 && db != null && db.selectedField == fieldIndex;
        background.color = isSelected ? SelectedGreen : Color.white;
    }

    public void OnClick(int player)
    {
        if (db == null) db = Database.instance;
        if (db == null)
            return;

        if (isRandom)
        {
            db.SetSelectedFieldPersistent(-1);
        }
        else
        {
            if (field == null || !field.active || fieldIndex < 0)
                return;
            db.SetSelectedFieldPersistent(fieldIndex);
        }

        FieldSelect fs = FieldSelect.instance;
        if (fs != null)
        {
            fs.RefreshAllSelectionLooks();
            fs.UpdateSelectedPortrait();
        }
    }
}
