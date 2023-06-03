using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityProjectCloner;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Relay.Scheduler;
using Unity.Services.Relay.Http;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Random = UnityEngine.Random;

public class LogTunnel : NetworkBehaviour
{
    public UnityTransport _transport;
    
    //public static KeyValuePair<string, Lobby> NotInLobby = new KeyValuePair<string, Lobby>(null, null);

    private ulong? _currentClientId = null;
    private Player _currentPlayer = null;
    private Lobby _currentLobby = null;

    private Dictionary<string, Lobby> _queryCache = new Dictionary<string, Lobby>(); // <string = lobbyId, Lobby>
    private Dictionary<ulong, ChatClientHandle> ConnectedHandles = new Dictionary<ulong, ChatClientHandle>();

    private QueryLobbiesOptions _queryOptions = null;
    private QueryResponse _queryResponse = null;
    private bool _queryComplete = false;

    public Dictionary<string, Lobby> FoundLobbies => _queryCache;
    public Lobby CurrentLobby => _currentLobby;

    //public bool IsHandled => Tunnel.IsSpawned && UserInstance.HasValue;
    public bool IsAdmin => NetworkManager.IsHost || NetworkManager.IsServer;
    //public bool IsGuest => IsHandled && UserInstance.Value.IsGuest;
    //public bool IsLoggedIn => IsHandled && !IsGuest;
    //public string MyName => IsLoggedIn ? UserInstance.Value.UserName : IsGuest ? $"Guest: {Tunnel.NetworkManager.LocalClientId}" : "[No Online Prescence]";// == null? $"Guest:{OwnerClientId}" : UserInstance.UserName;
    //public int InstanceCount => _logInstance.Count;
    public int ClientCount => ConnectedHandles.Count;
    public List<NetworkObject> ClientOwnedObjects => NetworkManager.LocalClient.OwnedObjects;

    //public string ProfileName;
    //public bool LobbyHeartBeat;
    //public bool LobbyQueryUpdate;

    public bool IsSignedIn =>
        UnityServices.State == ServicesInitializationState.Initialized &&
        AuthenticationService.Instance.IsSignedIn;

    #region NET_CODE

    public override void OnNetworkSpawn()
    {
        if (IsAdmin)
            ServerSetup();

        else
            ClientSetup();
    }

    public override void OnNetworkDespawn()
    {
        //BuildAndAddLog("[SERVER END]");

        if (IsAdmin)
            ServerCleanUp();

        else
            ClientCleanUp();
    }

    private void ServerCleanUp()
    {
        Debug.Log("Server Cleanup...");

        ConnectedHandles.Clear();

        //if (AUTO_SAVE_SERVER)
        //    SaveFullServer();

        Debug.Log("Server Ended...");
    }

    private void ClientCleanUp()
    {

    }

    private void ServerSetup()
    {
        Debug.Log("Server Setup...");

        //if (AUTO_LOAD_SERVER)
        //    LoadFullServer();
        //else
        //{
        //    _registryInstance = UserRegistry.Default;
        //    _serverInstance = ServerProfile.Default;
        //}

        NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("Message", RecieveMessage);
        NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("LoginRequest", RecieveLogin_RegisterUserRequest);
        NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("Poosh", Poosh);

        NetworkManager.OnClientConnectedCallback += RegisterGuestChatClient;
        NetworkManager.OnClientDisconnectCallback += UnRegisterChatClient;

        ChatMessage("[SERVER START]");

        NetworkClient adminClient;
        if (!Tunnel.NetworkManager.ConnectedClients.TryGetValue(AdminClientId, out adminClient))
        {
            Debug.LogError($"adminClient not found: {AdminClientId}");
            return;
        }

        _userInstance = new UserProfile(ServerInstance, AdminHandle, AdminClientId);
        UserRegistration adminReg = new UserRegistration(ServerInstance, _userInstance.Value, UserCredential.Null);
        ChatClientHandle adminHandle = new ChatClientHandle(adminClient, adminReg);

        if (ConnectedHandles.ContainsKey(AdminClientId))
        {
            Debug.Log($"How? you have: {ConnectedHandles.Count}");
            ConnectedHandles[AdminClientId] = adminHandle;
        }


        else
            ConnectedHandles.Add(AdminClientId, adminHandle);

        ChatMessage($"{adminHandle.ClientName} joined the server!");
    }
    private void ClientSetup()
    {
        Debug.Log("Client Setup...");

        NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("Log", RecieveServerChatLog);
        NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("Response", RecieveServerResponse);
    }

    #endregion

    public async void HostLobby(string serverName, int maxPlayers, bool isPrivate = false)
    {
        if (!IsSignedIn)
        {
            Debug.Log("Sign in first...");
            return;
        }

        //Debug.Log("Pooshed");

        //await Authenticate();

        //Debug.Log($"Authenticated: {AuthenticationService.Instance.IsSignedIn}");

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);

        Debug.Log("Allocated!");

        string joindCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        Debug.Log($"JoinCode: {joindCode}");

