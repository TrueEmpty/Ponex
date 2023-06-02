using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ChatManager : MonoBehaviour
{
    NetworkConnector nC;
    NetSetup nS;
    public static ChatManager instance;

    public GameObject holder;
    public InputField input;
    public Text chatShow;

    [SerializeField]
    bool dirty = false;

    [SerializeField]
    bool updating = false;

    public string roomID = "0000000000"; // Server = 0000000000, Friends = roomID, ALL = 0000000001
    string serverID = "0000000000";
    string allID = "0000000001";
    public List<string> selectedFriend = new List<string>();
    public bool creatingChat = false;

    public Text server;
    public Text friends;
    public Text all;

    [SerializeField]
    public List<Chat> chats = new List<Chat>();

    [SerializeField]
    float loadChats = -1;
    float loadChatsTimer = 30;

    float loadlocalChats = -1;
    float loadloaclChatsTimer = 2;

    bool min = false;
    public GameObject chatArea;

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
    }

    // Start is called before the first frame update
    void Start()
    {
        nC = NetworkConnector.instance;
        nS = NetSetup.instance;
    }

    // Update is called once per frame
    void Update()
    {
        holder.SetActive(nS.IsLoggedIn);
        chatArea.SetActive(!min);

        if (dirty)
        {
            string chatText = "";
            Chat c = chats.Find(x => x.cid == roomID);

            if (c != null)
            {
                chatText = c.MessageString();
            }

            chatShow.text = chatText;
            dirty = false;
        }

        if(loadChats >= 0)
        {
            loadChats += Time.deltaTime;
        }

        if(loadChats >= loadChatsTimer)
        {
            loadChats = -1;
            nC.SendRPC(EndpointHelpers.GET_PLAYER_ROOMS_URL(nS.yourself.uid), null, GetChats, RPC.RequestType.GET);
        }

        if (loadlocalChats >= 0)
        {
            loadlocalChats += Time.deltaTime;
        }

        if (loadlocalChats >= loadloaclChatsTimer && roomID != "" && roomID != null)
        {
            loadlocalChats = -1;
            nC.SendRPC(EndpointHelpers.GET_PLAYER_ROOM_URL(roomID, nS.yourself.uid), null, GetChat, RPC.RequestType.GET);
        }
    }

    public void ChangeRoom(string room)
    {
        roomID = room;
        dirty = true;

        server.color = (roomID == serverID) ? Color.green : Color.red;
        all.color = (roomID == allID) ? Color.green : Color.red;
        friends.color = (roomID != serverID && roomID != allID) ? Color.green : Color.red;

        if(roomID != null && roomID != "")
        {
            loadChats = -1;
            loadlocalChats = -1;
            nC.SendRPC(EndpointHelpers.GET_PLAYER_ROOM_URL(roomID, nS.yourself.uid), null, GetChat, RPC.RequestType.GET);
        }
    }

    public void AllowedValues(InputField iF)
    {
        string letters = "abcdefghijklmnopqrstuvwxyz";
        string numbers = "1234567890";
        string special = " !?<>.|@#$%^&*()-_=+/~`";
        string allowedValues = letters + letters.ToUpper() + numbers + special;

        if(iF.text != "" && iF.text != null)
        {
            string outText = "";
            char[] iFT = iF.text.ToCharArray();

            for(int i = 0; i < iFT.Length; i++)
            {
                if(allowedValues.Contains(iFT[i]))
                {
                    outText += iFT[i];
                }
            }

            iF.text = outText;
        }
    }

    public void SendChat()
    {
        if (input.text != "" && input.text != null)
        {
            if(roomID != null && roomID != "")
            {
                if (roomID == serverID)//Will add admin privlage later
                {
                    input.text = "";
                }
                else
                {
                    nC.SendRPC(EndpointHelpers.ADD_ROOM_MESSAGES_URL(roomID, nS.yourself.uid, nS.yourself.username, input.text), null, GetChat, RPC.RequestType.POST);
                    input.text = "";
                }
            }
            else if(selectedFriend.Count > 0 && !creatingChat)
            {
                //Create Chat
                nC.SendRPC(EndpointHelpers.CREATE_ROOM_URL(nS.yourself.uid, nS.yourself.username, input.text), null, CreateChat, RPC.RequestType.POST);
                input.text = "";
                creatingChat = true;
            }
        }
    }

    public void CreateChat(CallbackVar cbV)
    {
        if (!cbV.error)
        {
            Chat fChat = JsonUtility.FromJson<Chat>(cbV.data);

            if (fChat != null)
            {
                Chat cChat = chats.Find(x => x.cid == fChat.cid);

                if (cChat != null)
                {
                    cChat = fChat;
                }
                else
                {
                    chats.Add(fChat);
                }

                //Send Add Players
                if(selectedFriend.Count > 0)
                {
                    for(int i = 0; i < selectedFriend.Count; i++)
                    {
                        nC.SendRPC(EndpointHelpers.ADD_PLAYER_TO_ROOM_URL(fChat.cid,selectedFriend[i]), null, GetChat, RPC.RequestType.POST);
                    }
                }
            }
        }
        else
        {
        }

        dirty = true;
        creatingChat = false;
    }

    public void MinimizeChat()
    {
        min = !min;
    }

    public void GetChat(CallbackVar cbV)
    {
        if (!cbV.error)
        {
            Chat fChat = JsonUtility.FromJson<Chat>(cbV.data);

            if (fChat != null)
            {
                int cChat = chats.FindIndex(x => x.cid == fChat.cid);

                if (cChat >= 0)
                {
                    chats[cChat] = new Chat(fChat);
                }
                else
                {
                    chats.Add(fChat);
                }
            }
            else
            {
                Debug.Log("Json Didnt convert");
            }
        }
        else
        {
        }

        dirty = true;
        loadChats = 0;
        loadlocalChats = 0;
    }

    public void GetChats(CallbackVar cbV)
    {
        if (!cbV.error)
        {
            string chatListUnpacked = cbV.data.Trim()[1..^1];
            string[] chatListSplit = chatListUnpacked.Split("},{", System.StringSplitOptions.RemoveEmptyEntries);

            if (chatListSplit.Length > 0)
            {
                for (int i = 0; i < chatListSplit.Length; i++)
                {
                    string newData = chatListSplit[i];

                    if (newData.Substring(0, 1) != "{")
                    {
                        newData = "{" + newData;
                    }

                    if (newData.Substring(newData.Length - 1, 1) != "}")
                    {
                        newData = newData + "}";
                    }

                    Chat fChat = JsonUtility.FromJson<Chat>(newData);

                    if (fChat != null)
                    {
                        int cChat = chats.FindIndex(x => x.cid == fChat.cid);

                        if (cChat >= 0)
                        {
                            chats[cChat] = new Chat(fChat);
                        }
                        else
                        {
                            chats.Add(fChat);
                        }
                    }
                    else
                    {
                        Debug.Log("Json Didnt convert");
                    }
                }
            }
        }
        else
        {
        }

        dirty = true;
        loadChats = 0;
    }
}

