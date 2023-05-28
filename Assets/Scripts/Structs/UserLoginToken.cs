using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct UserLoginToken : IElementBoxing
{
    public UserProfile Profile;
    public ServerProfile Server;
    public CustomResponseCode LoginResponse;

    public UserLoginToken(ServerProfile server, CustomResponseCode code = CustomResponseCode.Guest_Connection)
    {
        Profile = new UserProfile();
        Server = server;
        LoginResponse = code;
    }
    public UserLoginToken(UserProfile profile, ServerProfile server, CustomResponseCode code)
    {
        Profile = profile;
        Server = server;
        LoginResponse = code;
    }

    public UserLoginToken(Element loginElement)
    {
        Profile = new UserProfile();
        Server = new ServerProfile();
        LoginResponse = default;
        UnBox(loginElement);
    }

    public Element Box(Element parent = null)
    {
        Element myElement = new Element(LogBookElement.Login.ToString(), parent);
        myElement.AddChildSafe(Profile.Box());
        myElement.AddChildSafe(Server.Box());
        myElement.AddValueSafe(LogBookElement.Code_Response.ToString(), LoginResponse.ToString());
        return myElement;
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
