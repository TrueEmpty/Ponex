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
    ButtonInteraction bi;

    bool setup = false;
    static readonly Color RandomGray = new Color(0.4f, 0.4f, 0.4f, 1f);
    static readonly Color RandomMark = new Color(0.72f, 0.72f, 0.72f, 1f);
    string lastSelectionKey;
    float nextSelectionCheck;

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
        bi = GetComponent<ButtonInteraction>();
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

        if (bi != null)
        {
            if (ch.active) bi.Activate();
            else bi.Deactivate();
        }

        setup = true;
    }

    void UpdateSelection()
    {
        if (db == null || outline == null)
            return;

        // Stagger selection refreshes — was Find+GetColor every frame × every grab
        if (Time.unscaledTime < nextSelectionCheck)
            return;
        nextSelectionCheck = Time.unscaledTime + 0.1f;

        if (isRandom)
        {
            Player randomHolder = null;
            if (db.players != null)
            {
                for (int i = 0; i < db.players.Count; i++)
                {
                    Player x = db.players[i];
                    if (x != null && x.wantRandomCharacter)
                    {
                        randomHolder = x;
                        break;
                    }
                }
            }
            string key = randomHolder != null
                ? "r|" + randomHolder.index + "|" + randomHolder.skinColorIndex
                : "r|none";
            if (key == lastSelectionKey)
                return;
            lastSelectionKey = key;
            outline.effectColor = randomHolder != null
                ? PlayerSkin.GetColor(randomHolder, db)
                : new Color(0.55f, 0.55f, 0.55f, 1f);
            return;
        }

        if (ch == null)
            return;

        Player holder = null;
        if (db.players != null)
        {
            for (int i = 0; i < db.players.Count; i++)
            {
                Player x = db.players[i];
                if (x != null && !x.wantRandomCharacter && x.name == ch.name)
                {
                    holder = x;
                    break;
                }
            }
        }

        string selKey = holder != null
            ? "h|" + holder.index + "|" + holder.skinColorIndex + "|" + ch.name
            : "h|none|" + ch.name;
        if (selKey == lastSelectionKey)
            return;
        lastSelectionKey = selKey;

        outline.effectColor = holder != null
            ? PlayerSkin.GetColor(holder, db)
            : ch.portraitColor;
    }

    public void OnClick(int player)
    {
        Player p = db.players.Find(x => x.index == player);
        if (p == null)
            return;

        if (isRandom)
        {
            if (db.RandomCharacter() == null)
            {
                Debug.LogWarning("Random character pick failed — no active characters.");
                return;
            }

            p.SetRandomCharacterPending();
            if (p.pso != null)
            {
                PlayerColors pc = PlayerSkin.GetPlayerColors(p, db);
                if (pc != null)
                    p.pso.SetCircleColor(pc);
            }
            db.RememberSelectedCharacter(p.index, Database.RandomCharacterSentinel);
            Debug.Log("Player " + player + " selected Random character (rolls at match start).");
            return;
        }

        if (ch == null || !ch.active)
            return;

        p.wantRandomCharacter = false;
        Debug.Log("Setting up Player: " + player + " With character: " + ch.name);

        p.SetUpCharacter(ch);
        PlayerSkin.AssignUniqueForCharacter(p, db);
        if (p.pso != null)
        {
            PlayerColors pc = PlayerSkin.GetPlayerColors(p, db);
            if (pc != null)
                p.pso.SetCircleColor(pc);
        }
        db.RememberSelectedCharacter(p.index, ch.name);
    }
}
