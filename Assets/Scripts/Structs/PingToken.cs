using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PingToken : IElementBoxing
{
    public UserList? RequestedList;
    public UserProfile? RequestedProfile;
    public ServerProfile? RequestedServerInfo;
    public ResponseCode Response;

    public PingToken(ServerProfile server, ResponseCode code = ResponseCode.Guest_Connection)
    {
        RequestedList = null;
        RequestedProfile = null;
        RequestedServerInfo = server;
        Response = code;
    }
    public PingToken(ResponseCode code, UserList? list = null, UserProfile? profile = null, ServerProfile? server = null)
    {
        RequestedList = list;
        RequestedProfile = profile;
        RequestedServerInfo = server;
        Response = code;
    }

    public PingToken(Element loginElement)
    {
        RequestedList = null;
        RequestedProfile = null;
        RequestedServerInfo = null;
        Response = default;
        UnBox(loginElement);
    }

    public Element Box(Element parent = null)
    {
        Element myElement = new Element(LogBookElement.Response.ToString(), parent);
        if (RequestedList.HasValue) myElement.AddChildSafe(RequestedList.Value.Box());
        if (RequestedProfile.HasValue) myElement.AddChildSafe(RequestedProfile.Value.Box());
        if (RequestedServerInfo.HasValue) myElement.AddChildSafe(RequestedServerInfo.Value.Box());
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
                        RequestedProfile = new UserProfile(childPair.Value[0]);
                        break;

                    case LogBookElement.Profile_Server:
                        RequestedServerInfo = new ServerProfile(childPair.Value[0]);
                        break;
                }
            }
        }
        catch { Debug.Log("UserLoginToken Un-Boxing failed!"); }
    }
}
