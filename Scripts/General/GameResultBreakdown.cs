using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameResultBreakdown : MonoBehaviour
{
    Database db;
    public int playerIndex = -1;

    public Color selected = Color.yellow;
    public Color notSelected = Color.gray;
    public Color winner = Color.green;
    public Color loser = Color.gray;

    RectTransform rt;
    public Outline border;
    public Image teamColor;
    public Text winnerSymbol;
    public Text playerName;
    public Text charName;
    public Text info;
    public RawImage portrait;

    float alpha = 200;
    float FourLessWidth = 200;
    float FiveMoreWidth = 160;
    float height = 0;

    public float scrollspeed = 10;
    int compScrollDir = 1;

    void Start()
    {
        alpha = (200f / 255f);
        rt = GetComponent<RectTransform>();
        db = Database.instance;
        SelectorClickable.Ensure(gameObject, 180f);
    }

    void Update()
    {
        Resize();

        if (db == null || playerIndex < 0 || playerIndex >= db.players.Count)
            return;

        UpdateInfo();

        // Humans currently scrolling this card (their own or a CPU's)
        bool humanScrollingThis = false;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player scroller = db.players[i];
            if (scroller == null || scroller.computer || scroller.winScrollTarget != this)
                continue;

            humanScrollingThis = true;
            HandleManualScroll(scroller);
        }

        // CPU cards auto-scroll only when nobody has taken over this card
        Player cardOwner = db.players[playerIndex];
        if (cardOwner != null && cardOwner.computer && !humanScrollingThis)
            ComputerScroll();
    }

    void Resize()
    {
        int pCC = transform.parent != null ? transform.parent.childCount : 1;

        if (pCC > 4)
            rt.sizeDelta = new Vector2(FiveMoreWidth, rt.sizeDelta.y);
        else
            rt.sizeDelta = new Vector2(FourLessWidth, rt.sizeDelta.y);

        if (info != null)
            height = info.GetComponent<RectTransform>().sizeDelta.y;
    }

    void UpdateInfo()
    {
        Player p = db.players[playerIndex];
        Color c = db.playerColors[playerIndex].color;
        Color t = db.teamColors[p.team];
        t.a = alpha;

        bool someoneScrolling = false;
        for (int i = 0; i < db.players.Count; i++)
        {
            if (db.players[i] != null && db.players[i].winScrollTarget == this)
            {
                someoneScrolling = true;
                break;
            }
        }

        border.effectColor = someoneScrolling ? selected : notSelected;
        teamColor.color = t;

        if (p.won)
        {
            winnerSymbol.text = "W";
            winnerSymbol.color = winner;
        }
        else
        {
            winnerSymbol.text = "L";
            winnerSymbol.color = loser;
        }

        if (p.computer)
        {
            playerName.text = "CPU" + (playerIndex + 1);
            playerName.color = Color.gray;
        }
        else
        {
            playerName.text = (p.nickName == "" || p.nickName == null) ? "P" + (playerIndex + 1) : p.nickName;
            playerName.color = c;
        }

        charName.text = p.name;
        charName.color = (p.currentHealth > 0) ? charName.color : Color.gray;

        string infoOut = "Damage Dealt: " + p.damageDealt;
        infoOut += "\n";
        infoOut += "Damage Taken: " + p.damageTaken;
        infoOut += "\n";
        infoOut += "Ball Hits: " + p.ballHits;
        infoOut += "\n";
        infoOut += "Longest Ball Ownership: " + p.longestBallOwnership + "s";
        infoOut += "\n";
        infoOut += "Biggest Hit Dealt: " + p.highestSingleDamgeDealt;
        infoOut += "\n";
        infoOut += "Biggest Hit Taken: " + p.highestSingleDamageTaken;
        infoOut += "\n";
        infoOut += "Ults used: " + p.ultsUsed;
        infoOut += "\n";
        infoOut += "# of Dashes: " + p.numberOfDashes;
        infoOut += "\n";
        infoOut += "After Death Hits: " + p.afterDeathHits;
        infoOut += "\n";
        infoOut += "After Death Damage Dealt: " + p.afterDeathDamage;

        info.text = infoOut;
        portrait.texture = p.portrait;
    }

    /// <summary>Selector click — enter manual scroll for this card (yours or a CPU's).</summary>
    void OnClick(int player)
    {
        if (db == null || player < 0 || player >= db.players.Count)
            return;

        Player p = db.players[player];
        if (p == null || p.computer || p.cLink == null)
            return;

        // Leave any other card first
        p.winScrollTarget = this;
    }

    void HandleManualScroll(Player scroller)
    {
        ControllerLink cL = scroller.cLink;
        if (cL == null || info == null)
            return;

        // Exit scroll: bump (Jump), super (Interact), or Menu (Esc / Start / Options)
        if (WasPressed(cL, "Jump") || WasPressed(cL, "Interact") || WasPressed(cL, "Menu"))
        {
            scroller.winScrollTarget = null;
            return;
        }

        Vector3 curPos = info.transform.localPosition;
        ControllerButtons move = cL["Move"];
        float axisY = move != null ? move.value.y : 0f;

        if (Mathf.Abs(axisY) > 0.01f)
            curPos.y += axisY * scrollspeed * 20f * Time.deltaTime;

        if (curPos.y < 0)
            curPos.y = 0;
        else if (curPos.y > height)
            curPos.y = height;

        info.transform.localPosition = curPos;
    }

    static bool WasPressed(ControllerLink cL, string action)
    {
        ControllerButtons b = cL[action];
        return b != null && b.wasPressedThisFrame;
    }

    void ComputerScroll()
    {
        if (info == null)
            return;

        Vector3 curPos = info.transform.localPosition;
        curPos.y += compScrollDir * (scrollspeed / 3) * Time.deltaTime;

        if (curPos.y < 0)
        {
            curPos.y = 0;
            compScrollDir *= -1;
        }
        else if (curPos.y > height)
        {
            curPos.y = height;
            compScrollDir *= -1;
        }

        info.transform.localPosition = curPos;
    }
}
