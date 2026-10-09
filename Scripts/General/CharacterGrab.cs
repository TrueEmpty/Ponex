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
    int lastHolderIndex = int.MinValue;
    int lastHolderSkin = int.MinValue;
    float nextSelectionCheck;

    void Start()
    {
        db = Database.instance;
        Setup();
        // Spread roster refreshes across frames instead of spiking all tiles together.
        nextSelectionCheck = Time.unscaledTime + (transform.GetSiblingIndex() % 8) * 0.0125f;
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
            SelectorTileVisual.ApplyRandom(background, outline, icon, nameText, bi);
            setup = true;
            return;
        }

        SelectorTileVisual.ApplyEntry(
            background, ch.portraitColor, icon, ch.icon, ch.active,
            nameText, ch.name, bi);

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
            int holderIndex = randomHolder != null ? randomHolder.index : -1;
            int holderSkin = randomHolder != null ? randomHolder.skinColorIndex : -1;
            if (holderIndex == lastHolderIndex && holderSkin == lastHolderSkin)
                return;
            lastHolderIndex = holderIndex;
            lastHolderSkin = holderSkin;
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

        int selectedIndex = holder != null ? holder.index : -1;
        int selectedSkin = holder != null ? holder.skinColorIndex : -1;
        if (selectedIndex == lastHolderIndex && selectedSkin == lastHolderSkin)
            return;
        lastHolderIndex = selectedIndex;
        lastHolderSkin = selectedSkin;

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
