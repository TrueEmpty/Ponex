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

    void HandleActions()
    {
        string clickAction = "Jump";

        if (cLink[clickAction].wasPressedThisFrame &&
            clicking == 0 &&
            !cooldown)
        {
            clicking += 0.001f;
        }

        if (clicking > 0)
        {
            if (!cLink[clickAction].isPressed)
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
                ((cLink[clickAction].isPressed && !cooldown) ? .25f : 20)
                * Time.deltaTime;

            if (clicking > 1)
            {
                foreach (Collider hit in ucd.trackedColliders)
                {
                    if (hit == null)
                        continue;

                    Debug.Log(hit.name);

                    int clickPlayer = ClickPlayerIndex();
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