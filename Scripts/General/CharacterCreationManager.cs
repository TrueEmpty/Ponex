using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Dev-only Character Creation sandbox. Slot 0 is the draft fighter; all other slots are CPUs.
/// Esc opens the authoring menu; Ctrl+Shift+O toggles object-assign highlighting.
/// </summary>
[DefaultExecutionOrder(-250)]
public class CharacterCreationManager : MonoBehaviour
{
    public const string SceneName = "Character Creation";

    public static CharacterCreationManager instance;
    public static bool IsActive => instance != null && instance.sessionActive;

    Database db;
    MenuManager mm;

    public bool sessionActive;
    bool sandboxBuilt;
    bool menuOpen;
    bool objectAssignMode;
    bool playerInfoDirty;
    float savedTimeScale = 1f;

    enum MenuTab { Character, Level, Commands }
    MenuTab activeTab = MenuTab.Character;

    bool infiniteBump;
    bool infiniteSuper;
    bool infiniteDash;
    bool infiniteAllSkills;
    int commandTargetMode; // 0 = everyone, 1 = draft only

    // Draft authorship state
    readonly List<GameObject> characterObjects = new List<GameObject>();
    readonly List<GameObject> lifelineObjects = new List<GameObject>();
    readonly Dictionary<Renderer, Color[]> highlightRestore = new Dictionary<Renderer, Color[]>();
    GameObject lastSeededCharacter;
    GameObject lastSeededLifeline;

    GameObject menuRoot;
    GameObject menuPanel;
    GameObject hintLabel;
    GameObject tabCharacter;
    GameObject tabLevel;
    GameObject tabCommands;
    GameObject levelListRoot;
    Text statusText;
    Text distanceText;
    Text commandsStatusText;
    Text levelStatusText;
    Button tabBtnCharacter;
    Button tabBtnLevel;
    Button tabBtnCommands;
    Toggle targetEveryoneToggle;
    Toggle infBumpToggle;
    Toggle infSuperToggle;
    Toggle infDashToggle;

    InputField nameField;
    InputField descField;
    InputField superNameField;
    Slider healthSlider;
    Slider moveSlider;
    Slider pushSlider;
    Slider bumpSpeedSlider;
    Slider bumpMaxSlider;
    Slider dashSpeedSlider;
    Slider dashMaxSlider;
    Slider superSpeedSlider;
    Slider superMaxSlider;
    Slider charWallSlider;
    Slider lifeWallSlider;
    Text healthVal;
    Text moveVal;
    Text pushVal;
    Text bumpSpeedVal;
    Text bumpMaxVal;
    Text dashSpeedVal;
    Text dashMaxVal;
    Text superSpeedVal;
    Text superMaxVal;
    Text charWallVal;
    Text lifeWallVal;
    Toggle ignoreFacingToggle;
    Toggle playerInfoDirtyToggle;

    public static void EnsureExists()
    {
        if (SceneManager.GetActiveScene().name != SceneName)
            return;
        if (instance != null)
            return;
        GameObject go = new GameObject("CharacterCreationManager");
        go.AddComponent<CharacterCreationManager>();
    }

    void Awake()
    {
        if (SceneManager.GetActiveScene().name != SceneName)
        {
            Destroy(gameObject);
            return;
        }
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        sessionActive = true;
        db = Database.instance;
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name != SceneName)
            return;

