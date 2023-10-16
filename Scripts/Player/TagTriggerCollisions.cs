using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TagTriggerCollisions : MonoBehaviour
{
    public List<string> tagPull;
    public List<GameObject> hits = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(hits.Count > 0)
        {
            for(int i = hits.Count - 1; i >= 0; i--)
            {
                if(hits[i] == null)
                {
                    hits.RemoveAt(i);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(tagPull.Exists(x=> x.ToLower().Trim() == other.tag.ToLower().Trim()))
        {
            hits.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (hits.Count > 0)
        {
            hits.RemoveAll(x => x = other.gameObject);
        }
    }
}
