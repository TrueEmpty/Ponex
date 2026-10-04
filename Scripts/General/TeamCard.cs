using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player team card — short click cycles team color for that player.
/// </summary>
public class TeamCard : MonoBehaviour
{
    public int attachedIndex = -1;

    RawImage teamImage;
    Text playerText;
    Text infoText;
    GameObject ready;
    Outline outline;

    void Awake()
    {
        teamImage = GetComponent<RawImage>();

        if (transform.childCount > 0)
            playerText = transform.GetChild(0).GetComponent<Text>();
        if (transform.childCount > 1)
            infoText = transform.GetChild(1).GetComponent<Text>();
        if (transform.childCount > 2)
            ready = transform.GetChild(2).gameObject;

        outline = GetComponent<Outline>();
        if (outline == null && playerText != null)
            outline = playerText.GetComponent<Outline>();
    }

    public void Bind(int playerIndex)
    {
        attachedIndex = playerIndex;
        Refresh();
    }

    public void Refresh()
    {
        Database db = Database.instance;
        if (db == null || attachedIndex < 0)
        {
            gameObject.SetActive(false);
            return;
        }

        Player p = db.players.Find(x => x.index == attachedIndex);
        if (p == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        Color c = (attachedIndex >= 0 && attachedIndex < db.playerColors.Count)
            ? db.playerColors[attachedIndex].color
            : Color.white;

        if (playerText != null)
        {
            if (p.computer)
            {
                playerText.text = "CPU" + (attachedIndex + 1);
                playerText.color = Color.gray;
            }
            else
            {
                playerText.text = string.IsNullOrEmpty(p.nickName) ? "P" + (attachedIndex + 1) : p.nickName;
                playerText.color = c;
            }
        }

        ApplyTeamVisual(p.team);

        if (ready != null)
            ready.SetActive(p.characterSelected);

        if (outline != null)
            outline.effectColor = p.characterSelected ? Color.green : new Color(0f, 0.45f, 1f, 1f);
    }

    void ApplyTeamVisual(int team)
    {
        string name;
        Color col;
        switch (team)
        {
            case 2: name = "Team: Red"; col = Color.red; break;
            case 3: name = "Team: Green"; col = Color.green; break;
            case 4: name = "Team: Yellow"; col = Color.yellow; break;
            default: name = "Team: Blue"; col = Color.blue; break;
        }

        if (infoText != null)
            infoText.text = name;

        Image img = GetComponent<Image>();
        if (img != null)
            img.color = col;
        else if (teamImage != null)
            teamImage.color = col;
    }

    public void OnClick(int player)
    {
        Database db = Database.instance;
        if (db == null) return;

        // Only the owning player (or someone controlling that CPU) can change this card
        if (player != attachedIndex)
            return;

        Player p = db.players.Find(x => x.index == attachedIndex);
        if (p == null || p.characterSelected)
            return;

        p.team++;
        if (p.team > 4) p.team = 1;

        // Keep lane partners on the same team (old behavior)
        Player partner = db.players.Find(x => x.facing == p.facing && x != p);
        if (partner != null)
            partner.team = p.team;

        TeamSelect ts = TeamSelect.instance;
        if (ts != null)
            ts.RefreshAll();
        else
            Refresh();
    }
}
