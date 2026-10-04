using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NariTail : MonoBehaviour
{
    public int healthIndex = 1;
    public NariTailUpkeep nTU;
    PlayerGrab pG;
    Rigidbody rb;
    OffsetFollow oF;
    Database db;

    public string tagHit = "Ball";
    float destroy = -1;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
        rb = GetComponent<Rigidbody>();
        oF = GetComponent<OffsetFollow>();
        db = Database.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if(pG.player != null && destroy < 0)
        {
            if(pG.player.currentHealth < healthIndex)
            {
                destroy = 0;
                nTU.tails.Remove(gameObject);
            }
        }
        else if (destroy > 1)
        {
            Destroy(gameObject);
        }
        else if(destroy >= 0)
        {
            destroy += Time.deltaTime*2;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (pG.IsLinked() && destroy < 0)
        {
            if (collision.transform.tag.ToLower().Trim() == tagHit.ToLower().Trim())
            {
                //Check if you own the object
                PlayerGrab tpG = collision.gameObject.GetComponent<PlayerGrab>();
                BallInfo tbI = collision.gameObject.GetComponent<BallInfo>();
                bool pass = true;

                if (tpG != null)
                {
                    if (tpG.IsLinked())
                    {
                        if (tpG.playerIndex == pG.playerIndex)
                        {
                            pass = false;
                        }
                        else
                        {
                            Player tp = db.players[tpG.playerIndex];
                            Player yp = db.players[pG.playerIndex];

                            if (tp.team == yp.team)
                            {
                                pass = false;
                            }
                        }
                    }
                }

                if (pass)
                {
                    // Sever from this segment outward — HP lost must go through ApplyGoalDamage
                    // so taken/dealt stats (and dedupe) match other characters.
                    int targetHealth = Mathf.Max(0, healthIndex - 1);
                    int toLose = pG.player.currentHealth - targetHealth;
                    if (toLose <= 0)
                        return;

                    int ballId = collision.gameObject.GetInstanceID();
                    int lost = pG.player.ApplyGoalDamage(toLose, ballId);
                    if (lost <= 0)
                        return;

                    // Nari's max tracks body length
                    pG.player.maxHealth = pG.player.currentHealth;

                    if (tpG != null && tpG.IsLinked() && tpG.player != null)
                    {
                        tpG.player.RecordDamageDealt(lost);
                        if (tpG.player.computer)
                            ComputerAI.OnPaddleHitBall(tpG.player);
                    }

                    if (pG.player.computer)
                        ComputerAI.OnTookGoalDamage(pG.player, lost);

                    destroy = 0;
                    if (nTU != null && nTU.tails != null)
                        nTU.tails.Remove(gameObject);
                    if (oF != null)
                        oF.parent = null;

                    rb.linearVelocity = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
                }
            }
        }
    }
}
