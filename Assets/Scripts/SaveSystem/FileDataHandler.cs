using System;
using System.IO;
using System.Text;
using UnityEngine;

public class FileDataHandler
{
    public const string BackupExtension = ".bak";
    public const string TempExtension = ".tmp";

    private static readonly Encoding fileEncoding = new UTF8Encoding(false);

    private bool encryptData;
    private string encryptionCodeWord = "UATAIAM";

    public FileDataHandler(bool encryptData)
    {
        this.encryptData = encryptData;
    }

    public bool Exists(string fullPath) => File.Exists(fullPath);

    // Save data to a file. Writes to a temp file first, so a crash mid-write never destroys the previous save.
    public bool SaveData(string fullPath, GameData gameData)
    {
        string tempPath = fullPath + TempExtension;

        try
        {
            // 1. Create the directory where the file will be saved if it doesn't already exist
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            // 2. Serialize the C# game data object into a JSON string
            string dataToSave = JsonUtility.ToJson(gameData, true);

            // 2.5. Encrypt the data if encryption is enabled
            if (encryptData)
                dataToSave = EncryptDecrypt(dataToSave);

            // 3. Write to a temp file, then swap it in and keep the old file as a backup
            File.WriteAllText(tempPath, dataToSave, fileEncoding);
            ReplaceFile(tempPath, fullPath);
            return true;
        }
        catch (Exception e)
        {
            // Log any errors that might occur during the save process
            Debug.LogError("Error occured when trying to save data to file: " + fullPath + "\n" + e);
            TryDelete(tempPath);
            return false;
        }
    }

    // Load data from a file. Returns null if the file is missing or unreadable (falls back to the backup first).
    public GameData LoadData(string fullPath)
    {
        GameData loadData = ReadFile(fullPath);

        string backupPath = fullPath + BackupExtension;
        if (loadData == null && File.Exists(backupPath))
        {
            loadData = ReadFile(backupPath);

            if (loadData != null)
                Debug.LogWarning("Save file was unreadable, loaded its backup instead: " + backupPath);
        }

        return loadData;
    }

    public void DeleteData(string fullPath)
    {
        try
        {
            // 1. Check if the file exists before attempting to delete it
            if (File.Exists(fullPath))
            {
                // 2. Delete the file along with its backup
                File.Delete(fullPath);
                Debug.Log("Data file deleted successfully: " + fullPath);
            }
            else
            {
                Debug.LogWarning("Data file not found for deletion: " + fullPath);
            }

            TryDelete(fullPath + BackupExtension);
            TryDelete(fullPath + TempExtension);
        }
        catch (Exception e)
        {
            Debug.LogError("Error occurred when trying to delete data file: " + fullPath + "\n" + e);
        }
    }

    private GameData ReadFile(string path)
    {
        // 1. Check if the file exists
        if (!File.Exists(path))
            return null;

        try
        {
            // 2. Read the serialized data from the file
            string dataToLoad = File.ReadAllText(path, fileEncoding);

            if (string.IsNullOrWhiteSpace(dataToLoad))
                return null;

            // 3. Decrypt only if the file is actually encrypted, so toggling encryption never bricks old saves
            if (!IsPlainJson(dataToLoad))
                dataToLoad = EncryptDecrypt(dataToLoad);

            // 4. Deserialize the JSON string back into a C# game data object
            return JsonUtility.FromJson<GameData>(dataToLoad);
        }
        catch (Exception e)
        {
            Debug.LogError("Error occurred when trying to load data from file: " + path + "\n" + e);
            return null;
        }
    }

    private static void ReplaceFile(string sourcePath, string destinationPath)
    {
        string backupPath = destinationPath + BackupExtension;

        if (!File.Exists(destinationPath))
        {
            File.Move(sourcePath, destinationPath);
            return;
        }

        try
        {
            File.Replace(sourcePath, destinationPath, backupPath);
        }
        catch (PlatformNotSupportedException)
        {
            // Some platforms don't support File.Replace, so do the same thing by hand
            File.Copy(destinationPath, backupPath, true);
            File.Delete(destinationPath);
            File.Move(sourcePath, destinationPath);
        }
    }

    private static bool IsPlainJson(string data) => data[0] == '{';

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not delete file: " + path + "\n" + e.Message);
        }
    }

    private string EncryptDecrypt(string data)
    {
        char[] modifiedData = new char[data.Length];

        for (int i = 0; i < data.Length; i++)
            modifiedData[i] = (char)(data[i] ^ encryptionCodeWord[i % encryptionCodeWord.Length]);

        return new string(modifiedData);
    }
}
