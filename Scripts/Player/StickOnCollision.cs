using System.Collections.Generic;
using UnityEngine;

public class StickOnCollision : MonoBehaviour
{
    public List<GameObject> stuckObjects = new List<GameObject>();
    public List<string> tagHits = new List<string>();
    public Vector3 offset = Vector3.zero;
    public Vector3 outVelocity = Vector3.zero;
    public Vector3 randomVelocityMin = Vector3.zero;
    public Vector3 randomVelocityMax = Vector3.zero;

    [Tooltip("Seconds after a throw before another catch is allowed.")]
    public float reCatchDelay = 0.75f;

    [Tooltip("When throwing, set stuck ball PlayerGrab.playerIndex to this object's owner.")]
    public bool assignOwnerOnThrow = true;

    float nextCatchTime;
    PlayerGrab ownerGrab;

    /// <summary>Fired once when a ball is newly stuck.</summary>
    public System.Action onCatch;

    /// <summary>Fired when ball(s) are thrown / unstuck.</summary>
    public System.Action onThrow;

    void Start()
    {
        ownerGrab = GetComponent<PlayerGrab>();
    }

    public bool CanCatch
    {
        get { return Time.time >= nextCatchTime && stuckObjects.Count == 0; }
    }

    void Update()
    {
        if (stuckObjects.Count <= 0)
            return;

        for (int i = stuckObjects.Count - 1; i >= 0; i--)
        {
            if (stuckObjects[i] != null)
            {
                stuckObjects[i].transform.position = transform.position
                    + (transform.right * offset.x)
                    + (transform.up * offset.y)
                    + (transform.forward * offset.z);
            }
        }
    }

    public void Unstick(GameObject obj = null)
    {
        List<GameObject> unstickObjs = new List<GameObject>();

        if (obj != null)
        {
            int sO = stuckObjects.FindIndex(x => x == obj);
            if (sO >= 0 && sO < stuckObjects.Count)
                unstickObjs.Add(obj);
        }
        else
        {
            unstickObjs = new List<GameObject>(stuckObjects);
        }

        if (unstickObjs.Count <= 0)
            return;

        if (ownerGrab == null)
            ownerGrab = GetComponent<PlayerGrab>();

        bool threwAny = false;
        for (int i = unstickObjs.Count - 1; i >= 0; i--)
        {
            GameObject uS = unstickObjs[i];
            if (uS == null)
                continue;

            Rigidbody soRb = uS.GetComponent<Rigidbody>();
            if (soRb != null)
            {
                float ranX = Random.Range(randomVelocityMin.x, randomVelocityMax.x);
                float ranY = Random.Range(randomVelocityMin.y, randomVelocityMax.y);
                float ranZ = Random.Range(randomVelocityMin.z, randomVelocityMax.z);

                soRb.linearVelocity =
                    (transform.right * (outVelocity.x + ranX))
                    + (transform.up * (outVelocity.y + ranY))
                    + (transform.forward * (outVelocity.z + ranZ));
            }

            if (assignOwnerOnThrow && ownerGrab != null && ownerGrab.IsLinked())
            {
                PlayerGrab ballGrab = uS.GetComponent<PlayerGrab>();
                if (ballGrab != null)
                    ballGrab.playerIndex = ownerGrab.playerIndex;
            }

            stuckObjects.Remove(uS);
            threwAny = true;
        }

        nextCatchTime = Time.time + Mathf.Max(0f, reCatchDelay);
        if (threwAny)
            onThrow?.Invoke();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!CanCatch)
            return;

        if (!tagHits.Exists(x => x.ToLower().Trim() == collision.gameObject.tag.ToLower().Trim()))
            return;

        if (stuckObjects.Exists(x => x == collision.gameObject))
            return;

        stuckObjects.Add(collision.gameObject);

        Rigidbody caughtRb = collision.rigidbody;
        if (caughtRb == null)
            caughtRb = collision.gameObject.GetComponent<Rigidbody>();
        if (caughtRb != null)
            caughtRb.linearVelocity = Vector3.zero;

        onCatch?.Invoke();
    }
}
