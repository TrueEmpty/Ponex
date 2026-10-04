using UnityEngine;
using UnityEngine.UI;

public class BallSelect : MonoBehaviour
{
    public static BallSelect instance;
    Database db;
    MenuManager mm;

    int lastShownBall = -10;
    float nextShow = 0;
    float showSwap = .25f;

    GameObject shownBall = null;
    public Text ballName;

    public Transform controlsHolder;
    bool setup;

    private void Awake()
    {
        if (instance != null)
            Destroy(this);
        else
            instance = this;
    }

    void Start()
    {
        db = Database.instance;
        mm = MenuManager.instance;
    }

    void Update()
    {
        if (!setup)
            SetupControls();

        bool dontProceed = false;

        if (db.selectedBall >= 0 && db.selectedBall < db.balls.Count)
        {
            Ball b = db.balls[db.selectedBall];
            if (ballName != null)
            {
                ballName.text = b.name;
                ballName.color = b.color;
            }
        }
        else
        {
            if (ballName != null)
            {
                ballName.text = "Random";
                ballName.color = Color.black;
            }
        }

        if (lastShownBall != db.selectedBall && !dontProceed)
        {
            if (db.selectedBall >= 0 && db.selectedBall < db.balls.Count)
            {
                UpdateShownBall(db.selectedBall);
                lastShownBall = db.selectedBall;
            }
            else if (nextShow < Time.time)
            {
                UpdateShownBall(Random.Range(0, db.balls.Count));
                nextShow = Time.time + showSwap;
            }
        }
    }

    void SetupControls()
    {
        if (setup) return;

        // BallStepButton / ButtonInteraction / BoxCollider are authored on the UI
        BallStepButton[] steps = GetComponentsInChildren<BallStepButton>(true);
        if (steps == null || steps.Length == 0)
            Debug.LogWarning("BallSelect: no BallStepButton components found. Run Tools/Ponex/Bake Selector UI.");

        setup = true;
    }

    void UpdateShownBall(int ballToShow)
    {
        if (shownBall != null)
            Destroy(shownBall);

        if (db.balls == null || ballToShow < 0 || ballToShow >= db.balls.Count)
            return;

        Ball b = db.balls[ballToShow];
        if (b.selection == null)
            return;

        shownBall = Instantiate(b.selection);
        shownBall.transform.position = new Vector3(0, 0, 5);
    }

    private void OnDisable()
    {
        if (shownBall != null)
            Destroy(shownBall);
    }
}
