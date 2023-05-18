using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveUntilCollision : MonoBehaviour
{
    public Vector3 moveDir = Vector3.zero;
    public float speed = 5;

    public List<string> tags = new List<string>();

    bool run = true;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (run)
        {
            transform.position += transform.right * moveDir.x * speed * Time.deltaTime;
            transform.position += transform.up * moveDir.y * speed * Time.deltaTime;
            transform.position += transform.forward * moveDir.z * speed * Time.deltaTime;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (tags.Exists(x => x.ToLower().Trim() == collision.transform.tag.ToLower().Trim()))
        {
            run = false;
        }
    }
}
