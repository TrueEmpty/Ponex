using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BallInfo))]
public class MimicBall : MonoBehaviour
{
    Database db;
    BallInfo bI;
    Renderer ren;
    Material mimicMaterial;
    Ball baseBallStats;

    [Tooltip("Wait before first transformation.")]
    public Vector2 firstChangeDelay = new Vector2(2f, 5f);

    [Tooltip("How long to stay as Mimic between forms.")]
    public Vector2 mimicHoldRange = new Vector2(2f, 6f);

    [Tooltip("How long to stay transformed as another ball.")]
    public Vector2 formHoldRange = new Vector2(8f, 24f);

    readonly List<string> excludeForms = new List<string>
    {
        "Mimic Ball",
        "Orbit Ball"
    };

    string currentForm = "Mimic Ball";
    float switchAt;
    bool transformed;
    readonly List<Component> addedBehaviours = new List<Component>();
    GameObject sizeBallChild;
    GameObject sizeAdjustedChild;

    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();
        ren = GetComponent<Renderer>();
        if (ren != null)
            mimicMaterial = ren.sharedMaterial;

        if (bI != null && bI.ball != null)
            baseBallStats = new Ball(bI.ball);

        // Always keep Mimic identity so CloneBall / respawn stay Mimic
        if (bI != null && bI.ball != null)
            bI.ball.name = "Mimic Ball";

