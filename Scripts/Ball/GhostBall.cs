using UnityEngine;

public class GhostBall : MonoBehaviour
{
    Database db;
    BallInfo bI;
    Renderer ren;
    Material matInstance;
    public GameObject smokePoof;

    float vanishTimer = 0;
    public Vector3 vanishRange; //Min, Max, Nextswitch
    Color baseColor = Color.gray;
    Color vanishColor = Color.gray;
    public float returnTime = 2;

    [Range(0, 100)]
    public int vanishAlpha = 1;

    bool vanished = false;
    bool colorDirty = true;

    void Start()
    {
        db = Database.instance;
        bI = GetComponent<BallInfo>();

        if (smokePoof != null)
            BallBlast.SmokePoofPrefab = smokePoof;

        if (bI.projectionOn)
        {
            ren = GetComponent<Renderer>();
            matInstance = ren.material;
            baseColor = matInstance.color;
            vanishColor = baseColor;
            vanishColor.a = (float)vanishAlpha / 100;
            SetSwitch();
            colorDirty = true;
        }
    }

    void Update()
    {
        if (!db.gameStart || !bI.ballReady || !bI.projectionOn)
            return;

        if (colorDirty && matInstance != null)
        {
            matInstance.color = vanished ? vanishColor : baseColor;
            colorDirty = false;
        }

        if (vanishTimer >= (vanished ? returnTime : vanishRange.z))
        {
            vanished = !vanished;
            colorDirty = true;

            if (vanished && smokePoof != null)
                Instantiate(smokePoof, transform.position, transform.rotation);

            if (vanished)
                SetSwitch();

            vanishTimer = 0;
        }

        vanishTimer += Time.deltaTime;
    }

    void SetSwitch()
    {
        vanishRange.z = Random.Range(vanishRange.x, vanishRange.y);
    }
}
