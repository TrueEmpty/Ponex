using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerGrab))]
public class TestInfo : MonoBehaviour
{
    PlayerGrab pg;

    public RawImage background;
    public Text playerName;
    public Text characterName;
    public Text health;
    public Image bump;
    public Image super;
    public Outline superOutline;
    bool superlastState = false;
    bool runSuperEffect = false;
    float superEffectPecent = 0;
    int superEffectDir = 1;
    public Image dash;

    Database db;

    public Color superNotReady = Color.red;
    public Color superReady = Color.yellow;
    bool setup = false;

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
        bump = transform.GetChild(3).GetComponent<Image>();
        super = transform.GetChild(4).GetComponent<Image>();
        superOutline = transform.GetChild(4).GetChild(0).GetComponent<Outline>();
        dash = transform.GetChild(5).GetComponent<Image>();

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

        bump.fillAmount = p.bump.amount/p.bump.max;

        float sP = p.super.amount / p.super.max;

        super.fillAmount = sP;

        //Change Super's Color based on Ready
        Color superColor = superReady;
        bool currentSuperState = false;

        // Meter ready = Enough(). readyPercent is also used as Garmen Drive-on, so don't require it here.
        bool meterReady = p.super != null && p.super.cost > 0 && p.super.amount >= p.super.cost;
        if (meterReady)
        {
            superColor = superReady;
            currentSuperState = true;
        }
        else if (p.super == null || p.super.cost <= 0 || p.super.amount <= 0)
        {
            superColor = superNotReady;
        }
        else
        {
            superColor = Color.Lerp(superNotReady, superReady, (p.super.amount / p.super.cost) / 1.5f);
        }

        super.color = superColor;

        superOutline.enabled = currentSuperState;

        if (!superlastState && currentSuperState)
        {
            runSuperEffect = true;
        }

        superlastState = currentSuperState;

        if (!currentSuperState)
        {
            runSuperEffect = false;
            superOutline.effectDistance = new Vector2(1, -1);
            superEffectPecent = 0;
            superEffectDir = 1;
        }

        if (runSuperEffect)
        {
            superEffectPecent += superEffectDir * 300 * Time.deltaTime;

            if (superEffectDir == 1)
            {
                if (superEffectPecent > 100)
                {
                    superEffectPecent = 100;
                    superEffectDir = -1;
                }
            }
            else
            {
                if (superEffectPecent < 0)
                {
                    runSuperEffect = false;
                    superOutline.effectDistance = new Vector2(1, -1);
                    superEffectPecent = 0;
                    superEffectDir = 1;
                }
            }

            superOutline.effectDistance = new Vector2(Mathf.Lerp(1, 30, superEffectPecent / 100), -1);
        }

        dash.fillAmount = p.dash.amount / p.dash.max;
    }
}
