using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerSelection : MonoBehaviour
{
    Vector3 lookatPoint;
    public Vector3 lookAtUp;
    float startX;
    public float maxHorizontal = 1;
    public Vector2 speedRange = Vector2.one;
    float speed = 1;
    int dir = 1;
    [Range(0, 1)]
    public float speedChangeChance = .35f;

    // Start is called before the first frame update
    void Start()
    {
        lookatPoint = transform.position + (transform.forward * 2);
        speed = Random.Range(speedRange.x, speedRange.y);
        startX = transform.position.x;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 tPos = transform.position;
        tPos.x += dir * speed * Time.deltaTime;

        if (speedChangeChance >= Random.Range(0f, 1f))
        {
            speed = Random.Range(speedRange.x, speedRange.y);
        }

        float abC = Mathf.Abs(maxHorizontal);
        if (tPos.x > startX + abC)
        {
            dir *= -1;
            tPos.x = startX + abC;
        }
        else if (tPos.x < startX - abC)
        {
            dir *= -1;
            tPos.x = startX - abC;
        }

        transform.position = tPos;
        transform.LookAt(lookatPoint, lookAtUp);
    }
}
