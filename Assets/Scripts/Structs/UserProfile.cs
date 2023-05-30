using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct UserProfile : IElementBoxing
{
    private string _userName;
    private ulong? _userId;

    public string UserName => _userName == null ? "[Name not set]" : _userName;
    public ulong UserID => _userId == null ? ulong.MaxValue : _userId.Value;
    public bool IsGuest => !_userId.HasValue;

    public static UserProfile Admin = new UserProfile(Logger.AdminHandle, Logger.AdminClientId);
    public static UserProfile Null = new UserProfile();
    
    public UserProfile(ulong guestId)
    {
        _userName = $"Guest:{guestId}";
        _userId = null;
    }

    public UserProfile(string clientName = null, ulong? userId = null, ServerProfile? server = null)
    {
        _userName = clientName;
        _userId = userId;
        //_server = server;
    }

    public UserProfile(Element profileElement)
    {
        _userName = null;
        _userId = null;
        UnBox(profileElement);
    }

    public void ChangeProfile(ulong? newUserId)
    {
        _userId = newUserId;
    }

    public void ChangeProfile(string newName)
    {
        _userName = newName;
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
