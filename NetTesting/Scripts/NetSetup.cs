using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class NetSetup : MonoBehaviour
{
    public static NetSetup instance;
    NetworkManager nM;

    public Client yourself = new Client();
    public List<Client> friends = new List<Client>();
    const int m_MaxConnections = 8;
    public string RelayJoinCode = "";
    public string playerID = null;
    public bool waiting = false;

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

    public bool IsLoggedIn
    {
        get { return (yourself.uid != null && yourself.uid != ""); }
    }

    public bool IsAuthenticated
    {
        get { return (playerID != null && playerID != ""); }
    }

    #region Main Server Relay
    void Start()
    {
        nM = NetworkManager.Singleton;
    }

    void Update()
    {
        if(IsLoggedIn)
        {
            if(!IsAuthenticated && !waiting)
            {
                waiting = true;
                AuthenticatingAPlayer();
            }
        }
    }

    public void CloseApp()
    {
        Application.Quit();
    }

    public async void AuthenticatingAPlayer()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            playerID = AuthenticationService.Instance.PlayerId;
            waiting = false;
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
            waiting = false;
        }
    }

    #region Host
    public void HostGame()
    {
        StartCoroutine(ConfigureTransportAndStartNgoAsHost());
    }

    public static async Task<RelayContainer> AllocateRelayServerAndGetJoinCode(int maxConnections, string region = null)
    {
        Allocation allocation;
        string createJoinCode;
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections, region);
        }
        catch (Exception e)
        {
            Debug.LogError($"Relay create allocation request failed {e.Message}");
            throw;
        }

        Debug.Log($"server: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"server: {allocation.AllocationId}");

        try
        {
            createJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Join Key is " + createJoinCode);
        }
        catch
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        return new RelayContainer(new RelayServerData(allocation, "dtls"), createJoinCode);
    }

    IEnumerator ConfigureTransportAndStartNgoAsHost()
    {
        var serverRelayUtilityTask = AllocateRelayServerAndGetJoinCode(m_MaxConnections);
        yield return new WaitUntil(() => serverRelayUtilityTask.IsCompleted);
        if (serverRelayUtilityTask.IsFaulted)
        {
            Debug.LogError("Exception thrown when attempting to start Relay Server. Server not started. Exception: " + serverRelayUtilityTask.Exception.Message);
            yield break;
        }

        RelayContainer relayContainer = serverRelayUtilityTask.Result;

        // Display the joinCode to the user.
        RelayJoinCode = relayContainer.keyCode;

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayContainer.relayServerData);
        NetworkManager.Singleton.StartHost();
    }
    #endregion

    #region Join
    public void JoinGame(string code)
    {
        RelayJoinCode = code;
        StartCoroutine(ConfigureTransportAndStartNgoAsConnectingPlayer());
    }

    public static async Task<RelayServerData> JoinRelayServerFromJoinCode(string joinCode)
    {
        JoinAllocation allocation;
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch(Exception e)
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        Debug.Log($"client: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"host: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");
        Debug.Log($"client: {allocation.AllocationId}");

        return new RelayServerData(allocation, "dtls");
    }

    IEnumerator ConfigureTransportAndStartNgoAsConnectingPlayer()
    {
        // Populate RelayJoinCode beforehand through the UI
        var clientRelayUtilityTask = JoinRelayServerFromJoinCode(RelayJoinCode);

        yield return new WaitUntil(() => clientRelayUtilityTask.IsCompleted);

        if (clientRelayUtilityTask.IsFaulted)
        {
            Debug.LogError("Exception thrown when attempting to connect to Relay Server. Exception: " + clientRelayUtilityTask.Exception.Message);
            yield break;
        }

        var relayServerData = clientRelayUtilityTask.Result;

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        NetworkManager.Singleton.StartClient();
        yield return null;
    }
    #endregion

    #endregion
}

[System.Serializable]
public class Client
{
    public string uid = "";
    public string username = "";
    public List<string> friends = new List<string>(); //Username,[]Groups they are in including Friend Requests
    public string online = new DateTime(1990,1,1,1,1,1,1,DateTimeKind.Local).ToString();
    public string status = "";

    public Client()
    {

    }

    public Client(Client client)
    {
        this.uid = client.uid;
        this.username = client.username;
        this.friends = client.friends;
        this.online = client.online;
        this.status = client.status;
    }

    /*public void SetUID()
    {
        string allAvaliable = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        uid = "";

        for (int i = 0; i < 10; i++)
        {
            uid += allAvaliable.Substring(Random.Range(0, allAvaliable.Length), 1);
        }
    }*/

    public bool isOnline
    {
        get { return (DateTime.Now - DateTime.Parse(online)).Minutes <= 5; }
    }

    public override string ToString()
    {
        string result = "uid:" + uid;
        result += "\n username:" + username;

        string friendsList = "";
        if (friends.Count > 0)
        {
            for (int i = 0; i < friends.Count; i++)
            {
                if (i > 0)
                {
                    friendsList += ",";
                }

                friendsList += friends[i];
            }
        }
        result += "\n friends:" + friendsList;

        result += "\n online:" + online;
        result += "\n status:" + status;

        return JsonUtility.ToJson(this, true);
    }
}

public class RelayContainer
{
    public RelayServerData relayServerData;
    public string keyCode;

    public RelayContainer(RelayServerData relayServerData, string keyCode)
    {
        this.relayServerData = relayServerData;
        this.keyCode = keyCode;
    }
}