        db = Database.instance;
        mm = MenuManager.instance;
        sessionActive = true;
        EnsureEventSystemForUi();
        BuildHint();
        BuildMenu();
        SetMenuOpen(false);
        BeginSandboxMatch();
    }

    void Update()
    {
        if (!sessionActive)
            return;

        EnsureDraftIsHuman();
        EnforceCreationHpFloor();
        EnsureJoiningEnabled();
        ClaimUnboundControllers();
        HandleHotkeys();
        ApplyInfiniteSkills();

        if (objectAssignMode && !menuOpen)
            HandleObjectAssignClicks();

        TrySeedAssignListsFromSpawn();

        if (menuOpen)
            RefreshDistanceLabel();
    }

    /// <summary>
    /// After StartGame spawns the draft, keep assign lists pointed at the real character/lifeline
    /// (never the play-field root).
    /// </summary>
    void TrySeedAssignListsFromSpawn()
    {
        Player p = GetDraftPlayer();
        if (p == null)
            return;

        if (p.spawnedPlayer == lastSeededCharacter && p.spawnedLifeline == lastSeededLifeline)
            return;

        lastSeededCharacter = p.spawnedPlayer;
        lastSeededLifeline = p.spawnedLifeline;

        // Reject a field instance that was wrongly bound as the character prefab
        if (IsFieldObject(p.spawnedPlayer))
        {
            Debug.LogWarning("[CharacterCreation] Draft character was a Field — clearing; re-assign with Ctrl+Shift+O.");
            if (p.character != null)
                p.character.prefabs = null;
#if UNITY_EDITOR
            CharacterCreationDraft asset = CharacterCreationDraft.LoadOrCreate();
            if (asset != null)
            {
                CharacterCreationDraft.RepairMissingMaterialRefs(asset);
                if (asset.sourceCharacterPrefab != null && !CharacterCreationDraft.IsFieldPrefab(asset.sourceCharacterPrefab))
                {
                    if (p.character == null)
                        p.character = new ObjectInfo();
                    p.character.prefabs = asset.sourceCharacterPrefab;
                }
            }
#endif
            characterObjects.Clear();
            return;
        }

        if (FirstLive(characterObjects) == null && p.spawnedPlayer != null && !IsFieldObject(p.spawnedPlayer))
        {
            characterObjects.Clear();
            CollectHierarchy(p.spawnedPlayer, characterObjects);
        }

        if (FirstLive(lifelineObjects) == null && p.spawnedLifeline != null && !IsFieldObject(p.spawnedLifeline))
        {
            lifelineObjects.Clear();
            CollectHierarchy(p.spawnedLifeline, lifelineObjects);
        }

        if (objectAssignMode)
            ApplyHighlights();
    }

    static bool IsFieldObject(GameObject go)
    {
        if (go == null)
            return false;
        if (go.GetComponentInParent<Field_Info>() != null)
            return true;
        if (go.GetComponentInChildren<Field_Info>(true) != null)
            return true;
        return false;
    }

    /// <summary>Slot 0 is always the human draft — never hand it to ComputerAI.</summary>
    void EnsureDraftIsHuman()
    {
        Player draft = GetDraftPlayer();
        if (draft == null)
            return;
        if (draft.computer)
            draft.computer = false;
    }

    void EnforceCreationHpFloor()
    {
        if (db == null || db.players == null)
            return;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p != null && p.currentHealth < 1)
                p.currentHealth = 1;
        }
    }

    void EnsureJoiningEnabled()
    {
        PlayerInputManager pim = PlayerInputManager.instance;
        if (pim == null)
            pim = FindAnyObjectByType<PlayerInputManager>();
        if (pim == null)
            return;

        if (!pim.joiningEnabled)
            pim.EnableJoining();
    }

    /// <summary>
    /// Re-bind any ControllerLink that exists in the scene but isn't on the draft yet
    /// (covers joins that raced roster setup or failed a one-shot Start bind).
    /// </summary>
    void ClaimUnboundControllers()
    {
        if (db == null)
            return;

        ControllerLink[] links = FindObjectsByType<ControllerLink>();
        Player draft = GetDraftPlayer();
        if (draft == null)
            return;

        for (int i = 0; i < links.Length; i++)
        {
            ControllerLink cL = links[i];
            if (cL == null)
                continue;

            bool onDraft = draft.cLink == cL
                || (draft.inputLinks != null && draft.inputLinks.Contains(cL));
            if (onDraft)
                continue;

            BindControllerToDraft(cL);
        }
    }

    void HandleHotkeys()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return;

        if (kb.escapeKey.wasPressedThisFrame)
            SetMenuOpen(!menuOpen);

        bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
        bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
        if (!ctrl || !shift)
            return;

        // Tabs
        if (kb.lKey.wasPressedThisFrame)
            OpenMenuTab(MenuTab.Level);
        else if (kb.kKey.wasPressedThisFrame)
            OpenMenuTab(MenuTab.Commands);
        else if (kb.jKey.wasPressedThisFrame)
            OpenMenuTab(MenuTab.Character);
        // Object assign
        else if (kb.oKey.wasPressedThisFrame)
            ToggleObjectAssign(!objectAssignMode);
        // Commands
        else if (kb.hKey.wasPressedThisFrame)
            CmdResetHp();
        else if (kb.rKey.wasPressedThisFrame)
            CmdResetAllStats();
        else if (kb.bKey.wasPressedThisFrame)
            SetInfiniteBump(!infiniteBump);
        else if (kb.sKey.wasPressedThisFrame)
            SetInfiniteSuper(!infiniteSuper);
        else if (kb.dKey.wasPressedThisFrame)
            SetInfiniteDash(!infiniteDash);
        else if (kb.iKey.wasPressedThisFrame)
            SetInfiniteAllSkills(!infiniteAllSkills);
        else if (kb.gKey.wasPressedThisFrame)
            CmdFillSkillsOnce();
    }

    void OpenMenuTab(MenuTab tab)
    {
        activeTab = tab;
        if (!menuOpen)
            SetMenuOpen(true);
        else
            ShowTab(tab);

        if (tab == MenuTab.Level)
            RebuildLevelList();
    }

    void ApplyInfiniteSkills()
    {
        if (!infiniteBump && !infiniteSuper && !infiniteDash && !infiniteAllSkills)
            return;
        if (db == null || db.players == null)
            return;

        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null)
                continue;
            if (commandTargetMode == 1 && p.index != 0)
                continue;

            if (infiniteAllSkills || infiniteBump)
                FillSkill(p.bump);
            if (infiniteAllSkills || infiniteSuper)
                FillSkill(p.super);
            if (infiniteAllSkills || infiniteDash)
                FillSkill(p.dash);
        }
    }

    static void FillSkill(Skill s)
    {
        if (s == null)
            return;
        if (s.max < 1f)
            s.max = 1f;
        s.amount = s.max;
    }

    void OnDestroy()
    {
        if (sessionActive && Application.isPlaying)
            PersistWorkingDraftForEditor();

        ClearHighlights();
        if (instance == this)
            instance = null;
    }

    /// <summary>
    /// Writes the in-progress character/field/objects into ActiveDraft so the next
    /// Play Mode session restores them without needing Save Character.
    /// </summary>
    public void PersistWorkingDraftForEditor()
    {
#if UNITY_EDITOR
        if (db == null)
            db = Database.instance;

        if (menuOpen)
            PushUiToDraft();

        Player draft = GetDraftPlayer();
        if (draft == null)
            return;

        // Bake current world placement into offsets before writing the draft
        SyncOffsetsFromWorld(draft);

        CharacterCreationDraft asset = CharacterCreationDraft.LoadOrCreate();
        if (asset == null)
            return;

        ApplyAssignedRootsToDraft();

        GameObject charRoot = FirstLive(characterObjects) ?? draft.spawnedPlayer;
        GameObject lifeRoot = FirstLive(lifelineObjects) ?? draft.spawnedLifeline;
        if (IsFieldObject(charRoot))
            charRoot = null;
        if (IsFieldObject(lifeRoot))
            lifeRoot = null;

        // Always remember the authored source prefab (keeps real materials).
        // Never replace it with a pink draft bake or a Field_Info root.
        if (charRoot != null)
        {
            GameObject source = FindPrefabAssetOrKeep(charRoot, asset.sourceCharacterPrefab);
            if (source != null
                && !CharacterCreationDraft.IsFieldPrefab(source)
                && !CharacterCreationDraft.PrefabHasMissingMaterials(source))
            {
                asset.sourceCharacterPrefab = source;
                if (draft.character == null)
                    draft.character = new ObjectInfo();
                draft.character.prefabs = source;
            }
            else if (!IsFieldObject(charRoot))
            {
                GameObject baked = CharacterCreationDraft.SaveDraftPrefab(
                    charRoot, CharacterCreationDraft.DraftCharacterPrefabPath);
                if (baked != null && !CharacterCreationDraft.IsFieldPrefab(baked))
                {
                    if (draft.character == null)
                        draft.character = new ObjectInfo();
                    draft.character.prefabs = baked;
                }
                else if (asset.sourceCharacterPrefab != null
                    && !CharacterCreationDraft.IsFieldPrefab(asset.sourceCharacterPrefab))
                {
                    if (draft.character == null)
                        draft.character = new ObjectInfo();
                    draft.character.prefabs = asset.sourceCharacterPrefab;
                }
            }
        }

        if (lifeRoot != null)
        {
            GameObject source = FindPrefabAssetOrKeep(lifeRoot, asset.sourceLifelinePrefab);
            if (source != null
                && !CharacterCreationDraft.IsFieldPrefab(source)
                && !CharacterCreationDraft.PrefabHasMissingMaterials(source))
            {
                asset.sourceLifelinePrefab = source;
                if (draft.lifeline == null)
                    draft.lifeline = new ObjectInfo();
                draft.lifeline.prefabs = source;
            }
            else if (!IsFieldObject(lifeRoot))
            {
                GameObject baked = CharacterCreationDraft.SaveDraftPrefab(
                    lifeRoot, CharacterCreationDraft.DraftLifelinePrefabPath);
                if (baked != null && !CharacterCreationDraft.IsFieldPrefab(baked))
                {
                    if (draft.lifeline == null)
                        draft.lifeline = new ObjectInfo();
                    draft.lifeline.prefabs = baked;
                }
                else if (asset.sourceLifelinePrefab != null
                    && !CharacterCreationDraft.IsFieldPrefab(asset.sourceLifelinePrefab))
                {
                    if (draft.lifeline == null)
                        draft.lifeline = new ObjectInfo();
                    draft.lifeline.prefabs = asset.sourceLifelinePrefab;
                }
            }
        }

        int field = db != null ? db.selectedField : -1;
        asset.CaptureFromPlayer(draft, field, playerInfoDirty);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Debug.Log("[CharacterCreation] Working draft autosaved (field=" + field + ", name=" + draft.name + ")");
#endif
    }

    // ─── Controller routing ────────────────────────────────────────────

    /// <summary>Route every joined device onto the draft player (index 0).</summary>
    public int BindControllerToDraft(ControllerLink cL)
    {
        if (db == null || cL == null)
            return -1;

        EnsureDraftRoster();
        Player draft = GetDraftPlayer();
        if (draft == null)
            return -1;

        draft.computer = false;
        draft.cLink = cL;
        cL.index = draft.index;
        if (draft.inputLinks == null)
            draft.inputLinks = new List<ControllerLink>();
        if (!draft.inputLinks.Contains(cL))
            draft.inputLinks.Add(cL);

        if (!db.controllers.Contains(cL))
            db.controllers.Add(cL);

        // Selectors are lobby-only; destroy if one got attached earlier
        if (draft.pso != null)
        {
            Destroy(draft.pso.gameObject);
            draft.pso = null;
        }

        statusTextSafe("Controller bound to draft slot (" + draft.inputLinks.Count + " device(s))");
        return draft.index;
    }

    Player GetDraftPlayer()
    {
        if (db == null || db.players == null)
            return null;
        for (int i = 0; i < db.players.Count; i++)
        {
            if (db.players[i] != null && db.players[i].index == 0)
                return db.players[i];
        }
        return db.players.Count > 0 ? db.players[0] : null;
    }

    // ─── Match setup ───────────────────────────────────────────────────

    void BeginSandboxMatch()
    {
        if (sandboxBuilt)
            return;
        if (db == null)
            db = Database.instance;
        if (mm == null)
            mm = MenuManager.instance;
        if (db == null)
            return;

        sandboxBuilt = true;
        db.aiTrainingSession = false;
        db.gametype = Gametype.Vs;
        db.levelSelect = false;
        db.ballSelect = false;
        db.positionSelect = false;
        db.teamSelect = false;
        db.selectedField = -1;
        db.EnsureBallSlotsPublic();
        db.ballCount = 1;
        db.ballSlots[0] = -1;
        db.selectedBall = -1;
        db.someoneWon = false;
        db.winnerScreen = false;
        db.gameStart = false;
        db.startingGame = false;
        Time.timeScale = 1f;

        db.players.Clear();
        // Drop stale controller refs from a previous lobby join so new pads can pair
        if (db.controllers != null)
            db.controllers.RemoveAll(x => x == null);
        db.minPlayers = 2;
        db.maxPlayers = 8;

        EnsureJoiningEnabled();

        // Slot 0 draft — always human. No Test spawn: assigned objects / working draft only.
        db.PlayerAdd(null);
        Player draft = db.players[db.players.Count - 1];
        Characters seed = LoadWorkingDraftOrEmpty();
        if (seed != null)
            draft.SetUpCharacter(seed);
        else
        {
            draft.name = "New Character";
            draft.maxHealth = 10;
            draft.currentHealth = 10;
            draft.character = new ObjectInfo();
            draft.lifeline = new ObjectInfo();
            draft.bump = new Skill();
            draft.super = new Skill();
            draft.dash = new Skill();
        }
        draft.characterSelected = true;
        draft.wantRandomCharacter = false;
        draft.computer = false;
        draft.inputLinks = new List<ControllerLink>();
        draft.cLink = null;

        // Slot 1+ CPUs
        db.PlayerAdd(null);
        Player cpu = db.players[db.players.Count - 1];
        Characters opp = db.RandomCharacter();
        if (opp != null)
            cpu.SetUpCharacter(opp);
        ComputerAI.SetDifficulty(cpu, ComputerAI.CpuDifficulty.Easy);
        ComputerAI.AssignMatchBrain(cpu);
        cpu.characterSelected = true;
        cpu.computer = true;

        PullDraftIntoUi(draft);

        if (mm != null)
        {
            mm.openMenu.Clear();
            mm.OpenMenu("Playing");
        }

        // Bind any pads that already exist, then start the match
        ControllerLink[] existing = FindObjectsByType<ControllerLink>();
        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] != null)
                BindControllerToDraft(existing[i]);
        }

        db.CharactersPicked("balls");

        string status;
        if (seed != null && seed.character != null && seed.character.prefabs != null)
            status = "Restored working draft '" + seed.name + "' — assign objects with Ctrl+Shift+O if needed";
        else
            status = "No character spawned — Ctrl+Shift+O, LMB a hierarchy to assign as the draft character";
        statusTextSafe(status);
    }

    Characters LoadWorkingDraftOrEmpty()
    {
#if UNITY_EDITOR
        CharacterCreationDraft asset = CharacterCreationDraft.LoadOrCreate();
        if (asset != null && asset.hasSession)
        {
            if (asset.fieldIndex >= 0 && db.fields != null && asset.fieldIndex < db.fields.Count)
                db.selectedField = asset.fieldIndex;

            playerInfoDirty = asset.playerInfoCustom;
            Characters fromDraft = asset.ToCharacters();
            if (string.IsNullOrWhiteSpace(fromDraft.name))
                fromDraft.name = "New Character";
            return fromDraft;
        }
#endif
        return null;
    }

    void EnsureDraftRoster()
    {
        if (db == null)
            db = Database.instance;
        if (db == null)
            return;
        if (db.players == null)
            db.players = new List<Player>();
        if (!sandboxBuilt)
        {
            BeginSandboxMatch();
            return;
        }
        if (db.players.Count == 0)
        {
            sandboxBuilt = false;
            BeginSandboxMatch();
        }
    }

    void SeedObjectListsFromPlayer(Player p)
    {
        characterObjects.Clear();
        lifelineObjects.Clear();
        if (p == null)
            return;
        if (p.spawnedPlayer != null)
            CollectHierarchy(p.spawnedPlayer, characterObjects);
        if (p.spawnedLifeline != null)
            CollectHierarchy(p.spawnedLifeline, lifelineObjects);
    }

    static void CollectHierarchy(GameObject root, List<GameObject> into)
    {
        if (root == null || into.Contains(root))
            return;
        into.Add(root);
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t != null && t.gameObject != null && !into.Contains(t.gameObject))
                into.Add(t.gameObject);
        }
    }

    // ─── Esc menu ──────────────────────────────────────────────────────

    void SetMenuOpen(bool open)
    {
        menuOpen = open;
        if (menuRoot != null)
            menuRoot.SetActive(open);

        if (open)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            Player draft = GetDraftPlayer();
            if (draft != null)
                PullDraftIntoUi(draft);
            // Do not auto-recalc wall offsets here — that was overwriting saved insets on every Esc
            ShowTab(activeTab);
            if (activeTab == MenuTab.Level)
                RebuildLevelList();
            SyncCommandToggles();
        }
        else
        {
            Time.timeScale = savedTimeScale > 0.01f ? savedTimeScale : 1f;
            PushUiToDraft();
        }
    }

    void ShowTab(MenuTab tab)
    {
        activeTab = tab;
        if (tabCharacter != null) tabCharacter.SetActive(tab == MenuTab.Character);
        if (tabLevel != null) tabLevel.SetActive(tab == MenuTab.Level);
        if (tabCommands != null) tabCommands.SetActive(tab == MenuTab.Commands);
        ColorTabButton(tabBtnCharacter, tab == MenuTab.Character);
        ColorTabButton(tabBtnLevel, tab == MenuTab.Level);
        ColorTabButton(tabBtnCommands, tab == MenuTab.Commands);
    }

    static void ColorTabButton(Button btn, bool on)
    {
        if (btn == null)
            return;
        Image img = btn.GetComponent<Image>();
        if (img != null)
            img.color = on ? new Color(0.25f, 0.55f, 0.85f, 1f) : new Color(0.18f, 0.22f, 0.28f, 1f);
    }

    public void ToggleObjectAssign(bool on)
    {
        objectAssignMode = on;
        if (on)
        {
            // Refresh lists from live spawns if empty
            Player draft = GetDraftPlayer();
            ApplyHighlights();
            statusTextSafe("Object Assign ON — LMB character (green), RMB lifeline (red). Field clicks ignored. Ctrl+Shift+O to exit.");
        }
        else
        {
            ClearHighlights();
            statusTextSafe("Object Assign OFF");
        }
        if (hintLabel != null)
        {
            Text t = hintLabel.GetComponent<Text>();
            if (t != null)
                t.text = objectAssignMode
                    ? "OBJECT ASSIGN — LMB=Character  RMB=Lifeline  |  Ctrl+Shift+O exit"
                    : "Esc menu | Ctrl+Shift+J/L/K tabs | O assign | H HP | R reset | B/S/D inf | I all | G fill";
        }
    }

    void HandleObjectAssignClicks()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        bool lmb = mouse.leftButton.wasPressedThisFrame;
        bool rmb = mouse.rightButton.wasPressedThisFrame;
        if (!lmb && !rmb)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            cam = FindAnyObjectByType<Camera>();
        if (cam == null)
            return;

        Vector2 screen = mouse.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(screen);
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f))
            return;

        GameObject go = hit.collider != null ? hit.collider.gameObject : hit.transform.gameObject;
        if (go == null)
            return;

        if (IsFieldObject(go))
        {
            statusTextSafe("Can't assign the play-field — click the character / lifeline hierarchy instead");
            return;
        }

        GameObject root = ResolveAssignableRoot(go);
        if (root == null)
        {
            statusTextSafe("Couldn't resolve a character/lifeline root from that click");
            return;
        }

        if (IsFieldObject(root))
        {
            statusTextSafe("Can't assign the play-field — click the character / lifeline hierarchy instead");
            return;
        }

        if (lmb)
            ToggleHierarchy(root, characterObjects, lifelineObjects, "character");
        else
            ToggleHierarchy(root, lifelineObjects, characterObjects, "lifeline");

        ApplyAssignedRootsToDraft();
        ApplyHighlights();
    }

    static GameObject ResolveAssignableRoot(GameObject go)
    {
        if (go == null)
            return null;

        // Never climb into / return the field root
        Field_Info field = go.GetComponentInParent<Field_Info>();
        if (field != null && (go == field.gameObject || go.transform.IsChildOf(field.transform)))
        {
            // If this hit is under the field but also under a PlayerGrab (character placed oddly), prefer PlayerGrab
            PlayerGrab underField = go.GetComponentInParent<PlayerGrab>();
            if (underField == null || underField.transform.IsChildOf(field.transform) == false)
                return null;
        }

        PlayerGrab pgRoot = go.GetComponentInParent<PlayerGrab>();
        if (pgRoot != null)
        {
            Transform climb = pgRoot.transform;
            while (climb.parent != null
                && climb.parent.GetComponent<PlayerGrab>() != null
                && climb.parent.GetComponent<Field_Info>() == null)
                climb = climb.parent;
            if (IsFieldObject(climb.gameObject))
                return null;
            return climb.gameObject;
        }

#if UNITY_EDITOR
        GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);
        if (prefabRoot != null && !IsFieldObject(prefabRoot))
            return prefabRoot;
