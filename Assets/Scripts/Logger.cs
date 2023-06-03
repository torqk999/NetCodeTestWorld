using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public enum Chattribute
{
    Name = 32,
    Message = 512,
    Credential = 1024,
    LogData = 2048
}

public class Logger //: NetworkBehaviour
{
    //// TESTING AREA ////
    public string Derp;
    public NetworkClient Derpette = null;
    public Element TestElement;
    public int Derpin = 0;
    //////////////////////

    public LogTunnel Tunnel;
    
    public static string ServerHandle { get; private set; } = "[SERVER]";
    public static string AdminHandle { get; private set; } = "[ADMIN]";
    public static ulong AdminClientId { get; private set; } = 0;
    public static ulong ServerID { get; private set; } = 69;

    [SerializeField]
    private bool AUTO_LOAD_SERVER;
    [SerializeField]
    private bool AUTO_SAVE_SERVER;
    [SerializeField]
    public const int LOG_INSTANCE_MAX = 500; // Needs full implementation...

    //private Dictionary<ulong, ChatClientHandle> ConnectedHandles = new Dictionary<ulong, ChatClientHandle>();

    private LogBook _logInstance;
    private UserProfile? _userInstance;


    private UserRegistry? _registryInstance;
    private ServerProfile? _serverInstance;
    

    public LogBook LogInstance => _logInstance;
    public UserRegistry RegistryInstance => _registryInstance.HasValue ? _registryInstance.Value : UserRegistry.Null;
    public ServerProfile ServerInstance => _serverInstance.HasValue ? _serverInstance.Value : ServerProfile.Null;
    public UserProfile? UserInstance => _userInstance; // nullable for the sake of the interface interpreter.

    public bool IsHandled => Tunnel.IsSpawned && UserInstance.HasValue;
    public bool IsAdmin => Tunnel.NetworkManager.IsHost || Tunnel.NetworkManager.IsServer;
    public bool IsGuest => IsHandled && UserInstance.Value.IsGuest;
    public bool IsLoggedIn => IsHandled && !IsGuest;
    public string MyName => IsLoggedIn ? UserInstance.Value.UserName : IsGuest ? $"Guest: {Tunnel.NetworkManager.LocalClientId}" : "[No Online Prescence]";// == null? $"Guest:{OwnerClientId}" : UserInstance.UserName;
    public int InstanceCount => _logInstance.Count;
    //public int ClientCount => ConnectedHandles.Count;
    //public List<NetworkObject> ClientOwnedObjects => Tunnel.NetworkManager.LocalClient.OwnedObjects;

    public static int StringSizeBytes(string input) // Apparantly .Net says so.... https://www.red-gate.com/simple-talk/blogs/how-big-is-a-string-in-net/#:~:text=A%20string%20is%20composed%20of,a%204%2Dbyte%20type%20descriptor)
    {
        return 18 + (input.Length * 2);
    }

    #region Client
    public void SetClientName(string newName)
    {
        //NetworkManager.ServerClientId
        // <<<--- Add Request change name logic here
        //UserInstance.SetUserName(newName) = newName;
    }

    void foo0()
    {
        
    }
    public void ChatMessage(string message)
    {
        if (IsAdmin)
            BuildAndDistributeLog(message);
        else
            SendClientMessage(message);
    }
    private void SendClientMessage(string message)
    {
        Debug.Log("Sending Log to server...");
        FastBufferWriter messageBuffer = new FastBufferWriter((int)Chattribute.Message, Allocator.Persistent, (int)Chattribute.Message);
        messageBuffer.WriteValueSafe(message);
        Debug.Log($"messageBuffer: {messageBuffer}");
        Tunnel.NetworkManager.CustomMessagingManager.SendNamedMessage("Message", AdminClientId, messageBuffer);
    }
    public void SendLoginUserRequest(string loginName, string passWord)
    {
        // Maybe add some sort of response logic to the ui interface whether or not the credentials
        // were the right length, used proper characters, etc. Things the user does not have to
        // wait for a response from the server for
        Debug.Log("Sending Login request to server...");
        UserCredential credential = new UserCredential(loginName, passWord);
        FastBufferWriter credentialBuffer = new FastBufferWriter((int)Chattribute.Credential, Allocator.Persistent, (int)Chattribute.Credential);
        credentialBuffer.WriteValueSafe(ElementBoxHelper.PackElementTree(credential.Box()));
        Tunnel.NetworkManager.CustomMessagingManager.SendNamedMessage("LoginRequest", AdminClientId, credentialBuffer);
    }

