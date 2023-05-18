using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfterLifetime : MonoBehaviour
{
    public float duration = .5f;
    float lifetime = 0;

    // Start is called before the first frame update
    void Start()
    {
        lifetime = Time.time + duration;
    }

    // Update is called once per frame
    void Update()
    {
        if (lifetime < Time.time)
        {
            Destroy(gameObject);
        }
    }
}
