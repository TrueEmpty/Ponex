using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FriendRequestBox : MonoBehaviour
{
    NetSetup nS;
    NetworkConnector nC;
    LoginManager lM;

    public FriendGroupFoldout fgf;
    public int selection = -1;
    public Text friendUserName;
    public GameObject acceptFriend;
    Client c;
    public bool sent = false;

    // Start is called before the first frame update
    void Start()
    {
        nS = NetSetup.instance;
        nC = NetworkConnector.instance;
        lM = LoginManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (selection >= 0 && selection < fgf.friends.Count)
        {
            c = fgf.friends[selection];

            friendUserName.text = c.username;
            acceptFriend.SetActive(!sent);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AcceptFriendRequest()
    {
        nC.SendRPC(EndpointHelpers.ADD_FRIEND_URL(nS.yourself.uid, c.username), null, lM.GetFriends, RPC.RequestType.POST);
        fgf.friends.RemoveAt(selection);
    }

    public void DeleteFriendRequest()
    {
        nC.SendRPC(EndpointHelpers.REMOVE_FRIEND_URL(nS.yourself.uid, c.username), null, lM.GetFriends, RPC.RequestType.POST);
        fgf.friends.RemoveAt(selection);
    }
}