    public void Poosh(ulong clientId, FastBufferReader garbago)
    {
        Derpin++;
        Debug.Log($"Derpin: {Derpin}");
    }

    public void foo2()
    {
        FastBufferWriter writer = new FastBufferWriter(16, Allocator.Persistent);
        writer.WriteValueSafe(true);
        Tunnel.NetworkManager.CustomMessagingManager.SendNamedMessage("Poosh", AdminClientId, writer);
    }

    public void RecieveServerChatLog(ulong clientId, FastBufferReader buffer)
    {
        string packedLog;
        buffer.ReadValueSafe(out packedLog);

        Element logElement = ElementBoxHelper.BuildElementTree(packedLog, Enum.GetNames(typeof(LogBookElement)));

        _logInstance.AddLog(new ChatLog(logElement));
    }
    public void RecieveServerResponse(ulong clientId, FastBufferReader buffer)
    {

        string packedLogin;
        buffer.ReadValueSafe(out packedLogin);
        ResponseCode responseCode;
        Element loginElement = ElementBoxHelper.BuildElementTree(packedLogin, Enum.GetNames(typeof(LogBookElement)), out responseCode);
        Debug.Log($"ServerResponse: {responseCode}");

        PingToken responseToken = new PingToken(loginElement);

        _userInstance = responseToken.RequestedProfile;
        _serverInstance = responseToken.RequestedServerInfo;
    }
    #endregion

    #region FileSystem
    #region Testing
    public void TestWriteFile(string text)
    {
        ServerDataBase.SaveDefaultTextFile(ServerDataBase.TestFilePath, text);
    }
    public string TestReadFile()
    {
        return ServerDataBase.LoadDefaultTextFile(ServerDataBase.TestFilePath);
    }
    public void BuildTestElementTree()
    {
        TestElement = ElementBoxHelper.BuildElementTree(ServerDataBase.LoadDefaultTextFile(ServerDataBase.LogBookFilePath), Enum.GetNames(typeof(LogBookElement)));
    }
    #endregion
    public void SaveLogInstance()
    {
        ServerDataBase.SaveDefaultTextFile(ServerDataBase.LogBookFilePath, ElementBoxHelper.PackElementTree(_logInstance.Box()));
    }
    public void SaveRegistryInstance()
    {
        if (!_registryInstance.HasValue)
        {
            Debug.LogError("No Registry!");
            return;
        }
        ServerDataBase.SaveDefaultTextFile(ServerDataBase.RegistryFilePath, ElementBoxHelper.PackElementTree(_registryInstance.Value.Box()));
    }
    public void SaveServerProfileInstance()
    {
        if (!_serverInstance.HasValue)
        {
            Debug.LogError("No ServerProfile!");
            return;
        }
        ServerDataBase.SaveDefaultTextFile(ServerDataBase.ServerFilePath, ElementBoxHelper.PackElementTree(_serverInstance.Value.Box()));
    }

    public void LoadLogInstance()
    {
        _logInstance = new LogBook(ElementBoxHelper.BuildElementTree(
            ServerDataBase.LoadDefaultTextFile(ServerDataBase.LogBookFilePath),
            Enum.GetNames(typeof(LogBookElement))));
    }
    public void LoadRegistryInstance()
    {
        _registryInstance = new UserRegistry(ElementBoxHelper.BuildElementTree(
            ServerDataBase.LoadDefaultTextFile(ServerDataBase.RegistryFilePath),
            Enum.GetNames(typeof(LogBookElement))));
    }
    public void LoadServerInstance()
    {
        _serverInstance = new ServerProfile(ElementBoxHelper.BuildElementTree(
            ServerDataBase.LoadDefaultTextFile(ServerDataBase.ServerFilePath),
            Enum.GetNames(typeof(LogBookElement))));
    }

    public void SaveFullServer()
    {
        SaveLogInstance();
        SaveRegistryInstance();
        SaveServerProfileInstance();
    }

    public void LoadFullServer()
    {
        LoadLogInstance();
        LoadRegistryInstance();
        LoadServerInstance();
    }

    public void ClearRegistry()
    {
        if (_registryInstance.HasValue)
            _registryInstance.Value.Clear();
    }
    #endregion

