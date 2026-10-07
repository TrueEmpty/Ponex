using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared computer controller. Drop on any player root (or auto-added for CPU players).
/// Character scripts read Decision / FreeRoam / Candle intents instead of each calling Evaluate.
/// New characters: add this + set Mode — move/dash/super intents come for free.
/// </summary>
[DefaultExecutionOrder(-80)]
[RequireComponent(typeof(PlayerGrab))]
public class ComputerBrain : MonoBehaviour
{
    public enum Mode
    {
        LanePaddle,
        FreeRoam,
        Candle
    }

    public Mode mode = Mode.LanePaddle;
    public List<string> hitTags = new List<string> { "Wall", "Walls", "Obstacle" };
    public float wallStopDistance = 1.38f;
    public float freeRoamArrive = 0.55f;
    public float freeRoamDashMin = 1.6f;

    PlayerGrab pg;
    Database db;
    Rigidbody rb;
    IyolitMovement iyolit;

    public ComputerAI.Decision Lane { get; private set; }
    public ComputerAI.FreeRoamDecision FreeRoam { get; private set; }
    public Thought Thought { get; private set; } = Thought.Nothing;

    public int MoveDir => Lane.moveDir;
    public bool WantBump => Lane.wantBump;
    public bool WantSuper => Lane.wantSuper;
    public bool WantDash => mode == Mode.FreeRoam ? FreeRoam.wantDash : Lane.wantDash;

    void Awake()
    {
        pg = GetComponent<PlayerGrab>();
        rb = GetComponent<Rigidbody>();
        iyolit = GetComponent<IyolitMovement>();
        if (iyolit != null)
            mode = Mode.Candle;
        else if (GetComponent<NariMove>() != null)
            mode = Mode.FreeRoam;
    }

    void Start()
    {
        db = Database.instance;
    }

    void Update()
    {
        if (db == null)
            db = Database.instance;
        if (db == null || !db.gameStart || pg == null || !pg.IsLinked() || pg.player == null)
            return;
        if (!pg.player.computer || pg.player.currentHealth <= 0)
            return;

        switch (mode)
        {
            case Mode.LanePaddle:
                TickLane();
                break;
            case Mode.FreeRoam:
                // NariMove already runs EvaluateFreeRoam — don't double-tick
                if (GetComponent<NariMove>() == null)
                    TickFreeRoam();
                break;
            case Mode.Candle:
                TickCandle();
                break;
        }
    }

    void TickLane()
    {
        Lane = ComputerAI.Evaluate(transform, pg.player, hitTags, wallStopDistance);
        if (Lane.wantBump)
            Thought = Thought.MoveUp;
        else if (Lane.wantSuper)
            Thought = Thought.MoveDown;
        else
            Thought = Thought.Nothing;
    }

    void TickFreeRoam()
    {
        Vector3 cur = rb != null ? rb.linearVelocity.normalized : Vector3.up;
        FreeRoam = ComputerAI.EvaluateFreeRoam(
            transform, pg.player, cur, freeRoamArrive, freeRoamDashMin,
            dir => PaddleWall.WallInDirection(transform, dir.x >= 0f ? 1 : -1, hitTags, wallStopDistance));
        Lane = ComputerAI.GetLastDecision(pg.playerIndex);
        Thought = Thought.Nothing;
    }

    void TickCandle()
    {
        // Iyolit: sit on candles; pick the candle nearest the AI threat aim point
        if (iyolit == null || iyolit.positions == null || iyolit.positions.Count == 0)
            return;
        if (iyolit.moving)
            return;

        Lane = ComputerAI.Evaluate(transform, pg.player, hitTags, wallStopDistance);
        Thought = Lane.wantSuper ? Thought.MoveDown : (Lane.wantBump ? Thought.MoveUp : Thought.Nothing);

        // Prefer predicted bounce on our side via future points already on BallInfo
        Vector3 aim = transform.position;
        if (ComputerAI.TryGetAimPoint(pg.player, transform, out Vector3 threatAim))
            aim = threatAim;

        int best = iyolit.currentPosition;
        float bestScore = float.PositiveInfinity;
        for (int i = 0; i < iyolit.positions.Count; i++)
        {
            Transform t = iyolit.positions[i];
            if (t == null)
                continue;
            float d = (t.position - aim).sqrMagnitude;
            if (d < bestScore)
            {
                bestScore = d;
                best = i;
            }
        }

        if (best != iyolit.currentPosition && !iyolit.moving)
            iyolit.currentPosition = best;

        if (Lane.wantSuper && iyolit != null)
            iyolit.ActivateSuper();
    }

    /// <summary>Ensure a brain exists on a spawned player (called from Database).</summary>
    public static ComputerBrain Ensure(GameObject root, Mode preferred = Mode.LanePaddle)
    {
        if (root == null)
            return null;
        ComputerBrain brain = root.GetComponent<ComputerBrain>();
        if (brain == null)
            brain = root.AddComponent<ComputerBrain>();
        if (root.GetComponent<IyolitMovement>() != null)
            brain.mode = Mode.Candle;
        else if (root.GetComponent<NariMove>() != null)
            brain.mode = Mode.FreeRoam;
        else
            brain.mode = preferred;
        return brain;
    }
}
