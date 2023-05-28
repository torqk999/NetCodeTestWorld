using System;
using System.Collections.Generic;

[Serializable]
public struct UserRegistry : IElementBoxing
{
    private Dictionary<string, UserRegistration> _registrations;

    public UserRegistry(int capacity = 0)
    {
        _registrations = new Dictionary<string, UserRegistration>(capacity);
    }
    public UserRegistry(Element registryElement)
    {
        _registrations = new Dictionary<string, UserRegistration>();
        UnBox(registryElement);
    }

    public Dictionary<string, UserRegistration> Registrations => _registrations;
    public int Count => _registrations.Count;
    public UserRegistration this[string loginName] { get { return _registrations[loginName]; } set { _registrations[loginName] = value; } }

    public bool AddRegistration(UserCredential credential, out UserProfile? newProfile)
    {
        if (_registrations.ContainsKey(credential.LoginName))
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
        if (!_registrations.ContainsKey(credential.LoginName))
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
        throw new NotImplementedException();
    }

    public void UnBox(Element consume)
    {
        throw new NotImplementedException();
    }
}
