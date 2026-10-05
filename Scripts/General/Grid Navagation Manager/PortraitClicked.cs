using UnityEngine;
using UnityEngine.UI;

public class PortraitClicked : MonoBehaviour
{
    Database db;
    CharacterSelect cS;
    public int attachedIndex = 0;

    public RawImage image;
    public Text playerText;
    public Text infoText;
    public GameObject ready;
    public Outline outline;

    Text readyText;

    void Start()
    {
        db = Database.instance;
        cS = CharacterSelect.instance;

        image = GetComponent<RawImage>();
        playerText = transform.GetChild(0).GetChild(0).GetComponent<Text>();
        ready = transform.GetChild(0).GetChild(1).gameObject;
        outline = GetComponent<Outline>();

        if (ready != null)
            readyText = ready.GetComponent<Text>();

        EnsureInfoText();
    }

    void EnsureInfoText()
    {
        if (infoText != null)
            return;

        Transform holder = transform.GetChild(0);
        Transform existing = holder.Find("Change Skin");
        if (existing != null)
        {
            infoText = existing.GetComponent<Text>();
            return;
        }

        GameObject go = new GameObject("Change Skin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(holder, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(3f, 42f);
        rt.sizeDelta = new Vector2(179f, 28f);

        infoText = go.GetComponent<Text>();
        infoText.font = playerText != null ? playerText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (infoText.font == null)
            infoText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        infoText.fontSize = 14;
        infoText.alignment = TextAnchor.MiddleCenter;
        infoText.horizontalOverflow = HorizontalWrapMode.Overflow;
        infoText.verticalOverflow = VerticalWrapMode.Overflow;
        infoText.raycastTarget = false;
        infoText.text = "Change Skin";
    }

    void Update()
    {
        Player p = db.players.Find(x => x.index == attachedIndex);

        if (p == null)
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
        Color skin = PlayerSkin.GetColor(p, db);

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
            playerText.color = skin;
        }

        if (outline != null)
            outline.effectColor = skin;

        if (infoText != null)
        {
            infoText.gameObject.SetActive(true);
            infoText.text = "Change Skin";
            infoText.color = skin;
        }

        if (ready != null)
        {
            ready.SetActive(p.characterSelected);
            if (readyText != null && p.characterSelected)
                readyText.color = skin;
        }
    }

    public void OnClick(int player)
    {
        if (player < 0 || player >= db.players.Count)
            return;

        Player p = db.players.Find(x => x.index == attachedIndex);
        if (p == null)
            return;

        // Owning player (or anyone clicking a CPU portrait) cycles skin
        if (attachedIndex == player || p.computer)
        {
            PlayerSkin.Cycle(p, db);
            RefreshSelectorColor(p);
            LoadPortrait(p);
        }
    }

    void RefreshSelectorColor(Player p)
    {
        if (p == null || p.pso == null)
            return;

        PlayerColors pc = PlayerSkin.GetPlayerColors(p, db);
        if (pc != null)
            p.pso.SetCircleColor(pc);
    }

    public void OnLongClick(int player)
    {
        if (player < 0 || player >= db.players.Count)
            return;

        Player p = db.players.Find(x => x.index == attachedIndex);
        if (p == null)
            return;

        if (p.computer)
        {
            db.players.RemoveAll(x => x.index == attachedIndex);
        }
        else if (!p.computer)
        {
            db.controllers.RemoveAll(x => x.index == attachedIndex);
            p.computer = true;
            p.cpuDifficulty = ComputerAI.CpuDifficulty.Training;
        }
    }
}
