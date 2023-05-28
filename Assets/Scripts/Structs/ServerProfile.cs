using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ServerProfile : IElementBoxing
{
    private string _serverName;
    private ulong? _serverId;
    private DateTime? _creation;
    private DateTime? _last_activation;
    private DateTime? _last_deactivation;
    public string ServerName => _serverName == null ? "Un-named Server" : _serverName;
    public ulong ServerId => _serverId.HasValue? _serverId.Value : ulong.MaxValue;
    public bool IsRegistered => _serverId.HasValue;

    //public static ServerProfile Default = new ServerProfile();
    public ServerProfile(string serverName = null, ulong? serverId = null, DateTime? creation = null, DateTime? lastActivation = null, DateTime? lastDeActivation = null)
    {
        _serverName = serverName;
        _serverId = serverId;
        _creation = creation;
        _last_activation = lastActivation;
        _last_deactivation = lastDeActivation;
    }
    public ServerProfile(Element serverElement)
    {
        _serverName = null;
        _serverId = null;
        _creation = null;
        _last_activation = null;
        _last_deactivation = null;
        UnBox(serverElement);
    }

    public Element Box(Element parent = null)
    {
        Element myElement = new Element(LogBookElement.Profile_Server.ToString(), parent);
        myElement.AddValueSafe(LogBookElement.Name.ToString(), _serverName);
        myElement.AddValueSafe(LogBookElement.ID_Server.ToString(), _serverId.HasValue ? _serverId.Value.ToString() : null);
        return myElement;
    }

    public void UnBox(Element serverElement)
    {
        try
        {
            foreach(KeyValuePair<string, List<string>> valuePair in serverElement.Values)
            {
                LogBookElement element;

                if (!ElementBoxHelper.TryParseCastEnum(valuePair.Key, out element))
                    continue;

                switch(element)
                {
                    case LogBookElement.Name:
                        _serverName = valuePair.Value[0];
                        break;

                    case LogBookElement.ID_Server:
                        ulong serverId;
                        _serverId = ulong.TryParse(valuePair.Value[0], out serverId) ? serverId : null;
                        break;
                }
            }
        }
        catch { Debug.Log("ServerProfile Un-Boxing failed!"); }
    }
}