#endif

        Transform t = go.transform;
        while (t.parent != null)
        {
            Transform parent = t.parent;
            if (parent.GetComponent<Field_Info>() != null)
                break;
            if (parent.GetComponent<Camera>() != null)
                break;
            if (parent.GetComponent<Canvas>() != null)
                break;
            if (parent.name == "DontDestroyOnLoad")
                break;
            // Stop at outermost sibling group that still isn't the scene root dump
            if (parent.parent == null)
                break;
            t = parent;
        }

        if (IsFieldObject(t.gameObject))
            return null;
        return t.gameObject;
    }

    void ToggleHierarchy(GameObject root, List<GameObject> primary, List<GameObject> other, string label)
    {
        List<GameObject> members = new List<GameObject>();
        CollectHierarchy(root, members);

        bool removing = primary.Contains(root);
        for (int i = 0; i < members.Count; i++)
        {
            GameObject m = members[i];
            if (m == null)
                continue;
            other.Remove(m);
            if (removing)
                primary.Remove(m);
            else if (!primary.Contains(m))
                primary.Add(m);
        }

        // Keep the hierarchy root first so save/spawn uses that prefab
        if (!removing)
        {
            primary.Remove(root);
            primary.Insert(0, root);
        }

        statusTextSafe((removing ? "Removed from " : "Assigned as ") + label + ": " + root.name
            + " (" + members.Count + " objects)");
    }

    void ApplyAssignedRootsToDraft()
    {
        Player p = GetDraftPlayer();
        if (p == null)
            return;
        if (p.character == null)
            p.character = new ObjectInfo();
        if (p.lifeline == null)
            p.lifeline = new ObjectInfo();

        GameObject charRoot = FirstLive(characterObjects);
        GameObject lifeRoot = FirstLive(lifelineObjects);
        if (charRoot != null && !IsFieldObject(charRoot))
        {
            GameObject src = FindPrefabAssetOrKeep(charRoot, p.character.prefabs);
            if (src != null && !CharacterCreationDraft.IsFieldPrefab(src))
                p.character.prefabs = src;
        }
        if (lifeRoot != null && !IsFieldObject(lifeRoot))
        {
            GameObject src = FindPrefabAssetOrKeep(lifeRoot, p.lifeline.prefabs);
            if (src != null && !CharacterCreationDraft.IsFieldPrefab(src))
                p.lifeline.prefabs = src;
        }

#if UNITY_EDITOR
        // Keep source refs on the draft asset so reloads don't use pink bakes / field roots
        CharacterCreationDraft asset = CharacterCreationDraft.LoadOrCreate();
        if (asset != null)
        {
            if (p.character.prefabs != null
                && !CharacterCreationDraft.IsFieldPrefab(p.character.prefabs)
                && !CharacterCreationDraft.PrefabHasMissingMaterials(p.character.prefabs))
                asset.sourceCharacterPrefab = p.character.prefabs;
            if (p.lifeline.prefabs != null
                && !CharacterCreationDraft.IsFieldPrefab(p.lifeline.prefabs)
                && !CharacterCreationDraft.PrefabHasMissingMaterials(p.lifeline.prefabs))
                asset.sourceLifelinePrefab = p.lifeline.prefabs;
            EditorUtility.SetDirty(asset);
        }
#endif
    }

    static GameObject FirstLive(List<GameObject> list)
    {
        if (list == null)
            return null;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null)
                return list[i];
        }
        return null;
    }

    void ApplyHighlights()
    {
        ClearHighlights();
        TintList(characterObjects, new Color(0.15f, 0.95f, 0.25f, 1f));
        TintList(lifelineObjects, new Color(0.95f, 0.15f, 0.15f, 1f));
    }

    void TintList(List<GameObject> list, Color color)
    {
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            if (go == null)
                continue;
            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < rends.Length; r++)
            {
                Renderer rend = rends[r];
                if (rend == null || highlightRestore.ContainsKey(rend))
                    continue;
                Material[] mats = rend.materials;
                Color[] saved = new Color[mats.Length];
                for (int m = 0; m < mats.Length; m++)
                {
                    saved[m] = mats[m].HasProperty("_Color") ? mats[m].color : Color.white;
                    if (mats[m].HasProperty("_Color"))
                        mats[m].color = Color.Lerp(saved[m], color, 0.65f);
                    if (mats[m].HasProperty("_BaseColor"))
                        mats[m].SetColor("_BaseColor", Color.Lerp(mats[m].GetColor("_BaseColor"), color, 0.65f));
                }
                highlightRestore[rend] = saved;
            }
        }
    }

    void ClearHighlights()
    {
        foreach (var kv in highlightRestore)
        {
            Renderer rend = kv.Key;
            if (rend == null)
                continue;
            Material[] mats = rend.materials;
            Color[] saved = kv.Value;
            for (int m = 0; m < mats.Length && m < saved.Length; m++)
            {
                if (mats[m].HasProperty("_Color"))
                    mats[m].color = saved[m];
            }
        }
        highlightRestore.Clear();
    }

    // ─── Wall distance ─────────────────────────────────────────────────

    public void RecalculateWallDistances(bool applyToSliders)
    {
        Player p = GetDraftPlayer();
        if (p == null || db == null)
            return;

        SyncOffsetsFromWorld(p);

        if (applyToSliders)
        {
            if (charWallSlider != null && p.character != null)
                charWallSlider.SetValueWithoutNotify(p.character.positionOffset.y);
            if (lifeWallSlider != null && p.lifeline != null)
                lifeWallSlider.SetValueWithoutNotify(p.lifeline.positionOffset.y);
            UpdateSliderLabels();
        }

        RefreshDistanceLabel();
    }

    /// <summary>
    /// Write ObjectInfo offsets from live transforms using the same axes StartGame uses
    /// (right = x, up = y, forward = z from the spawn wall).
    /// </summary>
    void SyncOffsetsFromWorld(Player p)
    {
        if (p == null || db == null)
            return;
        if (p.character == null)
            p.character = new ObjectInfo();
        if (p.lifeline == null)
            p.lifeline = new ObjectInfo();

        if (p.spawnedPlayer != null)
        {
            Vector3 wall = WallAnchor(p.facing);
            Transform t = p.spawnedPlayer.transform;
            Vector3 delta = t.position - wall;
            float coop = (p.position == 0) ? 1f : 1.5f;
            p.character.positionOffset = new Vector3(
                Vector3.Dot(delta, t.right),
                Vector3.Dot(delta, t.up) / coop,
                Vector3.Dot(delta, t.forward));
        }

        if (p.spawnedLifeline != null)
        {
            Vector3 wall = WallAnchor(p.facing);
            Transform t = p.spawnedLifeline.transform;
            // Lifeline is placed on the wall then offset in its own local axes
            Vector3 lane = p.spawnedPlayer != null ? p.spawnedPlayer.transform.position : t.position;
            switch (p.facing)
            {
                case Facing.Up:
                case Facing.Down:
                    wall.x = lane.x;
                    break;
                case Facing.Left:
                case Facing.Right:
                    wall.y = lane.y;
                    break;
            }
            wall.z = db.FieldPlaySize;
            Vector3 delta = t.position - wall;
            p.lifeline.positionOffset = new Vector3(
                Vector3.Dot(delta, t.right),
                Vector3.Dot(delta, t.up),
                Vector3.Dot(delta, t.forward));
        }
    }

    Vector3 WallAnchor(Facing facing)
    {
        float half = db != null ? db.FieldPlaySize * 0.5f : 0f;
        float z = db != null ? db.FieldPlaySize : 0f;
        switch (facing)
        {
            case Facing.Up: return new Vector3(0f, -half, z);
            case Facing.Down: return new Vector3(0f, half, z);
            case Facing.Left: return new Vector3(half, 0f, z);
            case Facing.Right: return new Vector3(-half, 0f, z);
            default: return new Vector3(0f, 0f, z);
        }
    }

    void RefreshDistanceLabel()
    {
        if (distanceText == null)
            return;
        Player p = GetDraftPlayer();
        if (p == null)
        {
            distanceText.text = "Distances: (no draft)";
            return;
        }
        float c = p.character != null ? p.character.positionOffset.y : 0f;
        float l = p.lifeline != null ? p.lifeline.positionOffset.y : 0f;
        distanceText.text = "Wall inset — Character: " + c.ToString("0.###")
            + "  Lifeline: " + l.ToString("0.###")
            + "\nObjects — Char: " + characterObjects.Count + "  Life: " + lifelineObjects.Count
            + (playerInfoDirty ? "  |  PlayerInfo: NEW PREFAB" : "  |  PlayerInfo: default (null)");
    }

    // ─── UI ↔ draft ────────────────────────────────────────────────────

    void PullDraftIntoUi(Player p)
    {
        if (p == null)
            return;
        if (nameField != null) nameField.SetTextWithoutNotify(p.name ?? "");
        if (descField != null) descField.SetTextWithoutNotify(p.superDescription ?? "");
        if (superNameField != null) superNameField.SetTextWithoutNotify(p.superName ?? "");
        if (healthSlider != null) healthSlider.SetValueWithoutNotify(p.maxHealth);
        if (moveSlider != null) moveSlider.SetValueWithoutNotify(p.movementSpeed);
        if (pushSlider != null) pushSlider.SetValueWithoutNotify(p.pushBack);
        if (p.bump != null)
        {
            if (bumpSpeedSlider != null) bumpSpeedSlider.SetValueWithoutNotify(p.bump.speed);
            if (bumpMaxSlider != null) bumpMaxSlider.SetValueWithoutNotify(p.bump.max);
        }
        if (p.dash != null)
        {
            if (dashSpeedSlider != null) dashSpeedSlider.SetValueWithoutNotify(p.dash.speed);
            if (dashMaxSlider != null) dashMaxSlider.SetValueWithoutNotify(p.dash.max);
        }
        if (p.super != null)
        {
            if (superSpeedSlider != null) superSpeedSlider.SetValueWithoutNotify(p.super.speed);
            if (superMaxSlider != null) superMaxSlider.SetValueWithoutNotify(p.super.max);
        }
        if (p.character != null && charWallSlider != null)
            charWallSlider.SetValueWithoutNotify(p.character.positionOffset.y);
        if (p.lifeline != null && lifeWallSlider != null)
            lifeWallSlider.SetValueWithoutNotify(p.lifeline.positionOffset.y);
        if (ignoreFacingToggle != null) ignoreFacingToggle.SetIsOnWithoutNotify(p.ignoreFacing);
        if (playerInfoDirtyToggle != null) playerInfoDirtyToggle.SetIsOnWithoutNotify(playerInfoDirty);
        UpdateSliderLabels();
        RefreshDistanceLabel();
    }

    void PushUiToDraft()
    {
        Player p = GetDraftPlayer();
        if (p == null)
            return;

        if (nameField != null) p.name = nameField.text.Trim();
        if (descField != null) p.superDescription = descField.text;
        if (superNameField != null) p.superName = superNameField.text;
        if (healthSlider != null)
        {
            p.maxHealth = Mathf.RoundToInt(healthSlider.value);
            p.currentHealth = Mathf.Min(p.currentHealth, p.maxHealth);
        }
        if (moveSlider != null) p.movementSpeed = moveSlider.value;
        if (pushSlider != null) p.pushBack = pushSlider.value;
        if (p.bump == null) p.bump = new Skill();
        if (p.dash == null) p.dash = new Skill();
        if (p.super == null) p.super = new Skill();
        if (bumpSpeedSlider != null) p.bump.speed = bumpSpeedSlider.value;
        if (bumpMaxSlider != null) { p.bump.max = bumpMaxSlider.value; p.bump.amount = Mathf.Min(p.bump.amount, p.bump.max); }
        if (dashSpeedSlider != null) p.dash.speed = dashSpeedSlider.value;
        if (dashMaxSlider != null) { p.dash.max = dashMaxSlider.value; p.dash.amount = Mathf.Min(p.dash.amount, p.dash.max); }
        if (superSpeedSlider != null) p.super.speed = superSpeedSlider.value;
        if (superMaxSlider != null) { p.super.max = superMaxSlider.value; }
        if (p.character == null) p.character = new ObjectInfo();
        if (p.lifeline == null) p.lifeline = new ObjectInfo();
        if (charWallSlider != null) p.character.positionOffset.y = charWallSlider.value;
        if (lifeWallSlider != null) p.lifeline.positionOffset.y = lifeWallSlider.value;
        if (ignoreFacingToggle != null) p.ignoreFacing = ignoreFacingToggle.isOn;
        if (playerInfoDirtyToggle != null) playerInfoDirty = playerInfoDirtyToggle.isOn;

        ApplyAssignedRootsToDraft();
    }

    static GameObject FindPrefabAssetOrKeep(GameObject live, GameObject fallback)
    {
#if UNITY_EDITOR
        if (live == null || IsFieldObject(live))
            return CharacterCreationDraft.IsFieldPrefab(fallback) ? null : fallback;
        GameObject src = PrefabUtility.GetCorrespondingObjectFromSource(live);
        if (src != null && !CharacterCreationDraft.IsFieldPrefab(src))
            return src;
        GameObject nearest = PrefabUtility.GetNearestPrefabInstanceRoot(live);
        if (nearest != null)
        {
            GameObject nearestSrc = PrefabUtility.GetCorrespondingObjectFromSource(nearest);
            if (nearestSrc != null && !CharacterCreationDraft.IsFieldPrefab(nearestSrc))
                return nearestSrc;
        }
#endif
        return CharacterCreationDraft.IsFieldPrefab(fallback) ? null : fallback;
    }

    void UpdateSliderLabels()
    {
        SetVal(healthVal, healthSlider, "0");
        SetVal(moveVal, moveSlider, "0.##");
        SetVal(pushVal, pushSlider, "0");
        SetVal(bumpSpeedVal, bumpSpeedSlider, "0.##");
        SetVal(bumpMaxVal, bumpMaxSlider, "0.##");
        SetVal(dashSpeedVal, dashSpeedSlider, "0.##");
        SetVal(dashMaxVal, dashMaxSlider, "0.##");
        SetVal(superSpeedVal, superSpeedSlider, "0.##");
        SetVal(superMaxVal, superMaxSlider, "0.##");
        SetVal(charWallVal, charWallSlider, "0.###");
        SetVal(lifeWallVal, lifeWallSlider, "0.###");
    }

    static void SetVal(Text label, Slider s, string fmt)
    {
        if (label != null && s != null)
            label.text = s.value.ToString(fmt);
    }

    // ─── Commands / Level ──────────────────────────────────────────────

    List<Player> CommandTargets()
    {
        var list = new List<Player>();
        if (db == null || db.players == null)
            return list;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null)
                continue;
            if (commandTargetMode == 1 && p.index != 0)
                continue;
            list.Add(p);
        }
        return list;
    }

    void CmdResetHp()
    {
        List<Player> targets = CommandTargets();
        for (int i = 0; i < targets.Count; i++)
            targets[i].currentHealth = targets[i].maxHealth;
        CmdNote("Reset HP (" + targets.Count + ")");
    }

    void CmdResetAllStats()
    {
        List<Player> targets = CommandTargets();
        for (int i = 0; i < targets.Count; i++)
        {
            Player p = targets[i];
            p.currentHealth = p.maxHealth;
            p.damageDealt = 0;
            p.damageTaken = 0;
            p.ballHits = 0;
            p.longestBallOwnership = 0;
            p.highestSingleDamgeDealt = 0;
            p.highestSingleDamageTaken = 0;
            p.ultsUsed = 0;
            p.numberOfDashes = 0;
            p.afterDeathHits = 0;
            p.afterDeathDamage = 0;
            p.won = false;
            FillSkill(p.bump);
            FillSkill(p.super);
            FillSkill(p.dash);
        }
        CmdNote("Reset all stats (" + targets.Count + ")");
    }

    void CmdResetChosenStat(string which)
    {
        List<Player> targets = CommandTargets();
        for (int i = 0; i < targets.Count; i++)
        {
            Player p = targets[i];
            switch (which)
            {
                case "hp": p.currentHealth = p.maxHealth; break;
                case "bump": if (p.bump != null) { p.bump.amount = 0; } break;
                case "super": if (p.super != null) { p.super.amount = 0; } break;
                case "dash": if (p.dash != null) { p.dash.amount = 0; } break;
                case "match":
                    p.damageDealt = 0;
                    p.damageTaken = 0;
                    p.ballHits = 0;
                    p.ultsUsed = 0;
                    p.numberOfDashes = 0;
                    break;
            }
        }
        CmdNote("Reset " + which + " (" + targets.Count + ")");
    }

    void CmdFillSkillsOnce()
    {
        List<Player> targets = CommandTargets();
        for (int i = 0; i < targets.Count; i++)
        {
            FillSkill(targets[i].bump);
            FillSkill(targets[i].super);
            FillSkill(targets[i].dash);
        }
        CmdNote("Filled bump/super/dash once (" + targets.Count + ")");
    }

    void SetInfiniteBump(bool on)
    {
        infiniteBump = on;
        if (!on)
            infiniteAllSkills = false;
        SyncCommandToggles();
        CmdNote(on ? "Infinite BUMP ON" : "Infinite BUMP OFF");
    }

    void SetInfiniteSuper(bool on)
    {
        infiniteSuper = on;
        if (!on)
            infiniteAllSkills = false;
        SyncCommandToggles();
        CmdNote(on ? "Infinite SUPER ON" : "Infinite SUPER OFF");
    }

    void SetInfiniteDash(bool on)
    {
        infiniteDash = on;
        if (!on)
            infiniteAllSkills = false;
        SyncCommandToggles();
        CmdNote(on ? "Infinite DASH ON" : "Infinite DASH OFF");
    }

    void SetInfiniteAllSkills(bool on)
    {
        infiniteAllSkills = on;
        infiniteBump = on;
        infiniteSuper = on;
        infiniteDash = on;
        SyncCommandToggles();
        CmdNote(on ? "Infinite ALL skills ON" : "Infinite ALL skills OFF");
    }

    void SyncCommandToggles()
    {
        if (infBumpToggle != null) infBumpToggle.SetIsOnWithoutNotify(infiniteBump || infiniteAllSkills);
        if (infSuperToggle != null) infSuperToggle.SetIsOnWithoutNotify(infiniteSuper || infiniteAllSkills);
        if (infDashToggle != null) infDashToggle.SetIsOnWithoutNotify(infiniteDash || infiniteAllSkills);
        if (targetEveryoneToggle != null) targetEveryoneToggle.SetIsOnWithoutNotify(commandTargetMode == 0);
    }

    void CmdNote(string msg)
    {
        statusTextSafe(msg);
        if (commandsStatusText != null)
            commandsStatusText.text = msg;
    }

    void RebuildLevelList()
    {
        if (levelListRoot == null || db == null || db.fields == null)
            return;

        for (int i = levelListRoot.transform.childCount - 1; i >= 0; i--)
            Destroy(levelListRoot.transform.GetChild(i).gameObject);

        float y = -4f;
        for (int i = 0; i < db.fields.Count; i++)
        {
            Field f = db.fields[i];
            if (f == null)
                continue;
            int index = i;
            string label = (i == db.selectedField ? "> " : "  ") + (string.IsNullOrEmpty(f.name) ? ("Field " + i) : f.name)
                + (f.active ? "" : " (off)");
            MakeButton(levelListRoot.transform, "Field_" + i, label, new Vector2(4, y), new Vector2(660, 28), () => LoadLevel(index));
            y -= 32f;
        }

        RectTransform rt = levelListRoot.GetComponent<RectTransform>();
        if (rt != null)
            rt.sizeDelta = new Vector2(670f, Mathf.Max(200f, -y + 8f));
    }

    void LoadLevel(int fieldIndex)
    {
        if (db == null || db.fields == null || fieldIndex < 0 || fieldIndex >= db.fields.Count)
        {
            statusTextSafe("Invalid field index");
            return;
        }

        PushUiToDraft();
        SetMenuOpen(false);

        // Preserve draft authorship + devices across field reload
        Player draft = GetDraftPlayer();
        List<ControllerLink> links = new List<ControllerLink>();
        if (draft != null && draft.inputLinks != null)
            links.AddRange(draft.inputLinks);
        if (draft != null && draft.cLink != null && !links.Contains(draft.cLink))
            links.Add(draft.cLink);

        db.selectedField = fieldIndex;
        db.AbortCurrentMatch();

        if (db.players != null)
        {
            for (int i = 0; i < db.players.Count; i++)
            {
                Player p = db.players[i];
                if (p == null)
                    continue;
                p.characterSelected = true;
                p.wantRandomCharacter = false;
                p.currentHealth = p.maxHealth;
                if (p.index == 0)
                {
                    p.computer = false; // never AI in Character Creation
                    p.inputLinks = new List<ControllerLink>(links);
                    p.cLink = links.Count > 0 ? links[links.Count - 1] : null;
                    for (int L = 0; L < links.Count; L++)
                    {
                        if (links[L] != null)
                            links[L].index = p.index;
                    }
                }
                else
                {
                    p.computer = true;
                    ComputerAI.SetDifficulty(p, ComputerAI.CpuDifficulty.Easy);
                }
            }
        }

        characterObjects.Clear();
        lifelineObjects.Clear();
        lastSeededCharacter = null;
        lastSeededLifeline = null;
        ClearHighlights();

        if (mm != null)
        {
            mm.openMenu.Clear();
            mm.OpenMenu("Playing");
        }

        db.levelSelect = false;
        db.ballSelect = false;
        db.positionSelect = false;
        db.CharactersPicked("balls");

        string fname = db.fields[fieldIndex] != null ? db.fields[fieldIndex].name : ("#" + fieldIndex);
        statusTextSafe("Loaded level: " + fname);
        if (levelStatusText != null)
            levelStatusText.text = "Loaded: " + fname;
    }

    // ─── Save ──────────────────────────────────────────────────────────

    void OnClickResume()
    {
        PushUiToDraft();
        SetMenuOpen(false);
    }

    void OnClickRecalcDistances()
    {
        RecalculateWallDistances(applyToSliders: true);
        statusTextSafe("Recalculated wall insets from live objects");
    }

    void OnClickObjectAssign()
    {
        SetMenuOpen(false);
        ToggleObjectAssign(true);
    }

    void OnClickSaveCharacter()
    {
        PushUiToDraft();
        Player p = GetDraftPlayer();
        if (p == null || string.IsNullOrWhiteSpace(p.name))
        {
            statusTextSafe("Save failed — set a character name first");
            return;
        }

#if UNITY_EDITOR
        string safe = SanitizeFileName(p.name);
        string charFolder = "Assets/Resources/Characters";
        EnsureFolder(charFolder);

        // Optional Player Info prefab
        GameObject savedInfo = null;
        if (playerInfoDirty)
        {
            savedInfo = SavePlayerInfoPrefab(p, safe);
            p.playerInfo = savedInfo;
        }
        else
        {
            p.playerInfo = null;
        }

        // Optional character / lifeline prefabs from assigned roots
        SaveAssignedPrefabs(p, safe);

        string path = charFolder + "/" + safe + ".asset";
        CharacterData asset = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<CharacterData>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.characterName = p.name;
        asset.maxHealth = p.maxHealth;
        asset.movementSpeed = p.movementSpeed;
        asset.pushBack = p.pushBack;
        asset.ignoreFacing = p.ignoreFacing;
        asset.bump = p.bump != null ? new Skill(p.bump) : new Skill();
        asset.super = p.super != null ? new Skill(p.super) : new Skill();
        asset.dash = p.dash != null ? new Skill(p.dash) : new Skill();
        asset.character = p.character != null ? new ObjectInfo(p.character) : new ObjectInfo();
        asset.lifeline = p.lifeline != null ? new ObjectInfo(p.lifeline) : new ObjectInfo();
        asset.selector = p.selector;
        asset.playerInfo = playerInfoDirty ? p.playerInfo : null;
        asset.superName = p.superName;
        asset.superDescription = p.superDescription;
        asset.portraitColor = p.portraitColor;
        asset.portrait = p.portrait;
        asset.icon = p.icon;
        asset.active = true;
        if (asset.rosterOrder <= 0)
            asset.rosterOrder = 100;

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Hot-reload into Database roster
        Characters runtime = asset.ToCharacters();
        int idx = db.characters.FindIndex(c => c != null && c.name == runtime.name);
        if (idx >= 0)
            db.characters[idx] = runtime;
        else
            db.characters.Add(runtime);

        statusTextSafe("Saved CharacterData → " + path
            + (playerInfoDirty ? " (+ Player Info prefab)" : " (playerInfo=null)"));
#else
        statusTextSafe("Save only works in the Unity Editor");
#endif
    }

