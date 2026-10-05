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

    static readonly Color RandomGray = new Color(0.4f, 0.4f, 0.4f, 1f);
    static readonly Color RandomMark = new Color(0.72f, 0.72f, 0.72f, 1f);
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
            if (background != null)
                background.color = RandomGray;

            if (icon != null)
            {
                icon.texture = null;
                icon.enabled = false;
            }

            if (nameText != null)
            {
                nameText.text = "?";
                nameText.color = RandomMark;
                nameText.alignment = TextAnchor.MiddleCenter;
                nameText.resizeTextForBestFit = true;
                nameText.resizeTextMinSize = 20;
                nameText.resizeTextMaxSize = 72;

                RectTransform rt = nameText.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            if (outline != null)
                outline.effectColor = new Color(0.55f, 0.55f, 0.55f, 1f);

            if (bi != null)
                bi.Activate();

            setup = true;
            UpdateSelection();
            return;
        }

        if (field == null)
            return;

        if (background != null)
            background.color = Color.white;

        if (icon != null)
        {
            icon.enabled = true;
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
        if (background == null)
            return;

        if (isRandom)
        {
            bool selected = db != null && db.selectedField < 0;
            background.color = selected ? SelectedGreen : RandomGray;
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
            db.selectedField = -1;
        }
        else
        {
            if (field == null || !field.active || fieldIndex < 0)
                return;
            db.selectedField = fieldIndex;
        }

        FieldSelect fs = FieldSelect.instance;
        if (fs != null)
        {
            fs.RefreshAllSelectionLooks();
            fs.UpdateSelectedPortrait();
        }
    }
}
