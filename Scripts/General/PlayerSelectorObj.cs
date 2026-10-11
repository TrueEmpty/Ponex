using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSelectorObj : MonoBehaviour
{
    Database db;

    public ControllerLink cLink = null;
    public int pI = 0;
    PlayerColors pC;

    RectTransform rt;
    Rigidbody rb;
    Image circle;
    Image clickimg;

    float width = 100;
    float height = 100;

    public float speed = 500;

    float clicking = 0;
    bool cooldown = false;

    UniversalCollisionDetector ucd;
    readonly Dictionary<Collider, SelectorTargetInfo> targetInfo =
        new Dictionary<Collider, SelectorTargetInfo>(16);

    Text text;

    public int cpuControl = -1;

    bool lastControllingCpu;
    int lastCpuControlShown = int.MinValue;
    Sprite lastCircleSprite;
    float nextCpuSelectCheck;
    float lastClickFill = -1f;
    float lastLeaveFill = -1f;
    static RectTransform cachedActiveBack;

    struct SelectorTargetInfo
    {
        public bool isCpuDifficulty;
        public bool isPortrait;
    }

    void Awake()
    {
        db = Database.instance;

        rt = GetComponent<RectTransform>();
        circle = GetComponent<Image>();
        clickimg = transform.GetChild(0).GetComponent<Image>();

        RectTransform rootRect = transform.root.GetComponent<RectTransform>();

        if (rootRect != null)
        {
            width = rootRect.rect.width;
            height = rootRect.rect.height;
        }

        ucd = GetComponent<UniversalCollisionDetector>();
        text = transform.GetChild(1).GetComponent<Text>();
        EnsureCursorPhysics();

        // Main cursor image unfills radially while holding Leave (disconnect)
        if (circle != null)
        {
            circle.type = Image.Type.Filled;
            circle.fillMethod = Image.FillMethod.Radial360;
            circle.fillOrigin = (int)Image.Origin360.Top;
            circle.fillClockwise = true;
            circle.fillAmount = 1f;
        }
    }

    void Update()
    {
        if (cLink != null)
        {
            if (db != null && db.storySharedCursor && !IsSharedCursorOwner())
            {
                if (circle != null)
                    circle.enabled = false;
                if (clickimg != null)
                    clickimg.enabled = false;
                return;
            }
            if (circle != null)
                circle.enabled = true;
            if (clickimg != null)
                clickimg.enabled = true;

            // Win screen: while manually scrolling a stats card, Move scrolls text — freeze cursor/clicks
            if (IsWinScrolling())
            {
                ShowOverlay();
                UpdateLeaveUnfill();
                ControllingUI();
                return;
            }

            MoveCircle();
            HandleActions();
            HandleCharacterSelectShortcuts();
            ShowOverlay();
            UpdateLeaveUnfill();
            ControllingUI();
            CPUSelections();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    bool IsWinScrolling()
    {
        if (db == null || !db.winnerScreen || db.players == null)
            return false;
        if (pI < 0 || pI >= db.players.Count)
            return false;

        Player p = db.players[pI];
        return p != null && p.winScrollTarget != null;
    }

    bool IsMenuSelectable(Collider hit)
    {
        if (hit == null)
            return false;
        if (db == null || !db.pauseMenuOpen)
            return true;

        MenuManager mm = MenuManager.instance;
        if (mm == null)
            return true;
        MenuClass pause = mm.FindMenu("Pause");
        if (pause == null || pause.holder == null)
            return true;
        Transform root = pause.holder.transform;
        return hit.transform == root || hit.transform.IsChildOf(root);
    }

    bool HasHitOnTopMenu()
    {
        if (ucd == null)
            return false;
        for (int i = 0; i < ucd.trackedColliders.Count; i++)
        {
            if (IsUnderTopMenu(ucd.trackedColliders[i]))
                return true;
        }
        return false;
    }

    static bool IsUnderTopMenu(Collider hit)
    {
        if (hit == null)
            return false;
        MenuManager mm = MenuManager.instance;
        if (mm == null)
            return true;
        MenuClass top = mm.GetOpenMenu(false);
        if (top == null || top.holder == null)
            return true;
        Transform root = top.holder.transform;
        return hit.transform == root || hit.transform.IsChildOf(root);
    }

    int ClickPlayerIndex()
    {
        // On win screen always use the human cursor owner (so CPU cards can be opened for scroll)
        if (db != null && db.winnerScreen)
            return pI;
        return ControllingCPU() ? cpuControl : pI;
    }

    bool SharedCursor()
    {
        return db != null && db.storySharedCursor;
    }

    bool IsSharedCursorOwner()
    {
        if (!SharedCursor())
            return true;
        PlayerSelectorObj[] all = FindObjectsByType<PlayerSelectorObj>(FindObjectsInactive.Exclude);
        int best = pI;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].pI < best)
                best = all[i].pI;
        }
        return pI == best;
    }

    Vector2 ReadMove()
    {
        if (!SharedCursor())
        {
            ControllerButtons own = cLink != null ? cLink["Move"] : null;
            return own != null ? own.value : Vector2.zero;
        }

        Vector2 sum = Vector2.zero;
        if (db.controllers != null)
        {
            for (int i = 0; i < db.controllers.Count; i++)
            {
                ControllerLink link = db.controllers[i];
                ControllerButtons move = link != null ? link["Move"] : null;
                if (move != null)
                    sum += move.value;
            }
        }
        if (sum.sqrMagnitude > 1f)
            sum.Normalize();
        return sum;
    }

    bool ReadPressed(string action)
    {
        return ReadButton(action, false);
    }

    bool ReadPressedDown(string action)
    {
        return ReadButton(action, true);
    }

    bool ReadButton(string action, bool down)
    {
        if (!SharedCursor())
        {
            ControllerButtons own = cLink != null ? cLink[action] : null;
            if (own == null)
                return false;
            return down ? own.wasPressedThisFrame : own.isPressed;
        }

        if (db.controllers != null)
        {
            for (int i = 0; i < db.controllers.Count; i++)
            {
                ControllerLink link = db.controllers[i];
                ControllerButtons button = link != null ? link[action] : null;
                if (button == null)
                    continue;
                if (down ? button.wasPressedThisFrame : button.isPressed)
                    return true;
            }
        }
        return false;
    }

    public void SetCircleColor(PlayerColors c)
    {
        pC = c;
    }

    void MoveCircle()
    {
        if (rt == null)
            return;

        Vector2 move = ReadMove();

        int multiplier = ReadPressed("Sprint") ? 2 : 1;

        float dt = Time.timeScale < 0.01f ? Time.unscaledDeltaTime : Time.deltaTime;
        Vector2 current = rt.anchoredPosition;
        Vector2 next = current + new Vector2(
            move.x * speed * multiplier * dt,
            move.y * speed * multiplier * dt
        );

        float hW = width / 2;
        float hH = height / 2;

        next = new Vector2(
            Mathf.Clamp(next.x, -hW, hW),
            Mathf.Clamp(next.y, -hH, hH)
        );

        // Assigning an unchanged RectTransform still dirties its Canvas.
        if ((next - current).sqrMagnitude > 0.000001f)
        {
            rt.anchoredPosition = next;
            SyncCursorBody();
        }
    }

    void EnsureCursorPhysics()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere == null)
            sphere = gameObject.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = 25f;
        sphere.center = Vector3.zero;
        sphere.includeLayers = 1 << 5; // UI
        sphere.excludeLayers = 0;

        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (circle != null)
            circle.raycastTarget = false;

        SyncCursorBody();
    }

    void SyncCursorBody()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;

        Vector3 pos = transform.position;
        if ((rb.position - pos).sqrMagnitude > 0.000001f)
            rb.position = pos;
        if (rb.rotation != transform.rotation)
            rb.rotation = transform.rotation;
    }

    bool IsCharacterSelectOpen()
    {
        MenuManager mm = MenuManager.instance;
        if (mm == null)
            return false;

        return mm.IsMenuVisible("Character Select") || mm.IsMenuVisible("Characters");
    }

    bool WasPressed(string action)
    {
        if (cLink == null)
            return false;
        ControllerButtons b = cLink[action];
        return b != null && b.wasPressedThisFrame;
    }

    /// <summary>
    /// Character Select: Super (Interact) unready / jump to Back;
    /// Dash Left/Right cycle skins for the controlled slot.
    /// </summary>
    void HandleCharacterSelectShortcuts()
    {
        if (!IsCharacterSelectOpen() || db == null || db.players == null)
            return;

        Player self = FindPlayer(pI);
        if (self == null)
            return;

        // Super button = Interact in the input map
        if (WasPressed("Interact"))
        {
            if (self.characterSelected)
            {
                if (CharacterSelect.instance != null)
                    CharacterSelect.instance.PlayerUnconfirm(pI);
            }
            else
            {
                JumpSelectorToBack();
            }
        }

        int skinDir = 0;
        if (WasPressed("DashRight"))
            skinDir = 1;
        else if (WasPressed("DashLeft"))
            skinDir = -1;

        if (skinDir == 0)
            return;

        int skinPlayer = ClickPlayerIndex();
        Player target = FindPlayer(skinPlayer);
        if (target == null || target.wantRandomCharacter)
            return;

        // Only cycle your own (or a CPU you're driving)
        if (skinPlayer != pI && !(target.computer && ControllingCPU() && cpuControl == skinPlayer))
            return;

        PlayerSkin.Cycle(target, db, skinDir);

        PlayerColors pc = PlayerSkin.GetPlayerColors(target, db);
        if (pc != null && target.pso != null)
            target.pso.SetCircleColor(pc);
        if (skinPlayer == pI && pc != null)
            SetCircleColor(pc);
    }

    void JumpSelectorToBack()
    {
        if (rt == null)
            return;

        RectTransform backRt = FindActiveBackRect();
        if (backRt == null)
            return;

        rt.position = backRt.position;

        float hW = width / 2;
        float hH = height / 2;
        rt.anchoredPosition = new Vector2(
            Mathf.Clamp(rt.anchoredPosition.x, -hW, hW),
            Mathf.Clamp(rt.anchoredPosition.y, -hH, hH)
        );
        SyncCursorBody();
    }

    static RectTransform FindActiveBackRect()
    {
        if (cachedActiveBack != null && cachedActiveBack.gameObject.activeInHierarchy)
            return cachedActiveBack;

        BackButtonClick[] backs = UnityEngine.Object.FindObjectsByType<BackButtonClick>(FindObjectsInactive.Exclude);
        for (int i = 0; i < backs.Length; i++)
        {
            BackButtonClick b = backs[i];
            if (b == null || !b.isActiveAndEnabled || !b.gameObject.activeInHierarchy)
                continue;

            RectTransform brt = b.GetComponent<RectTransform>();
            if (brt != null)
            {
                cachedActiveBack = brt;
                return brt;
            }
        }

        // Fallback: name match if EnsureAll hasn't run yet
        GameObject[] all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null || !go.activeInHierarchy)
                continue;

            string n = go.name != null ? go.name.Trim() : "";
            if (!n.Equals("Back", System.StringComparison.OrdinalIgnoreCase)
                && !n.StartsWith("Back ", System.StringComparison.OrdinalIgnoreCase)
                && !n.EndsWith(" Back", System.StringComparison.OrdinalIgnoreCase))
                continue;

            RectTransform brt = go.GetComponent<RectTransform>();
            if (brt != null)
            {
                cachedActiveBack = brt;
                return brt;
            }
        }

        return null;
    }

    void HandleActions()
    {
        if (cLink == null || (cLink["Jump"] == null && !SharedCursor()))
            return;

        if (ReadPressedDown("Jump") &&
            clicking == 0 &&
            !cooldown)
        {
            clicking += 0.001f;
        }

        if (clicking > 0)
        {
            if (!ReadPressed("Jump"))
            {
                cooldown = true;
            }
            else
            {
                //Add pressed state
                foreach (Collider hit in ucd.trackedColliders)
                {
                    if (hit == null || !IsMenuSelectable(hit))
                        continue;

                    hit.SendMessage("Pressed", SendMessageOptions.DontRequireReceiver);
                }
            }

            float clickDt = Time.timeScale < 0.01f ? Time.unscaledDeltaTime : Time.deltaTime;
            clicking +=
                ((ReadPressed("Jump") && !cooldown) ? .25f : 20)
                * clickDt;

            if (clicking > 1)
            {
                int clickPlayer = ClickPlayerIndex();
                bool hittingCpuLevel = false;
                for (int i = 0; i < ucd.trackedColliders.Count; i++)
                {
                    Collider c = ucd.trackedColliders[i];
                    if (c != null && GetTargetInfo(c).isCpuDifficulty)
                    {
                        hittingCpuLevel = true;
                        break;
                    }
                }

                bool topOnly = HasHitOnTopMenu();
                foreach (Collider hit in ucd.trackedColliders)
                {
                    if (hit == null || !IsMenuSelectable(hit))
                        continue;
                    if (topOnly && !IsUnderTopMenu(hit))
                        continue;

                    // Prefer the CPU level chip over the portrait skin cycle when both overlap
                    if (hittingCpuLevel && GetTargetInfo(hit).isPortrait)
                        continue;

                    if (cooldown)
                    {
                        // Short Press
                        hit.SendMessage("OnClick", clickPlayer, SendMessageOptions.DontRequireReceiver);
                    }
                    else
                    {
                        // Long Press
                        hit.SendMessage("OnLongClick", clickPlayer, SendMessageOptions.DontRequireReceiver);
                    }
                }

                clicking = 0;
                cooldown = false;
            }
        }
    }

    void ControllingUI()
    {
        bool controlling = ControllingCPU();
        if (text != null && (controlling != lastControllingCpu || cpuControl != lastCpuControlShown))
        {
            text.enabled = controlling;
            if (controlling)
                text.text = "CPU " + (cpuControl + 1);
            lastControllingCpu = controlling;
            lastCpuControlShown = cpuControl;
        }

        if (circle != null && db != null && pC != null && db.playerColors != null && db.playerColors.Count > 0)
        {
            Sprite want = controlling ? db.playerColors[^1].sprite : pC.sprite;
            if (want != lastCircleSprite)
            {
                circle.sprite = want;
                lastCircleSprite = want;
            }
        }
    }

    public bool ControllingCPU()
    {
        return (cpuControl >= 0);
    }

    void ShowOverlay()
    {
        if (clickimg != null)
        {
            float fill = clicking;
            if (float.IsNaN(fill) || float.IsInfinity(fill))
                fill = 0f;
            fill = Mathf.Clamp01(fill);
            if (Mathf.Abs(fill - lastClickFill) > 0.0001f)
            {
                clickimg.fillAmount = fill;
                lastClickFill = fill;
            }
        }
    }

    /// <summary>
    /// Main selector image (not the click-hold child) drains while holding Leave / disconnect.
    /// </summary>
    void UpdateLeaveUnfill()
    {
        if (circle == null || cLink == null)
            return;

        float fill = Mathf.Clamp01(1f - cLink.LeaveHoldProgress);
        if (Mathf.Abs(fill - lastLeaveFill) > 0.0001f)
        {
            circle.fillAmount = fill;
            lastLeaveFill = fill;
        }
    }

    void CPUSelections()
    {
        if (db == null || db.players == null)
            return;
        // Character-select CPU handoff does not need per-frame scanning
        if (Time.unscaledTime < nextCpuSelectCheck)
            return;
        nextCpuSelectCheck = Time.unscaledTime + 0.15f;

        Player p = null;
        for (int i = 0; i < db.players.Count; i++)
        {
            if (db.players[i] != null && db.players[i].index == pI)
            {
                p = db.players[i];
                break;
            }
        }
        if (p == null || !p.characterSelected)
            return;

        if (cpuControl < 0)
        {
            for (int i = 0; i < db.players.Count; i++)
            {
                Player c = db.players[i];
                if (c != null && c.computer && !c.characterSelected)
                {
                    if (p.pso != null)
                        p.pso.cpuControl = c.index;
                    break;
                }
            }
        }
        else
        {
            Player f = null;
            for (int i = 0; i < db.players.Count; i++)
            {
                if (db.players[i] != null && db.players[i].index == cpuControl)
                {
                    f = db.players[i];
                    break;
                }
            }
            if (f == null || f.characterSelected)
                cpuControl = -1;
        }
    }

    void CollisionEntered(Collider other)
    {
        if (other != null)
        {
            CacheTargetInfo(other);
            other.SendMessage("OnHighlighted", pI, SendMessageOptions.DontRequireReceiver);
        }
    }

    void CollisionExited(Collider other)
    {
        if (other != null)
        {
            targetInfo.Remove(other);
            other.SendMessage("OnUnHighlighted", pI, SendMessageOptions.DontRequireReceiver);
        }
    }

    Player FindPlayer(int index)
    {
        if (db == null || db.players == null)
            return null;

        for (int i = 0; i < db.players.Count; i++)
        {
            Player player = db.players[i];
            if (player != null && player.index == index)
                return player;
        }
        return null;
    }

    SelectorTargetInfo GetTargetInfo(Collider target)
    {
        if (!targetInfo.TryGetValue(target, out SelectorTargetInfo info))
            info = CacheTargetInfo(target);
        return info;
    }

    SelectorTargetInfo CacheTargetInfo(Collider target)
    {
        SelectorTargetInfo info = new SelectorTargetInfo
        {
            isCpuDifficulty = target.GetComponent<CpuDifficultyButton>() != null,
            isPortrait = target.GetComponent<PortraitClicked>() != null
        };
        targetInfo[target] = info;
        return info;
    }
}