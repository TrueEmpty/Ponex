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
    CpuDifficultyButton cpuLevelButton;

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

        // Remove leftover "Change Skin" label from older sessions / prefab children
        Transform holder = transform.childCount > 0 ? transform.GetChild(0) : null;
        if (holder != null)
        {
            Transform changeSkin = holder.Find("Change Skin");
            if (changeSkin != null)
                Destroy(changeSkin.gameObject);
        }
        infoText = null;

        EnsureCpuLevelButton();
    }

    void EnsureCpuLevelButton()
    {
        if (cpuLevelButton != null)
            return;

        Transform holder = transform.childCount > 0 ? transform.GetChild(0) : null;
        Font font = playerText != null ? playerText.font : null;
        cpuLevelButton = CpuDifficultyButton.Ensure(holder, attachedIndex, font);
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

        if (p.wantRandomCharacter || db.RemembersRandomCharacter(p.index))
        {
            if (!p.wantRandomCharacter)
                p.SetRandomCharacterPending();

            if (image != null)
            {
                image.texture = null;
                image.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            }

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

            RefreshCpuLevelButton(p);

            // Random pick still shows a big "?" on the portrait
            EnsureRandomMark();
            if (infoText != null)
            {
                infoText.gameObject.SetActive(true);
                infoText.text = "?";
                infoText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
                infoText.fontSize = 48;
                RectTransform rt = infoText.rectTransform;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(120f, 120f);
            }

            if (ready != null)
            {
                ready.SetActive(p.characterSelected);
                if (readyText != null && p.characterSelected)
                    readyText.color = skin;
            }
            return;
        }

        int cC = db.characters.FindIndex(x => x.name == p.name);
        if (cC < 0 || cC >= db.characters.Count)
        {
            db.ApplyRememberedOrRandomCharacter(p);
            if (p.wantRandomCharacter)
            {
                LoadPortrait(p);
                return;
            }
            cC = db.characters.FindIndex(x => x.name == p.name);
        }

        if (image != null)
        {
            image.texture = p.portrait;
            image.color = Color.white;
        }

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

        RefreshCpuLevelButton(p);

        if (infoText != null)
            infoText.gameObject.SetActive(false);

        if (ready != null)
        {
            ready.SetActive(p.characterSelected);
            if (readyText != null && p.characterSelected)
                readyText.color = skin;
        }
    }

    void EnsureRandomMark()
    {
        if (infoText != null)
            return;

        Transform holder = transform.GetChild(0);
        GameObject go = new GameObject("Random Mark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(holder, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(120f, 120f);

        infoText = go.GetComponent<Text>();
        infoText.font = playerText != null ? playerText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (infoText.font == null)
            infoText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        infoText.alignment = TextAnchor.MiddleCenter;
        infoText.horizontalOverflow = HorizontalWrapMode.Overflow;
        infoText.verticalOverflow = VerticalWrapMode.Overflow;
        infoText.raycastTarget = false;
    }

    void RefreshCpuLevelButton(Player p)
    {
        EnsureCpuLevelButton();
        if (cpuLevelButton == null)
            return;

        cpuLevelButton.attachedIndex = attachedIndex;

        // Keep CPU name near center; level text sits further right on its own
        if (playerText != null)
        {
            RectTransform nameRt = playerText.rectTransform;
            nameRt.anchoredPosition = new Vector2(-3.001831f, 101.3f);
        }

        cpuLevelButton.Refresh();
    }

    public void OnClick(int player)
    {
        if (player < 0 || player >= db.players.Count)
            return;

        Player p = db.players.Find(x => x.index == attachedIndex);
        if (p == null || p.wantRandomCharacter)
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
        if (db == null || db.players == null)
            return;

        Player p = db.players.Find(x => x.index == attachedIndex);
        if (p == null)
            return;

        if (p.computer)
        {
            if (!db.IsPastCharacterSelect() && db.players.Count > db.minPlayers)
                db.players.RemoveAll(x => x.index == attachedIndex);
            return;
        }

        // Only the owning human (or whoever is driving this slot) can disconnect it
        if (player != attachedIndex)
            return;

        db.DisconnectPlayer(attachedIndex);
    }
}
