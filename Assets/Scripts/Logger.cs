using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum Chattribute
{
    Name = 32,
    Message = 512,
    Credential = 1024,
    LogData = 2048
}

public class Logger : NetworkBehaviour
{
    public Element TestElement;
    public static string ServerHandle { get; private set; } = "[SERVER]";
    public static string AdminHandle { get; private set; } = "[ADMIN]";
    public static ulong AdminClientId { get; private set; } = 0;
    public static ulong ServerID { get; private set; } = 69;


    private Dictionary<ulong, ChatClientHandle> ConnectedHandles = new Dictionary<ulong, ChatClientHandle>();
    //private Dictionary<string, UserRegistration> KnownRegistrations = new Dictionary<string, UserRegistration>();
    private UserRegistry RegistryInstance;
    private LogBook LogInstance;
    public ServerProfile ServerInstance { get; private set; }
    public UserProfile UserInstance { get; private set; }

    public ChatClientHandle this[ulong clientId]
    {
        get { ChatClientHandle returnHandle; if (!ConnectedHandles.TryGetValue(clientId, out returnHandle)) return null; return returnHandle; }
        set { try { ConnectedHandles.Add(clientId, value); } catch { ConnectedHandles[clientId] = value; } }
    }
    public ChatLog this[int index]
    {
        get { try { return LogInstance[index]; } catch { Debug.Log($"ChatLog: Out of Bounds index. Attempt: {index} | Count: {LogInstance.Count}"); return ChatLog.Null; } }
        set { try { LogInstance[index] = value; } catch { Debug.Log($"ChatLog: Out of Bounds index. Attempt: {index} | Count: {LogInstance.Count}"); } }
    }

    public bool IsAdmin => NetworkManager.IsHost || NetworkManager.IsServer;
    public bool IsGuest => IsSpawned && UserInstance.IsGuest;
    public bool IsLoggedIn => IsSpawned && !IsGuest;
    public string MyName => IsLoggedIn ? UserInstance.UserName : IsGuest ? $"Guest: {NetworkManager.LocalClientId}" : "[No Online Prescence]";// == null? $"Guest:{OwnerClientId}" : UserInstance.UserName;
    public int InstanceCount => LogInstance.Count;
    public int ClientCount => ConnectedHandles.Count;
    //public int UserCount =>
    public List<NetworkObject> ClientOwnedObjects => NetworkManager.LocalClient.OwnedObjects;

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
    public void ChatMessage(string message)
    {
        if (IsServer)
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
        NetworkManager.CustomMessagingManager.SendNamedMessage("Message", AdminClientId, messageBuffer);
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
        NetworkManager.CustomMessagingManager.SendNamedMessage("LoginRequest", AdminClientId, credentialBuffer);
    }
    public void RecieveChatLog(ulong clientId, FastBufferReader buffer)
    {
        string packedLog;
        buffer.ReadValueSafe(out packedLog);

        Element logElement = ElementBoxHelper.BuildElementTree(packedLog, Enum.GetNames(typeof(LogBookElement)));

        LogInstance.AddLog(new ChatLog(logElement));
    }
    public void RecieveUserLoginResponse(ulong clientId, FastBufferReader buffer)
    {

        string packedLogin;
        buffer.ReadValueSafe(out packedLogin);
        Element loginElement = ElementBoxHelper.BuildElementTree(packedLogin, Enum.GetNames(typeof(LogBookElement)));


        UserInstance = new UserProfile(loginElement);

    }
    


