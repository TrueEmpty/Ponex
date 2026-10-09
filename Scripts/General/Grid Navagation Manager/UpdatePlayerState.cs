using UnityEngine;

/// <summary>
/// Changes the per-player UI navigation state when a grid item is confirmed or cancelled.
/// Kept as a separate component because existing scenes and prefabs reference this script.
/// </summary>
public class UpdatePlayerState : MonoBehaviour
{
    public string newState = "";
    public string cancelState = "";

    public void OnClick(int player)
    {
        SetState(player, newState);
    }

    public void OnCancel(int player)
    {
        SetState(player, cancelState);
    }

    static void SetState(int player, string state)
    {
        if (string.IsNullOrEmpty(state))
            return;

        Database db = Database.instance;
        if (db == null || db.players == null)
            return;

        if (player == -10)
        {
            for (int i = 0; i < db.players.Count; i++)
            {
                if (db.players[i] != null)
                    db.players[i].state = state;
            }
            return;
        }

        Player target = db.players.Find(p => p != null && p.index == player);
        if (target != null)
            target.state = state;
    }
}
