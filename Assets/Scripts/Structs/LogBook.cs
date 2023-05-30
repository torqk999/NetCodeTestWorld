using System;
using System.Collections.Generic;
using UnityEngine;

public struct LogBook : IElementBoxing
{
    private int MAX_LOGS;
    private List<ChatLog> _logs;
    //public ulong ServerId { get; private set; } // Expected to be permanent.
    //public DateTime TimeOfCreation { get; private set; } // Expected to be permanent.
    private DateTime _timeOfCreation;
    public DateTime FirstLogTime => _logs.Count < 1 ? _timeOfCreation : _logs[0].TimeStamp;
    public DateTime OldestLogTime => _logs.Count < 1 ? FirstLogTime : _logs[_logs.Count - 1].TimeStamp;
    public int Count => _logs.Count;
    public ChatLog this[int i] { get { return _logs[i]; } set { _logs[i] = value; } }

    public string GetName<T>(T enumValue)
    {
        return Enum.GetName(typeof(T), enumValue);
    }

    public LogBook(Element bookElement, int maxLogs = Logger.LOG_INSTANCE_MAX)
    {
        MAX_LOGS = maxLogs;
        _logs = new List<ChatLog>();
        _timeOfCreation = DateTime.Now;

        UnBox(bookElement);
    }
    public LogBook(int maxLogs = Logger.LOG_INSTANCE_MAX)
    {
        MAX_LOGS = maxLogs;
        _timeOfCreation = DateTime.Now;
        _logs = new List<ChatLog>();
    }
    //public LogBook(ulong id) : this(id, DateTime.Now) { }
    public void AddLog(ChatLog log)
    {
        Debug.Log("Adding Log...");
        if (DateTime.Compare(log.TimeStamp, FirstLogTime) < 0) // Refuse logs before book creation
            return;

        bool sort = DateTime.Compare(log.TimeStamp, OldestLogTime) < 0; // Check if added to end
        _logs.Add(log);

        if (sort)
            _logs.Sort(new TimeCompare());
    }
    public void RemoveLog(ChatLog log)
    {
        if (DateTime.Compare(log.TimeStamp, FirstLogTime) < 0) // Won't be found before creation
            return;

        int index = _logs.FindIndex(x => x == log);
        if (index < 0) // Wasn't found
            return;

        _logs.RemoveAt(index);
    }
    public void ClearLogs()
    {
        _logs.Clear();
    }

    public void UnBox(Element bookElement)
    {
        try
        {
            /*foreach (KeyValuePair<string, List<string>> valuePair in bookElement.Values)
            {
                LogBookElement element;

                if (!ElementBoxHelper.TryParseCastEnum(valuePair.Key, out element))
                    continue;

                switch (element)
                {
                    case LogBookElement.TimeStamp:
                        TimeOfCreation = new DateTime(long.Parse(valuePair.Value[0]));
                        break;

                    //case LogBookElement.ID_Server:
                    //    ServerId = ulong.Parse(valuePair.Value[0]);
                    //    break;

                    default:
                        break;
                }
            }*/

            foreach (KeyValuePair<string, List<Element>> childPair in bookElement.Children)
            {
                LogBookElement element;// = (LogBookElement)objOut;

                if (!ElementBoxHelper.TryParseCastEnum(childPair.Key, out element))
                    continue;

                if (element != LogBookElement.Log)
                    continue;

                if (childPair.Value == null)
                    continue;

                foreach(Element logElement in childPair.Value)
                    _logs.Add(new ChatLog(logElement));
            }

        }
        catch
        {
            Debug.LogError("LogBook Un-Boxing failed!");
        }
    }

    public Element Box(Element parent = null)
    {
        try
        {
            Element myElement = new Element(LogBookElement.Book.ToString(), parent);

            //myElement.AddValueSafe(LogBookElement.TimeStamp.ToString(), TimeOfCreation.Ticks.ToString());
            //myElement.AddValueSafe(LogBookElement.ID_Server.ToString(), ServerId.ToString());

            foreach (ChatLog log in _logs)
                myElement.AddChildSafe(log.Box(myElement));

            Debug.Log($"Log Count: {_logs.Count}/{myElement.Children.Count}");

            return myElement;
        }
        catch { Debug.LogError("LogBook Boxing failed!"); return null; }
    }
}