        ScheduleMimicHold();
    }

    void Update()
    {
        if (db == null || !db.gameStart || bI == null || !bI.ballReady || !bI.projectionOn)
            return;

        if (Time.time < switchAt)
            return;

        if (!transformed)
            EnterRandomForm();
        else
            ExitForm();
    }

    void OnDestroy()
    {
        CleanupAdded();
    }

    void ScheduleMimicHold()
    {
        transformed = false;
        switchAt = Time.time + Random.Range(mimicHoldRange.x, mimicHoldRange.y);
        if (Time.time < 0.1f)
            switchAt = Time.time + Random.Range(firstChangeDelay.x, firstChangeDelay.y);
    }

    void ScheduleFormHold()
    {
        transformed = true;
        switchAt = Time.time + Random.Range(formHoldRange.x, formHoldRange.y);
    }

    void EnterRandomForm()
    {
        if (db == null || db.balls == null || db.balls.Count == 0)
            return;

        List<Ball> options = new List<Ball>();
        for (int i = 0; i < db.balls.Count; i++)
        {
            Ball b = db.balls[i];
            if (b == null || string.IsNullOrEmpty(b.name))
                continue;
            if (excludeForms.Exists(x => x == b.name))
                continue;
            options.Add(b);
        }

        if (options.Count == 0)
            return;

        Ball pick = options[Random.Range(0, options.Count)];
        ApplyForm(pick);
        ScheduleFormHold();
    }

    void ApplyForm(Ball target)
    {
        ExitForm(false);
        currentForm = target.name;

        if (ren != null && target.prefab != null)
        {
            Renderer tr = target.prefab.GetComponent<Renderer>();
            if (tr != null && tr.sharedMaterial != null)
                ren.sharedMaterial = tr.sharedMaterial;
        }

        // Keep Mimic identity / prefab so duplicates remain Mimics
        if (bI.ball != null)
        {
            GameObject mimicPrefab = baseBallStats != null ? baseBallStats.prefab : bI.ball.prefab;
            bI.ball.name = "Mimic Ball";
            bI.ball.prefab = mimicPrefab;
            bI.ball.damage = target.damage;
            bI.ball.startSpeed = target.startSpeed;
            bI.ball.minSpeed = target.minSpeed;
            bI.ball.maxSpeed = target.maxSpeed;
            bI.ball.speedIncrease = target.speedIncrease;
        }

        switch (target.name)
        {
            case "Frost Ball":
                addedBehaviours.Add(gameObject.AddComponent<FrostBall>());
                break;
            case "Ghost Ball":
                GhostBall ghost = gameObject.AddComponent<GhostBall>();
                ghost.vanishRange = new Vector3(2f, 5f, 3f);
                ghost.returnTime = 2f;
                ghost.vanishAlpha = 15;
                addedBehaviours.Add(ghost);
                break;
            case "Time Ball":
                addedBehaviours.Add(gameObject.AddComponent<TimeBall>());
                break;
            case "Illusion Ball":
                addedBehaviours.Add(gameObject.AddComponent<IllusionBall>());
                break;
            case "Clone Ball":
                CloneBall clone = gameObject.AddComponent<CloneBall>();
                CloneBall donor = null;
                if (target.prefab != null)
                    donor = target.prefab.GetComponent<CloneBall>();
                if (donor != null)
                {
                    clone.popMainTimer = donor.popMainTimer;
                    clone.sizeRange = donor.sizeRange;
                    clone.growthRange = donor.growthRange;
                    clone.maxClones = donor.maxClones;
                    clone.cloneChance = donor.cloneChance;
                    clone.cloneBallChild = donor.cloneBallChild;
                }
                addedBehaviours.Add(clone);
                break;
            case "Size Ball":
                EnsureSizeChildren();
                SizeBall size = gameObject.AddComponent<SizeBall>();
                size.sizeSwitch = new Vector3(4f, 9f, 6f);
                size.sizeRange = new Vector2(0.35f, 1.1f);
                size.ball = sizeBallChild != null ? sizeBallChild.transform : transform;
                size.adjusted = sizeAdjustedChild != null ? sizeAdjustedChild.transform : transform;
                addedBehaviours.Add(size);
                break;
            case "Burn Ball":
                addedBehaviours.Add(gameObject.AddComponent<BurnBall>());
                break;
            case "Meteor Ball":
                addedBehaviours.Add(gameObject.AddComponent<MeteorBall>());
                break;
            case "Nano Ball":
                addedBehaviours.Add(gameObject.AddComponent<NanoBall>());
                break;
            case "Shape Ball":
                addedBehaviours.Add(gameObject.AddComponent<ShapeBall>());
                break;
            case "Mine Ball":
                MineBall mine = gameObject.AddComponent<MineBall>();
                MineBall mineDonor = target.prefab != null ? target.prefab.GetComponent<MineBall>() : null;
                if (mineDonor != null)
                {
                    mine.dropIntervalRange = mineDonor.dropIntervalRange;
                    mine.mineScale = mineDonor.mineScale;
                    mine.minePrefab = mineDonor.minePrefab;
                    mine.mineBodyMaterial = mineDonor.mineBodyMaterial;
                    mine.mineFuseMaterial = mineDonor.mineFuseMaterial;
                    mine.bombBodyMaterial = mineDonor.bombBodyMaterial;
                    mine.bombFuseMaterial = mineDonor.bombFuseMaterial;
                }
                addedBehaviours.Add(mine);
                break;
            case "Shock Ball":
                ShockBall shock = gameObject.AddComponent<ShockBall>();
                ShockBall shockDonor = target.prefab != null ? target.prefab.GetComponent<ShockBall>() : null;
                if (shockDonor != null)
                {
                    shock.idleStartSpeed = shockDonor.idleStartSpeed;
                    shock.idleMinSpeed = shockDonor.idleMinSpeed;
                    shock.idleMaxSpeed = shockDonor.idleMaxSpeed;
                    shock.zapStartSpeed = shockDonor.zapStartSpeed;
                    shock.zapMinSpeed = shockDonor.zapMinSpeed;
                    shock.zapMaxSpeed = shockDonor.zapMaxSpeed;
                    shock.zapDuration = shockDonor.zapDuration;
                    shock.idleIntervalRange = shockDonor.idleIntervalRange;
                    shock.shockDuration = shockDonor.shockDuration;
                    shock.shockMoveFactor = shockDonor.shockMoveFactor;
                    shock.idleMaterial = shockDonor.idleMaterial;
                    shock.zapMaterial = shockDonor.zapMaterial;
                }
                addedBehaviours.Add(shock);
                break;
            case "Explosion Ball":
                ExplosionBall boom = gameObject.AddComponent<ExplosionBall>();
                ExplosionBall boomDonor = target.prefab != null ? target.prefab.GetComponent<ExplosionBall>() : null;
                if (boomDonor != null)
                {
                    boom.lightChanceOnWall = boomDonor.lightChanceOnWall;
                    boom.explodeRadius = boomDonor.explodeRadius;
                    boom.pushForce = boomDonor.pushForce;
                    boom.smokeScale = boomDonor.smokeScale;
                    boom.bodyMaterial = boomDonor.bodyMaterial;
                    boom.fuseMaterial = boomDonor.fuseMaterial;
                    boom.bandMaterial = boomDonor.bandMaterial;
                    boom.smokePoof = boomDonor.smokePoof;
                }
                addedBehaviours.Add(boom);
                break;
            case "Speed Ball":
                if (bI.ball != null)
                {
                    bI.ball.maxSpeed = Mathf.Max(bI.ball.maxSpeed, 10f);
                    bI.ball.speedIncrease = Mathf.Max(bI.ball.speedIncrease, 1.35f);
                }
                break;
        }
    }

    void ExitForm(bool schedule = true)
    {
        CleanupAdded();
        currentForm = "Mimic Ball";

        if (ren != null && mimicMaterial != null)
            ren.sharedMaterial = mimicMaterial;

        if (bI != null && baseBallStats != null)
        {
            GameObject mimicPrefab = baseBallStats.prefab;
            bI.ball = new Ball(baseBallStats);
            bI.ball.name = "Mimic Ball";
            bI.ball.prefab = mimicPrefab;
        }

        if (schedule)
            ScheduleMimicHold();
    }

    void CleanupAdded()
    {
        for (int i = 0; i < addedBehaviours.Count; i++)
        {
            if (addedBehaviours[i] != null)
                Destroy(addedBehaviours[i]);
        }
        addedBehaviours.Clear();

        if (sizeBallChild != null)
            Destroy(sizeBallChild);
        if (sizeAdjustedChild != null)
            Destroy(sizeAdjustedChild);
        sizeBallChild = null;
        sizeAdjustedChild = null;
        if (ren != null)
            ren.enabled = true;
    }

    void EnsureSizeChildren()
    {
        if (sizeBallChild == null)
        {
            sizeBallChild = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sizeBallChild.name = "SizeVisual";
            sizeBallChild.transform.SetParent(transform, false);
            sizeBallChild.transform.localPosition = Vector3.zero;
            sizeBallChild.transform.localScale = Vector3.one;
            Object.Destroy(sizeBallChild.GetComponent<Collider>());
            Renderer r = sizeBallChild.GetComponent<Renderer>();
            if (r != null && ren != null)
                r.sharedMaterial = ren.sharedMaterial;
            // Hide root mesh while size visual is active
            if (ren != null)
                ren.enabled = false;
        }

        if (sizeAdjustedChild == null)
        {
            sizeAdjustedChild = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sizeAdjustedChild.name = "SizeAdjusted";
            sizeAdjustedChild.transform.SetParent(transform, false);
            sizeAdjustedChild.transform.localPosition = Vector3.zero;
            sizeAdjustedChild.transform.localScale = Vector3.one * 0.7f;
            Object.Destroy(sizeAdjustedChild.GetComponent<Collider>());
            sizeAdjustedChild.SetActive(false);
            Renderer r = sizeAdjustedChild.GetComponent<Renderer>();
            if (r != null && ren != null)
                r.sharedMaterial = ren.sharedMaterial;
        }
    }
}
