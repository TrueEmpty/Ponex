using UnityEngine;
using UnityEngine.UI;

public class CharacterGrab : MonoBehaviour
{
    Database db;
    public Characters ch;
    public bool isRandom = false;

    Image background;
    Outline outline;
    RawImage icon;
    Text nameText;

    bool setup = false;
    static readonly Color RandomGray = new Color(0.4f, 0.4f, 0.4f, 1f);
    static readonly Color RandomMark = new Color(0.72f, 0.72f, 0.72f, 1f);

    void Start()
    {
        db = Database.instance;
        Setup();
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

        if (ch != null)
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
        icon = transform.GetChild(0).GetComponent<RawImage>();
        nameText = transform.GetChild(1).GetComponent<Text>();
    }

    void UpdateLook()
    {
        if (isRandom)
        {
            background.color = RandomGray;
            if (outline != null)
                outline.effectColor = new Color(0.55f, 0.55f, 0.55f, 1f);

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
                rt.pivot = new Vector2(0.5f, 0.5f);
            }

            setup = true;
            return;
        }

        background.color = ch.portraitColor;
        if (icon != null)
        {
            icon.enabled = true;
            icon.texture = ch.icon;
            icon.color = ch.active ? Color.white : Color.black;
        }
        nameText.text = ch.name;
        nameText.color = Color.white;
        setup = true;
    }

    void UpdateSelection()
    {
        if (db == null || outline == null)
            return;

        if (isRandom)
        {
            outline.effectColor = new Color(0.55f, 0.55f, 0.55f, 1f);
            return;
        }

        if (ch == null)
            return;

        Player holder = db.players != null
            ? db.players.Find(x => x != null && x.name == ch.name)
            : null;

        if (holder != null)
            outline.effectColor = PlayerSkin.GetColor(holder, db);
        else
            outline.effectColor = ch.portraitColor;
    }

    public void OnClick(int player)
    {
        Player p = db.players.Find(x => x.index == player);
        if (p == null)
            return;

        Characters pick = ch;
        if (isRandom)
        {
            pick = db.RandomCharacter();
            if (pick == null)
            {
                Debug.LogWarning("Random character pick failed — no active characters.");
                return;
            }
            Debug.Log("Setting up Player: " + player + " With random character: " + pick.name);
        }
        else
        {
            Debug.Log("Setting up Player: " + player + " With character: " + pick.name);
        }

        p.SetUpCharacter(pick);
        PlayerSkin.AssignUniqueForCharacter(p, db);
        if (p.pso != null)
        {
            PlayerColors pc = PlayerSkin.GetPlayerColors(p, db);
            if (pc != null)
                p.pso.SetCircleColor(pc);
        }
        if (db != null)
            db.RememberSelectedCharacter(p.index, pick.name);
    }
}
