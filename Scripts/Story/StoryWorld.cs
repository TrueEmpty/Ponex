using UnityEngine;

public class StoryWorld : MonoBehaviour
{
    public static StoryWorld instance;

    Camera cam;
    Transform avatar;
    StoryPlayer player;
    StoryShot shot;
    bool cutscene;
    float cutsceneT;
    float bumpHeld;
    bool begun;

    public bool Paused => StoryPauseMenu.IsOpen;
    public Transform CameraTransform => cam != null ? cam.transform : null;

    const float RoomSize = 96f;
    const float WallHeight = 12f;
    const float WallThickness = 1f;
    const float CameraBack = 4.2f;
    const float CameraHeight = 2.4f;
    const float CameraLookHeight = 1.2f;
    static readonly RaycastHit[] cameraHits = new RaycastHit[24];

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        BeginIfReady();
    }

    public static void BeginIfReady()
    {
        StoryWorld world = instance != null ? instance : FindAnyObjectByType<StoryWorld>();
        if (world == null)
        {
            GameObject go = new GameObject("Story World");
            world = go.AddComponent<StoryWorld>();
        }
        world.Begin();
    }

    public void Begin()
    {
        if (begun)
            return;
        begun = true;
        instance = this;
        BuildRoom();

        shot = BuildIntroShot();
        StorySaveData save = null;
        bool saved = StorySession.loadSavedTransform
            && StoryProgress.TryGetSave(StorySession.character, out save)
            && save != null
            && save.hasTransform;
        bool autoSkip = Database.instance != null && Database.instance.skipStoryCutscenes;

        if (saved)
        {
            ApplySavedPose(save);
            return;
        }

        if (autoSkip)
            CompleteCutscene();
        else
            BeginCutscene();
    }

    StoryShot BuildIntroShot()
    {
        Vector3 stand = new Vector3(0f, 1f, 0f);
        Quaternion face = Quaternion.identity;
        Vector3 look = stand + Vector3.up * 1.2f;
        Vector3 endCam = stand - face * Vector3.forward * 4.2f + Vector3.up * 2.4f;
        return new StoryShot()
            .Place(avatar, stand, face)
            .Camera(new Vector3(0f, 9f, -16f), endCam, look);
    }

    void ApplySavedPose(StorySaveData save)
    {
        cutscene = false;
        if (player != null)
            player.cutsceneLocked = false;
        CharacterController body = avatar.GetComponent<CharacterController>();
        if (body != null)
            body.enabled = false;
        avatar.SetPositionAndRotation(save.Position, save.Rotation);
        PlantOnFloor(body);
        if (body != null)
            body.enabled = true;
        SnapPlayCamera();
    }

    void BuildRoom()
    {
        float half = RoomSize * 0.5f;
        float wallReach = RoomSize + WallThickness * 2f;
        CreateBox("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(RoomSize, 1f, RoomSize), new Color(0.35f, 0.33f, 0.3f));
        CreateBox("Wall North", new Vector3(0f, WallHeight * 0.5f, half + WallThickness * 0.5f), new Vector3(wallReach, WallHeight, WallThickness), new Color(0.55f, 0.52f, 0.48f));
        CreateBox("Wall South", new Vector3(0f, WallHeight * 0.5f, -(half + WallThickness * 0.5f)), new Vector3(wallReach, WallHeight, WallThickness), new Color(0.55f, 0.52f, 0.48f));
        CreateBox("Wall East", new Vector3(half + WallThickness * 0.5f, WallHeight * 0.5f, 0f), new Vector3(WallThickness, WallHeight, wallReach), new Color(0.5f, 0.48f, 0.45f));
        CreateBox("Wall West", new Vector3(-(half + WallThickness * 0.5f), WallHeight * 0.5f, 0f), new Vector3(WallThickness, WallHeight, wallReach), new Color(0.5f, 0.48f, 0.45f));

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Test";
        body.transform.SetParent(transform, false);
        body.transform.position = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        Collider extra = body.GetComponent<Collider>();
        if (extra != null)
            Destroy(extra);
        Renderer rend = body.GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = new Color(0.2f, 0.45f, 0.95f);
        avatar = body.transform;
        player = body.AddComponent<StoryPlayer>();
        player.Setup(this);
        PlantOnFloor(body.GetComponent<CharacterController>());

        GameObject lightGo = new GameObject("Story Light");
        lightGo.transform.SetParent(transform, false);
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;

        GameObject camGo = new GameObject("Story Camera");
        camGo.transform.SetParent(transform, false);
        cam = camGo.AddComponent<Camera>();
        cam.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
        SnapPlayCamera();
    }

    void BeginCutscene()
    {
        cutscene = true;
        cutsceneT = 0f;
        bumpHeld = 0f;
        if (player != null)
            player.cutsceneLocked = true;
        if (cam != null && shot != null)
        {
            cam.transform.position = shot.cameraFrom;
            cam.transform.rotation = Quaternion.LookRotation(shot.lookAt - shot.cameraFrom, Vector3.up);
        }
    }

    void CompleteCutscene()
    {
        cutscene = false;
        bumpHeld = 0f;
        if (shot != null)
            shot.Apply(cam, avatar);
        if (player != null)
            player.cutsceneLocked = false;
    }

    void SnapPlayCamera()
    {
        if (cam == null || avatar == null)
            return;
        Vector3 look = avatar.position + Vector3.up * CameraLookHeight;
        Vector3 want = PullCamera(look, avatar.position - avatar.forward * CameraBack + Vector3.up * CameraHeight);
        cam.transform.position = want;
        cam.transform.rotation = Quaternion.LookRotation(look - want, Vector3.up);
    }

    void Update()
    {
        if (!cutscene || cam == null || shot == null)
            return;

        if (StoryPlayer.JumpHeld())
            bumpHeld += Time.deltaTime;
        else
            bumpHeld = 0f;

        cutsceneT += Time.deltaTime / 4.5f;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(cutsceneT));
        Vector3 camPos = Vector3.Lerp(shot.cameraFrom, shot.cameraTo, t);
        camPos = PullCamera(shot.lookAt, camPos);
        cam.transform.position = camPos;
        cam.transform.rotation = Quaternion.LookRotation(shot.lookAt - cam.transform.position, Vector3.up);

        if (t >= 1f || bumpHeld >= 3f)
            CompleteCutscene();
    }

    void LateUpdate()
    {
        if (cutscene || cam == null || avatar == null || Paused)
            return;

        Vector3 look = avatar.position + Vector3.up * CameraLookHeight;
        Vector3 want = avatar.position - avatar.forward * CameraBack + Vector3.up * CameraHeight;
        Vector3 next = Vector3.Lerp(cam.transform.position, want, 8f * Time.deltaTime);
        next = PullCamera(look, next);
        cam.transform.position = next;
        cam.transform.rotation = Quaternion.Slerp(
            cam.transform.rotation,
            Quaternion.LookRotation(look - cam.transform.position, Vector3.up),
            8f * Time.deltaTime);
    }

    public void TogglePause()
    {
        if (StoryPauseMenu.IsOpen)
        {
            StoryPauseMenu.Close();
            return;
        }

        Transform canvas = null;
        Database db = Database.instance;
        if (db != null && db.playerSelectors != null)
            canvas = db.playerSelectors.parent;
        if (canvas == null)
        {
            Canvas found = FindAnyObjectByType<Canvas>();
            if (found != null)
                canvas = found.transform;
        }
        StoryPauseMenu.Open(canvas);
    }

    public void Interact()
    {
        Debug.Log("Story interact. Nothing in this room yet.");
    }

    void PlantOnFloor(CharacterController body)
    {
        if (avatar == null || body == null)
            return;
        bool wasEnabled = body.enabled;
        body.enabled = false;
        float feet = avatar.position.y + body.center.y - body.height * 0.5f;
        if (feet < 0.02f)
            avatar.position += Vector3.up * (0.02f - feet);
        body.enabled = wasEnabled;
    }

    Vector3 PullCamera(Vector3 from, Vector3 want)
    {
        Vector3 delta = want - from;
        float dist = delta.magnitude;
        if (dist < 0.05f)
            return want;

        Vector3 dir = delta / dist;
        const float radius = 0.35f;
        int count = Physics.SphereCastNonAlloc(from, radius, dir, cameraHits, dist, ~0, QueryTriggerInteraction.Ignore);
        float nearest = dist;
        for (int i = 0; i < count; i++)
        {
            Transform hit = cameraHits[i].transform;
            if (hit == null || hit == avatar || hit.IsChildOf(avatar))
                continue;
            float allowed = cameraHits[i].distance - 0.2f;
            if (allowed < nearest)
                nearest = allowed;
        }

        if (nearest < 0.4f)
            nearest = 0.4f;
        Vector3 pos = from + dir * nearest;
        if (pos.y < 0.45f)
            pos.y = 0.45f;
        return pos;
    }

    void CreateBox(string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform, true);
        go.transform.position = position;
        go.transform.localScale = scale;
        Renderer rend = go.GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = color;
    }
}
