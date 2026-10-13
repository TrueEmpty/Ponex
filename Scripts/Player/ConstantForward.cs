using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantForward : MonoBehaviour
{
    Rigidbody rb;

    public float speed = 1;
    public float time = -1;
    float lifetime = 0;
    bool halted;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>Stop forward travel and hold position.</summary>
    public void Halt()
    {
        halted = true;
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = Vector3.zero;
    }

    void Update()
    {
        if (rb == null)
            return;

        if (halted || !Database.MatchPlayActive)
        {
            if (halted)
                rb.linearVelocity = Vector3.zero;
            return;
        }

        rb.linearVelocity = transform.up * speed;
        lifetime += Time.deltaTime;

        if (time != -1 && time <= lifetime)
            Halt();
    }
}
