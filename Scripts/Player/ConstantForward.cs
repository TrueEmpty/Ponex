using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantForward : MonoBehaviour
{
    Rigidbody rb;

    public float speed = 1;
    public float time = -1;
    float lifetime = 0;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        rb.velocity = transform.up * speed;
        lifetime += Time.deltaTime;

        if (time != -1 && time <= lifetime)
        {
            rb.velocity = Vector3.zero;
        }
    }
}
