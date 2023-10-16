using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateField : MonoBehaviour
{
    Database db;
    FieldSelect fS;
    GridControl gC;
    MenuManager mm;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        fS = FieldSelect.instance;
        mm = MenuManager.instance;
        gC = GetComponent<GridControl>();
    }

    public void PlayerAdded(PlayerAdded pA)
    {
        int cI = fS.characterGrabs.FindIndex(x => x.container == pA.owner);

        if (cI >= 0 && cI < db.fields.Count)
        {
            db.selectedField = cI;
        }
    }

    public void OnLoop(Looped looped)
    {
        switch (looped.direction)
        {
            case LoopDirection.Up:
                //Move to options
                break;
            case LoopDirection.Down:
                //Move to options
                break;
            case LoopDirection.Left:
                //Page Down
                fS.PageChange(-1);
                break;
            case LoopDirection.Right:
                //Page Up
                fS.PageChange(1);
                break;
        }
    }

    public void OnClick(int player)
    {
        db.CharactersPicked("fields");
    }

    public void OnCancel(int player)
    {
        db.UpdateCharSelect();
        mm.BackMenu();
    }
}
