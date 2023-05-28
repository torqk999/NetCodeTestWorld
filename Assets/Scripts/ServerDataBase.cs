using System.IO;
using System.Collections.Generic;
using UnityEngine;

public static class ServerDataBase
{
    public static string DefaultServerFilePath { get; private set; } = @"\Assets\TestSaves";
    public static string DefaultLogBookFileName { get; private set; } = @"\DefaultLogBookSave.txt";
    public static string DefaultRegistryFileName { get; private set; } = @"\DefaultRegistrySave.txt";

    public static string DirectoryPath => Directory.GetCurrentDirectory() + DefaultServerFilePath;
    public static string LogBookFilePath => DirectoryPath + DefaultLogBookFileName;
    public static string RegistryFilePath => DirectoryPath + DefaultRegistryFileName;


    public static string LoadDefaultRegistryFile()
    {
        if (!Directory.Exists(DirectoryPath) || !File.Exists(RegistryFilePath))
            GenerateNewDefaultRegistryFile();

        return File.ReadAllText(RegistryFilePath);
    }
    public static string LoadDefaultLogBookFile()
    {
        Debug.Log("Reading LogBook...");

        if (!Directory.Exists(DirectoryPath) || !File.Exists(LogBookFilePath))
            GenerateNewDefaultLogBookFile();

        return File.ReadAllText(LogBookFilePath);
    }

    public static void GenerateNewDefaultRegistryFile()
    {
        Debug.Log("Creating Registry...");

        if (!Directory.Exists(DirectoryPath))
        {
            Directory.CreateDirectory(DirectoryPath);
            Debug.Log("Directory Created!");
        }

        File.CreateText(RegistryFilePath);
        Debug.Log("Registry Created!");
    }
    public static void GenerateNewDefaultLogBookFile()
    {
        Debug.Log("Creating LogBook...");

        if (!Directory.Exists(DirectoryPath))
        {
            Directory.CreateDirectory(DirectoryPath);
            Debug.Log("Directory Created!");
        }

        File.CreateText(LogBookFilePath);
        Debug.Log("LogBook Created!");
    }

    public static void SaveDefaultLogBookFile(string contents)
    {
        if (!Directory.Exists(DirectoryPath) || !File.Exists(LogBookFilePath))
            GenerateNewDefaultLogBookFile();

        File.WriteAllText(LogBookFilePath, contents);
    }
    public static void SaveDefaultRegistryFile(string contents)
    {
        if (!Directory.Exists(DirectoryPath) || !File.Exists(RegistryFilePath))
            GenerateNewDefaultLogBookFile();

        File.WriteAllText(RegistryFilePath, contents);
    }
}
