using System;
using System.Collections.Generic;
using UnityEngine;

public interface IElementBoxing
{
    public void UnBox(Element consume);
    public Element Box(Element parent = null);
}

/*[Serializable]
public class FlexibleString : List<string>
{
    public FlexibleString() : base()
    {
    }

    public FlexibleString(IEnumerable<string> collection) : base(collection)
    {
    }

    public FlexibleString(int capacity) : base(capacity)
    {
    }

    public string Value
    {
        get { return Count < 1 ? null : this.[0]; }
        set { if (Count < 1) Add(value);
            else this.[0] = value; }
    }

}*/

[Serializable]
public class Element
{
    [SerializeField]
    private string _name;
    private Element _parent;

    //[SerializeField]
    //private List<Element> _childList = new List<Element>();

    private Dictionary<string, List<string>> _values = new Dictionary<string, List<string>>();
    private Dictionary<string, List<Element>> _children = new Dictionary<string, List<Element>>();

    public string Name => _name;
    public Element Parent => _parent;
    public Dictionary<string, List<string>> Values => _values;
    public Dictionary<string, List<Element>> Children => _children;
    public bool HasValue => _values.Count > 0;
    public bool HasChild => _children.Count > 0;

    public Element(string name, Element parent)
    {
        _parent = parent;
        _name = name != null ? name : parent != null ? parent.Children.Count.ToString() : null;
    }

    public void SetValues(Dictionary<string, List<string>> values = null)
    {
        _values.Clear();
        if (values != null)
        {
            foreach(KeyValuePair<string, List<string>> pair in values)
            {
                _values.Add(pair.Key, pair.Value);
            }
        }
    }

    public void AddChildSafe(Element newChild)
    {
        AddChildSafe(new List<Element>() { newChild });
    }

    public void AddChildSafe(List<Element> newChildren)
    {
        if (newChildren == null || newChildren.Count < 1)
            return;

        if (_children.ContainsKey(newChildren[0].Name))
            _children[newChildren[0].Name].AddRange(newChildren);

        else
        {
            List<Element> newList = new List<Element>();
            newList.AddRange(newChildren);
            _children.Add(newChildren[0].Name, newList);
        }     
    }

    public void AddValueSafe(string name, string value = null)
    {
        AddValueSafe(name, new List<string>() { value });
    }

    /// <summary>
    /// Not that safe yet...
    /// </summary>
    /// <param name="name">Name of value field</param>
    /// <param name="values">List of values</param>
    public void AddValueSafe(string name, List<string> values = null)
    {
        if (name == null || name == string.Empty)
            return;

        if (_values.ContainsKey(name))
            _values[name].AddRange(values);

        else
        {
            List<string> newList = new List<string>();
            newList.AddRange(values);
            _values.Add(name, newList);
        } 
    }
}
