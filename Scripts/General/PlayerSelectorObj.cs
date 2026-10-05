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
    Image circle;
    Image clickimg;

    float width = 100;
    float height = 100;

    public float speed = 500;

    float clicking = 0;
    bool cooldown = false;

   List<GameObject> overlappingTargets = new List<GameObject>();

    UniversalCollisionDetector ucd;

    Text text;

    public int cpuControl = -1;

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
    }

    void Update()
    {
        if (cLink != null)
        {
            // Win screen: while manually scrolling a stats card, Move scrolls text — freeze cursor/clicks
            if (IsWinScrolling())
            {
                ShowOverlay();
                ControllingUI();
                return;
            }

            MoveCircle();
            HandleActions();
            HandleCharacterSelectShortcuts();
            ShowOverlay();
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

    int ClickPlayerIndex()
    {
        // On win screen always use the human cursor owner (so CPU cards can be opened for scroll)
        if (db != null && db.winnerScreen)
            return pI;
        return ControllingCPU() ? cpuControl : pI;
    }

    public void SetCircleColor(PlayerColors c)
    {
        pC = c;
    }

    void MoveCircle()
    {
        if (rt == null)
            return;

        Vector2 move = cLink["Move"].value;

        int multiplier = cLink["Sprint"].isPressed ? 2 : 1;

        rt.anchoredPosition += new Vector2(
            move.x * speed * multiplier * Time.deltaTime,
            move.y * speed * multiplier * Time.deltaTime
        );

        float hW = width / 2;
        float hH = height / 2;

        rt.anchoredPosition = new Vector2(
            Mathf.Clamp(rt.anchoredPosition.x, -hW, hW),
            Mathf.Clamp(rt.anchoredPosition.y, -hH, hH)
        );
    }

    bool IsCharacterSelectOpen()
    {
        MenuManager mm = MenuManager.instance;
        if (mm == null)
            return false;

        MenuClass open = mm.GetOpenMenu(true);
        if (open == null || string.IsNullOrEmpty(open.title))
            return false;

        string title = open.title.Trim().ToLowerInvariant();
        return title == "character select" || title == "characters";
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

        Player self = db.players.Find(x => x.index == pI);
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
        Player target = db.players.Find(x => x.index == skinPlayer);
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
    }

    static RectTransform FindActiveBackRect()
    {
        BackButtonClick[] backs = UnityEngine.Object.FindObjectsByType<BackButtonClick>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < backs.Length; i++)
        {
            BackButtonClick b = backs[i];
            if (b == null || !b.isActiveAndEnabled || !b.gameObject.activeInHierarchy)
                continue;

            RectTransform brt = b.GetComponent<RectTransform>();
            if (brt != null)
                return brt;
        }

        // Fallback: name match if EnsureAll hasn't run yet
        GameObject[] all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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
                return brt;
        }

        return null;
    }

    void HandleActions()
    {
        string clickAction = "Jump";
        ControllerButtons clickBtn = cLink != null ? cLink[clickAction] : null;
        if (clickBtn == null)
            return;

        if (clickBtn.wasPressedThisFrame &&
            clicking == 0 &&
            !cooldown)
        {
            clicking += 0.001f;
        }

        if (clicking > 0)
        {
            if (!clickBtn.isPressed)
            {
                cooldown = true;
            }
            else
            {
                //Add pressed state
                foreach (Collider hit in ucd.trackedColliders)
                {
                    if (hit == null)
                        continue;

                    hit.SendMessage("Pressed", SendMessageOptions.DontRequireReceiver);
                }
            }

            clicking +=
                ((clickBtn.isPressed && !cooldown) ? .25f : 20)
                * Time.deltaTime;

            if (clicking > 1)
            {
                int clickPlayer = ClickPlayerIndex();
                bool hittingCpuLevel = false;
                for (int i = 0; i < ucd.trackedColliders.Count; i++)
                {
                    Collider c = ucd.trackedColliders[i];
                    if (c != null && c.GetComponent<CpuDifficultyButton>() != null)
                    {
                        hittingCpuLevel = true;
                        break;
                    }
                }

                foreach (Collider hit in ucd.trackedColliders)
                {
                    if (hit == null)
                        continue;

                    // Prefer the CPU level chip over the portrait skin cycle when both overlap
                    if (hittingCpuLevel && hit.GetComponent<PortraitClicked>() != null)
                        continue;

                    Debug.Log(hit.name);

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
        text.enabled = ControllingCPU();
        text.text = "CPU " + (cpuControl + 1);

        if (circle != null)
        {
            circle.sprite = (ControllingCPU()) ? db.playerColors[^1].sprite : pC.sprite;
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
            clickimg.fillAmount = clicking;
        }
    }

    void CPUSelections()
    {
        Player p = db.players.Find(x => x.index == pI);

        if (p != null)
        {
            if(p.characterSelected)
            {
                if (cpuControl < 0)
                {
                    //Check for new not selected cpu

                    //if there is a computer that still needs to be locked in then set the player to that computer to select for them
                    Player c = db.players.Find(x => !x.characterSelected && x.computer);

                    if (c != null)
                    {
                        p.pso.cpuControl = c.index;
                    }
                }
                else
                {
                    Player f = db.players.Find(x => x.index == cpuControl);

                    if (f != null)
                    {
                        if (f.characterSelected)
                        {
                            cpuControl = -1;
                        }
                    }
                    else
                    {
                        cpuControl = -1;
                    }
                }
            }
        }
    }

    void CollisionEntered(Collider other)
    {
        Debug.Log(other.gameObject.name + " Entered Collision");
        other.SendMessage("OnHighlighted", pI, SendMessageOptions.DontRequireReceiver);
    }

    void CollisionExited(Collider other)
    {
        Debug.Log(other.gameObject.name + " Left Collision");
        other.SendMessage("OnUnHighlighted", pI, SendMessageOptions.DontRequireReceiver);
    }
}