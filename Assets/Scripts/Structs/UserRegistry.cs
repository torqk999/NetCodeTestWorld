using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct UserRegistry : IElementBoxing
{
    private Dictionary<string, UserRegistration> _registrations;

    public static UserRegistry Null = new UserRegistry(true);
    public static UserRegistry Default = new UserRegistry(false);

    public UserRegistry(bool @null)
    {
        _registrations = null;
        if (!@null)
            _registrations = new Dictionary<string, UserRegistration>();
    }
    public UserRegistry(Element registryElement)
    {
        _registrations = new Dictionary<string, UserRegistration>();
        UnBox(registryElement);
    }

    public Dictionary<string, UserRegistration> Registrations => _registrations;
    public int Count => _registrations.Count;
    public UserRegistration this[string loginName] { get { return _registrations[loginName]; } set { _registrations[loginName] = value; } }

    public void Clear()
    {
        _registrations.Clear();
    }
    public bool TryGetValue(string loginName, out UserRegistration registration)
    {
        registration = UserRegistration.Null;
        if (_registrations == null)
            return false;
        return _registrations.TryGetValue(loginName, out registration);
    }

    public bool AddRegistration(UserCredential credential, out UserProfile? newProfile)
    {
        if (_registrations == null || _registrations.ContainsKey(credential.LoginName))
        {
            newProfile = null;
            return false;
        }

        UserRegistration newRegistration = new UserRegistration((ulong)_registrations.Count, credential);
        newProfile = newRegistration.Profile;
        _registrations.Add(credential.LoginName, newRegistration);
        return true;
    }

    public bool RemoveRegistration(UserCredential credential)
    {
        if (_registrations == null || !_registrations.ContainsKey(credential.LoginName))
            return false;

        // Do a swap with the last id so that unique id's are conserved.

        ulong biggestId = 0;
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
        }

        ///////

        _registrations.Remove(credential.LoginName);
        return true;
    }

    public Element Box(Element parent = null)
    {
        if (_registrations == null)
            return null;

        Element myElement = new Element(LogBookElement.Registry.ToString());

        foreach (UserRegistration registration in _registrations.Values)
            myElement.AddChildSafe(registration.Box());

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
