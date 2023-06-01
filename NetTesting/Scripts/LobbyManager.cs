using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager instance;
    NetSetup nS;
    DataBank db;

    [SerializeField]
    public List<LobbyPlayer> players = new List<LobbyPlayer>();

    public GameObject lobbyPlayerDisplay;
    public Transform playerHolder;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
        }
        //players.Add(new LobbyPlayer("", "", 0, 0));
    }

    // Start is called before the first frame update
    void Start()
    {
        nS = NetSetup.instance;
        db = DataBank.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if(players.Count > 0)
        {
            for(int i = 0; i < players.Count; i++)
            {
                LobbyPlayer lp = players[i];

                if (lp.clientId == NetworkManager.Singleton.LocalClientId)
                {
                    lp.uID = nS.yourself.uid;
                    lp.username = nS.yourself.username;
                }
            }
        }
    }

    public void AddPlayer(string uID, string username, ulong clientId)
    {
        bool pass = true;

        if (players.Count > 0)
        {
            if (players.Exists(x => x.clientId == clientId || x.uID == uID))
            {
                pass = false;
            }
        }

        if(pass)
        {
            int pos = -1;

            for (int i = 0; i < 8; i++)
            {
                if (!players.Exists(x => x.position == i))
                {
                    pos = i;
                    break;
                }
            }

            players.Add(new LobbyPlayer(uID, username, clientId, pos));

            GameObject go = Instantiate(lobbyPlayerDisplay, playerHolder);

            LobbyPlayerDisplay lpd = go.GetComponent<LobbyPlayerDisplay>();

            if(lpd != null)
            {
                lpd.selection = players.Count - 1;
            }
        }
    }

    public void UpdatePlayerList(List<LobbyPlayer> lplays)
    {
        players = lplays;

        if (players.Count > 0 && players.Count > playerHolder.childCount)
        {
            for(int i = playerHolder.childCount; i < players.Count; i++)
            {
                GameObject go = Instantiate(lobbyPlayerDisplay, playerHolder);

                LobbyPlayerDisplay lpd = go.GetComponent<LobbyPlayerDisplay>();

                if (lpd != null)
                {
                    lpd.selection = i;
                }
            }
        }
    }

    public void Reset()
    {
        players.Clear();
    }
}

[System.Serializable]
public class LobbyPlayer
{
    public string uID;
    public string username;
    public ulong clientId;
    public int position = -1;

    public LobbyPlayer()
    {

    }

    public LobbyPlayer(string uID, string username, ulong clientId, int position)
    {
        this.uID = uID;
        this.username = username;
        this.clientId = clientId;
        this.position = position;
    }

    public override string ToString()
    {
        string result = "uid:" + uID;
        result += "\n username:" + username;
        result += "\n clientId:" + clientId.ToString();
        result += "\n position:" + position.ToString();

        return JsonUtility.ToJson(this, true);
    }

    public string ToJson()
    {
        string result = "{\"uid\":\"" + uID + "\",";
        result += "\"username\":\"" + username + "\",";
        result += "\"clientId\":\"" + clientId.ToString() + "\",";
        result += "\"position\":\"" + position.ToString() + "\"}";

        return result;
    }
}
