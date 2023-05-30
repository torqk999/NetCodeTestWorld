using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct UserRegistration : IElementBoxing
{
    public DateTime TimeOfRegistration { get; private set; }
    public UserProfile Profile { get; private set; }
    public UserCredential Credential { get; private set; }

    //public static readonly UserRegistration Admin = new UserRegistration(Se UserProfile.Admin, UserCredential.Null);
    public static readonly UserRegistration Null = new UserRegistration(ServerProfile.Null, UserProfile.Null, UserCredential.Null);
    
    public UserRegistration(ServerProfile server, UserProfile profile, UserCredential credential)
    {
        TimeOfRegistration = DateTime.Now;
        Profile = profile;
        Credential = credential;
    }
    public UserRegistration(ServerProfile server, ulong userId, UserCredential credential)
    {
        TimeOfRegistration = DateTime.Now;
        Profile = new UserProfile(server, credential.LoginName, userId);
        Credential = credential;
    }

    public UserRegistration(Element registrationElement)
    {
        TimeOfRegistration = DateTime.Now;
        Profile = new UserProfile();
        Credential = new UserCredential();
        UnBox(registrationElement);
    }

    public bool CheckCredential(UserCredential check)
    {
        return Credential == check;
    }

    public Element Box(Element parent = null)
    {
        Element myElement = new Element(LogBookElement.Registration.ToString(), parent);

        myElement.AddValueSafe(LogBookElement.TimeStamp.ToString(), TimeOfRegistration.Ticks.ToString());
        myElement.AddChildSafe(Profile.Box());
        myElement.AddChildSafe(Credential.Box());

        return myElement;
    }

    public void UnBox(Element registrationElement)
    {
        try
        {
            LogBookElement element;

            foreach (KeyValuePair<string, List<Element>> childPair in registrationElement.Children)
            {
                if (!ElementBoxHelper.TryParseCastEnum(childPair.Key, out element))
                    continue;

                switch(element)
                {
                    case LogBookElement.Profile_User:
                        Profile = new UserProfile(childPair.Value[0]);
                        break;

                    case LogBookElement.Credential:
                        Credential = new UserCredential(childPair.Value[0]);
                        break;
                }
            }

            if (!registrationElement.Values.ContainsKey(LogBookElement.TimeStamp.ToString()))
                return;

            TimeOfRegistration = new DateTime(long.Parse(registrationElement.Values[LogBookElement.TimeStamp.ToString()][0]));
        }
        catch { Debug.Log("Registration Un-Boxing failed!"); }
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
