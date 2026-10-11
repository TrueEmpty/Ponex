using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(50)]
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
    ScrollRect statScroll;
    float manualIgnoreExitUntil;
    float pendingScroll;

    void Start()
    {
        alpha = (200f / 255f);
        rt = GetComponent<RectTransform>();
        db = Database.instance;
        SelectorClickable.Ensure(gameObject, 180f);
        if (info != null)
            statScroll = info.GetComponentInParent<ScrollRect>();
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

    void LateUpdate()
    {
        if (Mathf.Abs(pendingScroll) < 0.0001f)
            return;
        ApplyScroll(pendingScroll);
        pendingScroll = 0f;
    }

    void Resize()
    {
        int pCC = transform.parent != null ? transform.parent.childCount : 1;

        if (pCC > 4)
            rt.sizeDelta = new Vector2(FiveMoreWidth, rt.sizeDelta.y);
        else
            rt.sizeDelta = new Vector2(FourLessWidth, rt.sizeDelta.y);

        height = ScrollRange();
    }

    void UpdateInfo()
    {
        Player p = db.players.Find(x => x != null && x.index == playerIndex);
        if (p == null)
            return;
        Color c = PlayerSkin.GetColor(p, db);
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

        if (p.match == null)
            p.match = new MatchStats();
        info.text = p.match.FormatWinScreen(p);
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
        manualIgnoreExitUntil = Time.unscaledTime + 0.2f;
    }

    void HandleManualScroll(Player scroller)
    {
        ControllerLink cL = scroller.cLink;
        if (cL == null || info == null)
            return;

        // Exit scroll: bump (Jump), super (Interact), or Menu (Esc / Start / Options).
        // Ignore the click that just opened this card.
        if (Time.unscaledTime > manualIgnoreExitUntil
            && (WasPressed(cL, "Jump") || WasPressed(cL, "Interact") || WasPressed(cL, "Menu")))
        {
            scroller.winScrollTarget = null;
            return;
        }

        ControllerButtons move = cL["Move"];
        float axisY = move != null ? move.value.y : 0f;
        if (Mathf.Abs(axisY) < 0.2f && move != null)
            axisY = move.flatValue.y;

        if (Mathf.Abs(axisY) > 0.01f)
        {
            float dt = Time.timeScale < 0.01f ? Time.unscaledDeltaTime : Time.deltaTime;
            // Half the old manual rate (scrollspeed * 10).
            pendingScroll += axisY * scrollspeed * 5f * dt;
        }
    }

    float ScrollRange()
    {
        if (statScroll == null && info != null)
            statScroll = info.GetComponentInParent<ScrollRect>();
        if (statScroll != null && statScroll.content != null && statScroll.viewport != null)
            return Mathf.Max(0f, statScroll.content.rect.height - statScroll.viewport.rect.height);
        if (info == null)
            return 0f;
        float view = info.rectTransform.rect.height;
        return Mathf.Max(0f, info.preferredHeight - Mathf.Max(8f, view));
    }

    void ApplyScroll(float delta)
    {
        if (statScroll == null && info != null)
            statScroll = info.GetComponentInParent<ScrollRect>();
        if (statScroll != null && statScroll.content != null)
        {
            float range = ScrollRange();
            Vector2 pos = statScroll.content.anchoredPosition;
            float y = pos.y + delta;
            if (y < 0f)
            {
                y = 0f;
                if (delta < 0f)
                    compScrollDir = 1;
            }
            else if (y > range)
            {
                y = range;
                if (delta > 0f)
                    compScrollDir = -1;
            }
            pos.y = y;
            statScroll.content.anchoredPosition = pos;
            statScroll.velocity = Vector2.zero;
            return;
        }

        if (info == null)
            return;
        Vector3 curPos = info.transform.localPosition;
        curPos.y = Mathf.Clamp(curPos.y + delta, 0f, height);
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

        float dt = Time.timeScale < 0.01f ? Time.unscaledDeltaTime : Time.deltaTime;
        pendingScroll += compScrollDir * (scrollspeed / 3f) * dt;
    }
}
