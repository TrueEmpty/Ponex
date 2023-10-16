using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gaurd : MonoBehaviour
{
    Rigidbody rb;
    ButtonManager bm;
    Database db;
    PlayerGrab pg;

    public List<string> hitTags = new List<string>();

    public GameObject defenders;
    public GameObject reflectors;
    public GameObject attackers;

    public List<Pawn> pawns = new List<Pawn>();

    #region AI
    bool thinking = false;
    public float thinkTime = .5f;

    [SerializeField]
    Thought thought = Thought.Nothing;

    //Starting Chance Weight
    public float chanceToDoNothing = 50; //Do Nothing
    public float chanceToMove = 100; //Move Left
    public float chanceToBump = 0; //Move Bump
    public float chanceToSuper = 0; //Move Super

    enum Thought
    {
        Nothing,
        MoveLeft,
        MoveRight,
        MoveUp,
        MoveDown
    }
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        bm = ButtonManager.instance;
        db = Database.instance;

        rb.useGravity = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(pg.IsLinked())
        {
            Player p = pg.player;

            if (db.gameStart && p.currentHealth > 1)
            {
                if (p.currentHealth == 1 && pawns.Count == 0)
                {
                    p.currentHealth = 0;
                }
                else
                {
                    if (p.computer)
                    {
                        if (!thinking)
                        {
                            thinking = true;
                            //StartCoroutine(AI());
                        }
                    }


                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (pg.IsLinked() && collision.transform.tag.ToLower().Trim() == "ball")
        {
            //Check if you own the object
            PlayerGrab tpG = collision.gameObject.GetComponent<PlayerGrab>();
            BallInfo tbI = collision.gameObject.GetComponent<BallInfo>();
            bool pass = true;

            if (tpG != null)
            {
                if (tpG.IsLinked())
                {
                    if (tpG.playerIndex == pg.playerIndex)
                    {
                        pass = false;
                    }
                }
            }

            if (pass)
            {
                int baseDamage = 0;

                if (tbI != null)
                {
                    baseDamage = tbI.ball.damage;
                }

                //Send out Ball hit to all
                pg.player.currentHealth -= baseDamage;

                if (pg.player.currentHealth <= 0)
                {
                    if(pawns.Count > 0)
                    {
                        pg.player.currentHealth = 1;
                    }
                    else
                    {
                        pg.player.currentHealth = 0;
                    }
                }
            }
        }
    }
}
