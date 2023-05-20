using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyObjectOnContact : MonoBehaviour
{
    public List<string> tagHit = new List<string>();
    public float delay = .1f;
    public bool selfDestroy = false;

    // Start is called before the first frame update
    void Start()
    {
        if (delay <= 0)
        {
            delay = 0.001f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (tagHit.Exists(x=> x.ToLower().Trim() == collision.transform.tag.ToLower().Trim()))
        {
            StartCoroutine(DestroyObject(selfDestroy ? gameObject : collision.gameObject,delay));
        }
    }

    IEnumerator DestroyObject(GameObject objectHit,float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        if(objectHit != null)
        {
            Destroy(objectHit);
        }

        yield return null;
    }
}
