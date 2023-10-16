using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PullObjectIn : MonoBehaviour
{
    public List<string> tagPull;
    public List<GameObject> hits = new List<GameObject>();
    public List<GameObject> ejecting = new List<GameObject>();

    public float strength = 0;
    public float distance = 5;
    public GameObject distancer;
    public bool active = true;
    public Vector3 scale = Vector3.one;
    public float disScale = 3;

    public bool ejectOnCollision = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(active)
        {
            SizeDistancer(distance);
            CollectHits();

            if (hits.Count > 0)
            {
                for(int i = hits.Count - 1; i >= 0; i--)
                {
                    GameObject pO = hits[i];

                    if(pO != null)
                    {
                        Rigidbody rb = pO.GetComponent<Rigidbody>();

                        if(rb != null)
                        {
                            Vector3 dif = transform.position - pO.transform.position;
                            int inverse = ejecting.Contains(pO) ? -1 : 1;

                            rb.AddForce(inverse * dif * strength * Time.deltaTime);
                        }
                    }
                }
            }
        }
        else
        {
            SizeDistancer(.01f,-1);
        }
    }

    void SizeDistancer(float amount,int dir = 1)
    {
        if(distancer != null)
        {
            Vector3 ds = distancer.transform.localScale;

            if (Mathf.Abs(ds.x - scale.x * amount) > .1f && Mathf.Abs(ds.x - scale.x * amount) < amount + .1f &&
                Mathf.Abs(ds.y - scale.y * amount) > .1f && Mathf.Abs(ds.y - scale.y * amount) < amount + .1f &&
                Mathf.Abs(ds.z - scale.z * amount) > .1f && Mathf.Abs(ds.z - scale.z * amount) < amount + .1f)
            {
                ds.x += dir * 2 * Time.deltaTime;
                ds.y += dir * 2 * Time.deltaTime;
                ds.z += dir * 2 * Time.deltaTime;

                distancer.transform.localScale = ds;
            }
            else
            {
                distancer.transform.localScale = scale * amount;
            }
        }
    }

    void CollectHits()
    {
        hits.Clear();

        GameObject[] allGO = GameObject.FindObjectsOfType<GameObject>();

        if (allGO.Length > 0)
        {
            Vector3 curPos = transform.position;

            for (int i = 0; i < allGO.Length; i++)
            {
                GameObject cGo = allGO[i];

                if(cGo != null)
                {
                    if(cGo.activeInHierarchy)
                    {
                        if (tagPull.Exists(x => x.ToLower().Trim() == cGo.tag.ToLower().Trim()))
                        {
                            if (Vector3.Distance(cGo.transform.position, curPos) <= distance * disScale)
                            {
                                hits.Add(cGo);
                            }
                        }
                    }
                }
            }
        }

        //Clean up ejecting
        if(ejecting.Count > 0)
        {
            for(int i = ejecting.Count - 1; i >= 0; i--)
            {
                if(!hits.Contains(ejecting[i]))
                {
                    ejecting.RemoveAt(i);
                }
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if(ejectOnCollision)
        {
            GameObject pO = collision.gameObject;

            if (hits.Contains(pO))
            {
                if(!ejecting.Contains(pO))
                {
                    ejecting.Add(pO);
                }
            }
        }
    }
}
