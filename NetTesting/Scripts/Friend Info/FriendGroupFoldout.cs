using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FriendGroupFoldout : MonoBehaviour
{
    NetSetup nS;
    LoginManager lM;
    public int groupIndex = 0;
    public List<Client> friends = new List<Client>();

    public GameObject friendInfoBox;
    public GameObject friendRequestBox;

    bool foldout = false;
    public Text foldoutName;
    public GameObject content;
    public RectTransform arrowHolder;

    // Start is called before the first frame update
    void Start()
    {
        nS = NetSetup.instance;
        lM = LoginManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if(groupIndex >= 0 && groupIndex < lM.groups.Count)
        {
            string group = lM.groups[groupIndex];

            if (nS.IsLoggedIn)
            {
                friends.Clear();
                List<string> yFriends = nS.yourself.friends;
                if (yFriends.Count > 0)
                {
                    for(int i = 0; i < yFriends.Count; i++)
                    {
                        switch (group.ToLower().Trim())
                        {
                            case "online":
                                string cG1 = ContainsGroup(yFriends[i]);
                                string cGfr1 = ContainsGroup(yFriends[i], "friend requests");
                                string cGfs1 = ContainsGroup(yFriends[i], "friend request sent");

                                if (cG1 != null && cGfr1 == null && cGfs1 == null)
                                {
                                    if (!friends.Exists(x => x.uid.Trim() == cG1))
                                    {
                                        Client fF1 = nS.friends.Find(x => x.uid.Trim() == cG1);

                                        if (fF1 != null)
                                        {
                                            if (fF1.isOnline)
                                            {
                                                friends.Add(fF1);
                                            }
                                        }
                                    }
                                }
                                break;
                            case "offline":
                                string cG2 = ContainsGroup(yFriends[i]);
                                string cGfr2 = ContainsGroup(yFriends[i], "friend requests");
                                string cGfs2 = ContainsGroup(yFriends[i], "friend request sent");

                                if (cG2 != null && cGfr2 == null && cGfs2 == null)
                                {
                                    if (!friends.Exists(x => x.uid.Trim() == cG2))
                                    {
                                        Client fF1 = nS.friends.Find(x => x.uid.Trim() == cG2);

                                        if (fF1 != null)
                                        {
                                            if(!fF1.isOnline)
                                            {
                                                friends.Add(fF1);
                                            }
                                        }
                                    }
                                }
                                break;
                            default:
                                string cG3 = ContainsGroup(yFriends[i], group);

                                if (cG3 != null)
                                {
                                    if (!friends.Exists(x => x.uid.Trim() == cG3))
                                    {
                                        Client fF1 = nS.friends.Find(x => x.uid.Trim() == cG3);

                                        if (fF1 != null)
                                        {
                                            friends.Add(fF1);
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }

                if(friends.Count > 0)
                {
                    friends.Sort((p1, p2) => p1.username.CompareTo(p2.username));

                    int cC = content.transform.childCount;
                    if (cC < friends.Count)
                    {
                        for (int i = cC; i < friends.Count; i++)
                        {
                            GameObject go = Instantiate((group.ToLower().Trim() == "friend requests" || group.ToLower().Trim() == "friend request sent") ? friendRequestBox : friendInfoBox, content.transform);

                            FriendRequestBox frb = go.GetComponent<FriendRequestBox>();

                            if (frb != null)
                            {
                                frb.fgf = this;
                                frb.selection = i;
                                frb.sent = (group.ToLower().Trim() == "friend request sent");
                            }

                            FriendInfoBox fib = go.GetComponent<FriendInfoBox>();

                            if (fib != null)
                            {
                                fib.fgf = this;
                                fib.selection = i;
                            }
                        }
                    }
                }
            }

            foldoutName.text = group + " (" + friends.Count + ")";
        }
        else
        {
            Destroy(gameObject);
        }
    }

    string ContainsGroup(string friendString,string group = null)
    {
        string result = null;

        string[] sA = friendString.Split(',');

        if(sA.Length > 1 && group != null)
        {
            for(int i = 1; i < sA.Length; i++)
            {
                if(sA[i].ToLower().Trim() == group.ToLower().Trim())
                {
                    result = sA[0].Trim();
                    break;
                }
            }
        }
        else if(sA.Length > 0 && group == null)
        {
            result = sA[0].Trim();
        }

        return result;
    }

    public void ToggleFoldout()
    {
        foldout = !foldout;
        content.SetActive(foldout);
        arrowHolder.rotation = Quaternion.Euler(0, 0, foldout ? 270 : 0);
    }
}
