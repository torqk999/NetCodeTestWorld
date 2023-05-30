using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct UserRegistry : IElementBoxing
{
    private Dictionary<string, UserRegistration> _registrations;
    private List<ulong> _recyleBin;

    public static UserRegistry Null = new UserRegistry(true);
    public static UserRegistry Default = new UserRegistry(false);

    public UserRegistry(bool @null)
    {
        _registrations = null;
        _recyleBin = null;
        if (!@null)
            _registrations = new Dictionary<string, UserRegistration>();
    }
    public UserRegistry(Element registryElement)
    {
        _registrations = new Dictionary<string, UserRegistration>();
        _recyleBin = new List<ulong>();
        UnBox(registryElement);
    }

    public Dictionary<string, UserRegistration> Registrations => _registrations;
    public int Count => _registrations.Count;
    public UserRegistration this[string loginName] { get { return _registrations[loginName]; } set { _registrations[loginName] = value; } }

    public void Clear()
    {
        _registrations.Clear();
    }
    public bool TryGetRegistration(ulong userId, out UserRegistration? result)
    {
        result = null;
        if (_registrations == null)
            return false;
        
        foreach(UserRegistration reg in _registrations.Values)
            if (reg.Profile.UserID == userId)
            {
                result = reg;
                return true;
            }
                
        return false;
    }
    public bool TryGetRegistration(string loginName, out UserRegistration? output)
    {
        output = null;
        if (_registrations == null)
            return false;
        UserRegistration reg;
        bool result = _registrations.TryGetValue(loginName, out reg);
        output = result? reg : null;
        return result;
    }

    public bool AddRegistration(UserCredential credential, out UserRegistration? newRegistration)
    {
        if (_registrations == null || _registrations.ContainsKey(credential.LoginName))
        {
            newRegistration = null;
            return false;
        }

        ulong newUserId;

        if (_recyleBin != null && _recyleBin.Count > 0)
            newUserId = _recyleBin[0];

        else
            newUserId = (ulong)_registrations.Count;

        newRegistration = new UserRegistration(newUserId, credential);
        _registrations.Add(credential.LoginName, newRegistration.Value);
        return true;
    }

    public bool RemoveRegistration(UserCredential credential)
    {
        if (_registrations == null || !_registrations.ContainsKey(credential.LoginName))
            return false;

        UserRegistration removeRequest = _registrations[credential.LoginName];

        _recyleBin.Add(removeRequest.Profile.UserID);
        _registrations.Remove(credential.LoginName);

        // Do a swap with the last id so that unique id's are conserved.

        /*ulong biggestId = 0;
        string biggestLoginName = null; // Get the user with the biggest Id
        foreach (UserRegistration reg in _registrations.Values)
        {
            if (reg.Profile.UserID > biggestId)
            {
                biggestId = reg.Profile.UserID;
                biggestLoginName = reg.Credential.LoginName;
                break;
            }
        }

        UserRegistration lastRegistrationId;
        if (biggestLoginName != null && _registrations.TryGetValue(biggestLoginName, out lastRegistrationId))
        {
            lastRegistrationId.Profile.ChangeProfile(_registrations[credential.LoginName].Profile.UserID);
        }*/

        ///////

        //_registrations.Remove(credential.LoginName);
        return true;
    }

    public Element Box(Element parent = null)
    {
        if (_registrations == null)
            return null;

        Element myElement = new Element(LogBookElement.Registry.ToString(), parent);

        foreach (UserRegistration registration in _registrations.Values)
            myElement.AddChildSafe(registration.Box());

        foreach (ulong recycle in _recyleBin)
            myElement.AddValueSafe(LogBookElement.ID_Recycle.ToString(), recycle.ToString());
        

        return myElement;
    }

    public void UnBox(Element registryElement)
    {
        try
        {
            if (!registryElement.Children.ContainsKey(LogBookElement.Registration.ToString()))
            {
                return;
            }

            List<Element> registrationElements = registryElement.Children[LogBookElement.Registration.ToString()];

            foreach (Element registrationElement in registrationElements)
            {
                UserRegistration registration = new UserRegistration(registrationElement);
                _registrations.Add(registration.Credential.LoginName, registration);
            }
        }
        catch { Debug.Log("Registry Un-Boxing Failed!"); }
    }
}
