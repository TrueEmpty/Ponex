using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class DataBank : NetworkBehaviour
{
    public static DataBank instance;
    NetworkManager nM;
    LobbyManager loM;
    NetSetup nS;

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

    public void Start()
    {
        nM = NetworkManager.Singleton;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        loM = LobbyManager.instance;
        nS = NetSetup.instance;
    }

    #region Client

    #region Sent From the Server To Clients

    [ClientRpc]
    void ServerConnectedClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (IsOwner) return;

        GameObject[] allGo = GameObject.FindObjectsOfType<GameObject>();

        if (allGo.Length > 0)
        {
            for (int i = allGo.Length - 1; i >= 0; i--)
            {
                allGo[i].SendMessage("ConnectedToServer", SendMessageOptions.DontRequireReceiver);
            }
        }

        Debug.Log("Client is Connected to server");

        //Send to Server to AddPlayer to lobby
        AddPlayerToLobbyServerRpc(nS.yourself.uid + "," + nS.yourself.username);
    }

    [ClientRpc]
    void ClientDisconnectedClientRpc(ulong clientId)
    {
        Debug.Log("Client (" + clientId + ") has Disconnected from the server");
    }

    [ClientRpc]
    public void SendToClientRpc(string data)
    {
        Debug.Log("Recieved a message to All \n" + data);
    }

    [ClientRpc]
    public void SendToClientRpc(string data, ClientRpcParams clientRpcParams = default)
    {
        if (IsOwner) return;

        Debug.Log("Recieved a message \n" + data);
    }

    [ClientRpc]
    public void UpdateLobbyPlayersClientRpc(string data)
    {
        Debug.Log("Came in ULP" + "\n" + data);

        string clientListUnpacked = data.Trim()[1..^1];
        string[] clientListSplit = clientListUnpacked.Split("},{", System.StringSplitOptions.RemoveEmptyEntries);

        if (clientListSplit.Length > 0)
        {
            List<LobbyPlayer> lplays = new List<LobbyPlayer>();

            for (int i = 0; i < clientListSplit.Length; i++)
            {
                string newData = clientListSplit[i];

                if (newData.Substring(0, 1) != "{")
                {
                    newData = "{" + newData;
                }

                if (newData.Substring(newData.Length - 1, 1) != "}")
                {
                    newData = newData + "}";
                }

                LobbyPlayer fClient = JsonUtility.FromJson<LobbyPlayer>(newData);

                if (fClient != null)
                {
                    lplays.Add(fClient);
                }
            }

            loM.UpdatePlayerList(lplays);            
        }
    }
    #endregion
    #endregion

    #region Server

    void OnClientConnected(ulong clientId)
    {
        // If isn't the Server/Host then we should early return here!
        if (!(IsServer || IsHost)) return;

        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            var client = NetworkManager.ConnectedClients[clientId];
            // Do things for this client

            // NOTE! In case you know a list of ClientId's ahead of time, that does not need change,
            // Then please consider caching this (as a member variable), to avoid Allocating Memory every time you run this function
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };

            ServerConnectedClientRpc(clientRpcParams);
            Debug.Log("Client is Connected to server:" + clientId);
        }
    }

    void OnClientDisconnected(ulong clientId)
    {
        // If isn't the Server/Host then we should early return here!
        if (!(IsServer || IsHost)) return;

        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            var client = NetworkManager.ConnectedClients[clientId];
            // Do things for this client
            string lPlaysList = "";

            if (loM.players.Count > 0)
            {
                int lpI = loM.players.FindIndex(x=> x.clientId == clientId);

                if(lpI >= 0 && lpI < loM.players.Count)
                {
                    loM.players.RemoveAt(lpI);
                }

                for (int i = 0; i < loM.players.Count; i++)
                {
                    if (i > 0)
                    {
                        lPlaysList += ",";
                    }

                    lPlaysList += loM.players[i].ToJson();
                }
            }

            UpdateLobbyPlayersClientRpc(lPlaysList);

            ClientDisconnectedClientRpc(clientId);
            Debug.Log("Client Disconnected from server:" + clientId);
        }
    }

    #region Sent From the Client To Server
    [ServerRpc(RequireOwnership = false)]
    public void SendToServerRpc(string data, ServerRpcParams serverRpcParams = default)
    {
        // If isn't the Server/Host then we should early return here!
        if (!(IsServer || IsHost)) return;

        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            var client = NetworkManager.ConnectedClients[clientId];
            // Do things for this client

            Debug.Log("Recieved a message from Client" + clientId + "\n" + data);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddPlayerToLobbyServerRpc(string data, ServerRpcParams serverRpcParams = default)
    {
        // If isn't the Server/Host then we should early return here!
        if (!(IsServer || IsHost)) return;

        var clientId = serverRpcParams.Receive.SenderClientId;
        string[] sData = data.Split(',');

        if(sData.Length > 1)
        {
            loM.AddPlayer(sData[0], sData[1], clientId);
        }

        string lPlaysList = "";

        if(loM.players.Count > 0)
        {
            for(int i = 0; i < loM.players.Count; i++)
            {
                if(i > 0)
                {
                    lPlaysList += ",";
                }

                lPlaysList += loM.players[i].ToJson();
            }
        }

        UpdateLobbyPlayersClientRpc(lPlaysList);
    }
    #endregion
    #endregion
}
