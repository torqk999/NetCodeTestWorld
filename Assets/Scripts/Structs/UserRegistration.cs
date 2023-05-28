using System;

[Serializable]
public struct UserRegistration : IElementBoxing
{
    public DateTime TimeOfRegistration { get; private set; }
    public UserProfile Profile { get; private set; }
    public UserCredential Credential { get; private set; }

    public UserRegistration(ulong userId, UserCredential credential)
    {
        TimeOfRegistration = DateTime.Now;
        Profile = new UserProfile(credential.LoginName, userId);
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