    #region Server
    public void RecieveMessage(ulong clientId, FastBufferReader buffer)
    {
        Debug.Log("Recieved Message to process...");
        ChatClientHandle chatHandle;
        if (!ConnectedHandles.TryGetValue(clientId, out chatHandle))
        {
            Debug.LogError($"chatHandle not found: {clientId}");
            return;
        }

        string message;
        buffer.ReadValueSafe(out message);

        BuildAndDistributeLog(message, chatHandle);

    }


    /// <summary>
    /// Local creation and appending of a log to the LogInstance.
    /// </summary>
    /// <param name="message">The string message.</param>
    /// <param name="handle">The handle who sent the message to be logged. A null reference assumes it was meant to be a message made by the server</param>
    /// <returns></returns>
    private ChatLog BuildAndAddLog(string message, ChatClientHandle handle = null)
    {
        Debug.Log($"ServerHandle: {ServerHandle} | AdminClientId: {AdminClientId}");
        ChatLog newLog = handle != null ? new ChatLog(message, handle) : new ChatLog(message, ServerHandle, AdminClientId);
        _logInstance.AddLog(newLog);
        return newLog;
    }

    /// <summary>
    /// Server will build the initial log, pack, and then distribute to all clients on the server
    /// </summary>
    /// <param name="message">The string message.</param>
    /// <param name="handle">The handle who sent the message to be logged. A null reference assumes it was meant to be a message made by the server.</param>
    private void BuildAndDistributeLog(string message, ChatClientHandle handle = null)
    {
        ChatLog newLog = BuildAndAddLog(message, handle);

        FastBufferWriter instanceLog = new FastBufferWriter((int)Chattribute.LogData, Allocator.Persistent, (int)Chattribute.LogData);

        string packedLog = ElementBoxHelper.PackElementTree(newLog.Box());
        Debug.Log($"PackedLog: {packedLog}");
        instanceLog.WriteValueSafe(packedLog);

        Tunnel.NetworkManager.CustomMessagingManager.SendNamedMessageToAll("Log", instanceLog);
        
    }
    public void RecieveLogin_RegisterUserRequest(ulong clientId, FastBufferReader credentials)
    {
        if (!_registryInstance.HasValue)
        {
            Debug.LogError("No Registry!");
            return;
        }

        Debug.Log("Checking Credentials...");
        NetworkClient netClient;
        if(!Tunnel.NetworkManager.ConnectedClients.TryGetValue(clientId, out netClient))
        {
            Debug.LogError($"netClient not found: {clientId}");
            return;
        }

        ChatClientHandle chatHandle;
        if (!ConnectedHandles.TryGetValue(clientId, out chatHandle))
        {
            Debug.LogError("not already a guest?");
            return;
            //ConnectedHandles.Add(clientId, new ChatClientHandle(netClient, existingRegistration.Profile));
        }

        string credentialsPacked;
        credentials.ReadValueSafe(out credentialsPacked);

        Element credentialElement = ElementBoxHelper.BuildElementTree(credentialsPacked, Enum.GetNames(typeof(LogBookElement)));

        FastBufferWriter loginResponseBuffer = new FastBufferWriter((int)Chattribute.Credential, Allocator.Persistent, (int)Chattribute.Credential);
        UserCredential credentialRequest = new UserCredential(credentialElement);
        UserRegistration? clientsRegistration;
        //UserProfile? newDefaultProfile;
        PingToken loginResponseToken;

        if (RegistryInstance.TryGetRegistration(credentialRequest.LoginName, out clientsRegistration))
        {
            if(!clientsRegistration.Value.CheckCredential(credentialRequest))
            {
                loginResponseToken = new PingToken(ServerInstance, ResponseCode.Incorrect_Credential);
                Debug.Log("Failed login!");
            }

            else
            {
                chatHandle.Registration = clientsRegistration;
                loginResponseToken = new PingToken(ResponseCode.Logged_In, null, clientsRegistration.Value.Profile, ServerInstance);
                ChatMessage($"Welcome back {chatHandle.ClientName}!");
                Debug.Log($"{chatHandle.ClientName} signed in.");
            }
        }
        else if (!RegistryInstance.AddRegistration(ServerInstance, credentialRequest, out clientsRegistration))
        {
            loginResponseToken = new PingToken(ResponseCode.Incorrect_Credential, null, new UserProfile(clientId), ServerInstance);
            Debug.LogError("Registration failed");
        }
        else
        {
            chatHandle.Registration = clientsRegistration;
            loginResponseToken = new PingToken(ResponseCode.Registered, null, clientsRegistration.Value.Profile, ServerInstance);

            ChatMessage($"{chatHandle.ClientName} Joined the server! Welcome!");
            Debug.Log($"{chatHandle.ClientName} registered.");
        }

        loginResponseBuffer.WriteValueSafe(ElementBoxHelper.PackElementTree(loginResponseToken.Box()));
        Tunnel.NetworkManager.CustomMessagingManager.SendNamedMessage("Response", clientId, loginResponseBuffer);
    }



