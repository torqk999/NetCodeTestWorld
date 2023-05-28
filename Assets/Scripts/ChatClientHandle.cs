using System.Text;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Only the server ever creates instances of these. They are the handshake between the
/// network client and the assumed logged in user. By default, when a server gets an
/// OnDetectedConnectedUser callback, it will create one with a null Profile by default,
/// which results in this handle being recognized as a Guest.
/// </summary>
public class ChatClientHandle
{
    public string ClientName => Profile.HasValue ? Profile.Value.UserName : $"Guest:{ClientId}";
    public ulong ClientId => NetClient == null ? ulong.MaxValue : NetClient.ClientId;
    public bool IsGuest => !Profile.HasValue;

    public NetworkClient NetClient;
    public UserProfile? Profile;

    public ChatClientHandle(NetworkClient client, UserProfile? profile = null)
    {
        Debug.Log($"Constructing Handle [client is null?:{client == null}]");
        NetClient = client;
        Profile = profile;
    }
    public ChatClientHandle(NetworkClient client) : this(client, null) { }
}
