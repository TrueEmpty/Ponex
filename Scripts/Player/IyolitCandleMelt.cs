using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGrab))]
public class IyolitCandleMelt : MonoBehaviour
{
    PlayerGrab pG;
    Database db;
    public IyolitMovement iM;
    public float maxHealth = 10;
    public float curHealth = 10;
    float maxSize = 2.5f;
    Vector3 startPos = Vector3.zero;
    Vector3 startScale = Vector3.one;
    float damage = 40;
    string tagHit = "Ball";

    public Transform wick;
    public GameObject superFlame;
    public GameObject superFlameGo;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
        db = Database.instance;
        startPos = transform.position;
        startScale = transform.localScale;
        if (startScale.y > 0.01f)
            maxSize = startScale.y;
    }

    // Update is called once per frame
    void Update()
    {
        // Don't melt / flame during pre-match countdown
        if (db == null)
            db = Database.instance;
        if (db == null || !db.gameStart || iM == null)
        {
            ClearFlame();
            SizeChange();
            return;
        }

        //Check For Consistant Damage
        if(iM.SuperOn() || (iM.CurrentCandle() == transform && iM.bumpActive))
        {
            //Spawn Flame On Candle that also makes flame embers per sec
            if (superFlameGo == null && superFlame != null && wick != null)
            {
                superFlameGo = Instantiate(superFlame, wick.position, wick.rotation);
                superFlameGo.transform.SetParent(wick, true);

                PlayerGrab cpG = superFlameGo.GetComponent<PlayerGrab>();

                if (cpG != null)
                {
                    cpG.playerIndex = pG.playerIndex;
                }

                IyolitFlameFx.Ensure(superFlameGo, IyolitFlameFx.Style.Candle);
                // Tuck side flames inward + drop center so the trio reads as one body flame
                IyolitFlameFx.LayoutCandleFlames(superFlameGo.transform);
            }

            curHealth -= ((damage / 100) * Time.deltaTime) * 2;
        }
        else
        {
            ClearFlame();

            if (iM.CurrentCandle() == transform)
            {
                curHealth -= (damage / 100) * Time.deltaTime;
            }
        }

        SizeChange();
        DestoryAtNoHp();
    }

    void ClearFlame()
    {
        if (superFlameGo != null)
        {
            Destroy(superFlameGo);
            superFlameGo = null;
        }
    }
    
    void SizeChange()
    {
        float perHp = maxHealth > 0.0001f ? Mathf.Clamp01(curHealth / maxHealth) : 0f;

        Vector3 scale = startScale;
        scale.y = perHp * maxSize;
        transform.localScale = scale;

        transform.position = startPos - (transform.up * ((1f - perHp) * maxSize));
    }

    void DestoryAtNoHp()
    {
        if(curHealth <= 0)
        {
            ClearFlame();
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (db == null || !db.gameStart)
            return;

        if (collision.transform.tag.ToLower().Trim() == tagHit.ToLower().Trim())
        {
            //Check if you own the object
            PlayerGrab tpG = collision.gameObject.GetComponent<PlayerGrab>();
            BallInfo tbI = collision.gameObject.GetComponent<BallInfo>();
            bool pass = true;

            if (tpG != null)
            {
                if (tpG.player != null)
                {
                    if (tpG.player == pG.player)
                    {
                        pass = false;
                    }
                }
            }

            if (pG.player != null && pass)
            {
                int baseDamage = 0;

                if (tbI != null)
                {
                    baseDamage = tbI.ball.damage;
                }

                //Send out Ball hit to all
                curHealth -= baseDamage * damage;
            }

            if (iM != null && (iM.CurrentCandle() == transform || iM.SuperOn()))
            {
                Rigidbody bRb = collision.gameObject.GetComponent<Rigidbody>();

                if(bRb != null)
                {
                    Vector3 bVe = bRb.linearVelocity;
                    bVe /= 2;
                    bRb.linearVelocity = bVe;

                    //Make a splat noise and some particles wax at hit point

                }
            }
        }
    }
}
