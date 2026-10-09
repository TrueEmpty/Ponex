using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class PullObjectIn : MonoBehaviour
{
    public List<string> tagPull;
    public List<GameObject> hits = new List<GameObject>();
    public List<GameObject> ejecting = new List<GameObject>();

    public float strength = 0;
    public float distance = 5;
    public GameObject distancer;
    public bool active = true;
    public Vector3 scale = Vector3.one;
    public float disScale = 3;

    public bool ejectOnCollision = true;

    readonly List<GameObject> liveBalls = new List<GameObject>(32);
    readonly Dictionary<GameObject, Rigidbody> hitBodies = new Dictionary<GameObject, Rigidbody>(32);
    readonly List<GameObject> staleBodyKeys = new List<GameObject>(8);
    Collider[] overlapBuffer = new Collider[32];
    float nextBodyCacheCleanup;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(active)
        {
            SizeDistancer(distance);
            CollectHits();

            if (hits.Count > 0)
            {
                for(int i = hits.Count - 1; i >= 0; i--)
                {
                    GameObject pO = hits[i];

                    if(pO != null)
                    {
                        if (!hitBodies.TryGetValue(pO, out Rigidbody rb))
                        {
                            rb = pO.GetComponent<Rigidbody>();
                            hitBodies[pO] = rb;
                        }

                        if(rb != null)
                        {
                            Vector3 dif = transform.position - pO.transform.position;
                            int inverse = ejecting.Contains(pO) ? -1 : 1;

                            rb.AddForce(inverse * dif * strength * Time.deltaTime);
                        }
                    }
                }
            }
        }
        else
        {
            SizeDistancer(.01f,-1);
        }
    }

    void SizeDistancer(float amount,int dir = 1)
    {
        if(distancer != null)
        {
            Vector3 ds = distancer.transform.localScale;

            if (Mathf.Abs(ds.x - scale.x * amount) > .1f && Mathf.Abs(ds.x - scale.x * amount) < amount + .1f &&
                Mathf.Abs(ds.y - scale.y * amount) > .1f && Mathf.Abs(ds.y - scale.y * amount) < amount + .1f &&
                Mathf.Abs(ds.z - scale.z * amount) > .1f && Mathf.Abs(ds.z - scale.z * amount) < amount + .1f)
            {
                ds.x += dir * 2 * Time.deltaTime;
                ds.y += dir * 2 * Time.deltaTime;
                ds.z += dir * 2 * Time.deltaTime;

                distancer.transform.localScale = ds;
            }
            else
            {
                distancer.transform.localScale = scale * amount;
            }
        }
    }

    void CollectHits()
    {
        hits.Clear();
        CleanupBodyCache();

        Vector3 curPos = transform.position;
        float maxDistance = distance * disScale;
        float maxDistanceSq = maxDistance * maxDistance;

        if (maxDistance >= 0f && PullsOnlyBallTag())
        {
            LiveBallRegistry.CopyLiveGameObjects(liveBalls);
            for (int i = 0; i < liveBalls.Count; i++)
            {
                GameObject ball = liveBalls[i];
                if (ball == null || !MatchesPullTag(ball.tag))
                    continue;
                if ((ball.transform.position - curPos).sqrMagnitude > maxDistanceSq)
                    continue;
                hits.Add(ball);
                if (!hitBodies.TryGetValue(ball, out Rigidbody body) || body == null)
                    hitBodies[ball] = ball.GetComponent<Rigidbody>();
            }
        }
        else if (maxDistance >= 0f)
        {
            int count = OverlapNearby(curPos, Mathf.Max(0f, maxDistance));
            for (int i = 0; i < count; i++)
            {
                Collider col = overlapBuffer[i];
                if (col == null)
                    continue;
                GameObject candidate = col.attachedRigidbody != null
                    ? col.attachedRigidbody.gameObject
                    : col.gameObject;
                if (candidate == null || !candidate.activeInHierarchy || !MatchesPullTag(candidate.tag)
                    || hits.Contains(candidate))
                    continue;
                hits.Add(candidate);
                if (!hitBodies.TryGetValue(candidate, out Rigidbody body) || body == null)
                    hitBodies[candidate] = col.attachedRigidbody != null
                        ? col.attachedRigidbody
                        : candidate.GetComponent<Rigidbody>();
            }
        }

        //Clean up ejecting
        if(ejecting.Count > 0)
        {
            for(int i = ejecting.Count - 1; i >= 0; i--)
            {
                if(!hits.Contains(ejecting[i]))
                {
                    ejecting.RemoveAt(i);
                }
            }
        }
    }

    void CleanupBodyCache()
    {
        if (Time.unscaledTime < nextBodyCacheCleanup)
            return;
        nextBodyCacheCleanup = Time.unscaledTime + 2f;
        staleBodyKeys.Clear();
        foreach (KeyValuePair<GameObject, Rigidbody> pair in hitBodies)
        {
            if (pair.Key == null || pair.Value == null)
                staleBodyKeys.Add(pair.Key);
        }
        for (int i = 0; i < staleBodyKeys.Count; i++)
            hitBodies.Remove(staleBodyKeys[i]);
    }

    bool PullsOnlyBallTag()
    {
        if (tagPull == null || tagPull.Count == 0)
            return false;
        bool foundBall = false;
        for (int i = 0; i < tagPull.Count; i++)
        {
            string tag = tagPull[i];
            if (string.IsNullOrWhiteSpace(tag))
                continue;
            if (!tag.Trim().Equals("Ball", StringComparison.OrdinalIgnoreCase))
                return false;
            foundBall = true;
        }
        return foundBall;
    }

    bool MatchesPullTag(string candidate)
    {
        if (string.IsNullOrEmpty(candidate) || tagPull == null)
            return false;
        for (int i = 0; i < tagPull.Count; i++)
        {
            string tag = tagPull[i];
            if (tag != null && tag.Trim().Equals(candidate, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    int OverlapNearby(Vector3 center, float radius)
    {
        int count;
        while ((count = Physics.OverlapSphereNonAlloc(
            center, radius, overlapBuffer, ~0, QueryTriggerInteraction.Collide)) == overlapBuffer.Length)
        {
            overlapBuffer = new Collider[overlapBuffer.Length * 2];
        }
        return count;
    }

    private void OnCollisionExit(Collision collision)
    {
        if(ejectOnCollision)
        {
            GameObject pO = collision.gameObject;

            if (hits.Contains(pO))
            {
                if(!ejecting.Contains(pO))
                {
                    ejecting.Add(pO);
                }
            }
        }
    }
}