    #endregion
    #region FileSystem
    public void TestWriteFile(string text)
    {
        ServerDataBase.SaveDefaultLogBookFile(text);
    }
    public void SaveTestElementTree()
    {
        ServerDataBase.SaveDefaultLogBookFile(ElementBoxHelper.PackElementTree(LogInstance.Box()/*TestElement*/));
    }
    public void SaveInstance(string targetPath = null)
    {

    }
    public void BuildTestElementTree()
    {
        string[] elementLegend = new string[] { "Log", "TimeStamp", "Message", "UserName", "IDs" };

        string rawStream = ServerDataBase.LoadDefaultLogBookFile();

        TestElement = ElementBoxHelper.BuildElementTree(rawStream, elementLegend);
    }
    public string TestReadFile()
    {
        return ServerDataBase.LoadDefaultLogBookFile();
    }
    public void GenerateNewClientFile()
    {
        Debug.Log("Sending call to database...");
        ServerDataBase.GenerateNewDefaultLogBookFile();
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
        LogInstance.AddLog(newLog);
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

        NetworkManager.CustomMessagingManager.SendNamedMessageToAll("Log", instanceLog);
        
    }
    public void RecieveLoginUserRequest(ulong clientId, FastBufferReader credentials)
    {
        Debug.Log("Checking Credentials...");
        NetworkClient netClient;
        if(!NetworkManager.ConnectedClients.TryGetValue(clientId, out netClient))
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
        

        UserCredential credentialRequest = new UserCredential(credentialElement);
        UserRegistration existingRegistration;
        UserLoginToken loginResponseToken;
        FastBufferWriter loginResponseBuffer = new FastBufferWriter((int)Chattribute.Credential, Allocator.Persistent, (int)Chattribute.Credential);

        if (RegistryInstance.Registrations.TryGetValue(credentialRequest.LoginName, out existingRegistration))
        {
            if(!existingRegistration.CheckCredential(credentialRequest))
            {
                loginResponseToken = new UserLoginToken(ServerInstance, CustomResponseCode.Incorrect_Credential);
                loginResponseBuffer.WriteValueSafe(ElementBoxHelper.PackElementTree(loginResponseToken.Box()));
                NetworkManager.CustomMessagingManager.SendNamedMessage("LoginResponse", clientId, loginResponseBuffer);
                return;
            }

            
            else
                chatHandle.Profile = existingRegistration.Profile;
        }
        else
        {
            //UserProfile newUserProfile = new UserProfile(credentialElement.Name, );
            //UserRegistration newRegistration = new UserRegistration();
            UserProfile? newUserProfile;
            if (RegistryInstance.AddRegistration(credentialRequest, out newUserProfile))
                chatHandle = new ChatClientHandle(netClient, newUserProfile);
        }


        
        

        ConnectedHandles.Add(clientId, chatHandle);
        ChatMessage($"{chatHandle.ClientName} Joined the server!");

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
        if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out newClient))
        {
            Debug.LogError($"networkClient not found: {clientId}");
            return;
        }
        UserProfile guestProfile = new UserProfile($"Guest:{clientId}");
        Debug.Log($"GuestProfile Made. Name: {guestProfile.UserName}");
        ChatClientHandle guestHandle = new ChatClientHandle(newClient, guestProfile);

        if (ConnectedHandles.ContainsKey(clientId))
            ConnectedHandles[clientId] = guestHandle;

        else
            ConnectedHandles.Add(clientId, guestHandle);

        //string loginResponsePacked = ElementBoxHelper.PackElementTree(guestProfile.Box());
        //Debug.Log($"loginResponsePacked: {loginResponsePacked}");
        //FastBufferWriter loginResponseBuffered = new FastBufferWriter((int)Chattribute.LogData, Allocator.Persistent, (int)Chattribute.LogData);
        //
        //loginResponseBuffered.WriteValueSafe(loginResponsePacked);


        /////// TESTING //////////
        //NetworkManager.CustomMessagingManager.SendNamedMessage("Login", clientId, loginResponseBuffered);

        ChatMessage($"{guestHandle.ClientName} joined the server!");
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

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            ServerSetup();

        else
            ClientSetup();
    }

    public override void OnNetworkDespawn()
    {
        BuildAndAddLog("[SERVER END]");

        if (IsServer)
            ServerCleanUp();

        else
            ClientCleanUp();
    }

    private void ServerCleanUp()
    {
        Debug.Log($"Removed Me: {ConnectedHandles.Remove(AdminClientId)}");
        Debug.Log($"Remaining Handles: {ConnectedHandles.Count}");
        Debug.Log("Server Ended...");
    }

    private void ClientCleanUp()
    {

    }
    
    private void ServerSetup()
    {
        Debug.Log("Server Setup...");

        NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("Message", RecieveMessage);
        NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("LoginRequest", RecieveLoginUserRequest);
        NetworkManager.OnClientConnectedCallback += RegisterGuestChatClient;
        NetworkManager.OnClientDisconnectCallback += UnRegisterChatClient;

        ChatMessage("[SERVER START]");

        NetworkClient adminClient;
        if(!NetworkManager.ConnectedClients.TryGetValue(AdminClientId, out adminClient))
        {
            Debug.LogError($"adminClient not found: {AdminClientId}");
            return;
        }

        UserInstance = UserProfile.Admin;
        ChatClientHandle adminHandle = new ChatClientHandle(adminClient, UserInstance);

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

        NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("Log", RecieveChatLog);
        NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("LoginResponse", RecieveUserLoginResponse);
    }
    void Start()
    {
        LogInstance = new LogBook(ulong.MaxValue);
    }
    void Update()
    {

    }
}
