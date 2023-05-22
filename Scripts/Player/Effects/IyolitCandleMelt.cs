using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGrab))]
public class IyolitCandleMelt : MonoBehaviour
{
    PlayerGrab pG;
    public IyolitMovement iM;
    public float maxHealth = 10;
    public float curHealth = 10;
    float maxSize = 2.5f;
    Vector3 startPos = Vector3.zero;
    float damage = 40;
    string tagHit = "Ball";

    public Transform wick;
    public GameObject superFlame;
    public GameObject superFlameGo;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
        startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        //Check For Consistant Damage
        if(iM.SuperOn() || (iM.CurrentCandle() == transform && iM.bumpActive))
        {
            //Spawn Flame On Candle that also makes flame embers per sec
            if (superFlameGo == null)
            {
                superFlameGo = Instantiate(superFlame, wick.position, wick.rotation);

                PlayerGrab cpG = superFlameGo.GetComponent<PlayerGrab>();

                if (cpG != null)
                {
                    cpG.player = pG.player;
                }

            }

            curHealth -= ((damage / 100) * Time.deltaTime) * 2;
        }
        else
        {
            if(superFlameGo != null)
            {
                Destroy(superFlameGo);
                superFlameGo = null;
            }

            if (iM.CurrentCandle() == transform)
            {
                curHealth -= (damage / 100) * Time.deltaTime;
            }
        }

        SizeChange();
        DestoryAtNoHp();
    }
    
    void SizeChange()
    {
        float perHp = curHealth / maxHealth;

        Vector3 scale = transform.localScale;
        scale.y = perHp * maxSize;
        transform.localScale = scale;

        transform.position = startPos - (transform.up * ((1-perHp) * maxSize));
    }

    void DestoryAtNoHp()
    {
        if(curHealth <= 0)
        {
            if(superFlameGo != null)
            {
                Destroy(superFlameGo);
            }

            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
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

            if (iM.CurrentCandle() == transform || iM.SuperOn())
            {
                Rigidbody bRb = collision.gameObject.GetComponent<Rigidbody>();

                if(bRb != null)
                {
                    Vector3 bVe = bRb.velocity;
                    bVe /= 2;
                    bRb.velocity = bVe;

                    //Make a splat noise and some particles wax at hit point

                }
            }
        }
    }
}
