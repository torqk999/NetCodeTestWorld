using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public static class ServerDataBase
{
    public static string DefaultServerFilePath { get; private set; } = @"\Assets\DefaultServer";
    public static string DefaultLogBookFileName { get; private set; } = @"\DefaultLogBookSave.txt";
    public static string DefaultRegistryFileName { get; private set; } = @"\DefaultRegistrySave.txt";
    public static string DefaultServerFileName { get; private set; } = @"\DefaultServerProfileSave.txt";
    public static string DefaultTestFileName { get; private set; } = @"\DefaultTestSave.txt";

    public static string DirectoryPath => Directory.GetCurrentDirectory() + DefaultServerFilePath;
    public static string LogBookFilePath => DirectoryPath + DefaultLogBookFileName;
    public static string RegistryFilePath => DirectoryPath + DefaultRegistryFileName;
    public static string ServerFilePath => DirectoryPath + DefaultServerFileName;
    public static string TestFilePath => DirectoryPath + DefaultTestFileName;

    public static void GenerateNewDefaultTextFile(string filePath)
    {
        Debug.Log("Creating New TextFile...");

        if (!Directory.Exists(DirectoryPath))
        {
            Directory.CreateDirectory(DirectoryPath);
            Debug.Log("Directory Created!");
        }

        using (StreamWriter sw = File.CreateText(filePath)) { }
        //File.CreateText(filePath);
        Debug.Log("File Created!");
    }
    public static string LoadDefaultTextFile(string filePath)
    {
        if (!Directory.Exists(DirectoryPath) || !File.Exists(filePath))
            GenerateNewDefaultTextFile(filePath);

        string output;

        using (StreamReader sr = File.OpenText(filePath))
            output = sr.ReadToEnd();

        return output;
    }
    public static void SaveDefaultTextFile(string filePath, string contents)
    {
        Debug.Log($"Saving file: {filePath}");
        Debug.Log($"Path exists: {Directory.Exists(DirectoryPath)}");
        if (Directory.Exists(DirectoryPath))
            Debug.Log($"File exists: {File.Exists(filePath)}");

        if (!Directory.Exists(DirectoryPath))
            Directory.CreateDirectory(DirectoryPath);

        using (StreamWriter sw = File.CreateText(filePath))
            sw.Write(contents);
        

            //File.WriteAllText(filePath, contents);
    }
}
