using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerGrab))]
public class GaurdInfo : MonoBehaviour
{
    //Image font used was Cooper Black in word with both Bold and Italizied
    PlayerGrab pg;

    public RawImage background;
    public Text playerName;
    public Text characterName;
    public Text health;
    public Text currency;

    [Header("Selectors")]
    public RawImage reflector_Im;
    public Outline reflector_Outline;
    public RawImage attacker_Im;
    public Outline attacker_Outline;
    public RawImage patroller_Im;
    public Outline patroller_Outline;
    public RawImage ranger_Im;
    public Outline ranger_Outline;
    public RawImage defender_Im;
    public Outline defender_Outline;

    [Space()]
    public Text description;

    Database db;

    public Color superNotReady = Color.red;
    public Color superReady = Color.yellow;
    bool setup = false;

    public Color afford;
    public Color broke;
    public Color highlighted;
    public Color notHighlighted;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        pg = GetComponent<PlayerGrab>();
    }

    // Update is called once per frame
    void Update()
    {
        if (setup)
        {
            if (pg != null && pg.player != null)
                ShowInfo();
        }
        else
        {
            Setup();
        }
    }

    void Setup()
    {
        background = GetComponent<RawImage>();
        playerName = transform.GetChild(0).GetComponent<Text>();
        characterName = transform.GetChild(1).GetComponent<Text>();
        health = transform.GetChild(2).GetComponent<Text>();

        setup = true;
    }

    void ShowInfo()
    {
        Player p = pg.player;
        if (p == null)
            return;

        Color c = PlayerSkin.GetColor(p, db);

        // Panel matches character skin; keep translucent like old team panels
        if (background != null)
        {
            Color bg = c;
            if (p.currentHealth <= 0)
                bg = Color.Lerp(c, Color.gray, 0.65f);
            bg.a = 0.27f;
            background.color = bg;
        }

        if (p.computer)
        {
            playerName.text = "CPU" + (p.index + 1);
            playerName.color = Color.gray;
        }
        else
        {
            playerName.text = (p.nickName == "" || p.nickName == null) ? "P" + (p.index + 1) : p.nickName;
            playerName.color = c;
        }

        characterName.text = p.name;
        characterName.color = c;
        health.text = p.currentHealth + "/" + p.maxHealth;

        int gold = p.super != null ? Mathf.FloorToInt(p.super.amount) : 0;
        if (currency != null)
            currency.text = gold + "g";

        int selected = Mathf.Clamp(Mathf.RoundToInt(p.movementSpeed), 0, 4);
        PaintUnit(0, reflector_Im, reflector_Outline, selected, gold);
        PaintUnit(1, attacker_Im, attacker_Outline, selected, gold);
        PaintUnit(2, patroller_Im, patroller_Outline, selected, gold);
        PaintUnit(3, ranger_Im, ranger_Outline, selected, gold);
        PaintUnit(4, defender_Im, defender_Outline, selected, gold);

        if (description != null)
            description.text = Describe(p, selected);
    }

    void PaintUnit(int index, RawImage icon, Outline outline, int selected, int gold)
    {
        PawnType type = PawnType.Reflector;
        if (index == 1) type = PawnType.Attacker;
        else if (index == 2) type = PawnType.Patroller;
        else if (index == 3) type = PawnType.Ranger;
        else if (index == 4) type = PawnType.Defender;

        if (icon != null)
            icon.color = gold >= Pawn.CostOf(type) ? afford : broke;
        if (outline != null)
            outline.effectColor = index == selected ? highlighted : notHighlighted;
    }

    string Describe(Player p, int selected)
    {
        Gaurd gaurd = p.spawnedPlayer != null ? p.spawnedPlayer.GetComponent<Gaurd>() : null;
        Pawn under = gaurd != null ? gaurd.hovering : null;
        if (under != null && under.level < 3)
        {
            int next = under.level + 1;
            int price = next * Pawn.CostOf(under.pawnType);
            return "<color=#2F6BFF>Hovering:</color> <color=#FFFFFF>" + UnitName(under.pawnType)
                + "</color> <color=#2F6BFF>" + next + "</color> <color=#FFD700>" + price + "g</color>";
        }

        PawnType type = TypeAt(selected);
        return UnitName(type) + " <color=#FFD700>" + Pawn.CostOf(type) + "g</color>";
    }

    static PawnType TypeAt(int selected)
    {
        if (selected == 1) return PawnType.Attacker;
        if (selected == 2) return PawnType.Patroller;
        if (selected == 3) return PawnType.Ranger;
        if (selected == 4) return PawnType.Defender;
        return PawnType.Reflector;
    }

    static string UnitName(PawnType type)
    {
        switch (type)
        {
            case PawnType.Attacker: return "Attacker";
            case PawnType.Patroller: return "Patroller";
            case PawnType.Ranger: return "Ranger";
            case PawnType.Defender: return "Defender";
            default: return "Reflector";
        }
    }
}
