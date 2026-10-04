using UnityEngine;

/// <summary>
/// Cycles db.selectedBall. step = +1 next, -1 previous. Use step = 0 for Random (-1).
/// </summary>
public class BallStepButton : MonoBehaviour
{
    public int step = 1;
    public bool setRandom = false;

    public void OnClick(int player)
    {
        Database db = Database.instance;
        if (db == null || db.balls == null || db.balls.Count == 0)
            return;

        if (setRandom)
        {
            db.selectedBall = -1;
            return;
        }

        db.selectedBall += step;

        if (db.selectedBall >= db.balls.Count)
            db.selectedBall = -1;
        else if (db.selectedBall < -1)
            db.selectedBall = db.balls.Count - 1;
    }
}
