using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BallSelect : MonoBehaviour
{
    public static BallSelect instance;
    ButtonManager bm;
    Database db;
    MenuManager mm;

    int lastShownBall = -10;
    float nextShow = 0;
    float showSwap = .25f;

    GameObject shownBall = null;
    public Text ballName;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        bm = ButtonManager.instance;
        mm = MenuManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if(db.selectedBall >= 0 && db.selectedBall < db.balls.Count)
        {
            Ball b = db.balls[db.selectedBall];
            ballName.text = b.name;
            ballName.color = b.color;
        }
        else //Random
        {
            ballName.text = "Random";
            ballName.color = Color.black;
        }



        if(lastShownBall != db.selectedBall)
        {
            if(db.selectedBall >= 0 && db.selectedBall < db.balls.Count)
            {
                UpdateShownBall(db.selectedBall);
                lastShownBall = db.selectedBall;
            }
            else if(nextShow < Time.time)
            {
                UpdateShownBall(Random.Range(0,db.balls.Count));
                nextShow = Time.time + showSwap;
            }
        }
    }

    void UpdateShownBall(int ballToShow)
    {
        if(shownBall != null)
        {
            Destroy(shownBall);
        }

        Ball b = db.balls[ballToShow];
        shownBall = Instantiate(b.selection);

        shownBall.transform.position = Vector3.zero;
    }
}
