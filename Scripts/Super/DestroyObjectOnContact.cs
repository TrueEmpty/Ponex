using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyObjectOnContact : MonoBehaviour
{
    public string tagHit = "Ball";
    public float delay = .1f;

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
        if (collision.transform.tag.ToLower().Trim() == tagHit.ToLower().Trim())
        {
            StartCoroutine(DestroyObject(collision.gameObject,delay));
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
