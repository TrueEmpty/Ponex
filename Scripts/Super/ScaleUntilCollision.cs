using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleUntilCollision : MonoBehaviour
{
    PlayerGrab pG;
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
        pG = GetComponent<PlayerGrab>();
    }

    // Update is called once per frame
    void Update()
    {
        if(pG != null)
        {
            if (!xMinHit || !xMaxHit || !yMinHit || !yMaxHit || !zMinHit || !zMaxHit)
            {
                transform.localScale += scaleAmount * speed * Time.deltaTime;
            }

            if (Mathf.Abs(scaleAmount.x) == 0)
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

            Vector3 pos = transform.position;

            pos += transform.right * raycastOffset.x;
            pos += transform.up * raycastOffset.y;
            pos += transform.forward * raycastOffset.z;

            if (Mathf.Abs(scaleAmount.x) > 0 && (!xMinHit || !xMaxHit))
            {
                float dis = (transform.lossyScale.x / 2) + .05f;

                if (!xMaxHit)
                {
                    if (TagHit(pos, transform.right, dis))
                    {
                        xMaxHit = true;
                    }
                }

                if (!xMinHit)
                {
                    if (TagHit(pos, transform.right * -1, dis))
                    {
                        xMinHit = true;
                    }
                }
            }

            if (Mathf.Abs(scaleAmount.y) > 0 && (!yMinHit || !yMaxHit))
            {
                float dis = (transform.lossyScale.y / 2) + .05f;

                if (!yMaxHit)
                {
                    if (TagHit(pos, transform.up, dis))
                    {
                        yMaxHit = true;
                    }
                }

                if (!yMinHit)
                {
                    if (TagHit(pos, transform.up * -1, dis))
                    {
                        yMinHit = true;
                    }
                }
            }

            if (Mathf.Abs(scaleAmount.z) > 0 && (!zMinHit || !zMaxHit))
            {
                float dis = (transform.lossyScale.z / 2) + .05f;

                if (!zMaxHit)
                {
                    if (TagHit(pos, transform.forward, dis))
                    {
                        zMaxHit = true;
                    }
                }

                if (!zMinHit)
                {
                    if (TagHit(pos, transform.forward * -1, dis))
                    {
                        zMinHit = true;
                    }
                }
            }
        }
    }

    bool TagHit(Vector3 pos,Vector3 dir,float distance)
    {
        bool result = false;

        RaycastHit[] hits = Physics.RaycastAll(pos, dir, distance);

        if(hits.Length > 0)
        {
            for(int i = 0; i < hits.Length; i++)
            {
                if(tags.Exists(x => x.ToLower().Trim() == hits[i].transform.tag.ToLower().Trim()))
                {
                    result = true;
                    break;
                }
            }
        }

        return result;
    }
}
