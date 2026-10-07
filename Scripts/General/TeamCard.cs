using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player team card — shows player + character, team color; click cycles team.
/// Same-facing players stay on the same team.
/// </summary>
public class TeamCard : MonoBehaviour
{
    public int attachedIndex = -1;

    RawImage teamImage;
    Text playerText;
    Text infoText;
    GameObject ready;
    Outline outline;
    bool wired;

    void Awake()
    {
        WireRefs();
    }

    void WireRefs()
    {
        if (wired)
            return;
        wired = true;

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

        if (GetComponent<BoxCollider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
                col.size = new Vector3(Mathf.Max(180f, rt.rect.width), Mathf.Max(80f, rt.rect.height), 1f);
            else
                col.size = new Vector3(300f, 100f, 1f);
        }
    }

    public void Bind(int playerIndex)
    {
        WireRefs();
        attachedIndex = playerIndex;
        Refresh();
    }

    public void Refresh()
    {
        WireRefs();
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

        Color playerColor = (attachedIndex >= 0 && attachedIndex < db.playerColors.Count)
            ? db.playerColors[attachedIndex].color
            : Color.white;

        string playerLabel;
        if (p.computer)
            playerLabel = "CPU" + (attachedIndex + 1);
        else if (!string.IsNullOrEmpty(p.nickName))
            playerLabel = p.nickName;
        else
            playerLabel = "P" + (attachedIndex + 1);

        string charLabel = string.IsNullOrEmpty(p.name) ? "—" : p.name;

        if (playerText != null)
        {
            playerText.text = playerLabel + "\n" + charLabel;
            playerText.color = p.computer ? Color.gray : playerColor;
            playerText.alignment = TextAnchor.MiddleCenter;
            if (playerText.fontSize > 22)
                playerText.fontSize = 20;
        }

        ApplyTeamVisual(p.team);

        if (ready != null)
            ready.SetActive(p.characterSelected);

        if (outline != null)
            outline.effectColor = p.characterSelected ? Color.green : new Color(0f, 0.45f, 1f, 1f);
    }

    void ApplyTeamVisual(int team)
    {
        string teamName;
        Color col;
        switch (team)
        {
            case 2: teamName = "Red"; col = new Color(0.9f, 0.2f, 0.2f, 1f); break;
            case 3: teamName = "Green"; col = new Color(0.2f, 0.75f, 0.25f, 1f); break;
            case 4: teamName = "Yellow"; col = new Color(0.95f, 0.85f, 0.15f, 1f); break;
            default: teamName = "Blue"; col = new Color(0.2f, 0.45f, 0.95f, 1f); break;
        }

        if (infoText != null)
            infoText.text = "Team: " + teamName;

        // Tint the card background with team color (keep readable alpha)
        Color tint = col;
        tint.a = 0.85f;
        Image img = GetComponent<Image>();
        if (img != null)
            img.color = tint;
        else if (teamImage != null)
            teamImage.color = tint;
    }

    public void OnClick(int player)
    {
        Database db = Database.instance;
        if (db == null || attachedIndex < 0)
            return;

        Player p = db.players.Find(x => x.index == attachedIndex);
        if (p == null || p.characterSelected)
            return;

        // Any lobby cursor can cycle a card (CPU cards included)
        p.team++;
        if (p.team > 4) p.team = 1;

        // Same wall / side must share a team
        for (int i = 0; i < db.players.Count; i++)
        {
            Player other = db.players[i];
            if (other == null || other == p) continue;
            if (other.facing == p.facing)
                other.team = p.team;
        }

        TeamSelect ts = TeamSelect.instance;
        if (ts != null)
        {
            ts.EnforceSameSideTeams();
            ts.RefreshAll();
        }
        else
            Refresh();

        if (db != null)
            db.RememberAllLobbySeats();
    }
}
