using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGrab))]
public class PassEffectOnTagHit : MonoBehaviour
{
    PlayerGrab pG;
    public GameObject effect;
    public List<string> tagsHit;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (tagsHit.Exists(x=> x.ToLower().Trim() == collision.transform.tag.ToLower().Trim()))
        {
            if(effect != null)
            {
                GameObject go = Instantiate(effect, collision.transform);

                if(go != null)
                {
                    go.transform.localPosition = Vector3.zero;
                    PlayerGrab vPG = go.GetComponent<PlayerGrab>();

                    if(vPG != null)
                    {
                        vPG.playerIndex = pG.playerIndex;
                    }
                }
            }
        }
    }
}
