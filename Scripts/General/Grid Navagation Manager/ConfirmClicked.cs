using UnityEngine;

/// <summary>
/// Per-player confirm for stages that need everyone ready (Character / Position / Team).
/// Field & Ball use RunOnClicked → CharactersPicked for a shared choice.
/// </summary>
public class ConfirmClicked : MonoBehaviour
{
    public void OnClick(int player)
    {
        MenuManager mm = MenuManager.instance;
        string title = "";
        if (mm != null)
        {
            MenuClass open = mm.GetOpenMenu();
            if (open != null)
                title = open.title;
        }

        switch (title.ToLower().Trim())
        {
            case "character select":
            case "characters":
                if (CharacterSelect.instance != null)
                    CharacterSelect.instance.PlayerConfirm(player);
                break;

            case "position select":
            case "positions":
                if (PositionSelect.instance != null)
                    PositionSelect.instance.PlayerConfirm(player);
                break;

            case "team select":
            case "teams":
                if (TeamSelect.instance != null)
                    TeamSelect.instance.PlayerConfirm(player);
                break;

            case "field select":
            case "fields":
                // Shared selection — advance whole lobby
                if (Database.instance != null)
                    Database.instance.CharactersPicked("fields");
                break;

            case "ball select":
            case "balls":
                if (Database.instance != null)
                    Database.instance.CharactersPicked("balls");
                break;
        }
    }
}
