using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class FileDataHandlerTests
{
    private string directory;
    private string path;

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "SpooktoberSaveTests_" + Guid.NewGuid().ToString("N"));
        path = Path.Combine(directory, "slot_01.sav");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
    }

    private static GameData MakeData(string chapter)
    {
        GameData data = new GameData { sceneName = "Shop", chapterID = "chapter-1" };
        data.metadata.chapterText = chapter;
        data.storyValues["humanity"] = 3;
        return data;
    }

    [Test]
    public void PlainRoundTrip()
    {
        FileDataHandler handler = new FileDataHandler(false);

        Assert.IsTrue(handler.SaveData(path, MakeData("Act 1")));
        StringAssert.StartsWith("{", File.ReadAllText(path));

        GameData loaded = handler.LoadData(path);
        Assert.AreEqual("Shop", loaded.sceneName);
        Assert.AreEqual(3, loaded.storyValues["humanity"]);
    }

    [Test]
    public void EncryptedRoundTripKeepsNonAsciiText()
    {
        FileDataHandler handler = new FileDataHandler(true);

        handler.SaveData(path, MakeData("Act 1 — 10:30 PM"));
        StringAssert.DoesNotStartWith("{", File.ReadAllText(path));

        GameData loaded = handler.LoadData(path);
        Assert.AreEqual("Act 1 — 10:30 PM", loaded.metadata.chapterText);
    }

    [Test]
    public void ReadsSavesWrittenWithTheOtherEncryptionSetting()
    {
        new FileDataHandler(true).SaveData(path, MakeData("encrypted"));
        Assert.AreEqual("encrypted", new FileDataHandler(false).LoadData(path).metadata.chapterText);

        new FileDataHandler(false).SaveData(path, MakeData("plain"));
        Assert.AreEqual("plain", new FileDataHandler(true).LoadData(path).metadata.chapterText);
    }

    [Test]
    public void SecondSaveKeepsABackupOfTheFirst()
    {
        FileDataHandler handler = new FileDataHandler(false);

        handler.SaveData(path, MakeData("first"));
        Assert.IsFalse(File.Exists(path + FileDataHandler.BackupExtension));

        handler.SaveData(path, MakeData("second"));
        Assert.IsTrue(File.Exists(path + FileDataHandler.BackupExtension));
        Assert.IsFalse(File.Exists(path + FileDataHandler.TempExtension));
        Assert.AreEqual("second", handler.LoadData(path).metadata.chapterText);
    }

    [Test]
    public void CorruptSaveFallsBackToTheBackup()
    {
        FileDataHandler handler = new FileDataHandler(true);
        handler.SaveData(path, MakeData("first"));
        handler.SaveData(path, MakeData("second"));

        File.WriteAllText(path, "this is not a save file");

        LogAssert.Expect(LogType.Error, new Regex("Error occurred when trying to load data"));
        LogAssert.Expect(LogType.Warning, new Regex("loaded its backup"));

        GameData loaded = handler.LoadData(path);
        Assert.IsNotNull(loaded);
        Assert.AreEqual("first", loaded.metadata.chapterText);
    }

    [Test]
    public void DeleteRemovesTheSaveAndItsBackup()
    {
        FileDataHandler handler = new FileDataHandler(false);
        handler.SaveData(path, MakeData("first"));
        handler.SaveData(path, MakeData("second"));

        handler.DeleteData(path);

        Assert.IsFalse(File.Exists(path));
        Assert.IsFalse(File.Exists(path + FileDataHandler.BackupExtension));
        Assert.IsNull(handler.LoadData(path));
    }

    [Test]
    public void MissingFileLoadsAsNull()
    {
        Assert.IsNull(new FileDataHandler(true).LoadData(path));
    }
}
