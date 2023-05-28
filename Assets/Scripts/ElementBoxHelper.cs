using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class ElementBoxHelper
{

    #region Tokens
    public const char CodonOpen    = '<';
    public const char CodonClose   = '>';
    public const char Label        = '@';
    public const char Value        = '#';
    public const char Terminate    = '\\';
    public const char ValueSplit   = ':';
    public static readonly char[] Tokens = new char[]
    {
        CodonOpen,
        CodonClose,
        Label,
        Value,
        Terminate,
        ValueSplit
    };
    #endregion
    #region ParserVars
    private static StringBuilder _name_buffer = new StringBuilder();
    private static StringBuilder _value_buffer = new StringBuilder();
    private static int EchelonCount = 0;
    private static bool In_Token_Block;
    private static bool Push_To_Name;
    private static bool Push_To_Value;
    private static bool Split_Value;
    private static bool Resolve_Token;
    private static bool Expecting_Code;
    private static bool Expecting_Value;
    #endregion
    public static int MAX_STREAM_LENGTH = 2000;

    public static bool TryParseCastEnum<T>(string input, out T result) where T : Enum
    {
        result = default;
        object resObj;
        if (!Enum.TryParse(typeof(T), input, out resObj))
            return false;

        try { result = (T)resObj; return true; }
        catch { return false; }
    }
    public static bool CheckForElementName(string nameCache, string[] elementLegend)
    {
        if (elementLegend != null)
            foreach (string elementName in elementLegend)
                if (elementName == nameCache)
                    return true;
        return false;
    }
    public static bool CheckForToken(char myChar)
    {
        foreach (char token in Tokens)
            if (token == myChar)
                return true;
        return false;
    }
    public static string CleanAttributeName(string desiredName)
    {
        if (desiredName == null)
            return "NULL_ATTRIBUTE_NAME";

        foreach (char token in Tokens)
            desiredName = desiredName.Replace(token.ToString(), "");

        if (desiredName.Length < 1)
            return "UNKNOWN_ATTRIBUTE_NAME";

        return desiredName;
    }

    public static string PackElementTree(Element root)
    {
        Debug.Log($"Packing ElementTree. Root: {root.Name}");
        return PackElement(root);
    }
    private static string PackValue(KeyValuePair<string, List<string>> valuePair)
    {
        string echelon = string.Empty;
        StringBuilder _packer = new StringBuilder();
        for (int i = 0; i < EchelonCount; i++)
            echelon += '\t';

        _packer.Append($"{echelon}{CodonOpen}{Value}{valuePair.Key??"NULL"}{CodonClose}");
        if (valuePair.Value == null || valuePair.Value.Count < 1)
        {
            _packer.Append($"NULL{CodonOpen}{Terminate}{valuePair.Key ?? "NULL"}{CodonClose}");
            return _packer.ToString();
        }

        _packer.Append($"{valuePair.Value[0]}");

        for (int i = 1; i < valuePair.Value.Count; i++)
            _packer.Append($"{CodonOpen}{Split_Value}\n{echelon}{CodonClose}{valuePair.Value[i]}");

        _packer.Append($"{CodonOpen}{Terminate}{valuePair.Key}{CodonClose}\n");
        return _packer.ToString();
        
    }
    private static string PackElement(Element next)
    {
        string echelon = string.Empty;
        StringBuilder _packer = new StringBuilder();
        for (int i = 0; i < EchelonCount; i++)
            echelon += '\t';

        _packer.Append($"{echelon}{CodonOpen}{Label}{next.Name}{CodonClose}\n");

        EchelonCount++;

        if (next.HasValue)
            foreach(KeyValuePair<string, List<string>> valuePair in next.Values)
                _packer.Append(PackValue(valuePair));
            
        if (next.HasChild)
            foreach (KeyValuePair<string, List<Element>> childPair in next.Children)
                if (childPair.Value != null)
                    foreach(Element childElement in childPair.Value)
                        _packer.Append(PackElement(childElement));

        EchelonCount--;

        _packer.Append($"{echelon}{CodonOpen}{Terminate}{next.Name}{CodonClose}\n");

        return _packer.ToString();
    }


    /// <summary>
    /// Returns the root of an element tree produced from a custom xml-styled text (.txt) file.
    /// </summary>
    /// <param name="dataStream">The stream from the text file.</param>
    /// <param name="elementLegend">All expected element names.</param>
    /// <param name="elementCount">The total amount of elements populated in tree.</param>
    /// <returns></returns>
    public static Element BuildElementTree(string dataStream, string[] elementLegend)//, out int elementCount)
    {
        Dictionary<string, List<string>> valuesCatalogue = new Dictionary<string, List<string>>();
        List<string> valueBuffer = new List<string>();

        int codonSize = 0;
        //elementCount = 0;

        Element currentBufferedElement = null;

        for (int i = 0; i < dataStream.Length && i < MAX_STREAM_LENGTH; i++)
        {
            if (Push_To_Value)
                _value_buffer.Append(dataStream[i]);

            if (In_Token_Block)
                codonSize++;

            // Token events
            if (CheckForToken(dataStream[i]))
            {
                switch (dataStream[i])
                {
                    // Head & Tail
                    case CodonOpen:
                        codonSize = 1;
                        _name_buffer.Clear();
                        In_Token_Block = true;
                        Expecting_Code = true;
                        break;

                    case CodonClose:

                        if (!In_Token_Block)
                            break;

                        Push_To_Name = false;
                        In_Token_Block = false;

                        if (Resolve_Token)
                        {
                            if (Expecting_Value)
                            {
                                _value_buffer.Remove(_value_buffer.Length - codonSize, codonSize);
                                valueBuffer.Add(_value_buffer.ToString());
                                List<string> values = new List<string>();
                                values.AddRange(valueBuffer);
                                valuesCatalogue.Add(_name_buffer.ToString(), values);
                                valueBuffer.Clear();
                                //currentBufferedElement.SetValues(valuesCatalogue);
                                Expecting_Value = false;
                                Push_To_Value = false;
                                Debug.Log($"Value Resolved! Name: {_name_buffer} | Value: {_value_buffer}");
                            }
                            else if (currentBufferedElement != null && currentBufferedElement.Name == _name_buffer.ToString())
                            {
                                currentBufferedElement.SetValues(valuesCatalogue);
                                valuesCatalogue.Clear();

                                Debug.Log($"Element Resolved! Name: {currentBufferedElement.Name}");

                                if (currentBufferedElement.Parent != null)
                                    currentBufferedElement = currentBufferedElement.Parent;
                            }
                        }
                        else if (Split_Value)
                        {
                            Split_Value = false;
                            _value_buffer.Remove(_value_buffer.Length - codonSize, codonSize);
                            valueBuffer.Add(_value_buffer.ToString());
                            Debug.Log($"Value split! Previous: {_value_buffer}");
                            _value_buffer.Clear();
                        }
                        else if (Expecting_Value)
                        {
                            Push_To_Value = true;
                            _value_buffer.Clear();
                            Debug.Log("Pushing Value(s)!");
                        }
                        else if (CheckForElementName(_name_buffer.ToString(), elementLegend))
                        {
                            Element newElement = new Element(_name_buffer.ToString(), currentBufferedElement);

                            if (currentBufferedElement != null)
                                currentBufferedElement.AddChildSafe(newElement);

                            currentBufferedElement = newElement;

                            //elementCount++;
                            Debug.Log($"Element added! Name: {_name_buffer}");
                        }

                        break;

                    // Actual Codes
                    case Label:
                    case Value:
                    case Terminate:

                        if (!In_Token_Block || !Expecting_Code)
                            break;

                        Expecting_Code = false;
                        Expecting_Value = dataStream[i] == Terminate ? Expecting_Value : dataStream[i] == Value;
                        Resolve_Token = dataStream[i] == Terminate;
                        Push_To_Name = true;//!Expecting_Value;
                        continue;

                    case ValueSplit:

                        if (!In_Token_Block || !Expecting_Code)
                            break;

                        Split_Value = true;
                        Expecting_Code = false;
                        break;
                }
            }

            if (Push_To_Name)
                _name_buffer.Append(dataStream[i]);
        }

        Debug.Log($"Stream finished reading. Total length: {dataStream.Length}\n" +
            $"Current buffered element exists: {currentBufferedElement != null}");

        return currentBufferedElement;
    }
    public static LogBook BuildLogBookFromDefaultClientFile()
    {
        string rawStream = ServerDataBase.LoadDefaultLogBookFile();
        Element root = BuildElementTree(rawStream, Enum.GetNames(typeof(LogBookElement)));
        return new LogBook();
    }
}
