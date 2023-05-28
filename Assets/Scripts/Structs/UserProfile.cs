using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ServerProfile : IElementBoxing
{
    private string _serverName;
    private ulong? _serverId;
    public string ServerName => _serverName == null ? "Un-named Server" : _serverName;
    public ulong ServerId => _serverId.HasValue? _serverId.Value : ulong.MaxValue;
    public bool IsRegistered => _serverId.HasValue;

    public static ServerProfile Default = new ServerProfile();
    public ServerProfile(string serverName = null, ulong? serverId = null)
    {
        _serverName = serverName;
        _serverId = serverId;
    }
    public ServerProfile(Element serverElement)
    {
        _serverName = null;
        _serverId = null;
        UnBox(serverElement);
    }

    public Element Box(Element parent = null)
    {
        throw new NotImplementedException();
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

[Serializable]
public struct UserRegistration : IElementBoxing
{
    public DateTime TimeOfRegistration;
    public UserProfile Profile;
    private UserCredential _credential;
    //public UserProfile Profile => _profile;

    public bool CheckCredential(UserCredential check)
    {
        return _credential == check;
    }

    public Element Box(Element parent = null)
    {
        throw new NotImplementedException();
    }

    public void UnBox(Element consume)
    {
        throw new NotImplementedException();
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
    public static bool operator ==(UserRegistration c1, UserRegistration c2)
    {
        return c1.Equals(c2);
    }
    public static bool operator !=(UserRegistration c1, UserRegistration c2)
    {
        return c1.Equals(c2);
    }
}

[Serializable]
public struct UserCredential : IElementBoxing
{
    public string _loginName { get; private set; }
    public string _password { get; private set; }

    public UserCredential(string loginName, string password)
    {
        _loginName = loginName;
        _password = password;
    }

    public Element Box(Element parent = null)
    {
        Element myElement = new Element(LogBookElement.Credential.ToString());
        myElement.AddValueSafe(LogBookElement.Name.ToString(), _loginName);
        myElement.AddValueSafe(LogBookElement.Password.ToString(), _password);
        return myElement;
    }

    public void UnBox(Element consume)
    {
        try
        {

        }
        catch { }
    }

    public override bool Equals(object obj)
    {
        if (!(obj is UserCredential))
            return false;
        UserCredential compare = (UserCredential)obj;
        return _loginName == compare._loginName && _password == compare._password;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public static bool operator ==(UserCredential c1, UserCredential c2)
    {
        return c1.Equals(c2);
    }

    public static bool operator !=(UserCredential c1, UserCredential c2)
    {
        return c1.Equals(c2);
    }
}

[Serializable]
public struct UserLoginToken : IElementBoxing
{
    public UserProfile Profile;
    public ServerProfile Server;

    public UserLoginToken(UserProfile profile, ServerProfile server)
    {
        Profile = profile;
        Server = server;
    }

    public UserLoginToken(Element loginElement)
    {
        Profile = UserProfile.Default;
        Server = ServerProfile.Default;
        UnBox(loginElement);
    }

    public Element Box(Element parent = null)
    {
        throw new NotImplementedException();
    }

    public void UnBox(Element loginElement)
    {
        try
        {
            foreach(KeyValuePair<string, List<Element>> childPair in loginElement.Children)
            {
                LogBookElement element;

                if (!ElementBoxHelper.TryParseCastEnum(childPair.Key, out element))
                    continue;

                switch(element)
                {
                    case LogBookElement.Profile_User:
                        Profile = new UserProfile(childPair.Value[0]);
                        break;

                    case LogBookElement.Profile_Server:
                        Server = new ServerProfile(childPair.Value[0]);
                        break;
                }
            }
        }
        catch { Debug.Log("UserLoginToken Un-Boxing failed!"); }
    }
}

[Serializable]
public struct UserProfile : IElementBoxing
{
    private string _userName;
    private ulong? _userId;
    //private ServerProfile? _server;
    
    public string UserName => _userName == null ? "[Not Signed In]" : _userName;
    public ulong UserID => _userId == null ? ulong.MaxValue : _userId.Value;
    //public ServerProfile Server => _server.HasValue? _server.Value : ServerProfile.UnRegistered;
    public bool IsGuest => !_userId.HasValue;

    public static UserProfile Admin = new UserProfile(Logger.AdminHandle, Logger.AdminClientId);
    public static UserProfile Default = new UserProfile("[Unknown user]");
    
    public UserProfile(ulong guestId)
    {
        _userName = $"Guest:{guestId}";
        _userId = null;
    }

    public UserProfile(string clientName, ulong? userId = null, ServerProfile? server = null)
    {
        _userName = clientName;
        _userId = userId;
        //_server = server;
    }

    public UserProfile(Element profileElement)
    {
        _userName = null;
        _userId = null;
        //_server = null;
        UnBox(profileElement);
    }

    public void UnBox(Element profileElement)
    {
        try
        {
            foreach (KeyValuePair<string, List<string>> valuePair in profileElement.Values)
            {
                LogBookElement element;

                if (!ElementBoxHelper.TryParseCastEnum(valuePair.Key, out element))
                    continue;

                switch (element)
                {
                    case LogBookElement.Name:
                        _userName = valuePair.Value[0];
                        break;

                    case LogBookElement.ID_User:
                        Debug.Log("====DEBUG====");
                        Debug.Log($"Unboxing profile user id. value is null: {valuePair.Value[0] == null}");
                        ulong userId;
                        _userId = ulong.TryParse(valuePair.Value[0], out userId)? userId : null;
                        break;
                }
            }
        }
        catch { Debug.Log("UserProfile Un-Boxing failed!"); }
    }
    public Element Box(Element parent = null)
    {
        Element profileElement = new Element(LogBookElement.Profile_User.ToString(), parent);
        profileElement.AddValueSafe(LogBookElement.Name.ToString(), _userName);
        profileElement.AddValueSafe(LogBookElement.ID_User.ToString(), _userId.HasValue? _userId.Value.ToString() : null);
        return profileElement;
    }
}