        CreateLobbyOptions lobbyOptions = new CreateLobbyOptions()
        {
            Data = new Dictionary<string, DataObject> { { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joindCode)} }
        };

        _currentLobby = await Lobbies.Instance.CreateLobbyAsync(serverName, maxPlayers, lobbyOptions);
        StartCoroutine(HeartBeat(_currentLobby.Id));
        _transport.SetHostRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

        Debug.Log($"CurrentLobbyName: {_currentLobby.Name}");
        Debug.Log("Lobby creation complete!");
    }

    public async void JoinLobby(string playerName, string lobbyId)
    {
        if (!IsSignedIn)
        {
            Debug.Log("Sign in first...");
            return;
        }

        Lobby lobby;
        if (!_queryCache.TryGetValue(lobbyId, out lobby))
        {
            Debug.Log("Lobby not found in cache...");
            return;
        }

        DataObject obj;
        if (!lobby.Data.TryGetValue("JoinCode", out obj))
        {
            Debug.Log("No join code...");
            return;
        }
            
        string joinCode = obj.Value;

        await Authenticate(playerName);

        if (!IsSignedIn)
        {
            Debug.Log("Not signed in...");
            return;
        }

        _currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        _transport.SetClientRelayData(joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port, joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData);
    }

    public async void LeaveLobby(string lobbyId)
    {

    }

    public async Task Authenticate(string playerName)
    {
        Debug.Log("Authenticating...");
        InitializationOptions initOptions = new InitializationOptions();

        initOptions.SetProfile(playerName == null ? $"Guest:{Random.Range(int.MinValue, int.MaxValue)}" : playerName);

        await UnityServices.InitializeAsync(initOptions);

        Debug.Log("UnityServices Async Init complete!");

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        AuthenticationService.Instance.pl

        Debug.Log($"Authenticated: {IsSignedIn}");
    }

    private async void LobbiesRequest()
    {
        _queryComplete = false;
        _queryResponse = await Lobbies.Instance.QueryLobbiesAsync(_queryOptions);

        _queryCache.Clear();
        foreach(Lobby foundLobby in _queryResponse.Results)
            _queryCache.Add(foundLobby.Id, foundLobby);

        /*List<string> joinedIds = await LobbyService.Instance.GetJoinedLobbiesAsync();

        _joinedCache.Clear();
        foreach (Lobby foundLobby in _queryCache.Values)
            foreach (string joinedId in joinedIds)
                if (foundLobby.Id == joinedId)
                {
                    _joinedCache.Add(foundLobby.Id, foundLobby);
                    break;
                }*/

        _queryComplete = true;
    }

    private IEnumerator LobbyQuery(QueryLobbiesOptions options = null, int seconds = 1, int timeOutSeconds = 5)
    {
        _queryOptions = options;
        WaitForSecondsRealtime delay = new WaitForSecondsRealtime(seconds);
        TimeSpan timeOut = new TimeSpan(0, 0, timeOutSeconds);
        DateTime lastQuery = DateTime.Now;
        Debug.Log("Query started!");
        bool await = false;
        while (true)
        {
            if (UnityServices.State == ServicesInitializationState.Initialized
                && AuthenticationService.Instance.IsSignedIn)
            {
                //Debug.Log("Query...");
                if (!await)
                {
                    //Debug.Log("Query lobbies...");
                    LobbiesRequest();
                    //Debug.Log("Await response...");
                    await = true;
                    lastQuery = DateTime.Now;
                }


                //Debug.Log($"result:\n");
                //
                //if (_queryResponse == null) { Debug.Log("Null task"); }
                //else if (!_queryResponse.IsCompleted) { Debug.Log("Not complete"); }
                ////else if (!response.IsCompletedSuccessfully) { Debug.Log("Did not complete successfully"); }
                //else if (!(_queryResponse.Result is QueryResponse)) { Debug.Log("Not query"); }
                //else if (_queryResponse.Result.Results == null) { Debug.Log("Null list"); }
                //else Debug.Log(_queryResponse.Result.Results.Count);


                if (_queryComplete)
                {
                    //Debug.Log("Cacheing result and reset await!");
                    await = false;
                    _queryCache.Clear();
                    foreach(Lobby lobbyFound in _queryResponse.Results)
                        _queryCache.Add(lobbyFound.Id, lobbyFound);// == null ? _queryCache : response.Result.Results;
                }

                if (!_queryComplete &&
                    lastQuery + timeOut < DateTime.Now)
                {
                    //Debug.Log("Time-out on query, resetting");
                    await = false;
                }
                //else
                    //Debug.Log("Awaiting response...");
                    
                //Debug.Log("Sleep...");
            }

            //Debug.Log("Boop");

            yield return delay;
        }
    }

    private static IEnumerator HeartBeat(string lobbyId , int seconds = 15)
    {
        WaitForSecondsRealtime delay = new WaitForSecondsRealtime(seconds);
        while (true)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LobbyQuery(new QueryLobbiesOptions()
        {
            Count = 20,
            Filters = new List<QueryFilter>() { new QueryFilter(field: QueryFilter.FieldOptions.AvailableSlots, op: QueryFilter.OpOptions.GT, value: "0") },
            Order = new List<QueryOrder>() { new QueryOrder(true, QueryOrder.FieldOptions.AvailableSlots)}
        }));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
