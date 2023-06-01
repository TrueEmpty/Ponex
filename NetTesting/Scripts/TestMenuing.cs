using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TestMenuing : MonoBehaviour
{
    NetSetup nS;
    public Screens screens = Screens.None;

    #region Main
    public GameObject offlineScreen;
    public GameObject onlineMainScreen;
    #endregion

    #region Join
    public GameObject joiningScreen;
    public InputField joinKeyIF;
    #endregion

    #region Lobby
    public GameObject lobbyScreen;
    public Text lobbyJoinKey;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        nS = NetSetup.instance;
    }

    void Update()
    {
        if(!nS.IsLoggedIn)
        {
            screens = Screens.None;
        }
        else
        {
            if(screens == Screens.None)
            {
                screens = Screens.Pre;
            }

            lobbyJoinKey.text = nS.RelayJoinCode;
        }

        offlineScreen.SetActive(screens == Screens.Pre);
        onlineMainScreen.SetActive(screens == Screens.Main);
        lobbyScreen.SetActive(screens == Screens.Lobby);
        joiningScreen.SetActive(screens == Screens.Join);
    }

    public void SwitchScreen(int newScreen)
    {
        screens = (Screens)newScreen;
    }

    #region Main
    public void HostGame()
    {
        nS.HostGame();
        screens = Screens.Lobby;
        LobbyManager.instance.AddPlayer(nS.yourself.uid,nS.yourself.username,NetworkManager.Singleton.LocalClientId);
    }

    #endregion

    #region Join
    public void JoinGame()
    {
        if(joinKeyIF.text != "" && joinKeyIF.text != null)
        {
            nS.JoinGame(joinKeyIF.text.ToUpper());
            screens = Screens.Lobby;
        }
    }

    #endregion

    #region Lobby

    #endregion
}

[System.Serializable]
public enum Screens
{
    None,
    Pre,
    Main,
    Join,
    Lobby
}
