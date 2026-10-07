using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gaurd : MonoBehaviour
{
    Rigidbody rb;
    Database db;
    PlayerGrab pg;

    public List<string> hitTags = new List<string>();

    public GameObject defenders;
    public GameObject reflectors;
    public GameObject attackers;

    public List<Pawn> pawns = new List<Pawn>();
    bool savedByPawns = false;

    [SerializeField]
#pragma warning disable CS0414 // Inspector AI-thought debug
    Thought thought = Thought.Nothing;
#pragma warning restore CS0414

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;

        rb.useGravity = false;
    }

    // Update is called once per frame
    void Update()
    {
        // Destroyed pawns leave null entries and can soft-lock Gaurd at 1 HP forever
        pawns.RemoveAll(pawn => pawn == null);

        if (pg.IsLinked())
        {
            Player p = pg.player;

            if (db.gameStart)
            {
                // Killing blow was absorbed by pawns; once they are gone, finish the player
                if (savedByPawns && pawns.Count == 0)
                {
                    if (p.currentHealth <= 1)
                        p.currentHealth = 0;
                    savedByPawns = false;
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

                if (tbI != null && tbI.ball != null)
                {
                    baseDamage = tbI.ball.damage;
                }

                int lost = pg.player.ApplyGoalDamage(baseDamage, collision.gameObject.GetEntityId().GetHashCode());

                if (lost > 0 && tpG != null && tpG.IsLinked() && tpG.player != null)
                    tpG.player.RecordDamageDealt(lost);

                pawns.RemoveAll(pawn => pawn == null);

                if (pg.player.currentHealth <= 1 && CharacterCreationManager.IsActive)
                    return;

                if (pg.player.currentHealth <= 0)
                {
                    if (pawns.Count > 0)
                    {
                        pg.player.currentHealth = 1;
                        savedByPawns = true;
                    }
                    else
                    {
                        pg.player.currentHealth = 0;
                        savedByPawns = false;
                    }
                }
            }
        }
    }
}
