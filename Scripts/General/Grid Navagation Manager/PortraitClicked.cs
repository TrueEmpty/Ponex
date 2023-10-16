using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortraitClicked : MonoBehaviour
{
    Database db;
    GridControl gC;
    CharacterSelect cS;
    public int attachedIndex = 0;

    // Use this for initialization
    void Start()
    {
        db = Database.instance;
        cS = CharacterSelect.instance;
        gC = GetComponent<GridControl>();
    }

    public void OnClick(int player)
    {
        if(player >= 0 && player < db.players.Count)
        {
            Player p = db.players[attachedIndex];

            if (attachedIndex == player)
            {
                //Change Skin

            }
            else
            {
                //Remove other players in that control
                if(gC.playersInControl.Count > 0)
                {
                    List<GridControl> oC = gC.fc;
                    oC.Remove(gC);

                    if (oC.Count > 0)
                    {
                        for (int i = gC.playersInControl.Count - 1; i >= 0; i--)
                        {
                            int c = gC.playersInControl[i];

                            if (c != attachedIndex)
                            {
                                int rC = Random.Range(0, oC.Count);
                                GridControl nG = oC[rC];
                                nG.AddPlayer(c);
                                gC.playersInControl.RemoveAt(i);
                            }
                        }
                    }
                }

                //Remove Player From Character Grab
                int cC = db.characters.FindIndex(x => x.name == p.name);

                if (cC < 0 || cC >= cS.characterGrabs.Count)
                {
                    Player ch = db.RandomCharacter();
                    p.SetUpCharacter(ch);

                    cC = db.characters.FindIndex(x => x.name == ch.name);
                }

                GridControl cgC = cS.characterGrabs[cC].gridControl;

                if (cgC.playersInControl.Exists(x => x == attachedIndex))
                {
                    cgC.playersInControl.Remove(attachedIndex);
                }

                db.players.RemoveAt(attachedIndex);
            }
        }
    }
}
