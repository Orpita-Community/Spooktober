using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// Plays the sample story (Assets/Data) through the real GameSystems prefab and the MainMenu/Shop scenes.
public class GameFlowSmokeTests
{
    private const string TestSaveFolder = "SmokeTestSaves";

    private static string SaveDirectory => Path.Combine(Application.persistentDataPath, TestSaveFolder);
    private static UI_Dialogue DialogueUI => UI.Instance.dialogueUI;
    private static string BodyText => Find<TextMeshProUGUI>(DialogueUI.transform, "BodyText").text;
    private static Sprite Portrait => Find<Image>(DialogueUI.transform, "Portrait").sprite;
    private static UI_DialogueChoice[] Choices => DialogueUI.GetComponentsInChildren<UI_DialogueChoice>(false);
    private static bool ChoicesShown => DialogueUI.gameObject.activeInHierarchy && !DialogueUI.IsTyping && Choices.Length > 0;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return null;

        Assert.IsNotNull(GameSystems.Instance, "Resources/GameSystems.prefab should be spawned before the first scene loads.");

        // Keep the test's saves away from real ones
        typeof(SaveManager).GetField("saveFolder", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(SaveManager.Instance, TestSaveFolder);
        DeleteTestSaves();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        DeleteTestSaves();
        yield return null;
    }

    [UnityTest]
    public IEnumerator SampleStory_PlaysWithReactionsRollbackAndQuickSaveLoad()
    {
        DialogueManager dialogue = DialogueManager.Instance;

        // New Game loads the Shop, where the intro trigger starts the opening
        GameManager.Instance.NewGame();
        yield return WaitUntil(() => !GameManager.Instance.IsTransitioning && SceneManager.GetActiveScene().name == "Shop", 15, "New Game to load the Shop");

        Assert.IsTrue(dialogue.IsActive, "The intro trigger should start the opening conversation.");
        StringAssert.Contains("Rain hammers", BodyText);
        Assert.AreEqual("Act 1 — 10:30 PM", StoryManager.Instance.CurrentChapterLabel);

        yield return WaitUntil(() => File.Exists(SlotPath("auto")), 10, "the intro autosave");

        // Lines, a Jump, then Vance's choices (one locked until the contract is read)
        yield return AdvanceUntil(() => ChoicesShown, "Vance's first choices");
        StringAssert.Contains("belongs to me", BodyText);
        StringAssert.Contains("Normal", Portrait.name);
        Assert.AreEqual(4, Choices.Length);
        Assert.IsFalse(Choices.Last().IsInteractable, "The contract choice should be locked.");

        yield return WaitUntil(() => !SaveManager.Instance.IsBusy, 5, "the autosave to finish");
        SaveManager.Instance.QuickSave();
        yield return WaitUntil(() => !SaveManager.Instance.IsBusy && File.Exists(SlotPath("quick")), 5, "the quick save");

        // A rude answer: Vance reacts with an angry portrait, then new options appear under his reaction
        yield return Pick("Get out");
        StringAssert.Contains("Such manners", BodyText);
        StringAssert.Contains("Angry", Portrait.name);
        Assert.AreEqual(1, Get("Cynicism"));

        yield return AdvanceUntil(() => ChoicesShown, "the follow-up choices");
        StringAssert.Contains("Such manners", BodyText);
        Assert.AreEqual(2, Choices.Length);

        // Rollback shows the earlier choice screen, read-only
        dialogue.RollBack();
        yield return null;
        Assert.IsTrue(dialogue.IsRollingBack);
        StringAssert.Contains("belongs to me", BodyText);
        Assert.IsTrue(Choices.All(choice => !choice.IsInteractable), "Past choices must be locked.");

        dialogue.RollForward();
        yield return null;
        Assert.IsFalse(dialogue.IsRollingBack);
        StringAssert.Contains("Such manners", BodyText);
        Assert.AreEqual(1, Get("Cynicism"), "Rollback must never re-apply effects.");

        // Quick load goes back to the saved choice screen with the saved story values
        SaveManager.Instance.QuickLoad();
        yield return WaitUntil(() => !SaveManager.Instance.IsRestoring && !GameManager.Instance.IsTransitioning, 15, "the quick load");

        Assert.IsTrue(dialogue.IsActive, "The saved dialogue should resume.");
        StringAssert.Contains("belongs to me", BodyText);
        Assert.AreEqual(4, Choices.Length);
        Assert.AreEqual(0, Get("Cynicism"));

        dialogue.RollBack();
        yield return null;
        StringAssert.Contains("How did you even get in", BodyText, "Rollback should reach lines read before the save.");
        dialogue.RollForward();
        yield return null;

        // Kind choices all the way to the Humanity ending
        yield return Pick("Give him the costume");
        yield return AdvanceUntil(() => ChoicesShown, "the contract choices");
        StringAssert.Contains("my name on it", BodyText);
        Assert.AreEqual("Act 2 — 11:47 PM", StoryManager.Instance.CurrentChapterLabel);
        Assert.IsTrue(Get("Flag Read Contract") == 1);

        yield return Pick("Remember him fixing");
        yield return AdvanceUntil(() => ChoicesShown, "the final choice");
        yield return Pick("Tear up");

        yield return AdvanceUntil(() => dialogue.IsActive && BodyText.Contains("The Last Customer"), "the Humanity ending");
        Assert.AreEqual(3, Get("Humanity"));
        yield return AdvanceUntil(() => !dialogue.IsActive, "the dialogue to close");

        // Pause stops time, Back resumes it
        GameManager.Instance.OpenPauseMenu();
        yield return null;
        Assert.IsTrue(UI.Instance.IsModalOpen);
        Assert.AreEqual(0f, Time.timeScale);
        UI.Instance.Back();
        Assert.AreEqual(1f, Time.timeScale);

        // The quick save slot describes where it was made
        SaveSlotInfo quick = SaveManager.Instance.GetSlotInfo(SaveManager.QuickSlot);
        Assert.IsTrue(quick.IsLoadable);
        Assert.AreEqual("Act 1 — 10:30 PM", quick.metadata.chapterText);
        Assert.AreEqual("Mr. Vance", quick.metadata.previewSpeaker);
        StringAssert.Contains("belongs to me", quick.metadata.previewText);
    }

