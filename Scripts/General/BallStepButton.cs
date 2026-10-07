using UnityEngine;

/// <summary>
/// Cycles ball loadout settings.
/// Default: cycles a ball slot type (including Random = -1).
/// editCount: cycles how many balls are in play (1-5).
/// </summary>
public class BallStepButton : MonoBehaviour
{
    public int step = 1;
    public bool setRandom = false;

    [Tooltip("Which loadout slot to edit (0-4). Ignored when editCount is true.")]
    public int slotIndex = 0;

    [Tooltip("If true, step changes ball count instead of ball type.")]
    public bool editCount = false;

    public void OnClick(int player)
    {
        Database db = Database.instance;
        if (db == null)
            return;

        if (editCount)
        {
            db.CycleBallCount(step == 0 ? 1 : step);
            if (BallSelect.instance != null)
                BallSelect.instance.RefreshLoadoutUI();
            return;
        }

        if (db.balls == null || db.balls.Count == 0)
            return;

        if (BallSelect.instance != null)
            BallSelect.instance.SetFocusSlot(slotIndex);

        db.CycleBallSlot(slotIndex, step, setRandom);
        if (BallSelect.instance != null)
            BallSelect.instance.RefreshLoadoutUI();
    }
}
