using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct UserCredential : IElementBoxing
{
    private string _loginName;
    private string _password;

    public string LoginName => _loginName;
    public bool IsBadCredential => _loginName == null || _password == null;
    public UserCredential(string loginName, string password)
    {
        _loginName = loginName;
        _password = password;
    }

    public UserCredential(Element credentialElement)
    {
        _loginName = null;
        _password = null;
        UnBox(credentialElement);
    }

    public Element Box(Element parent = null)
    {
        Element myElement = new Element(LogBookElement.Credential.ToString(), parent);
        myElement.AddValueSafe(LogBookElement.Name.ToString(), _loginName);
        myElement.AddValueSafe(LogBookElement.Password.ToString(), _password);
        return myElement;
    }

    public void UnBox(Element credentialElement)
    {
        try
        {
            foreach(KeyValuePair<string, List<string>> valuePair in credentialElement.Values)
            {
                LogBookElement element;

                if (!ElementBoxHelper.TryParseCastEnum(valuePair.Key, out element))
                    continue;

                switch(element)
                {
                    case LogBookElement.Name:
                        _loginName = valuePair.Value[0];
                        break;

                    case LogBookElement.Password:
                        _password = valuePair.Value[0];
                        break;
                }
            }
            
        }
        catch { Debug.Log("Credential Un-Boxing failed!"); }
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