#if UNITY_EDITOR
    GameObject SavePlayerInfoPrefab(Player p, string safe)
    {
        // Find live player info UI for this slot
        GameObject live = null;
        PlayerGrab[] grabs = FindObjectsByType<PlayerGrab>();
        for (int i = 0; i < grabs.Length; i++)
        {
            if (grabs[i] != null && grabs[i].playerIndex == p.index
                && grabs[i].GetComponent<TestInfo>() != null)
            {
                live = grabs[i].gameObject;
                break;
            }
        }

        string folder = "Assets/Prefabs/Player Infos";
        EnsureFolder(folder);
        string path = folder + "/" + safe + ".prefab";

        if (live != null)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(live, path);
            return prefab;
        }

        // Fallback: duplicate default Test info asset reference
        GameObject fallback = db != null ? db.playerInfo : null;
        if (fallback != null)
        {
            GameObject temp = Instantiate(fallback);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Destroy(temp);
            return prefab;
        }
        return null;
    }

    void SaveAssignedPrefabs(Player p, string safe)
    {
        GameObject charRoot = FirstLive(characterObjects) ?? p.spawnedPlayer;
        if (IsFieldObject(charRoot))
            charRoot = null;
        if (p.character != null && charRoot != null)
        {
            string folder = "Assets/Prefabs/_Characters";
            EnsureFolder(folder);
            GameObject src = PrefabUtility.GetCorrespondingObjectFromSource(charRoot);
            if (src != null && CharacterCreationDraft.IsFieldPrefab(src))
                src = null;
            if (src == null)
            {
                string path = folder + "/" + safe + ".prefab";
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(charRoot, path);
                if (!CharacterCreationDraft.IsFieldPrefab(saved))
                    p.character.prefabs = saved;
            }
            else
            {
                p.character.prefabs = src;
            }
        }

        GameObject lifeRoot = FirstLive(lifelineObjects) ?? p.spawnedLifeline;
        if (IsFieldObject(lifeRoot))
            lifeRoot = null;
        if (p.lifeline != null && lifeRoot != null)
        {
            string folder = "Assets/Prefabs/_Lifeline";
            EnsureFolder(folder);
            GameObject src = PrefabUtility.GetCorrespondingObjectFromSource(lifeRoot);
            if (src != null && CharacterCreationDraft.IsFieldPrefab(src))
                src = null;
            if (src == null)
            {
                string path = folder + "/" + safe + ".prefab";
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(lifeRoot, path);
                if (!CharacterCreationDraft.IsFieldPrefab(saved))
                    p.lifeline.prefabs = saved;
            }
            else
            {
                p.lifeline.prefabs = src;
            }
        }
    }

    static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
            return;
        string[] parts = assetPath.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
