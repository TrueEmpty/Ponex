using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayTesting : MonoBehaviour
{
    Database db;
    public FieldInfo field;
    public BallInfo ball;

    public int ballUsed = 0;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        Ball ballUsing = new Ball(db.balls[Random.Range(0, db.balls.Count)]);

        if (ballUsed >= 0 && ballUsed < db.balls.Count)
        {
            ballUsing = new Ball(db.balls[ballUsed]);
        }

        if(ball != null)
        {
            ball.ball = ballUsing;
            ball.ballReady = true;
        }

        field.ball = ballUsing;

        field.started = true;
    }
}
