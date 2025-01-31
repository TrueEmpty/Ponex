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
        bool dontProcced = false;

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

        #region Buttons
        List<Player> rP = db.players.FindAll(x => !x.computer && x.WithinLastUpdate);

        List<string> l = new List<string>();
        List<string> r = new List<string>();
        List<string> u = new List<string>();
        List<string> d = new List<string>();
        List<string> g = new List<string>();
        List<string> x = new List<string>();

        if (rP.Count > 0)
        {
            for (int z = 0; z < rP.Count; z++)
            {
                l.AddRange(rP[z].buttons.left);
                r.AddRange(rP[z].buttons.right);
                u.AddRange(rP[z].buttons.up);
                d.AddRange(rP[z].buttons.down);
                g.AddRange(rP[z].buttons.confirm);
                x.AddRange(rP[z].buttons.cancel);
            }
        }

        if (bm.KeyDown(r) || bm.KeyDown(u))
        {
            db.selectedBall++;

            if(db.selectedBall >= db.balls.Count)
            {
                db.selectedBall = -1;
            }

            for (int i = 0; i < db.players.Count; i++)
            {
                db.players[i].lastGridUpdate = Time.time;
            }
        }
        else if (bm.KeyDown(l) || bm.KeyDown(d))
        {
            db.selectedBall--;

            if (db.selectedBall < -1)
            {
                db.selectedBall = db.balls.Count - 1;
            }

            for (int i = 0; i < db.players.Count; i++)
            {
                db.players[i].lastGridUpdate = Time.time;
            }
        }
        else if (bm.KeyDown(g))
        {
            //Start game
            if (shownBall != null)
            {
                Destroy(shownBall);
            }
            dontProcced = true;
            db.CharactersPicked("balls");
        }
        else if (bm.KeyDown(x))
        {
            for(int i = 0; i < db.players.Count; i++)
            {
                db.players[i].lastGridUpdate = Time.time;
                db.players[i].characterSelected = false;
                db.players[i].gridLock = false;
            }

            if (shownBall != null)
            {
                Destroy(shownBall);
            }
            dontProcced = true;
            mm.BackMenu();
        }
        #endregion

        if (lastShownBall != db.selectedBall && !dontProcced)
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

        shownBall.transform.position = new Vector3(0,0,5);
    }
}
