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

        bool selected = db.players != null && db.players.Exists(x => x != null && x.name == ch.name);
        outline.effectColor = selected ? Color.white : ch.portraitColor;
    }

    public void OnClick(int player)
    {
        Debug.Log("Setting up Player: " + player + " With character: " + ch.name);
        Player p = db.players.Find(x => x.index == player);

        if(p != null)
        {
            p.SetUpCharacter(ch);
            if (db != null)
                db.RememberSelectedCharacter(p.index, ch.name);
        }
    }
}
