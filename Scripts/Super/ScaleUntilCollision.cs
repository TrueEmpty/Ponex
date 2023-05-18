using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleUntilCollision : MonoBehaviour
{
    public Vector3 scaleAmount = Vector3.one;
    public float speed = 5;

    public List<string> tags = new List<string>();

    bool xMinHit = false;
    bool xMaxHit = false;
    bool yMinHit = false;
    bool yMaxHit = false;
    bool zMinHit = false;
    bool zMaxHit = false;
    public Vector3 raycastOffset = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!xMinHit || !xMaxHit || !yMinHit || !yMaxHit || !zMinHit || !zMaxHit)
        {
            transform.localScale += scaleAmount * speed * Time.deltaTime;
        }

        if(Mathf.Abs(scaleAmount.x) == 0)
        {
            xMinHit = true;
            xMaxHit = true;
        }

        if (Mathf.Abs(scaleAmount.y) == 0)
        {
            yMinHit = true;
            yMaxHit = true;
        }

        if (Mathf.Abs(scaleAmount.z) == 0)
        {
            zMinHit = true;
            zMaxHit = true;
        }

        RaycastHit hit;
        Vector3 pos = transform.position;

        pos += transform.right * raycastOffset.x;
        pos += transform.up * raycastOffset.y;
        pos += transform.forward * raycastOffset.z;

        if (Mathf.Abs(scaleAmount.x) > 0 && (!xMinHit || !xMaxHit))
        {
            float dis = (transform.lossyScale.x / 2) + .05f;

            if(!xMaxHit)
            {
                if (Physics.Raycast(pos, transform.right, out hit, dis))
                {
                    if (tags.Exists(x => x.ToLower().Trim() == hit.transform.tag.ToLower().Trim()))
                    {
                        xMaxHit = true;
                    }
                }
            }

            if (!xMinHit)
            {
                if (Physics.Raycast(pos, transform.right * -1, out hit, dis))
                {
                    if (tags.Exists(x => x.ToLower().Trim() == hit.transform.tag.ToLower().Trim()))
                    {
                        xMinHit = true;
                    }
                }
            }
        }

        if (Mathf.Abs(scaleAmount.y) > 0 && (!yMinHit || !yMaxHit))
        {
            float dis = (transform.lossyScale.y / 2) + .05f;

            if (!yMaxHit)
            {
                if (Physics.Raycast(pos, transform.up, out hit, dis))
                {
                    if (tags.Exists(x => x.ToLower().Trim() == hit.transform.tag.ToLower().Trim()))
                    {
                        yMaxHit = true;
                    }
                }
            }

            if (!yMinHit)
            {
                if (Physics.Raycast(pos, transform.up * -1, out hit, dis))
                {
                    if (tags.Exists(x => x.ToLower().Trim() == hit.transform.tag.ToLower().Trim()))
                    {
                        yMinHit = true;
                    }
                }
            }
        }

        if (Mathf.Abs(scaleAmount.z) > 0 && (!zMinHit || !zMaxHit))
        {
            float dis = (transform.lossyScale.z / 2) + .05f;

            if (!zMaxHit)
            {
                if (Physics.Raycast(pos, transform.forward, out hit, dis))
                {
                    if (tags.Exists(x => x.ToLower().Trim() == hit.transform.tag.ToLower().Trim()))
                    {
                        zMaxHit = true;
                    }
                }
            }

            if (!zMinHit)
            {
                if (Physics.Raycast(pos, transform.forward * -1, out hit, dis))
                {
                    if (tags.Exists(x => x.ToLower().Trim() == hit.transform.tag.ToLower().Trim()))
                    {
                        zMinHit = true;
                    }
                }
            }
        }
    }
}
