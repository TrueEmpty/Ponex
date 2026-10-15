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

        currency.text = pg.player.super.amount + "g";
    }
}
