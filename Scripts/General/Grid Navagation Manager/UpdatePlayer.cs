using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatePlayer : MonoBehaviour
{
    Database db;
    CharacterSelect cS;
    GridControl gC;
    public string newState = "";
    public bool setNewBlank = false;
    public string cancelState = "";
    public bool setCancelBlank = false;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        cS = CharacterSelect.instance;
        gC = GetComponent<GridControl>();
    }

    public void PlayerAdded(PlayerAdded pA)
    {
        int cI = cS.characterGrabs.FindIndex(x => x.container == pA.owner);

        if(pA.p >= 0 && pA.p < db.players.Count)
        {
            Player p = db.players[pA.p];

            if (cI >= 0 && cI < db.characters.Count)
            {
                Player ch = db.characters[cI];

                p.SetUpCharacter(new(ch));
            }
        }
    }

    public void OnClick(int player)
    {
        if (newState != "" && !setNewBlank)
        {
            db.players[player].state = newState;
        }
    }

    public void OnCancel(int player)
    {
        if(cancelState != "" && !setCancelBlank)
        {
            db.players[player].state = cancelState;
        }
    }
}
