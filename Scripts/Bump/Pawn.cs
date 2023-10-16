using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : MonoBehaviour
{
    Database db;
    PlayerGrab pg;
    public int health = 1;
    Renderer ren;

    public List<PawnEffects> effects = new List<PawnEffects>();

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        pg = GetComponent<PlayerGrab>();
        ren = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if(health > 0)
        {
            if(health - 1 < db.pawnColors.Count)
            {
                ren.material.color = db.pawnColors[health - 1];
            }
            else
            {
                ren.material.color = db.pawnColors[^1];
            }

            //Show Effects
            if (effects.Count > 0)
            {
                for (int i = 0; i < effects.Count; i++)
                {
                    PawnEffects pe = effects[i];
                    Effects e = db.effects.Find(x => x.effect == pe.effect);

                    if(!pe.shown)
                    {
                        switch (pe.effect)
                        {
                            case Effect.Freeze: //Adds the Freeze effect to a ball hit
                                if (e != null)
                                {
                                    if (e.obj != null)
                                    {
                                        GameObject obj = Instantiate(e.obj);
                                        obj.transform.parent = transform;
                                        obj.transform.localPosition = Vector3.zero;

                                        PlayerGrab spg = obj.GetComponent<PlayerGrab>();

                                        if (spg != null)
                                        {
                                            spg.playerIndex = pg.playerIndex;
                                        }

                                        AddConstraintOnHit acoh = obj.GetComponent<AddConstraintOnHit>();

                                        if (acoh != null)
                                        {
                                            acoh.enabled = false;
                                        }

                                        pe.shown = true;
                                    }
                                }
                                break;
                            case Effect.Vamp: //Adds the Vamp effect to a ball hit
                                if (e != null)
                                {
                                    if (e.obj != null)
                                    {
                                        GameObject obj = Instantiate(e.obj);
                                        obj.transform.parent = transform;
                                        obj.transform.localPosition = Vector3.zero;

                                        PlayerGrab spg = obj.GetComponent<PlayerGrab>();

                                        if (spg != null)
                                        {
                                            spg.playerIndex = pg.playerIndex;
                                        }

                                        HealOnLifelineHit holh = obj.GetComponent<HealOnLifelineHit>();

                                        if (holh != null)
                                        {
                                            holh.enabled = false;
                                        }

                                        pe.shown = true;
                                    }
                                }
                                break;
                            case Effect.Bunker: //Burst into a swarm of 1 shot balls

                                pe.shown = true;
                                break;
                            default:
                                pe.shown = true;
                                break;
                        }
                    }
                }
            }
        }
        else
        {
            Destroy(gameObject);
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

                //Check Effects and add them to the ball
                if(effects.Count > 0)
                {
                    for(int i = 0; i < effects.Count; i++)
                    {                        
                        PawnEffects pe = effects[i];
                        Effects e = db.effects.Find(x => x.effect == pe.effect);

                        switch (pe.effect)
                        {
                            case Effect.Freeze: //Adds the Freeze effect to a ball hit
                                if(e != null)
                                {
                                    if(e.obj != null)
                                    {
                                        GameObject obj = Instantiate(e.obj);
                                        obj.transform.parent = collision.transform;
                                        obj.transform.localPosition = Vector3.zero;

                                        PlayerGrab spg = obj.GetComponent<PlayerGrab>();

                                        if(spg != null)
                                        {
                                            spg.playerIndex = pg.playerIndex;
                                        }
                                    }
                                }
                                break;
                            case Effect.Vamp: //Adds the Vamp effect to a ball hit
                                if (e != null)
                                {
                                    if (e.obj != null)
                                    {
                                        GameObject obj = Instantiate(e.obj);
                                        obj.transform.parent = collision.transform;
                                        obj.transform.localPosition = Vector3.zero;

                                        PlayerGrab spg = obj.GetComponent<PlayerGrab>();

                                        if (spg != null)
                                        {
                                            spg.playerIndex = pg.playerIndex;
                                        }
                                    }
                                }
                                break;
                            case Effect.Bunker: //Burst into a swarm of 1 shot balls
                                if(health - 1 <= 0)
                                {

                                }
                                break;
                            case Effect.WeaponUp: //Will increase the balls damage on hit
                                tbI.ball.damage += Mathf.FloorToInt(pe.amount);
                                break;
                        }
                    }
                }

                //Send out Ball hit to all
                health -= baseDamage;

                if(health <= 0)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}

[System.Serializable]
public class PawnEffects
{
    public Effect effect;
    public Vector3 dir = Vector3.zero;
    public float amount = 0;
    public bool shown = false;

    public PawnEffects(Effect effects, float amounts)
    {
        effect = effects;
        amount = amounts;
    }
}
