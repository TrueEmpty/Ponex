using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IyolitBumpMovement : MonoBehaviour
{
    float timeBeforeFall = 0;
    float rightAmount = 0;

    float timer = 0;

    // Start is called before the first frame update
    void Start()
    {
        timeBeforeFall = Random.Range(0f,1f);
        rightAmount = Random.Range(-1f, 1f);
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        transform.position += transform.up * ((timer >= timeBeforeFall) ? -.4f : 1) * 4.7f * Time.deltaTime;
        transform.position += transform.right * (rightAmount + Random.Range(-.4f,.4f)) * Time.deltaTime;
    }
}
