using System.Collections.Generic;
using UnityEngine;

public class TeamSelect : MonoBehaviour
{
    public static TeamSelect instance;
    Database db;
    MenuManager mm;

    public List<TeamCard> cards = new List<TeamCard>();
    bool setup;

    private void Awake()
    {
        if (instance != null)
            Destroy(this);
        else
            instance = this;
    }

    void Start()
    {
        db = Database.instance;
        mm = MenuManager.instance;
    }

    void Update()
    {
        if (!setup)
            Setup();
        else
            RefreshAll();
    }

    public void Setup()
    {
        if (setup) return;

        EnsureCardsOnAssignments();
        WireConfirmButton();

        if (cards == null || cards.Count == 0)
        {
            cards = new List<TeamCard>();
            TeamCard[] found = GetComponentsInChildren<TeamCard>(true);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null)
                    cards.Add(found[i]);
            }
        }

        if (cards.Count == 0)
            Debug.LogWarning("TeamSelect: no TeamCard components found.");

        EnforceSameSideTeams();

        setup = true;
        RefreshAll();
    }

    void EnsureCardsOnAssignments()
    {
        Transform assignments = FindChildNamed(transform, "Assignments");
        if (assignments == null && transform.childCount > 1)
            assignments = transform.GetChild(1);
        if (assignments == null)
            return;

        cards = new List<TeamCard>();
        for (int i = 0; i < assignments.childCount; i++)
        {
            Transform child = assignments.GetChild(i);
            if (child == null) continue;
            TeamCard card = child.GetComponent<TeamCard>();
            if (card == null)
                card = child.gameObject.AddComponent<TeamCard>();
            cards.Add(card);
        }
    }

    void WireConfirmButton()
    {
        Transform confirm = FindChildNamed(transform, "Confirm");
        if (confirm == null)
            return;

        RunOnClicked roc = confirm.GetComponent<RunOnClicked>();
        if (roc != null)
            Destroy(roc);

        if (confirm.GetComponent<ConfirmClicked>() == null)
            confirm.gameObject.AddComponent<ConfirmClicked>();

        if (confirm.GetComponent<BoxCollider>() == null)
        {
            BoxCollider col = confirm.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            RectTransform rt = confirm.GetComponent<RectTransform>();
            if (rt != null)
                col.size = new Vector3(Mathf.Max(170f, rt.rect.width), Mathf.Max(60f, rt.rect.height), 1f);
        }
    }

    Transform FindChildNamed(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform f = FindChildNamed(root.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }

    /// <summary>
    /// Players sharing a wall (same facing) must share a team.
    /// </summary>
    public void EnforceSameSideTeams()
    {
        if (db == null) return;

        for (int i = 0; i < db.players.Count; i++)
        {
            Player a = db.players[i];
            if (a == null) continue;
            for (int j = i + 1; j < db.players.Count; j++)
            {
                Player b = db.players[j];
                if (b == null) continue;
                if (a.facing == b.facing)
                    b.team = a.team;
            }
        }
    }

    public void RefreshAll()
    {
        if (db == null) return;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null) continue;
            if (i < db.players.Count)
                cards[i].Bind(db.players[i].index);
            else
                cards[i].Bind(-1);
        }
    }

    public bool HasAtLeastTwoTeams()
    {
        if (db == null) return false;
        List<int> teams = new List<int>();
        for (int i = 0; i < db.players.Count; i++)
        {
            if (!teams.Contains(db.players[i].team))
                teams.Add(db.players[i].team);
        }
        return teams.Count >= 2;
    }

    public void PlayerConfirm(int player)
    {
        Player p = db.players.Find(x => x.index == player);
        if (p == null) return;

        EnforceSameSideTeams();

        if (!HasAtLeastTwoTeams())
        {
            Debug.LogWarning("Team Select: need at least 2 different teams before confirming.");
            RefreshAll();
            return;
        }

        p.characterSelected = true;

        for (int i = 0; i < db.players.Count; i++)
        {
            if (db.players[i].computer)
                db.players[i].characterSelected = true;
        }

        RefreshAll();

        if (!db.players.Exists(x => !x.characterSelected))
        {
            if (HasAtLeastTwoTeams())
            {
                db.RememberAllLobbySeats();
                db.CharactersPicked("teams");
            }
            else
            {
                p.characterSelected = false;
                for (int i = 0; i < db.players.Count; i++)
                {
                    if (db.players[i].computer)
                        db.players[i].characterSelected = false;
                }
                RefreshAll();
                Debug.LogWarning("Team Select: need at least 2 different teams before confirming.");
            }
        }
    }
}
