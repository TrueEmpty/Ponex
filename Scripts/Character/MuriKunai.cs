using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Throw / blink controller for Muri. Down or Super throws; Up/Left/Right blinks to stuck kunai.
/// </summary>
[DefaultExecutionOrder(45)]
[RequireComponent(typeof(PlayerGrab), typeof(MuriMove))]
public class MuriKunai : MonoBehaviour
{
    public GameObject kunaiPrefab;
    public Transform aimArrow;
    public float throwCooldown = 0.15f;
    public float throwSpeed = 22f;
    public int maxKunai = 3;
    public float aimSweep = 70f;
    public float aimPeriod = 1.15f;
    public float spawnOffset = 0.38f;

    PlayerGrab pg;
    MuriMove move;
    MuriShield shield;
    ComputerBrain brain;
    float nextThrow;
    float nextBlink;
    float aimT = 0.5f;
    bool aimRight = true;
    bool waitingRelease;
    readonly List<MuriKunaiProjectile> active = new List<MuriKunaiProjectile>(4);

    public Vector3 ThrowDirection { get; private set; } = Vector3.up;

    void Awake()
    {
        pg = GetComponent<PlayerGrab>();
        move = GetComponent<MuriMove>();
        shield = GetComponent<MuriShield>();
        if (kunaiPrefab == null)
            kunaiPrefab = Resources.Load<GameObject>("MuriKunai");
        if (aimArrow == null)
        {
            Transform found = transform.Find("AimArrow");
            if (found != null)
                aimArrow = found;
        }
    }

    void Start()
    {
        brain = GetComponent<ComputerBrain>();
        SyncMeter();
        TickAim(0f);
    }

    void Update()
    {
        if (pg == null || !pg.IsLinked() || pg.player == null)
            return;
        if (pg.player.currentHealth <= 0)
        {
            if (aimArrow != null)
                aimArrow.gameObject.SetActive(false);
            return;
        }

        Prune();
        TickAim(Time.deltaTime);
        if (Database.instance != null && !Database.instance.gameStart)
            return;
        if (waitingRelease)
        {
            if (AnyActionHeld())
                return;
            waitingRelease = false;
        }
        HandleThrow();
        HandleBlink();
    }

    void TickAim(float dt)
    {
        bool canThrow = SlotsLeft() > 0;
        if (aimArrow != null)
            aimArrow.gameObject.SetActive(canThrow);
        if (!canThrow)
            return;

        float step = dt / Mathf.Max(0.05f, aimPeriod);
        if (aimRight)
        {
            aimT += step;
            if (aimT >= 1f)
            {
                aimT = 1f;
                aimRight = false;
            }
        }
        else
        {
            aimT -= step;
            if (aimT <= 0f)
            {
                aimT = 0f;
                aimRight = true;
            }
        }

        float angle = Mathf.Lerp(-aimSweep, aimSweep, aimT);
        Vector3 up = move != null ? move.StandUp : transform.up;
        ThrowDirection = (Quaternion.AngleAxis(angle, Vector3.forward) * up).normalized;
        if (ThrowDirection.sqrMagnitude < 0.0001f)
            ThrowDirection = up;

        if (aimArrow != null)
        {
            aimArrow.position = transform.position + ThrowDirection * (spawnOffset + 0.12f);
            aimArrow.rotation = Quaternion.LookRotation(Vector3.forward, ThrowDirection);
        }
    }

    void HandleThrow()
    {
        if (waitingRelease)
            return;
        if (Time.time < nextThrow || SlotsLeft() <= 0 || FlyingCount() > 0)
            return;
        if (kunaiPrefab == null)
            return;

        bool want = pg.inp.tf_down || pg.inp.tf_super;
        if (pg.player.computer)
            want = brain != null && (brain.Thought == Thought.MoveDown || brain.WantSuper);

        if (!want)
            return;

        Vector3 dir = ThrowDirection;
        Vector3 pos = transform.position + dir * spawnOffset;
        pos.z = transform.position.z;
        GameObject go = Instantiate(kunaiPrefab, pos, Quaternion.LookRotation(Vector3.forward, dir));
        PlayerGrab kpg = go.GetComponent<PlayerGrab>();
        if (kpg != null)
            kpg.playerIndex = pg.playerIndex;

        MuriKunaiProjectile proj = go.GetComponent<MuriKunaiProjectile>();
        if (proj == null)
            proj = go.AddComponent<MuriKunaiProjectile>();
        proj.Launch(this, dir, throwSpeed);
        active.Add(proj);

        if (pg.player.super != null)
            pg.player.super.Spend(1f);
        nextThrow = Time.time + throwCooldown;
        LockUntilRelease();
    }

