using UnityEngine;
using UnityEngine.UI;

public class CharacterGrab : MonoBehaviour
{
    Database db;
    public Characters ch;

    Image background;
    Outline outline;
    RawImage icon;
    Text nameText;

    bool setup = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        db = Database.instance;
        Setup();
    }

    // Update is called once per frame
    void Update()
    {
        if(ch != null)
        {
            if(!setup)
            {
                UpdateLook();
            }

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
        background.color = ch.portraitColor;
        icon.texture = ch.icon;
        icon.color = (ch.active) ? Color.white : Color.black;
        nameText.text = ch.name;
        setup = true;
    }

    //Will update outline based on who has it selected
    void UpdateSelection()
    {
        if (db == null || outline == null || ch == null)
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
        Debug.Log("Setting up Player: " + player + " With character: " + ch.name);
        Player p = db.players.Find(x => x.index == player);

        if(p != null)
        {
            p.SetUpCharacter(ch);
            // Duplicate picks get a free distinct skin automatically
            PlayerSkin.AssignUniqueForCharacter(p, db);
            if (p.pso != null)
            {
                PlayerColors pc = PlayerSkin.GetPlayerColors(p, db);
                if (pc != null)
                    p.pso.SetCircleColor(pc);
            }
            if (db != null)
                db.RememberSelectedCharacter(p.index, ch.name);
        }
    }
}