[System.Serializable]
public class Chat
{
    public string cid = "";
    public string owner = "";
    public List<string> players = new List<string>();   //uid,LastTimeTheyGrabbedAMessageFromThisRoom
    public string lastMessage = "";                     //Timestamp
    public List<string> message = new List<string>();  //SenderUsername,uid,Timestamp,Message

    public Chat(Chat c)
    {
        cid = c.cid;
        owner = c.owner;
        players = c.players;
        lastMessage = c.lastMessage;
        message = c.message;
    }

    public string MessageString(bool showTimeStamp = true)
    {
        string result = "";

        if(message.Count > 0)
        {
            for(int i = 0; i < message.Count; i++)
            {
                string[] mSplit = message[i].Split(',');

                if(i > 0)
                {
                    result += "\n";
                }

                System.DateTime tst = System.DateTime.Parse(mSplit[2]);

                string uname = mSplit[0];
                string uid = mSplit[1];
                string tstamp = (showTimeStamp) ? tst.ToString("ddd, MM/dd/yy hh:mm tt") + " " : "";
                string mes = mSplit[3];

                int c = (owner == uid) ? Color.green.GetHashCode() : Color.black.GetHashCode();

                result += tstamp + "<color=" + c.ToString() + ">" + uname + ":</color> " + mes;
            }
        }

        return result;
    }

    public override string ToString()
    {
        /*string result = "";

        result += sender;
        result += "|";

        result += recivers;
        result += "|";

        result += time.ToString();
        result += "|";

        result += message;
        result += "|";

        result += chatCode;*/

        return JsonUtility.ToJson(this, true);
    }
}