    public void RecieveLogoutUserRequest(ulong clientId, FastBufferReader credentials)
    {

    }

    public void RegisterGuestChatClient(ulong clientId)
    {
        if (clientId == AdminClientId)
        {
            Debug.Log("Admin Ignores own OnConnected callBack!");
            return;
        }

        NetworkClient newClient;
        if (!Tunnel.NetworkManager.ConnectedClients.TryGetValue(clientId, out newClient))
        {
            Debug.LogError($"networkClient not found: {clientId}");
            return;
        }

        Derpette = newClient;

        UserProfile guestProfile = new UserProfile(clientId);
        Debug.Log($"GuestProfile Made. Name: {guestProfile.UserName}");
        ChatClientHandle guestHandle = new ChatClientHandle(newClient);

        if (ConnectedHandles.ContainsKey(clientId))
            ConnectedHandles[clientId] = guestHandle;

        else
            ConnectedHandles.Add(clientId, guestHandle);

        PingToken guestResponseToken = new PingToken(ServerInstance);

        FastBufferWriter loginResponseBuffered = new FastBufferWriter((int)Chattribute.LogData, Allocator.Persistent, (int)Chattribute.LogData);
        loginResponseBuffered.WriteValueSafe(ElementBoxHelper.PackElementTree(guestProfile.Box()));
        Tunnel.NetworkManager.CustomMessagingManager.SendNamedMessage("Response", clientId, loginResponseBuffered);

        ChatMessage($"{guestHandle.ClientName} joined the server!");
    }

    private void foo0(NetworkClient client)
    {
        Debug.Log(client.OwnedObjects[0].GetComponent<Logger>().Derp);
    }

    public void UnRegisterChatClient(ulong clientId)
    {
        ChatClientHandle chatHandle;
        if(!ConnectedHandles.TryGetValue(clientId, out chatHandle))
        {
            Debug.LogError($"chatHandle not found: {clientId}");
            return;
        }
        ChatMessage($"{chatHandle.ClientName} left the server!");
        ConnectedHandles.Remove(clientId);
    }
    #endregion

    /*public override void OnNetworkSpawn()
    {
        if (IsAdmin)
            ServerSetup();

        else
            ClientSetup();
    }

    public override void OnNetworkDespawn()
    {
        BuildAndAddLog("[SERVER END]");

        if (IsAdmin)
            ServerCleanUp();

        else
            ClientCleanUp();
    }

    private void ServerCleanUp()
    {
        Debug.Log("Server Cleanup...");

        ConnectedHandles.Clear();

        if (AUTO_SAVE_SERVER)
            SaveFullServer();

        Debug.Log("Server Ended...");
    }

    private void ClientCleanUp()
    {

    }
    
    private void ServerSetup()
    {
        Debug.Log("Server Setup...");

        if (AUTO_LOAD_SERVER)
            LoadFullServer();
        else
        {
            _registryInstance = UserRegistry.Default;
            _serverInstance = ServerProfile.Default;
        }

        Tunnel.NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("Message", RecieveMessage);
        Tunnel.NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("LoginRequest", RecieveLogin_RegisterUserRequest);
        Tunnel.NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("Poosh", Poosh);

        Tunnel.NetworkManager.OnClientConnectedCallback += RegisterGuestChatClient;
        Tunnel.NetworkManager.OnClientDisconnectCallback += UnRegisterChatClient;

        ChatMessage("[SERVER START]");

        NetworkClient adminClient;
        if(!Tunnel.NetworkManager.ConnectedClients.TryGetValue(AdminClientId, out adminClient))
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

        Tunnel.NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("Log", RecieveServerChatLog);
        Tunnel.NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("Response", RecieveServerResponse);
    }*/
    void Start()
    {
        _logInstance = new LogBook(Logger.LOG_INSTANCE_MAX);
    }
    void Update()
    {
        if (Derpette != null && Derpette.OwnedObjects.Count > 0)
        {
            //Debug.Log(Derpette.OwnedObjects[0].GetComponent<Logger>())
        }
    }
}
