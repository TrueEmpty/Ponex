using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectionHighlight : MonoBehaviour
{
    Database db;
    ButtonManager bm;
    public GameObject selectedUI;

    public List<string> names = new List<string>();
    public List<string> tags = new List<string>();

    public int player = 1;

    float clicked = 0;

    void Start()
    {
        db = Database.instance;
        bm = ButtonManager.instance;
    }

    void Update()
    {
        SelectorMove();
    }

    void SelectorMove()
    {
        PlayerButtons pB = null;

        switch(player)
        {
            case 1:
                pB = db.settings.p1Buttons;
                break;
            case 2:
                pB = db.settings.p2Buttons;
                break;
            case 3:
                pB = db.settings.p3Buttons;
                break;
            case 4:
                pB = db.settings.p4Buttons;
                break;
        }

        transform.position += GetPlayerInput(pB, transform.position) * Time.deltaTime;

        //Check if it is over anything
        #region P1
        if (bm.KeyPressed(pB.left) && bm.KeyPressed(pB.right))
        {
            clicked += Time.deltaTime;
        }

        if (!bm.KeyPressed(pB.left) && !bm.KeyPressed(pB.right) &&
            !bm.KeyPressed(pB.bump) && !bm.KeyPressed(pB.super))
        {
            clicked = 0;
        }

        if (clicked > 0 && clicked < .25f)
        {
            clicked = 100;

            if (selectedUI != null)
            {
                Button butt = selectedUI.GetComponent<Button>();

                if(butt != null)
                {
                    if(butt.enabled)
                    {
                        butt.onClick.Invoke();
                    }
                }
                else
                {
                    selectedUI.SendMessage("OnClick", player, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
        #endregion
    }

    Vector3 GetPlayerInput(PlayerButtons pB, Vector3 curPos)
    {
        Vector3 result = Vector3.zero;

        if (bm.KeyPressed(pB.bump) && curPos.y < db.borderTop.position.y)
        {
            result.y += 1;
        }

        if (bm.KeyPressed(pB.super) && curPos.y > db.borderBot.position.y)
        {
            result.y -= 1;
        }

        if (bm.KeyPressed(pB.right) && curPos.x < db.borderRight.position.x)
        {
            result.x += 1;
        }

        if (bm.KeyPressed(pB.left) && curPos.x > db.borderLeft.position.x)
        {
            result.x -= 1;
        }

        return result;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(names.Exists(x=> x.ToLower().Trim() == other.gameObject.name.ToLower().Trim()) || names.Count <= 0)
        {
            if (tags.Exists(x => x.ToLower().Trim() == other.gameObject.tag.ToLower().Trim()) || tags.Count <= 0)
            {
                if(selectedUI != other.gameObject)
                {
                    selectedUI = other.gameObject;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (names.Exists(x => x.ToLower().Trim() == other.gameObject.name.ToLower().Trim()) || names.Count <= 0)
        {
            if (tags.Exists(x => x.ToLower().Trim() == other.gameObject.tag.ToLower().Trim()) || tags.Count <= 0)
            {
                if (selectedUI == other.gameObject)
                {
                    selectedUI = null;
                }
            }
        }
    }
}
