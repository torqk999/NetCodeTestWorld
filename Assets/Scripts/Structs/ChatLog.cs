using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
public class TimeCompare : Comparer<ChatLog>
{
    public override int Compare(ChatLog x, ChatLog y)
    {
        return DateTime.Compare(x.TimeStamp, y.TimeStamp);
    }
}

public struct ChatLog : IElementBoxing
{
    private const string null_message = "NULL_MESSAGE";
    private const string null_user_name = "NULL_USER_NAME";
    public DateTime TimeStamp { get; private set; }

    private FixedString512Bytes _message;
    private FixedString64Bytes _userName;
    private ulong? _clientID;
    private ulong? _userID;
    public string Message
    {
        get { return _message.ToString(); }
        private set { _message = new FixedString512Bytes(value ?? null_message); }
    }
    public string UserName
    {
        get { return _userName.ToString(); }
        private set { _userName = new FixedString64Bytes(value ?? null_user_name); }
    }
    public ulong? ClientID => _clientID;
    public ulong? UserID => _userID;

    public bool NameIsNull => _userName.ToString() == null_user_name;
    public bool MessageIsNull => _message.ToString() == null_message;

    public static ChatLog Null = new ChatLog(null_message);
    public ChatLog(string message, string userName, DateTime timeStamp, params ulong[] IDs)
    {
        TimeStamp = timeStamp;
        _message = new FixedString512Bytes(message == null ? null_message : message);
        _userName = new FixedString64Bytes(userName == null ? null_user_name : userName);
        try { _clientID = IDs[0]; } catch { _clientID = null; }
        try { _userID = IDs[1]; } catch { _userID = null; }
    }

    public ChatLog(string message, string userName, ulong userID) : this(message, userName, DateTime.Now, userID) { }
    //public ChatLog(string message, Logger logger) : this(message, logger.MyName, logger.OwnerClientId) { }
    public ChatLog(string message, ChatClientHandle handle) : this(message, handle.ClientName, handle.ClientId) { }
    public ChatLog(string message) : this(message, null, 0) { }

    public ChatLog(Element logElement)
    {
        _userName = new FixedString64Bytes(null_user_name);
        _message = new FixedString512Bytes(null_message);
        _clientID = null;
        _userID = null;
        TimeStamp = DateTime.Now;

        UnBox(logElement);
    }

    public static bool operator ==(ChatLog c1, ChatLog c2)
    {
        return c1.Equals(c2);
    }
    public static bool operator !=(ChatLog c1, ChatLog c2)
    {
        return !c1.Equals(c2);
    }
    public override bool Equals(object obj)
    {
        ChatLog? objCheck = (ChatLog)obj;
        if (!objCheck.HasValue)
            return false;
        ChatLog objCompare = objCheck.Value;

        return
            UserName == objCompare.UserName &&
            Message == objCompare.Message &&
            TimeStamp == objCompare.TimeStamp &&
            ClientID == objCompare.ClientID &&
            UserID == objCompare.UserID;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public void UnBox(Element logElement)
    {
        Debug.Log($"Unboxing chatlogElement...");
        try
        {
            int count = 0;
            foreach (KeyValuePair<string, List<string>> valuePair in logElement.Values)
            {
                LogBookElement element;

                count++;


                if (!ElementBoxHelper.TryParseCastEnum(valuePair.Key, out element))
                    continue;

                switch (element)
                {
                    case LogBookElement.Name:
                        _userName = new FixedString64Bytes(valuePair.Value[0]);
                        break;

                    case LogBookElement.Message:
                        _message = new FixedString512Bytes(valuePair.Value[0]);
                        break;

                    case LogBookElement.TimeStamp:

                        long timeStamp;
                        TimeStamp = long.TryParse(valuePair.Value[0], out timeStamp) ? new DateTime(timeStamp) : DateTime.Now;
                        break;

                    case LogBookElement.ID_Client:
                        ulong clientId;
                        _clientID = ulong.TryParse(valuePair.Value[0], out clientId) ? clientId : null;
                        break;

                    case LogBookElement.ID_User:
                        ulong userId;
                        _userID = ulong.TryParse(valuePair.Value[0], out userId) ? userId : null;
                        break;
                }
            }
        }
        catch { Debug.LogError("ChatLog Un-Boxing failed!"); }
    }

    public Element Box(Element parentBookElement = null)
    {
        try
        {
            Element myElement = new Element(LogBookElement.Log.ToString(), parentBookElement);
            myElement.AddValueSafe(LogBookElement.Name.ToString(), UserName);
            myElement.AddValueSafe(LogBookElement.Message.ToString(), Message);
            myElement.AddValueSafe(LogBookElement.TimeStamp.ToString(), TimeStamp.Ticks.ToString());
            myElement.AddValueSafe(LogBookElement.ID_Client.ToString(), ClientID.ToString());
            myElement.AddValueSafe(LogBookElement.ID_User.ToString(), UserID.ToString());
            return myElement;
        }
        catch { Debug.LogError("ChatLog Boxing failed!"); return null; }
    }
}
