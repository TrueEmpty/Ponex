using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FriendInfoBox : MonoBehaviour
{
    public FriendGroupFoldout fgf;
    public int selection = -1;
    public Text friendUserName;
    public Image friendOnline;
    public Text friendRank;
    public Text friendStatus;

    // Update is called once per frame
    void Update()
    {        
        if(selection >= 0 && selection < fgf.friends.Count)
        {
            Client c = fgf.friends[selection];

            friendUserName.text = c.username;
            friendOnline.color = c.isOnline ? Color.green : Color.white;
            friendRank.text = "UnRanked";
            friendStatus.text = c.status;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
