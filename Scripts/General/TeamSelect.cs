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
            Debug.LogWarning("TeamSelect: no TeamCard components found. Run Tools/Ponex/Bake Selector UI.");

        setup = true;
        RefreshAll();
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

    public void PlayerConfirm(int player)
    {
        Player p = db.players.Find(x => x.index == player);
        if (p == null) return;

        p.characterSelected = true;
        RefreshAll();

        if (!db.players.Exists(x => !x.characterSelected))
        {
            List<int> teams = new List<int>();
            for (int i = 0; i < db.players.Count; i++)
            {
                if (!teams.Contains(db.players[i].team))
                    teams.Add(db.players[i].team);
            }

            if (teams.Count >= 2)
            {
                db.CharactersPicked("teams");
            }
            else
            {
                p.characterSelected = false;
                RefreshAll();
                Debug.LogWarning("Team Select: need at least 2 different teams before confirming.");
            }
        }
    }
}