    private static IEnumerator Pick(string textStart)
    {
        UI_DialogueChoice choice = Choices.FirstOrDefault(c => Find<TextMeshProUGUI>(c.transform, "Label").text.StartsWith(textStart));

        Assert.IsNotNull(choice, $"No choice starting with \"{textStart}\" is shown.");
        Assert.IsTrue(choice.IsInteractable, $"\"{textStart}\" should be pickable.");

        choice.Button.onClick.Invoke();
        yield return null;
    }

    // One Advance per frame (the dialogue ignores input on the frame something was picked or started)
    private static IEnumerator AdvanceUntil(Func<bool> condition, string what, int maxSteps = 80)
    {
        for (int i = 0; i < maxSteps && !condition(); i++)
        {
            DialogueManager.Instance.Advance();
            yield return null;
        }

        Assert.IsTrue(condition(), "Never reached " + what + ". Screen text: " + BodyText);
    }

    private static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds, string what)
    {
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;

        while (!condition())
        {
            if (Time.realtimeSinceStartup > deadline)
                Assert.Fail("Timed out waiting for " + what + ".");

            yield return null;
        }
    }

    private static int Get(string variableName)
    {
#if UNITY_EDITOR
        Story_VariableSO variable = UnityEditor.AssetDatabase.LoadAssetAtPath<Story_VariableSO>($"Assets/Data/Story Data/Variables/Var - {variableName}.asset");
        Assert.IsNotNull(variable, "Missing sample variable " + variableName);
        return StoryManager.Instance.Get(variable);
#else
        return 0;
#endif
    }

    private static string SlotPath(string key) => SaveManager.Instance.GetSavePath(key);

    private static T Find<T>(Transform root, string name) where T : Component =>
        root.GetComponentsInChildren<T>(true).First(component => component.name == name);

    private static void DeleteTestSaves()
    {
        if (Directory.Exists(SaveDirectory))
            Directory.Delete(SaveDirectory, true);
    }
}
