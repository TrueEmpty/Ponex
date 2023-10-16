using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGrab))]
public class HealOnLifelineHit : MonoBehaviour
{
    PlayerGrab pG;
    public BallInfo bI;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
        bI = transform.parent.GetComponent<BallInfo>();
    }

    public void Ball_Hit(GameObject collision)
    {
        if(collision != null && pG != null && bI != null)
        {
            switch (collision.transform.tag)
            {
                case "Player":
                    PlayerGrab tpG = collision.gameObject.GetComponent<PlayerGrab>();

                    if (tpG != null)
                    {
                        if (tpG.player != pG.player)
                        {
                            Destroy(gameObject);
                        }
                    }
                    break;
                case "Lifeline":
                    PlayerGrab lpG = collision.gameObject.GetComponent<PlayerGrab>();

                    if (lpG != null)
                    {
                        if (lpG.player != pG.player)
                        {
                            pG.player.Heal(bI != null ? bI.ball.damage : 1);
                        }
                    }
                    break;
            }
        }
    }
}