    void HandleBlink()
    {
        if (waitingRelease)
            return;
        List<MuriKunaiProjectile> stuck = StuckList();
        if (stuck.Count == 0)
            return;
        if (Time.time < nextBlink)
            return;

        bool up = pg.inp.tf_up;
        bool left = pg.inp.tf_left;
        bool right = pg.inp.tf_right;
        if (pg.player.computer && brain != null)
        {
            up |= brain.Thought == Thought.MoveUp;
            left |= brain.Thought == Thought.MoveLeft;
            right |= brain.Thought == Thought.MoveRight;
        }
        if (!up && !left && !right)
            return;

        MuriKunaiProjectile target = SelectBlink(stuck, up, left, right);
        if (target == null)
            return;

        Vector3 dest = target.transform.position;
        if (MuriMove.IsOutsidePlayfield(dest, target.transform))
        {
            target.ConsumeForBlink();
            if (move != null)
                move.RecoverIfOutOfBounds();
            return;
        }

        Vector3 n = target.StickNormal;
        if (n.sqrMagnitude < 0.0001f)
            n = move != null ? move.StandUp : Vector3.up;
        dest += n.normalized * (move != null ? move.SitDistance(true) : 0.12f);
        dest.z = transform.position.z;
        transform.position = dest;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.position = dest;

        target.ConsumeForBlink();
        if (move != null)
        {
            move.PlaceOnBestSupport();
            move.RecoverIfOutOfBounds();
        }
        nextBlink = Time.time + 0.22f;
        LockUntilRelease();
    }

    void LockUntilRelease()
    {
        if (pg != null && pg.player != null && pg.player.computer)
            return;
        waitingRelease = true;
    }

    bool AnyActionHeld()
    {
        if (pg == null || pg.player == null || pg.player.computer)
            return false;
        Inputs i = pg.inp;
        return i.up || i.down || i.left || i.right || i.superHeld;
    }

    MuriKunaiProjectile SelectBlink(List<MuriKunaiProjectile> stuck, bool up, bool left, bool right)
    {
        if (stuck.Count == 1)
            return stuck[0];

        // Axes match PlayerGrab's facing remap so the button they press
        // picks the kunai that is furthest that way on the field.
        Vector3 rightAxis = BlinkRightAxis();
        int[] order = new int[stuck.Count];
        float[] along = new float[stuck.Count];
        for (int i = 0; i < stuck.Count; i++)
        {
            order[i] = i;
            along[i] = Vector3.Dot(Flatten(stuck[i].transform.position), rightAxis);
        }
        System.Array.Sort(order, (a, b) => along[a].CompareTo(along[b]));

        int leftIdx = order[0];
        int rightIdx = order[order.Length - 1];
        int midIdx;
        if (stuck.Count == 2)
        {
            float my = Vector3.Dot(Flatten(transform.position), rightAxis);
            midIdx = Mathf.Abs(along[order[0]] - my) <= Mathf.Abs(along[order[1]] - my)
                ? order[0] : order[1];
        }
        else
        {
            midIdx = order[order.Length / 2];
        }

        if (stuck.Count == 2)
        {
            if (left && !right) return stuck[leftIdx];
            if (right && !left) return stuck[rightIdx];
            return stuck[midIdx];
        }

        if (right && !left && !up) return stuck[rightIdx];
        if (left && !right && !up) return stuck[leftIdx];
        return stuck[midIdx];
    }

    Vector3 BlinkRightAxis()
    {
        Facing facing = pg != null && pg.player != null ? pg.player.facing : Facing.Up;
        Vector3 axis;
        switch (facing)
        {
            case Facing.Down: axis = Vector3.right; break;
            case Facing.Left: axis = Vector3.down; break;
            case Facing.Right: axis = Vector3.up; break;
            default: axis = Vector3.right; break;
        }
        return QuantizeCardinal(axis);
    }

    static Vector3 QuantizeCardinal(Vector3 v)
    {
        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
            return v.x >= 0f ? Vector3.right : Vector3.left;
        return v.y >= 0f ? Vector3.up : Vector3.down;
    }

    static Vector3 Flatten(Vector3 v)
    {
        v.z = 0f;
        return v;
    }

    public void OnKunaiHitBall()
    {
        if (shield != null)
            shield.Restore();
    }

    public void Refund(MuriKunaiProjectile proj)
    {
        if (proj != null)
            active.Remove(proj);
        if (pg != null && pg.player != null && pg.player.super != null)
            pg.player.super.Gain(1f);
    }

    int SlotsLeft()
    {
        if (pg != null && pg.player != null && pg.player.super != null)
            return Mathf.Max(0, Mathf.FloorToInt(pg.player.super.amount + 0.001f));
        return Mathf.Max(0, maxKunai - active.Count);
    }

    int FlyingCount()
    {
        int n = 0;
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i] != null && active[i].IsFlying)
                n++;
        }
        return n;
    }

    List<MuriKunaiProjectile> StuckList()
    {
        List<MuriKunaiProjectile> list = new List<MuriKunaiProjectile>(active.Count);
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i] != null && active[i].IsStuck)
                list.Add(active[i]);
        }
        return list;
    }

    void Prune()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (active[i] == null)
                active.RemoveAt(i);
        }
    }

    void SyncMeter()
    {
        if (pg == null || pg.player == null || pg.player.super == null)
            return;
        pg.player.super.max = maxKunai;
        if (pg.player.super.amount <= 0f)
            pg.player.super.amount = maxKunai;
        pg.player.super.cost = 1f;
    }

    public MuriKunaiProjectile ClosestStuckTo(Vector3 world)
    {
        MuriKunaiProjectile best = null;
        float bestD = float.PositiveInfinity;
        for (int i = 0; i < active.Count; i++)
        {
            MuriKunaiProjectile k = active[i];
            if (k == null || !k.IsStuck)
                continue;
            float d = (k.transform.position - world).sqrMagnitude;
            if (d < bestD)
            {
                bestD = d;
                best = k;
            }
        }
        return best;
    }
}
