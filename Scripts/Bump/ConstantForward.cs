using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantForward : MonoBehaviour
{
    Bump bump;
    Rigidbody rb;

    public float speed = 1;

    // Start is called before the first frame update
    void Start()
    {
        bump = GetComponent<Bump>();
        Rigidbody rb = GetComponent<Rigidbody>();

        rb.velocity += transform.up * speed;
    }
}
