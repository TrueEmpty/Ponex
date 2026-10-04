using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(PlayerGrab))]
public class NariMove : MonoBehaviour
{
    Rigidbody rb;
    Database db;
    PlayerGrab pg;
    NariTailUpkeep tailUpkeep;

    Vector3 moveDir = Vector3.up;
    float hheadCur = 1.3f;
    public List<string> hitTags = new List<string>();
    bool startup = false;
    float trueSpeed = 0;
    public float accel = 8f;

    [Header("Dash")]
    [Tooltip("Added on top of base movementSpeed while dashing.")]
    public float dashSpeedBonus = 4.5f;
    public float dashDuration = 3f;
    public Color dashParticleColor = new Color(0.25f, 1f, 0.4f, 1f);

    float dashTimer = 0f;
    bool dashing = false;
    readonly List<ParticleSystem> dashParticles = new List<ParticleSystem>();
    readonly HashSet<Transform> particleOwners = new HashSet<Transform>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pg = GetComponent<PlayerGrab>();
        db = Database.instance;
        tailUpkeep = GetComponent<NariTailUpkeep>();

        rb.useGravity = false;
    }

    void Update()
    {
        if (db == null || !db.gameStart || pg == null || pg.player == null || pg.player.currentHealth <= 0)
            return;

        if (!startup)
        {
            switch (pg.player.facing)
            {
                case Facing.Right:
                    moveDir = Vector3.left;
                    break;
                case Facing.Left:
                    moveDir = Vector3.right;
                    break;
                case Facing.Up:
                    moveDir = Vector3.down;
                    break;
                case Facing.Down:
                    moveDir = Vector3.up;
                    break;
            }

            startup = true;
            return;
        }

        if (pg.player.CanMove)
        {
            OnDash();
            OnMove();
        }

        if (dashing)
        {
            dashTimer -= Time.deltaTime;
            SyncDashParticles();
            if (dashTimer <= 0f)
                EndDash();
        }

        if (pg.player.dash != null)
            pg.player.dash.Charge();
    }

    float TargetSpeed => dashing
        ? pg.player.movementSpeed + dashSpeedBonus
        : pg.player.movementSpeed;

    void OnDash()
    {
        Skill d = pg.player.dash;
        if (d == null || d.max <= 0f || d.cost <= 0f || dashing)
            return;

        bool wantDash = pg.inp.tf_dashLeft || pg.inp.tf_dashRight;
        if (pg.player.computer)
        {
            ComputerAI.Decision ai = ComputerAI.GetLastDecision(pg.playerIndex);
            wantDash = ai.wantDash;
        }

        if (wantDash && d.amount >= d.cost)
            StartDash(d);
    }

    void StartDash(Skill d)
    {
        dashing = true;
        dashTimer = dashDuration;
        d.readyPercent = 0f;
        d.Spend();
        pg.player.RecordDash();
        trueSpeed = TargetSpeed;
        BeginDashParticles();
    }

    void EndDash()
    {
        dashing = false;
        dashTimer = 0f;
        trueSpeed = pg.player.movementSpeed;
        ClearDashParticles();
    }

    void OnMove()
    {
        if (pg.inp.right)
        {
            if (!WallInDirection(Vector3.right))
            {
                transform.rotation = Quaternion.Euler(0, 0, 90);
                moveDir = Vector3.right;
            }
        }
        else if (pg.inp.left)
        {
            if (!WallInDirection(Vector3.left))
            {
                transform.rotation = Quaternion.Euler(0, 0, 270);
                moveDir = Vector3.left;
            }
        }
        else if (pg.inp.up)
        {
            if (!WallInDirection(Vector3.up))
            {
                transform.rotation = Quaternion.Euler(0, 0, 180);
                moveDir = Vector3.up;
            }
        }
        else if (pg.inp.down)
        {
            if (!WallInDirection(Vector3.down))
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
                moveDir = Vector3.down;
            }
        }

        if (trueSpeed <= 0f)
            trueSpeed = TargetSpeed;
        else
            trueSpeed = Mathf.MoveTowards(trueSpeed, TargetSpeed, accel * Time.deltaTime);

        rb.linearVelocity = trueSpeed * moveDir;

        if (WallInDirection(moveDir))
        {
            rb.linearVelocity *= -1;
            moveDir *= -1;
            Vector3 nRot = transform.rotation.eulerAngles;
            nRot += new Vector3(0, 0, 180);
            transform.rotation = Quaternion.Euler(nRot);
        }
    }

    bool WallInDirection(Vector3 mD)
    {
        float distance = (hheadCur / 2f) + 0.2f;
        RaycastHit[] hits = Physics.RaycastAll(transform.position, mD, distance);
        if (hits == null || hits.Length == 0)
            return false;

        for (int i = 0; i < hits.Length; i++)
        {
            if (PaddleWall.MatchesWallTag(hits[i].transform.tag, hitTags))
                return true;
        }

        return false;
    }

    #region Dash particles
    void BeginDashParticles()
    {
        ClearDashParticles();

        Transform head = tailUpkeep != null && tailUpkeep.head != null ? tailUpkeep.head : transform;
        AttachDashParticle(head);

        if (tailUpkeep != null && tailUpkeep.tails != null)
        {
            for (int i = 0; i < tailUpkeep.tails.Count; i++)
            {
                GameObject t = tailUpkeep.tails[i];
                if (t != null)
                    AttachDashParticle(t.transform);
            }
        }
    }

    void SyncDashParticles()
    {
        if (!dashing || tailUpkeep == null || tailUpkeep.tails == null)
            return;

        Transform head = tailUpkeep.head != null ? tailUpkeep.head : transform;
        if (!particleOwners.Contains(head))
            AttachDashParticle(head);

        for (int i = 0; i < tailUpkeep.tails.Count; i++)
        {
            GameObject t = tailUpkeep.tails[i];
            if (t == null)
                continue;
            if (!particleOwners.Contains(t.transform))
                AttachDashParticle(t.transform);
        }
    }

    void AttachDashParticle(Transform owner)
    {
        if (owner == null || particleOwners.Contains(owner))
            return;

        GameObject go = new GameObject("NariDashParticles");
        go.transform.SetParent(owner, false);
        go.transform.localPosition = Vector3.zero;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 0.45f;
        main.startSpeed = 1.2f;
        main.startSize = 0.12f;
        main.startColor = dashParticleColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 18f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(dashParticleColor, 0f),
                new GradientColorKey(new Color(0.6f, 1f, 0.7f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = grad;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Additive");
            if (shader != null)
            {
                Material mat = new Material(shader);
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", dashParticleColor);
                renderer.material = mat;
            }
        }

        ps.Play();
        dashParticles.Add(ps);
        particleOwners.Add(owner);
    }

    void ClearDashParticles()
    {
        for (int i = 0; i < dashParticles.Count; i++)
        {
            ParticleSystem ps = dashParticles[i];
            if (ps == null)
                continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(ps.gameObject);
        }

        dashParticles.Clear();
        particleOwners.Clear();
    }

    void OnDisable()
    {
        ClearDashParticles();
        dashing = false;
    }
    #endregion
}
