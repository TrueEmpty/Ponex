using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BallInfo))]
public class TimeBall : MonoBehaviour
{
    struct Snapshot
    {
        public float time;
        public Vector3 position;
        public Vector3 velocity;
    }

    Database db;
    BallInfo bI;
    Rigidbody rb;

    [Tooltip("How far back in time the warp looks.")]
    public float rewindSeconds = 2f;

    [Tooltip("Minimum seconds between warps.")]
    public float warpIntervalMin = 1.25f;

    [Tooltip("Maximum seconds between warps.")]
    public float warpIntervalMax = 9f;

    readonly List<Snapshot> history = new List<Snapshot>(128);
    float nextWarpTime;
    bool armed;

    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();
        rb = GetComponent<Rigidbody>();
        ScheduleNextWarp();
    }

    void FixedUpdate()
    {
        if (db == null || !db.gameStart || bI == null || !bI.ballReady || !bI.projectionOn || rb == null)
            return;

        float now = Time.time;
        history.Add(new Snapshot
        {
            time = now,
            position = rb.position,
            velocity = rb.linearVelocity
        });

        float cutoff = now - (rewindSeconds + 0.75f);
        while (history.Count > 0 && history[0].time < cutoff)
            history.RemoveAt(0);

        if (!armed)
        {
            if (history.Count > 0 && now - history[0].time >= rewindSeconds * 0.85f)
            {
                armed = true;
                nextWarpTime = now + Random.Range(warpIntervalMin, warpIntervalMax);
            }
            return;
        }

        if (now < nextWarpTime)
            return;

        WarpBack();
        ScheduleNextWarp();
    }

    void WarpBack()
    {
        float targetTime = Time.time - rewindSeconds;
        Snapshot best = default;
        bool found = false;
        float bestDelta = float.MaxValue;

        for (int i = 0; i < history.Count; i++)
        {
            float delta = Mathf.Abs(history[i].time - targetTime);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                best = history[i];
                found = true;
            }
        }

        if (!found)
            return;

        Vector3 pos = best.position;
        pos.z = rb.position.z;
        rb.position = pos;
        transform.position = pos;

        Vector3 vel = best.velocity;
        vel.z = 0f;
        if (vel.sqrMagnitude < 0.0001f && bI.ball != null)
            vel = rb.linearVelocity.normalized * Mathf.Max(bI.ball.minSpeed, 1f);

        rb.linearVelocity = vel;
        history.Clear();
        armed = false;
    }

    void ScheduleNextWarp()
    {
        nextWarpTime = Time.time + Random.Range(warpIntervalMin, warpIntervalMax);
    }
}
