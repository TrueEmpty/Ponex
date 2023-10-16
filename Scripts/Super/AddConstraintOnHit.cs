using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddConstraintOnHit : MonoBehaviour
{
    public List<PlayerConstraintHolder> constrants;
    PlayerGrab pG;

    public List<string> hitTags;
    public List<string> destroyTags;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
    }

    public void Ball_Hit(GameObject collision)
    {
        if (collision != null && pG != null)
        {
            bool pass = true;
            PlayerGrab tpg = collision.GetComponent<PlayerGrab>();

            if (tpg != null)
            { 
                if(tpg.IsLinked())
                {
                    if (tpg.playerIndex == pG.playerIndex)
                    {
                        pass = false;
                    }

                    if (pass)
                    {
                        //Check if to apply constraint
                        if (hitTags.Exists(x => x.ToLower().Trim() == collision.transform.tag.ToLower().Trim()))
                        {
                            if(constrants.Count > 0)
                            {
                                for(int i = 0; i < constrants.Count; i++)
                                {
                                    PlayerConstraintHolder pC = constrants[i];

                                    tpg.player.AddConstraint(gameObject,pC.length,pC.constraint);
                                }
                            }
                        }

                        //Check if to Destory
                        if (destroyTags.Exists(x => x.ToLower().Trim() == collision.transform.tag.ToLower().Trim()))
                        {
                            Destroy(gameObject);
                        }
                    }
                }
            }
        }
    }
}
