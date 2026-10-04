using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PortraitClicked : MonoBehaviour
{
    Database db;
    CharacterSelect cS;
    public int attachedIndex = 0;

    public RawImage image;
    public Text playerText;
    public GameObject ready;
    public Outline outline;

    // Use this for initialization
    void Start()
    {
        db = Database.instance;
        cS = CharacterSelect.instance;

        image = GetComponent<RawImage>();
        playerText = transform.GetChild(0).GetChild(0).GetComponent<Text>();
        ready = transform.GetChild(0).GetChild(1).gameObject;
        outline = GetComponent<Outline>();
    }

    private void Update()
    {
        Player p = db.players.Find(x => x.index == attachedIndex);

        if(p == null)
        {
            Destroy(gameObject);
        }
        else
        {
            LoadPortrait(p);
        }
    }

    void LoadPortrait(Player p)
    {
        Color c = db.playerColors[p.index].color;

        //Check if a character is attached to the player if not restore last pick / random
        int cC = db.characters.FindIndex(x => x.name == p.name);
        if (cC < 0 || cC >= db.characters.Count)
        {
            db.ApplyRememberedOrRandomCharacter(p);
            cC = db.characters.FindIndex(x => x.name == p.name);
        }

        image.texture = p.portrait;
        if (p.computer)
        {
            playerText.text = "CPU" + (p.index + 1);
            playerText.color = Color.gray;
        }
        else
        {
            playerText.text = (p.nickName == "" || p.nickName == null) ? "P" + (p.index + 1) : p.nickName;
            playerText.color = c;
        }

        ready.SetActive(p.characterSelected);
    }

    public void OnClick(int player)
    {
        if(player >= 0 && player < db.players.Count)
        {
            Player p = db.players.Find(x=> x.index == attachedIndex);

            if (p != null)
            {
                if (attachedIndex == player || p.computer)
                {
                    //Change Skin
                    Debug.Log("Changing Skin");
                }
            }
        }
    }

    public void OnLongClick(int player)
    {
        if (player >= 0 && player < db.players.Count)
        {
            Player p = db.players.Find(x => x.index == attachedIndex);

            if(p != null)
            {
                //Change player type
                if (p.computer)
                {
                    db.players.RemoveAll(x => x.index == attachedIndex);
                }
                else if (!p.computer)
                {
                    db.controllers.RemoveAll(x => x.index == attachedIndex);
                    p.computer = true;
                }
            }
        }
    }
}
