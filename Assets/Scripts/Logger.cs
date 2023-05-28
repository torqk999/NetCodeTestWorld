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

    private Dictionary<ulong, ChatClientHandle> ConnectedClients = new Dictionary<ulong, ChatClientHandle>();
    private Dictionary<ulong, UserRegistration> KnownRegistrations = new Dictionary<ulong, UserRegistration>();
    private LogBook LogInstance;
    public UserProfile UserInstance { get; private set; }
    //List<ChatLog> Instance = new List<ChatLog>();

    public ChatClientHandle this[ulong clientId]
    {
        get { ChatClientHandle returnHandle; if (!ConnectedClients.TryGetValue(clientId, out returnHandle)) return null; return returnHandle; }
        set { try { ConnectedClients.Add(clientId, value); } catch { ConnectedClients[clientId] = value; } }
    }
    public ChatLog this[int index]
    {
        get { try { return LogInstance[index]; } catch { Debug.Log($"ChatLog: Out of Bounds index. Attempt: {index} | Count: {LogInstance.Count}"); return ChatLog.Null; } }
        set { try { LogInstance[index] = value; } catch { Debug.Log($"ChatLog: Out of Bounds index. Attempt: {index} | Count: {LogInstance.Count}"); } }
    }

    public bool IsAdmin => NetworkManager.IsHost || NetworkManager.IsServer;
    public bool IsGuest => UserInstance.IsGuest;
    public string MyName => UserInstance.UserName;// == null? $"Guest:{OwnerClientId}" : UserInstance.UserName;
    public int InstanceCount => LogInstance.Count;
    public List<NetworkObject> ClientOwnedObjects => NetworkManager.LocalClient.OwnedObjects;

    public static int StringSizeBytes(string input) // Apparantly .Net says so.... https://www.red-gate.com/simple-talk/blogs/how-big-is-a-string-in-net/#:~:text=A%20string%20is%20composed%20of,a%204%2Dbyte%20type%20descriptor)
    {
        return 18 + (input.Length * 2);
    }

    #region Client
    public void SetClientName(string newName)
    {
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
        NetworkManager.CustomMessagingManager.SendNamedMessage("Login", AdminClientId, credentialBuffer);
    }
    public void RecieveChatLog(ulong clientId, FastBufferReader buffer)
    {
        string packedLog;
        buffer.ReadValueSafe(out packedLog);
        int elementCount;
        Element logElement = ElementBoxHelper.BuildElementTree(packedLog, Enum.GetNames(typeof(LogBookElement)), out elementCount);
        Debug.Log($"log boxed. Final element count: {elementCount}");
        LogInstance.AddLog(new ChatLog(logElement));
    }
    public void RecieveUserLoginResponse(ulong clientId, FastBufferReader buffer)
    {
        string packedLogin;
        buffer.ReadValueSafe(out packedLogin);

        int elementCount;
        Element loginTokenElement = ElementBoxHelper.BuildElementTree(packedLogin, Enum.GetNames(typeof(LogBookElement)), out elementCount);
        UserInstance = new UserProfile(loginTokenElement);

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

        int elementCount;
        string rawStream = ServerDataBase.LoadDefaultLogBookFile();

        Debug.Log($"StreamLength: {rawStream.Length}");

        TestElement = ElementBoxHelper.BuildElementTree(rawStream, elementLegend, out elementCount);

        Debug.Log($"ElementCount: {elementCount}");
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
        if (!ConnectedClients.TryGetValue(clientId, out chatHandle))
        {
            Debug.LogError($"chatHandle not found: {clientId}");
            return;
        }

        string message;
        buffer.ReadValueSafe(out message);

        BuildAndDistributeLog(message, chatHandle);

    }

    /// <summary>
    /// Server will build the initial log, pack, and then distribute to all clients on the server
    /// </summary>
    /// <param name="message">The string message.</param>
    /// <param name="handle">The handle who sent the message to be logged. A null reference assumes it was meant to be a message made by the server.</param>
    private void BuildAndDistributeLog(string message, ChatClientHandle handle = null)
    {
        Debug.Log($"ServerHandle: {ServerHandle} | AdminClientId: {AdminClientId}");
        ChatLog newLog = handle != null ? new ChatLog(message, handle) : new ChatLog(message, ServerHandle, AdminClientId);
        FastBufferWriter instanceLog = new FastBufferWriter((int)Chattribute.LogData, Allocator.Persistent, (int)Chattribute.LogData);

        string packedLog = ElementBoxHelper.PackElementTree(newLog.Box());
        Debug.Log($"PackedLog: {packedLog}");
        instanceLog.WriteValueSafe(packedLog);

        NetworkManager.CustomMessagingManager.SendNamedMessageToAll("Log", instanceLog);
        LogInstance.AddLog(newLog);
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



        // <<<<---- Add Credential Logic

        //string name;
        //buffer.ReadValueSafe(out name);
        //Debug.Log($"name: [{name}]");

        ChatClientHandle chatHandle = new ChatClientHandle(netClient);
        ConnectedClients.Add(clientId, chatHandle);
        ChatMessage($"{chatHandle.ClientName} Joined the server!");

    }
    public void RecieveLogoutUserRequest(ulong clientId, FastBufferReader credentials)
    {

    }

    public void RegisterGuestChatClient(ulong clientId)
    {
        NetworkClient newClient;
        if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out newClient))
        {
            Debug.LogError($"networkClient not found: {clientId}");
            return;
        }
        UserProfile guestProfile = new UserProfile($"Guest:{clientId}");
        Debug.Log($"GuestProfile Made. Name: {guestProfile.UserName}");
        ChatClientHandle guestHandle = new ChatClientHandle(newClient, guestProfile);

        if (ConnectedClients.ContainsKey(clientId))
            ConnectedClients[clientId] = guestHandle;

        else
            ConnectedClients.Add(clientId, guestHandle);

        string loginResponsePacked = ElementBoxHelper.PackElementTree(guestProfile.Box());
        Debug.Log($"loginResponsePacked: {loginResponsePacked}");
        FastBufferWriter loginResponseBuffered = new FastBufferWriter((int)Chattribute.LogData, Allocator.Persistent, (int)Chattribute.LogData);

        loginResponseBuffered.WriteValueSafe(loginResponsePacked);

        NetworkManager.CustomMessagingManager.SendNamedMessage("Login", clientId, loginResponseBuffered);

        ChatMessage($"{guestHandle.ClientName} joined the server!");
    }

    public void UnRegisterChatClient(ulong clientId)
    {
        ChatClientHandle chatHandle;
        if(!ConnectedClients.TryGetValue(clientId, out chatHandle))
        {
            Debug.LogError($"chatHandle not found: {clientId}");
            return;
        }
        ChatMessage($"{chatHandle.ClientName} left the server!");
        ConnectedClients.Remove(clientId);
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
        if (IsServer)
        {
            Debug.Log($"Removed Me: {ConnectedClients.Remove(AdminClientId)}");
            Debug.Log($"Remaining Handles: {ConnectedClients.Count}");
            Debug.Log("Server Ended...");
        }
    }
    
    private void ServerSetup()
    {
        Debug.Log("Server Setup...");

        NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("Message", RecieveMessage);
        NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("Login", RecieveLoginUserRequest);
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

        if (ConnectedClients.ContainsKey(AdminClientId))
            ConnectedClients[AdminClientId] = adminHandle;

        else
            ConnectedClients.Add(AdminClientId, adminHandle);

        ChatMessage($"{adminHandle.ClientName} joined the server!");
    }
    private void ClientSetup()
    {
        Debug.Log("Client Setup...");

        NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("Log", RecieveChatLog);
        NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler("Login", RecieveUserLoginResponse);
    }
    void Start()
    {
        LogInstance = new LogBook(ulong.MaxValue);
    }
    void Update()
    {

    }
}