#endif

    static string SanitizeFileName(string name)
    {
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }

    void statusTextSafe(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
        Debug.Log("[CharacterCreation] " + msg);
    }

    // ─── UI builders ───────────────────────────────────────────────────

    void BuildHint()
    {
        Canvas canvas = GetOverlayCanvas();
        hintLabel = new GameObject("CC Hint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        hintLabel.transform.SetParent(canvas.transform, false);
        RectTransform rt = hintLabel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 8f);
        rt.sizeDelta = new Vector2(1100f, 40f);
        Text t = hintLabel.GetComponent<Text>();
        t.font = UiFont();
        t.fontSize = 12;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(1f, 1f, 1f, 0.85f);
        t.raycastTarget = false;
        t.text = "Esc menu | Ctrl+Shift+J/L/K tabs | O assign | H HP | R reset | B/S/D inf | I all | G fill";
    }

    void BuildMenu()
    {
        Canvas canvas = GetOverlayCanvas();

        // Dimmer catches clicks so world/game UI underneath doesn't steal them
        GameObject dim = new GameObject("CC Dimmer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dim.transform.SetParent(canvas.transform, false);
        RectTransform dimRt = dim.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        Image dimImg = dim.GetComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.45f);
        dimImg.raycastTarget = true;

        menuPanel = new GameObject("Character Creation Menu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        menuPanel.transform.SetParent(canvas.transform, false);
        RectTransform rt = menuPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(740f, 680f);
        Image menuBg = menuPanel.GetComponent<Image>();
        menuBg.color = new Color(0.07f, 0.09f, 0.12f, 0.96f);
        menuBg.raycastTarget = true;

        // Group dimmer + panel so SetMenuOpen toggles both
        GameObject group = new GameObject("CC Menu Group", typeof(RectTransform));
        group.transform.SetParent(canvas.transform, false);
        RectTransform grt = group.GetComponent<RectTransform>();
        grt.anchorMin = Vector2.zero;
        grt.anchorMax = Vector2.one;
        grt.offsetMin = Vector2.zero;
        grt.offsetMax = Vector2.zero;
        dim.transform.SetParent(group.transform, false);
        menuPanel.transform.SetParent(group.transform, false);
        menuRoot = group;

        Transform panel = menuPanel.transform;
        float y = -12f;
        MakeLabel(panel, "Title", new Vector2(16, y), new Vector2(700, 26), 18, "Character Creation").fontStyle = FontStyle.Bold;
        y -= 28f;
        statusText = MakeLabel(panel, "Status", new Vector2(16, y), new Vector2(700, 28), 12, "");
        y -= 32f;

        // Tabs
        tabBtnCharacter = MakeButton(panel, "TabChar", "Character (J)", new Vector2(16, y), new Vector2(220, 30), () => OpenMenuTab(MenuTab.Character));
        tabBtnLevel = MakeButton(panel, "TabLevel", "Level (L)", new Vector2(246, y), new Vector2(220, 30), () => OpenMenuTab(MenuTab.Level));
        tabBtnCommands = MakeButton(panel, "TabCmd", "Commands (K)", new Vector2(476, y), new Vector2(240, 30), () => OpenMenuTab(MenuTab.Commands));
        y -= 36f;

        float contentTop = y;
        tabCharacter = MakePanel(panel, "TabCharacter", new Vector2(8, contentTop), new Vector2(724, 520));
        tabLevel = MakePanel(panel, "TabLevel", new Vector2(8, contentTop), new Vector2(724, 520));
        tabCommands = MakePanel(panel, "TabCommands", new Vector2(8, contentTop), new Vector2(724, 520));

        BuildCharacterTab(tabCharacter.transform);
        BuildLevelTab(tabLevel.transform);
        BuildCommandsTab(tabCommands.transform);

        // Footer always visible
        float fy = -640f;
        MakeButton(panel, "Resume", "Resume", new Vector2(16, fy), new Vector2(160, 30), OnClickResume);
        MakeButton(panel, "ObjectAssign", "Object Assign", new Vector2(188, fy), new Vector2(150, 30), OnClickObjectAssign);
        MakeButton(panel, "Save", "Save Character", new Vector2(350, fy), new Vector2(180, 30), OnClickSaveCharacter);

        ShowTab(MenuTab.Character);
    }

    void BuildCharacterTab(Transform parent)
    {
        float y = -8f;
        distanceText = MakeLabel(parent, "Dist", new Vector2(8, y), new Vector2(700, 36), 12, "");
        y -= 40f;

        nameField = MakeInput(parent, "Name", new Vector2(8, y), new Vector2(340, 28), "Name");
        superNameField = MakeInput(parent, "SuperName", new Vector2(360, y), new Vector2(340, 28), "Super name");
        y -= 36f;
        descField = MakeInput(parent, "Desc", new Vector2(8, y), new Vector2(692, 28), "Description");
        y -= 40f;

        healthSlider = MakeLabeledSlider(parent, "Health", ref y, 1, 30, 10, out healthVal);
        moveSlider = MakeLabeledSlider(parent, "Move Speed", ref y, 0.5f, 20f, 5f, out moveVal);
        pushSlider = MakeLabeledSlider(parent, "Push Back", ref y, 0f, 3000f, 1000f, out pushVal);
        bumpSpeedSlider = MakeLabeledSlider(parent, "Bump Speed", ref y, 0f, 20f, 2f, out bumpSpeedVal);
        bumpMaxSlider = MakeLabeledSlider(parent, "Bump Max", ref y, 0f, 10f, 1f, out bumpMaxVal);
        dashSpeedSlider = MakeLabeledSlider(parent, "Dash Speed", ref y, 0f, 20f, 3f, out dashSpeedVal);
        dashMaxSlider = MakeLabeledSlider(parent, "Dash Max", ref y, 0f, 10f, 2f, out dashMaxVal);
        superSpeedSlider = MakeLabeledSlider(parent, "Super Speed", ref y, 0f, 20f, 5f, out superSpeedVal);
        superMaxSlider = MakeLabeledSlider(parent, "Super Max", ref y, 0f, 100f, 50f, out superMaxVal);
        charWallSlider = MakeLabeledSlider(parent, "Char Wall Inset", ref y, -2f, 8f, 1.2f, out charWallVal);
        lifeWallSlider = MakeLabeledSlider(parent, "Life Wall Inset", ref y, -2f, 4f, 0f, out lifeWallVal);

        ignoreFacingToggle = MakeToggle(parent, "Ignore Facing", new Vector2(8, y), false);
        playerInfoDirtyToggle = MakeToggle(parent, "Edit Player Info → new prefab", new Vector2(260, y), false);
        playerInfoDirtyToggle.onValueChanged.AddListener(v => playerInfoDirty = v);
        y -= 36f;
        MakeButton(parent, "Recalc", "Recalc Dist", new Vector2(8, y), new Vector2(140, 30), OnClickRecalcDistances);
    }

    void BuildLevelTab(Transform parent)
    {
        float y = -8f;
        MakeLabel(parent, "LevelHelp", new Vector2(8, y), new Vector2(700, 36), 12,
            "Pick a field to reload the sandbox on that level. Draft stats & controllers are kept.\nHotkey: Ctrl+Shift+L");
        y -= 44f;
        levelStatusText = MakeLabel(parent, "LevelStatus", new Vector2(8, y), new Vector2(700, 24), 12, "");
        y -= 28f;

        levelListRoot = new GameObject("LevelList", typeof(RectTransform));
        levelListRoot.transform.SetParent(parent, false);
        RectTransform lrt = levelListRoot.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 1f);
        lrt.anchorMax = new Vector2(0f, 1f);
        lrt.pivot = new Vector2(0f, 1f);
        lrt.anchoredPosition = new Vector2(8, y);
        lrt.sizeDelta = new Vector2(670f, 400f);
    }

    void BuildCommandsTab(Transform parent)
    {
        float y = -8f;
        MakeLabel(parent, "CmdHelp", new Vector2(8, y), new Vector2(700, 48), 12,
            "Hotkeys (Ctrl+Shift): H=HP  R=all stats  B/S/D=inf bump/super/dash  I=all inf  G=fill once\nTab: Ctrl+Shift+K");
        y -= 52f;
        commandsStatusText = MakeLabel(parent, "CmdStatus", new Vector2(8, y), new Vector2(700, 24), 12, "");
        y -= 30f;

        targetEveryoneToggle = MakeToggle(parent, "Target everyone (off = draft only)", new Vector2(8, y), true);
        targetEveryoneToggle.onValueChanged.AddListener(v => commandTargetMode = v ? 0 : 1);
        y -= 34f;

        MakeButton(parent, "ResetHP", "Reset HP", new Vector2(8, y), new Vector2(160, 30), CmdResetHp);
        MakeButton(parent, "ResetAll", "Reset All Stats", new Vector2(180, y), new Vector2(170, 30), CmdResetAllStats);
        MakeButton(parent, "FillOnce", "Fill Skills Once", new Vector2(362, y), new Vector2(160, 30), CmdFillSkillsOnce);
        y -= 36f;

        MakeButton(parent, "ClrBump", "Clear Bump", new Vector2(8, y), new Vector2(120, 28), () => CmdResetChosenStat("bump"));
        MakeButton(parent, "ClrSuper", "Clear Super", new Vector2(140, y), new Vector2(120, 28), () => CmdResetChosenStat("super"));
        MakeButton(parent, "ClrDash", "Clear Dash", new Vector2(272, y), new Vector2(120, 28), () => CmdResetChosenStat("dash"));
        MakeButton(parent, "ClrMatch", "Clear Match Stats", new Vector2(404, y), new Vector2(160, 28), () => CmdResetChosenStat("match"));
        y -= 40f;

        infBumpToggle = MakeToggle(parent, "Infinite Bump (Ctrl+Shift+B)", new Vector2(8, y), false);
        infBumpToggle.onValueChanged.AddListener(SetInfiniteBump);
        y -= 28f;
        infSuperToggle = MakeToggle(parent, "Infinite Super (Ctrl+Shift+S)", new Vector2(8, y), false);
        infSuperToggle.onValueChanged.AddListener(SetInfiniteSuper);
        y -= 28f;
        infDashToggle = MakeToggle(parent, "Infinite Dash (Ctrl+Shift+D)", new Vector2(8, y), false);
        infDashToggle.onValueChanged.AddListener(SetInfiniteDash);
        y -= 34f;
        MakeButton(parent, "InfAll", "Toggle Infinite ALL (I)", new Vector2(8, y), new Vector2(240, 30), () => SetInfiniteAllSkills(!infiniteAllSkills));
    }

    GameObject MakePanel(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    Slider MakeLabeledSlider(Transform parent, string label, ref float y, float min, float max, float value, out Text valueLabel)
    {
        MakeLabel(parent, label + "L", new Vector2(16, y), new Vector2(160, 22), 12, label);
        valueLabel = MakeLabel(parent, label + "V", new Vector2(620, y), new Vector2(80, 22), 12, value.ToString("0.##"));
        Slider s = MakeSlider(parent, label + "S", new Vector2(180, y - 2), new Vector2(430, 20), min, max, value);
        Text captured = valueLabel;
        s.onValueChanged.AddListener(_ =>
        {
            if (captured != null)
                captured.text = s.wholeNumbers ? s.value.ToString("0") : s.value.ToString("0.##");
        });
        y -= 28f;
        return s;
    }

    Canvas overlayCanvas;

    Canvas GetOverlayCanvas()
    {
        if (overlayCanvas != null)
            return overlayCanvas;

        EnsureEventSystemForUi();

        GameObject existing = GameObject.Find("CC_OverlayCanvas");
        if (existing != null)
        {
            overlayCanvas = existing.GetComponent<Canvas>();
            if (overlayCanvas != null)
                return overlayCanvas;
        }

        GameObject cGo = new GameObject("CC_OverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        overlayCanvas = cGo.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 5000;
        overlayCanvas.overrideSorting = true;

        CanvasScaler scaler = cGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        return overlayCanvas;
    }

    void EnsureEventSystemForUi()
    {
        UnityEngine.EventSystems.EventSystem es = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject go = new GameObject("EventSystem");
            es = go.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }

        // Project uses Input System — StandaloneInputModule will not receive mouse/touch.
        var old = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (old != null)
            Destroy(old);

        var uiModule = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (uiModule == null)
            uiModule = es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        uiModule.enabled = true;

        if (UnityEngine.EventSystems.EventSystem.current == null)
            UnityEngine.EventSystems.EventSystem.current = es;
    }

    static Font UiFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null)
            f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }

    Text MakeLabel(Transform parent, string name, Vector2 pos, Vector2 size, int fontSize, string text)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Text t = go.GetComponent<Text>();
        t.font = UiFont();
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = TextAnchor.UpperLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.text = text;
        return t;
    }

    InputField MakeInput(Transform parent, string name, Vector2 pos, Vector2 size, string placeholder)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.22f, 1f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(6, 4);
        trt.offsetMax = new Vector2(-6, -4);
        Text text = textGo.GetComponent<Text>();
        text.font = UiFont();
        text.fontSize = 13;
        text.color = Color.white;
        text.supportRichText = false;

        GameObject phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        phGo.transform.SetParent(go.transform, false);
        RectTransform prt = phGo.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = new Vector2(6, 4);
        prt.offsetMax = new Vector2(-6, -4);
        Text ph = phGo.GetComponent<Text>();
        ph.font = UiFont();
        ph.fontSize = 13;
        ph.fontStyle = FontStyle.Italic;
        ph.color = new Color(1f, 1f, 1f, 0.35f);
        ph.text = placeholder;

        InputField field = go.GetComponent<InputField>();
        field.textComponent = text;
        field.placeholder = ph;
        return field;
    }

    Slider MakeSlider(Transform parent, string name, Vector2 pos, Vector2 size, float min, float max, float value)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGo.transform.SetParent(go.transform, false);
        RectTransform bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.25f);
        bgRt.anchorMax = new Vector2(1f, 0.75f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bgGo.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.22f, 1f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        RectTransform faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.25f);
        faRt.anchorMax = new Vector2(1f, 0.75f);
        faRt.offsetMin = new Vector2(5f, 0f);
        faRt.offsetMax = new Vector2(-5f, 0f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fRt = fill.GetComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.offsetMin = Vector2.zero;
        fRt.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.3f, 0.55f, 0.85f, 1f);

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        RectTransform haRt = handleArea.GetComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.offsetMin = new Vector2(8f, 0f);
        haRt.offsetMax = new Vector2(-8f, 0f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform hRt = handle.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(14f, 0f);
        handle.GetComponent<Image>().color = Color.white;

        Slider s = go.GetComponent<Slider>();
        s.fillRect = fRt;
        s.handleRect = hRt;
        s.targetGraphic = handle.GetComponent<Image>();
        s.minValue = min;
        s.maxValue = max;
        s.wholeNumbers = Mathf.Approximately(min, Mathf.Round(min)) && Mathf.Approximately(max, Mathf.Round(max)) && (max - min) <= 40f;
        s.value = value;
        return s;
    }

    Toggle MakeToggle(Transform parent, string label, Vector2 pos, bool on)
    {
        GameObject go = new GameObject(label, typeof(RectTransform), typeof(Toggle));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(250f, 24f);

        GameObject box = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        box.transform.SetParent(go.transform, false);
        RectTransform brt = box.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 0.5f);
        brt.anchorMax = new Vector2(0f, 0.5f);
        brt.pivot = new Vector2(0f, 0.5f);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(20f, 20f);
        box.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.3f, 1f);

        GameObject check = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        check.transform.SetParent(box.transform, false);
        RectTransform crt = check.GetComponent<RectTransform>();
        crt.anchorMin = Vector2.zero;
        crt.anchorMax = Vector2.one;
        crt.offsetMin = new Vector2(3, 3);
        crt.offsetMax = new Vector2(-3, -3);
        check.GetComponent<Image>().color = new Color(0.4f, 0.9f, 0.5f, 1f);

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelGo.transform.SetParent(go.transform, false);
        RectTransform lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(28, 0);
        lrt.offsetMax = Vector2.zero;
        Text t = labelGo.GetComponent<Text>();
        t.font = UiFont();
        t.fontSize = 12;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleLeft;
        t.text = label;

        Toggle toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = box.GetComponent<Image>();
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = on;
        return toggle;
    }

    Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        return MakeButton(parent, label, label, pos, size, onClick);
    }

    Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.7f, 1f);
        Button btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        Text t = textGo.GetComponent<Text>();
        t.font = UiFont();
        t.fontSize = 12;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.text = label;
        return btn;
    }
}
