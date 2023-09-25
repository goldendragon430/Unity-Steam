using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI debugText = null;

    private FacepunchTransport transport;
    public Lobby? CurrentLobby { get; private set; } = null;

    public List<Lobby> Lobbies { get; private set; } = new List<Lobby>(capacity: 100);

    private void Start()
    {
        transport = NetworkManager.Singleton.GetComponent<FacepunchTransport>();

        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }
    private void OnDestroy()
    {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;

        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
    }
    public async void StartHost()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.StartHost();
        CurrentLobby = await SteamMatchmaking.CreateLobbyAsync(2);
        var hostedMultiplayerLobby = CurrentLobby.Value;
        hostedMultiplayerLobby.SetPublic();
        hostedMultiplayerLobby.SetJoinable(true);
        hostedMultiplayerLobby.SetData("Name", "Oleksandr Server");


        Debug.Log("Lobby has been created!");
    }

    public async void JoinServer()
    {
        await RefreshLobbies();
    }
    public async Task<bool> RefreshLobbies(int maxResults = 20)
    {
        try
        {
            Lobby[] lobbies = await SteamMatchmaking.LobbyList.RequestAsync();

            if (lobbies != null)
            {
                foreach (Lobby lobby in lobbies.ToList())
                {
                    string dd = lobby.GetData("Name");
                    //Debug.Log(dd);
                    if (dd == "Oleksandr Server")
                    {
                        Debug.Log("Success: " + lobby.Id);
                        await SteamMatchmaking.JoinLobbyAsync(lobby.Id);
                        break;
                    }
                }
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            Debug.Log("Error fetching multiplayer lobbies");
            return true;
        }
    }
    public void StartClient(SteamId id)
    {
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

        transport.targetSteamId = id;

        Debug.Log($"Joining room hosted by {transport.targetSteamId}", this);

        if (NetworkManager.Singleton.StartClient())
        {
            debugText.text = "Joined";
        }
    }
    private void OnApplicationQuit() => Disconnect();
    public void Disconnect()
    {
        CurrentLobby?.Leave();

        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.Shutdown();
    }

    #region Steam Callbacks

    // Accepted Steam Game Invite
    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
    {
        bool isSame = lobby.Owner.Id.Equals(id);

        Debug.Log($"Owner: {lobby.Owner}");
        Debug.Log($"Id: {id}");
        Debug.Log($"IsSame: {isSame}", this);

        StartClient(id);
    }

    private void OnLobbyInvite(Friend friend, Lobby lobby) => Debug.Log($"You got a invite from {friend.Name}", this);

    private void OnLobbyMemberLeave(Lobby lobby, Friend friend) { }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friend) {
        StartClient(friend.Id);
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        Debug.Log($"You have entered in lobby, clientId={NetworkManager.Singleton.LocalClientId}", this);

        if (NetworkManager.Singleton.IsHost)
            return;

        StartClient(lobby.Owner.Id);
    }

    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        if (result != Result.OK)
        {
            Debug.LogError($"Lobby couldn't be created!, {result}", this);
            return;
        }

    }

    #endregion

    #region Network Callbacks

    private void ClientConnected(ulong clientId) 
        {
          Debug.Log($"I'm connected, clientId={clientId}");
        }

    private void ClientDisconnected(ulong clientId)
    {
        Debug.Log($"I'm disconnected, clientId={clientId}");

        NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnected;
        NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnected;
    }

    private void OnServerStarted() {
        debugText.text = "Server Started";
    }

    private void OnClientConnectedCallback(ulong clientId) => Debug.Log($"Client connected, clientId={clientId}", this);

    private void OnClientDisconnectCallback(ulong clientId) => Debug.Log($"Client disconnected, clientId={clientId}", this);

    #endregion
} 