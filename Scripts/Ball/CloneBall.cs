using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BallInfo))]
public class CloneBall : MonoBehaviour
{
    Database db;
    BallInfo bI;
    float popTimer = 0;
    public Vector3 popMainTimer; //Min, Max, Nextswitch
    public Vector3 sizeRange; //Min, MaxPop,Max
    public Vector2 growthRange; //Min, Max
    public int maxClones = 5;
    int maxCloneBalls = 25;

    public GameObject cloneBallChild;

    public List<GameObject> cloneChildren = new List<GameObject>();

    [Range(0, 1)]
    public float cloneChance = .25f;
    float clonetimer = 0;

    void Awake()
    {
        if (cloneChildren == null)
            cloneChildren = new List<GameObject>();
    }

    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();

        if (cloneChildren == null)
            cloneChildren = new List<GameObject>();

        if (bI != null && bI.projectionOn)
        {
            int cbC = 0;
            for (int i = 0; i < LiveBallRegistry.Count; i++)
            {
                BallInfo gbI = LiveBallRegistry.GetAt(i);
                if (gbI == null || gbI.gameObject == gameObject || gbI.ball == null)
                    continue;
                if (gbI.ball.name != "Clone Ball" && gbI.ball.name != "Mimic Ball")
                    continue;

                cbC++;
                if (cbC >= maxCloneBalls)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            popMainTimer.z = Random.Range(popMainTimer.x, popMainTimer.y);
        }
    }

    void Update()
    {
        if (db == null || bI == null || !db.gameStart || !bI.ballReady || !bI.projectionOn)
            return;

        if (cloneChildren == null)
            cloneChildren = new List<GameObject>();

        // Pop clones off
        if (popTimer > popMainTimer.z)
        {
            for (int i = cloneChildren.Count - 1; i >= 0; i--)
            {
                GameObject child = cloneChildren[i];
                if (child == null)
                {
                    cloneChildren.RemoveAt(i);
                    continue;
                }

                if (child.transform.localScale.x <= sizeRange.y)
                    continue;

                GameObject spawnPrefab = bI.ball != null ? bI.ball.prefab : null;
                if (spawnPrefab == null)
                {
                    Destroy(child);
                    cloneChildren.RemoveAt(i);
                    continue;
                }

                GameObject ncb = Instantiate(spawnPrefab);
                ncb.transform.position = child.transform.position;
                ncb.transform.localScale = child.transform.localScale;

                BallInfo nBI = ncb.GetComponent<BallInfo>();
                if (nBI != null && bI.ball != null)
                {
                    nBI.ball = new Ball(bI.ball);
                    nBI.ballReady = true;
                    nBI.matchSlot = -1;
                    nBI.projectionOn = true;
                    nBI.anchor = ncb;
                }

                // Strip Mimic so popped children don't keep transforming
                MimicBall mimic = ncb.GetComponent<MimicBall>();
                if (mimic != null)
                    Destroy(mimic);

                Destroy(child);
                cloneChildren.RemoveAt(i);
            }

            popMainTimer.z = Random.Range(popMainTimer.x, popMainTimer.y);
            popTimer = 0;
        }

        // Create Clone
        if (clonetimer > 1)
        {
            if (cloneBallChild != null
                && cloneChance >= Random.Range(0f, 1f)
                && cloneChildren.Count < maxClones)
            {
                GameObject cC = Instantiate(cloneBallChild);
                cC.transform.SetParent(transform, false);
                cC.transform.localScale = Vector3.one * sizeRange.x;
                float randomX = Random.Range(-1f, 1f);
                float randomY = Random.Range(-1f, 1f);

                float totalSum = Mathf.Abs(randomX) + Mathf.Abs(randomY);
                if (totalSum < 0.0001f)
                    totalSum = 1f;

                float percentX = Mathf.Abs(randomX) / totalSum;
                float percentY = Mathf.Abs(randomY) / totalSum;

                float trueX = transform.localScale.x * percentX;
                float trueY = transform.localScale.y * percentY;

                if (randomX < 0)
                    trueX *= -1;
                if (randomY < 0)
                    trueY *= -1;

                cC.transform.localPosition = new Vector3(trueX, trueY, 0);

                SizeOverTime sot = cC.GetComponent<SizeOverTime>();
                if (sot != null)
                {
                    sot.growthRate = Random.Range(growthRange.x, growthRange.y);
                    sot.growthRange = new Vector2(sizeRange.x, sizeRange.z);
                }

                cloneChildren.Add(cC);
            }

            clonetimer = 0;
        }

        popTimer += Time.deltaTime;
        clonetimer += Time.deltaTime;
    }
}
