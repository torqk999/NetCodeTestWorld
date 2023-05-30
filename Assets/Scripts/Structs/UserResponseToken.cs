using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct UserResponseToken : IElementBoxing
{
    public UserProfile Profile;
    public ServerProfile Server;
    public ResponseCode Response;

    public UserResponseToken(ServerProfile server, ResponseCode code = ResponseCode.Guest_Connection)
    {
        Profile = new UserProfile();
        Server = server;
        Response = code;
    }
    public UserResponseToken(UserProfile profile, ServerProfile server, ResponseCode code)
    {
        Profile = profile;
        Server = server;
        Response = code;
    }

    public UserResponseToken(Element loginElement)
    {
        Profile = new UserProfile();
        Server = new ServerProfile();
        Response = default;
        UnBox(loginElement);
    }

    public Element Box(Element parent = null)
    {
        Element myElement = new Element(LogBookElement.Response.ToString(), parent);
        myElement.AddChildSafe(Profile.Box());
        myElement.AddChildSafe(Server.Box());
        myElement.AddValueSafe(LogBookElement.Code_Response.ToString(), Response.ToString());
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
