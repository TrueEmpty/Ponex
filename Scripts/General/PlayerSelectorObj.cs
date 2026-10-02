using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSelectorObj : MonoBehaviour
{
    Database db;

    public ControllerLink cLink = null;
    public int pI = 0;

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
            MoveCircle();
            HandleActions();
            ShowOverlay();
            CPUControllingUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCircleColor(PlayerColors c)
    {
        if (circle != null)
        {
            circle.sprite = c.sprite;
        }
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

                    if (cooldown)
                    {
                        // Short Press
                        hit.SendMessage("OnClick", (ControllingCPU() ? cpuControl : pI), SendMessageOptions.DontRequireReceiver);
                    }
                    else
                    {
                        // Long Press
                        hit.SendMessage("OnLongClick", (ControllingCPU() ? cpuControl : pI), SendMessageOptions.DontRequireReceiver);
                    }
                }

                clicking = 0;
                cooldown = false;
            }
        }
    }

    void CPUControllingUI()
    {
        text.enabled = ControllingCPU();
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