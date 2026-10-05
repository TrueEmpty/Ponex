using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGrab))]
public class IyolitLifelineCreate : MonoBehaviour
{
    public GameObject candle;
    PlayerGrab pG;
    IyolitMovement iM;
    int candlesToSpawn = 4;
    [Tooltip("World scale applied to each spawned candle (prefab default is ~3.25).")]
    public Vector3 candleSpawnScale = new Vector3(1.1f, 1f, 1.1f);

    List<IyolitCandleMelt> candles = new List<IyolitCandleMelt>();

    bool ready = false;
    string wallTag = "Wall";

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
        iM = pG.player.spawnedPlayer.GetComponent<IyolitMovement>();
        Invoke(nameof(SpawnCandles), .5f);
    }

    // Update is called once per frame
    void Update()
    {
        if (ready)
        {
            float totalCurHp = 0;

            if (candles.Count > 0)
            {
                for (int i = candles.Count - 1; i >= 0; i--)
                {
                    if (candles[i] == null)
                    {
                        candles.RemoveAt(i);
                    }
                    else
                    {
                        totalCurHp += candles[i].curHealth;
                    }
                }
            }

            pG.player.currentHealth = Mathf.CeilToInt(totalCurHp);
        }
    }

    void SpawnCandles()
    {
        float healthPerCandle = pG.player.maxHealth / candlesToSpawn;

        Vector3 origin = transform.position + transform.up;
        Vector3 posLeft = Vector3.zero;
        Vector3 posRight = Vector3.zero;
        bool hitLeft = TryWallHit(origin, -transform.right, out posLeft);
        bool hitRight = TryWallHit(origin, transform.right, out posRight);

        float wallSpan;
        Vector3 along;
        Vector3 wallOrigin;

        if (hitLeft && hitRight)
        {
            wallSpan = Vector3.Distance(posLeft, posRight);
            along = (posRight - posLeft).normalized;
            wallOrigin = posLeft;
        }
        else
        {
            // Fallback: playfield width (field.size + 10), centered on the lifeline
            wallSpan = Mathf.Max(8f, EstimatePlayWidth());
            along = transform.right;
            wallOrigin = transform.position - along * (wallSpan * 0.5f);
        }

        // Keep candles inside the wall segment. Margin tracks candle width and shrinks on tiny fields.
        float candleHalf = Mathf.Max(0.4f, candleSpawnScale.x) * 0.85f;
        float endMargin = Mathf.Clamp(candleHalf * 1.35f, 0.6f, wallSpan * 0.22f);
        // Match original feel on medium fields (~5u each side when span≈20) without going OOB on small ones
        endMargin = Mathf.Max(endMargin, Mathf.Min(5f, wallSpan * 0.2f));
        if (endMargin * 2f >= wallSpan)
            endMargin = wallSpan * 0.2f;

        float usable = Mathf.Max(0.01f, wallSpan - endMargin * 2f);
        float segment = candlesToSpawn > 1 ? usable / (candlesToSpawn - 1) : 0f;

        // Lift candles onto the floor/wall plane along her up
        Vector3 lift = transform.up * candleSpawnScale.y;
        Vector3 start = wallOrigin + along * endMargin + lift;

        for (int i = 0; i < candlesToSpawn; i++)
        {
            Vector3 spawnPos = start + along * (segment * i);
            // Snap depth/height to her wall plane so they sit with the lifeline
            spawnPos = ProjectOntoWallPlane(spawnPos);

            GameObject go = Instantiate(candle, spawnPos, transform.rotation);
            go.transform.localScale = candleSpawnScale;

            IyolitCandleMelt iCM = go.GetComponent<IyolitCandleMelt>();
            if (iCM != null)
            {
                iCM.iM = iM;
                iCM.maxHealth = healthPerCandle;
                iCM.curHealth = healthPerCandle;
            }

            PlayerGrab iPG = go.GetComponent<PlayerGrab>();
            if (iPG != null)
                iPG.playerIndex = pG.playerIndex;

            candles.Add(iCM);
            iM.AddPosition(go.transform.GetChild(0).GetChild(0), i == candlesToSpawn - 1);
        }

        ready = true;
    }

    Vector3 ProjectOntoWallPlane(Vector3 worldPos)
    {
        // Keep lateral placement; pin depth to lifeline position along up/forward
        Vector3 p = transform.position + transform.up * candleSpawnScale.y;
        // Move along right only from wall-span placement
        float lateral = Vector3.Dot(worldPos - p, transform.right);
        return p + transform.right * lateral;
    }

    bool TryWallHit(Vector3 origin, Vector3 dir, out Vector3 point)
    {
        point = Vector3.zero;
        // wallLayer on the prefab is 0 (Nothing) — ignore it and filter by tag instead
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, 1000f, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform == null)
                continue;
            string tag = hits[i].transform.tag;
            if (tag == null)
                continue;
            string t = tag.ToLowerInvariant().Trim();
            if (t == wallTag.ToLowerInvariant().Trim() || t == "walls" || t == "obstacle")
            {
                point = hits[i].point;
                return true;
            }
        }
        return false;
    }

    float EstimatePlayWidth()
    {
        Database db = Database.instance;
        if (db != null && db.FieldPlaySize > 0.01f)
            return db.FieldPlaySize;
        if (db != null && db.fields != null && db.fields.Count > 0)
        {
            int idx = Mathf.Clamp(db.selectedField, 0, db.fields.Count - 1);
            Field f = db.fields[idx];
            if (f != null && f.size > 0)
                return f.size + 10;
        }
        return 20f;
    }
}
