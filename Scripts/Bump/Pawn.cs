using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Pawn : MonoBehaviour
{
    Database db;
    PlayerGrab pg;

    public PawnType pawnType = PawnType.Ranger;
    public int health = 1;
    public int level = 1;
    Renderer ren;
    Material material;
    Color lastRenderedColor;
    bool hasRenderedColor;

    public List<PawnEffects> effects = new List<PawnEffects>();

    [Header("Gameplay data")]
    public List<Collider> colliders;

    [HideInInspector]
    public bool selectionPiece = false;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        pg = GetComponent<PlayerGrab>();
        ren = GetComponent<Renderer>();
        if (ren != null)
            material = ren.material;

        if (selectionPiece)
        {
            PrepareGhost();
            return;
        }

        if (GetComponent<DestroyOnDeath>() == null)
            gameObject.AddComponent<DestroyOnDeath>();
        if (GetComponent<ClearAfterTheGame>() == null)
            gameObject.AddComponent<ClearAfterTheGame>();

        if (home == Vector3.zero)
            home = transform.position;
        RegisterWithOwner();
        BeginRole();
    }

    void RegisterWithOwner()
    {
        if (pg == null || !pg.IsLinked() || pg.player == null || pg.player.spawnedPlayer == null)
            return;
        Gaurd owner = pg.player.spawnedPlayer.GetComponent<Gaurd>();
        if (owner != null)
            owner.RegisterPawn(this);
    }

    // Update is called once per frame
    void Update()
    {
        if (pg != null && pg.IsLinked() && pg.player != null && pg.player.currentHealth <= 0)
        {
            Destroy(gameObject);
            return;
        }

        if (selectionPiece)
        {
            PaintGhost();
            return;
        }

        Updater();
        TickRole();
    }

    void Updater()
    {
        if (health > 0)
        {
            if (material != null)
            {
                Color wantedColor;
                if (health - 1 < db.pawnColors.Count)
                {
                    wantedColor = db.pawnColors[health - 1];
                }
                else
                {
                    wantedColor = db.pawnColors[^1];
                }

                if (!hasRenderedColor || wantedColor != lastRenderedColor)
                {
                    material.color = wantedColor;
                    lastRenderedColor = wantedColor;
                    hasRenderedColor = true;
                }
            }

            //Show Effects
            if (effects.Count > 0)
            {
                for (int i = 0; i < effects.Count; i++)
                {
                    PawnEffects pe = effects[i];
                    Effects e = FindEffect(pe.effect);

                    if (!pe.shown)
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
            Die(null);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (selectionPiece)
            return;

        if (!IsBall(collision.collider))
        {
            if (IsStructure(collision.collider))
                Bounce(collision);
            else
                PassThrough(collision.collider);
            return;
        }

        if (pg == null)
            pg = GetComponent<PlayerGrab>();

        Claim(collision.gameObject);
        BallMovement ballMove = collision.gameObject.GetComponent<BallMovement>();
        if (ballMove != null)
            ballMove.BounceOffPawn(collision);

        {
            PlayerGrab tpG = collision.gameObject.GetComponent<PlayerGrab>();
            BallInfo tbI = collision.gameObject.GetComponent<BallInfo>();
            bool pass = !AlreadyOurBall(tpG);

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
                        Effects e = FindEffect(pe.effect);

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

            }
        }

        TakeBallHit(collision.gameObject);
    }

    void OnCollisionStay(Collision collision)
    {
        if (selectionPiece || collision == null)
            return;
        if (!IsBall(collision.collider) && !IsStructure(collision.collider))
            PassThrough(collision.collider);
    }

    Effects FindEffect(Effect effect)
    {
        for (int i = 0; i < db.effects.Count; i++)
        {
            Effects candidate = db.effects[i];
            if (candidate != null && candidate.effect == effect)
                return candidate;
        }
        return null;
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

public enum PawnType
{
    Reflector,
    Attacker,
    Patroller,
    Ranger,
    Defender
}