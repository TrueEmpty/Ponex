using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    public static LoginManager instance;
    NetSetup nS;
    NetworkConnector nC;
    ChatManager cM;

    public GameObject create;
    public InputField cUsername;
    public InputField cPassword;

    public GameObject login;
    public InputField lUsername;
    public InputField lPassword;

    public GameObject signIn;

    public GameObject profile;
    public Text pUsername;
    public Text pFriendCount;

    public string screen = "";

    [SerializeField]
    float friendUpdate = -1;
    float friendUpdateTime = 60;
    bool friendDisplay = false;
    public InputField friendRequestIF;
    public GameObject friends;
    public Transform friendsContentHolder;
    public GameObject friendsFoldoutGo;

    bool staySignedIn = false;
    public Toggle staySignedInTog;
    bool running = false;

    public List<string> groups = new List<string>();

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
        nS = NetSetup.instance;
        nC = NetworkConnector.instance;
        cM = ChatManager.instance;

        string ssIn = PlayerPrefs.GetString("Sign_In");

        if(ssIn != null && ssIn != "")
        {
            staySignedIn = bool.Parse(ssIn);
        }

        if(staySignedIn)
        {
            string uName = PlayerPrefs.GetString("Username");
            string pWord = PlayerPrefs.GetString("Password");

            if((uName != null && uName != "") && (pWord != null && pWord != ""))
            {
                lUsername.text = uName;
                lPassword.text = pWord;
                running = true;

                WWWForm form = new WWWForm();
                form.AddField("username", uName);
                form.AddField("password", pWord);

                nC.SendRPC(EndpointHelpers.LOGIN_URL, form, LoggedIn, RPC.RequestType.POST);
            }
        }

        staySignedInTog.isOn = staySignedIn;
    }

    // Update is called once per frame
    void Update()
    {
        signIn.SetActive(!nS.IsLoggedIn);
        profile.SetActive(nS.IsLoggedIn);
        create.SetActive(screen.ToLower().Trim() == "create new");
        login.SetActive(screen.ToLower().Trim() == "login");
        friends.SetActive(nS.IsLoggedIn && friendDisplay);

        cUsername.enabled = !running;
        cPassword.enabled = !running;

        lUsername.enabled = !running;
        lPassword.enabled = !running;

        if(nS.IsLoggedIn)
        {
            pUsername.text = nS.yourself.username;
            int onlineFriends = CountOfOnlineFriends();
            pFriendCount.text = "Friends (" + onlineFriends + "/" + nS.yourself.friends.Count + ")";

            if(friendUpdate >= 0)
            {
                friendUpdate += Time.deltaTime;
            }

            if(friendUpdate > friendUpdateTime)
            {
                nC.SendRPC(EndpointHelpers.GET_FRIENDS_URL(nS.yourself.uid),null, GetFriends, RPC.RequestType.GET);
                friendUpdate = -1;
            }

            if(nS.yourself.friends.Count > 0)
            {
                #region Add All Groups
                for (int i = 0; i < nS.yourself.friends.Count; i++)
                {
                    string fRow = nS.yourself.friends[i];
                    string[] fSplit = fRow.Split(',');

                    if(fSplit.Length > 1)
                    {
                        for (int r = 1; r < fSplit.Length; r++)
                        {
                            if(!groups.Exists(x=> x.ToLower().Trim() == fSplit[r].ToLower().Trim()))
                            {
                                groups.Add(fSplit[r]);
                            }
                        }
                    }
                }

                if (!groups.Contains("Online"))
                {
                    groups.Add("Online");
                }

                if (!groups.Contains("Offline"))
                {
                    groups.Add("Offline");
                }
                #endregion

                //Add new groups if needed
                int startNum = friendsContentHolder.childCount;
                int gDiff = groups.Count - startNum;

                if(gDiff > 0)
                {
                    for(int i = 0; i < gDiff; i++)
                    {
                        GameObject go = Instantiate(friendsFoldoutGo, friendsContentHolder);

                        FriendGroupFoldout fgf = go.GetComponent<FriendGroupFoldout>();

                        if(fgf != null)
                        {
                            fgf.groupIndex = startNum + i;
                        }
                    }
                }
            }
        }
    }

    #region Normal
    public void Login()
    {
        if ((lUsername.text != null && lUsername.text != "") && (lPassword.text != null && lPassword.text != ""))
        {
            running = true;

            WWWForm form = new WWWForm();
            form.AddField("username", lUsername.text);
            form.AddField("password", lPassword.text);

            nC.SendRPC(EndpointHelpers.LOGIN_URL, form, LoggedIn, RPC.RequestType.POST);
        }
    }

    public void LogOut()
    {
        nS.yourself = new Client();
        nS.friends.Clear();
        groups.Clear();
        friendUpdate = -1;
        screen = "";
    }

    public void UpdateStaySignedIn()
    {
        staySignedIn = staySignedInTog.isOn;
        PlayerPrefs.SetString("Sign_In", staySignedIn.ToString());
    }

    public void CreateNew()
    {
        if((cUsername.text != null && cUsername.text != "") && (cPassword.text != null && cPassword.text != ""))
        {
            running = true;

            WWWForm form = new WWWForm();
            form.AddField("username", cUsername.text);
            form.AddField("password", cPassword.text);

            nC.SendRPC(EndpointHelpers.CREATE_ACCOUNT_URL, form, LoggedIn, RPC.RequestType.POST);
        }
        else
        {
            //Tell them info is missing
        }
    }

    public void LoggedIn(CallbackVar cbV)
    {
        if (!cbV.error)
        {
            Client client = JsonUtility.FromJson<Client>(cbV.data);
            nS.yourself = new Client(client);
            nC.SendRPC(EndpointHelpers.GET_FRIENDS_URL(nS.yourself.uid), null, GetFriends, RPC.RequestType.GET);
            nC.SendRPC(EndpointHelpers.GET_PLAYER_ROOMS_URL(nS.yourself.uid), null, cM.GetChats, RPC.RequestType.GET);

            if (staySignedIn)
            {
                PlayerPrefs.SetString("Sign_In",staySignedIn.ToString());
                PlayerPrefs.SetString("Username",lUsername.text);
                PlayerPrefs.SetString("Password",lPassword.text);
            }
            else
            {
                PlayerPrefs.SetString("Sign_In", staySignedIn.ToString());
                PlayerPrefs.SetString("Username", "");
                PlayerPrefs.SetString("Password", "");
            }

            screen = "";
        }
        else
        {
        }

        running = false;
    }

    public void Cancel()
    {
        screen = "";
    }

    public void SwitchScreen(string newScreen)
    {
        screen = newScreen;
    }
    #endregion

    #region Friend Stuff
    
    public void ToggleFriendScreen()
    {
        friendDisplay = !friendDisplay;
    }

    public void SendFriendRequest()
    {
        if(friendRequestIF.text != "")
        {
            nC.SendRPC(EndpointHelpers.ADD_FRIEND_REQUEST_URL(nS.yourself.uid, friendRequestIF.text), null, GetFriends, RPC.RequestType.POST);
            friendRequestIF.text = "";
        }
    }

    public void AcceptFriendRequest()
    {
        if (friendRequestIF.text != "")
        {
            nC.SendRPC(EndpointHelpers.ADD_FRIEND_URL(nS.yourself.uid, friendRequestIF.text), null, GetFriends, RPC.RequestType.POST);
        }
    }

    public void DeleteFriendRequest()
    {
        if (friendRequestIF.text != "")
        {
            nC.SendRPC(EndpointHelpers.REMOVE_FRIEND_URL(nS.yourself.uid, friendRequestIF.text), null, GetFriends, RPC.RequestType.POST);
        }
    }

    public void GetFriends(CallbackVar cbV)
    {
        if (!cbV.error)
        {
            string clientListUnpacked = cbV.data.Trim()[1..^1];
            string[] clientListSplit = clientListUnpacked.Split("},{",System.StringSplitOptions.RemoveEmptyEntries);

            if(clientListSplit.Length > 0)
            {
                nS.friends.Clear();
                for (int i = 0; i < clientListSplit.Length; i++)
                {
                    string newData = clientListSplit[i];
                    
                    if(newData.Substring(0,1) != "{")
                    {
                        newData = "{" + newData;
                    }

                    if (newData.Substring(newData.Length - 1, 1) != "}")
                    {
                        newData = newData + "}";
                    }

                    Client fClient = JsonUtility.FromJson<Client>(newData);

                    if(fClient != null)
                    {
                        if (nS.yourself.uid == fClient.uid)
                        {
                            nS.yourself = fClient;
                        }
                        else
                        {
                            nS.friends.Add(fClient);
                        }
                    }
                }
            }
        }
        else
        {
        }

        friendUpdate = 0;
    }

    public int CountOfOnlineFriends()
    {
        int result = 0;
            
        if(nS.yourself.friends.Count > 0)
        {
            for(int i = 0; i < nS.yourself.friends.Count; i++)
            {
                string[] yFi = nS.yourself.friends[i].Split(',');

                if(yFi.Length > 0)
                {
                    Client yFr = nS.friends.Find(x => x.uid == yFi[0]);

                    if(yFr != null)
                    {
                        if(yFr.isOnline)
                        {
                            result++;
                        }
                    }
                }
            }
        }

        return result;
    }

    #endregion
}
