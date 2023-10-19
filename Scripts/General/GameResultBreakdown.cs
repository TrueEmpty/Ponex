using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameResultBreakdown : MonoBehaviour
{
    Database db;
    ButtonManager bm;
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

    // Start is called before the first frame update
    void Start()
    {
        alpha = (200f / 255f);
        rt = GetComponent<RectTransform>();
        db = Database.instance;
        bm = ButtonManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        Resize();

        if(playerIndex >= 0 && playerIndex < db.players.Count)
        {
            UpdateInfo();

            Player p = db.players[playerIndex];

            if (p.state.ToLower().Trim() != "winners")
            {
                if(p.computer)
                {
                    ComputerScroll();
                }
                else
                {
                    ButtonInput();
                }
            }
        }
    }

    void Resize()
    {
        int pCC = transform.parent.childCount;

        if (pCC > 4)
        {
            rt.sizeDelta = new Vector2(FiveMoreWidth,rt.sizeDelta.y);
        }
        else
        {
            rt.sizeDelta = new Vector2(FourLessWidth, rt.sizeDelta.y);
        }

        height = info.GetComponent<RectTransform>().sizeDelta.y;
    }

    void UpdateInfo()
    {
        Player p = db.players[playerIndex];
        Color c = db.playerColors[playerIndex];
        Color t = db.teamColors[p.team];
        t.a = alpha;

        border.effectColor = (p.state.ToLower().Trim() != "winners") ? selected : notSelected;
        teamColor.color = t;

        if(p.won)
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

        #region Output Info
        string infoOut = "Damage Dealt: " + p.damageDealt;
        infoOut += "\n";
        infoOut += "Damage Taken: " + p.damageTaken;
        infoOut += "\n";
        infoOut += "Ball Hits: " + p.ballHits;
        infoOut += "\n";
        infoOut += "Longest Ball Ownership: " + p.longestBallOwnership;
        infoOut += "\n";
        infoOut += "Highest Single Damge Dealt: " + p.highestSingleDamgeDealt;
        infoOut += "\n";
        infoOut += "Highest Single Damage Taken: " + p.highestSingleDamageTaken;
        infoOut += "\n";
        infoOut += "Ults used: " + p.ultsUsed;
        infoOut += "\n";
        infoOut += "# of Dashes: " + p.numberOfDashes;
        infoOut += "\n";
        infoOut += "After Death Hits: " + p.afterDeathHits;
        infoOut += "\n";
        infoOut += "After Death Damage Dealt: " + p.afterDeathDamage;

        info.text = infoOut;
        #endregion

        portrait.texture = p.portrait;
    }

    void ButtonInput()
    {
        Player p = db.players[playerIndex];

        Vector3 curPos = info.transform.localPosition;

        if(bm.KeyPressed(p.buttons.up))
        {
            curPos.y += scrollspeed * Time.deltaTime;
        }

        if(bm.KeyPressed(p.buttons.down))
        {
            curPos.y -= scrollspeed * Time.deltaTime;
        }

        if(curPos.y < 0)
        {
            curPos.y = 0;
        }
        else if (curPos.y > height)
        {
            curPos.y = height;
        }

        if(bm.KeyDown(p.buttons.cancel) || bm.KeyDown(p.buttons.confirm))
        {
            p.state = "Winners";
        }

        info.transform.localPosition = curPos;
    }

    void ComputerScroll()
    {
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
