using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Plays the real story (Assets/Data) through the real GameSystems prefab and the MainMenu/Shop scenes, down both endings.
public class GameFlowSmokeTests
{
    private const string TestSaveFolder = "SmokeTestSaves";

    private readonly List<string> titleCards = new List<string>();

    private static string SaveDirectory => Path.Combine(Application.persistentDataPath, TestSaveFolder);
    private static UI_Dialogue DialogueUI => UI.Instance.dialogueUI;
    private static bool CenteredLineShown => Find<Transform>(DialogueUI.transform, "CenterRoot").gameObject.activeSelf;
    private static string ShownText => Find<TextMeshProUGUI>(DialogueUI.transform, CenteredLineShown ? "CenterText" : "BodyText").text;
    private static StageScreen Stage => UI.Instance.stage.Current;
    private static UI_DialogueChoice[] Choices => DialogueUI.GetComponentsInChildren<UI_DialogueChoice>(false);
    private static bool ChoicesShown => DialogueUI.gameObject.activeInHierarchy && !DialogueUI.IsTyping && Choices.Length > 0;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return null;

        Assert.IsNotNull(GameSystems.Instance, "Resources/GameSystems.prefab should be spawned before the first scene loads.");

        // Keep the test's saves away from real ones, and don't wait on slow fades and title cards
        SetField(SaveManager.Instance, "saveFolder", TestSaveFolder);
        SetField(GameManager.Instance, "fadeDuration", .05f);
        SetField(GameManager.Instance, "endingFadeDuration", .05f);
        SetField(UI.Instance.fadeScreen, "titleFadeTime", .05f);
        SetField(UI.Instance.fadeScreen, "titleHoldTime", .05f);
        DeleteTestSaves();

        titleCards.Clear();
        UI.Instance.fadeScreen.OnTitleCardShown += RecordTitleCard;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        UI.Instance.fadeScreen.OnTitleCardShown -= RecordTitleCard;
        DeleteTestSaves();
        yield return null;
    }

    private void RecordTitleCard(TitleCard card) => titleCards.Add(card.title);

    [UnityTest]
    public IEnumerator HumanityRoute_TitleCardsStageSaveLoadRollbackAndTheEnd()
    {
        DialogueManager dialogue = DialogueManager.Instance;

        yield return StartNewGame();

        // The prologue opens on its title card and the rainy street
        CollectionAssert.AreEqual(new[] { "PROLOGUE" }, titleCards);
        StringAssert.Contains("October 31st.", ShownText);
        Assert.AreEqual("Prologue — October 31", StoryManager.Instance.CurrentChapterLabel);
        Assert.AreEqual("BG - Street", Stage.background.name);
        Assert.AreEqual(0, Stage.actors.Count);
        yield return WaitUntil(() => File.Exists(SlotPath("auto")), 10, "the prologue autosave");

        // Act 1 gets its own card, then Vance's first question
        yield return AdvanceUntil(() => ChoicesShown, "the first choice");
        CollectionAssert.AreEqual(new[] { "PROLOGUE", "ACT 1" }, titleCards);
        Assert.AreEqual("Act 1 — The Man in the Rain", StoryManager.Instance.CurrentChapterLabel);
        Assert.AreEqual("No.", ShownText);
        Assert.AreEqual("BG - Shop", Stage.background.name);
        AssertCast("Speaker - Adam", "Speaker - Vance");
        Assert.IsTrue(Actor("Speaker - Vance").speaking, "The speaker is lit, the others step back.");
        Assert.IsFalse(Actor("Speaker - Adam").speaking);

        yield return WaitUntil(() => !SaveManager.Instance.IsBusy, 5, "the autosave to finish");
        SaveManager.Instance.QuickSave();
        yield return WaitUntil(() => !SaveManager.Instance.IsBusy && File.Exists(SlotPath("quick")), 5, "the quick save");

        // A cynical answer is worth 3
        yield return Pick("Get out");
        Assert.AreEqual("You don't have to be frightened.", ShownText);
        Assert.AreEqual(3, Get("Cynicism"));

        // Rollback shows the earlier choice, read-only
        dialogue.RollBack();
        yield return null;
        Assert.IsTrue(dialogue.IsRollingBack);
        Assert.AreEqual("No.", ShownText);
        Assert.IsTrue(Choices.All(choice => !choice.IsInteractable), "Past choices must be locked.");
        dialogue.RollForward();
        yield return null;
        Assert.IsFalse(dialogue.IsRollingBack);
        Assert.AreEqual(3, Get("Cynicism"), "Rollback must never re-apply effects.");

        // Quick load goes back to the choice, with the old score and the same stage
        SaveManager.Instance.QuickLoad();
        yield return WaitUntil(() => !SaveManager.Instance.IsRestoring && !GameManager.Instance.IsTransitioning, 15, "the quick load");
        Assert.IsTrue(dialogue.IsActive, "The saved dialogue should resume.");
        Assert.AreEqual(2, Choices.Length);
        Assert.AreEqual(0, Get("Cynicism"));
        Assert.AreEqual("BG - Shop", Stage.background.name);
        AssertCast("Speaker - Adam", "Speaker - Vance");
        CollectionAssert.AreEqual(new[] { "PROLOGUE", "ACT 1" }, titleCards, "Loading a save doesn't replay title cards.");

        // Pause stops time, Back resumes it
        GameManager.Instance.OpenPauseMenu();
        yield return null;
        Assert.IsTrue(UI.Instance.IsModalOpen);
        Assert.AreEqual(0f, Time.timeScale);
        UI.Instance.Back();
        Assert.AreEqual(1f, Time.timeScale);
        yield return null; // The press that closed the menu is ignored by the dialogue that frame

        // Kind answers from here on
        yield return Pick("Look, we really are closed");
        yield return AdvanceUntil(() => ChoicesShown, "the ledger choice");
        yield return Pick("Do you know something about my uncle");
        yield return AdvanceUntil(() => ChoicesShown, "the signature choice");
        yield return Pick("Can't I wait until tomorrow");

        // The mirror puts the mask on Adam, and the inscription burns in as a centered line
        yield return AdvanceUntil(() => ShownText.Contains("The reflection is wearing the mask."), "the mirror");
        AssertCast("Speaker - Adam Masked");
        yield return AdvanceUntil(() => ShownText.Contains("THE MASK IS WORN ONCE."), "the inscription");
        Assert.IsTrue(CenteredLineShown);

        // Act 2: three memories, three choices
        yield return AdvanceUntil(() => ChoicesShown, "the cape choice");
        CollectionAssert.AreEqual(new[] { "PROLOGUE", "ACT 1", "ACT 2" }, titleCards);
        Assert.AreEqual("BG - Workshop Cape", Stage.background.name);
        AssertCast("Speaker - Young Adam", "Speaker - Alexander");
        yield return Pick("Make it");
        yield return AdvanceUntil(() => ChoicesShown, "the dress choice");
        Assert.AreEqual("BG - Workshop Dress", Stage.background.name);
        yield return Pick("Leave the patch");
        yield return AdvanceUntil(() => ChoicesShown, "the last choice");
        Assert.AreEqual("BG - Shop Memory", Stage.background.name);
        AssertCast("Speaker - Adam", "Speaker - Alexander");
        yield return Pick("I'm sorry");

        Assert.AreEqual(12, Get("Humanity"));
        Assert.AreEqual(0, Get("Cynicism"));

        // Act 3: Humanity is higher, so the mask lets go
        yield return AdvanceUntil(() => ShownText.Contains("The mask is broken away from Adam."), "the mask breaking");
        CollectionAssert.AreEqual(new[] { "PROLOGUE", "ACT 1", "ACT 2", "ACT 3" }, titleCards);
        AssertCast("Speaker - Adam", "Speaker - Vance");

        yield return AdvanceUntil(() => ShownText.Contains("Some things only need to be faced."), "the last line");
        Assert.IsTrue(CenteredLineShown);
        yield return AdvanceUntil(() => !dialogue.IsActive, "the story to end");

        yield return WaitUntil(() => SceneManager.GetActiveScene().name == "MainMenu" && !GameManager.Instance.IsTransitioning, 15,
            "the end card and the main menu");
        Assert.AreEqual("THE END", titleCards.Last());
        Assert.IsFalse(UI.Instance.stage.gameObject.activeSelf, "The stage is gone in the main menu.");

        // The quick save slot describes where it was made
        SaveSlotInfo quick = SaveManager.Instance.GetSlotInfo(SaveManager.QuickSlot);
        Assert.IsTrue(quick.IsLoadable);
        Assert.AreEqual("Act 1 — The Man in the Rain", quick.metadata.chapterText);
        Assert.AreEqual("Mr. Vance", quick.metadata.previewSpeaker);
        Assert.AreEqual("No.", quick.metadata.previewText);
    }

    [UnityTest]
    public IEnumerator CynicismRoute_EndsInTheColdShop()
    {
        yield return StartNewGame();

        foreach (string answer in new[] { "Get out", "Never mind", "Fine. I'll sign", "Don't make it", "Use new fabric", "You should have sold it" })
        {
            yield return AdvanceUntil(() => ChoicesShown, $"the choice \"{answer}\"");
            yield return Pick(answer);
        }

        Assert.AreEqual(18, Get("Cynicism"));
        Assert.AreEqual(0, Get("Humanity"));

        yield return AdvanceUntil(() => ShownText.Contains("Adam's reflection now wears Vance's grey coat."), "the grey coat");
        yield return AdvanceUntil(() => ShownText.Contains("Some doors don't close when you leave."), "the final text");
        Assert.AreEqual("BG - Shop Cold", Stage.background.name);
        AssertCast("Speaker - Adam Masked");

        yield return AdvanceUntil(() => !DialogueManager.Instance.IsActive, "the story to end");
        yield return WaitUntil(() => SceneManager.GetActiveScene().name == "MainMenu" && !GameManager.Instance.IsTransitioning, 15,
            "the end card and the main menu");
        Assert.AreEqual("THE END", titleCards.Last());
    }

    private static IEnumerator StartNewGame()
    {
        GameManager.Instance.NewGame();
        yield return WaitUntil(() => SceneManager.GetActiveScene().name == "Shop" && !GameManager.Instance.IsTransitioning && DialogueManager.Instance.IsActive,
            15, "New Game to open the prologue");
    }

    private static IEnumerator Pick(string textStart)
    {
        UI_DialogueChoice choice = Choices.FirstOrDefault(c => Find<TextMeshProUGUI>(c.transform, "Label").text.StartsWith(textStart));

        Assert.IsNotNull(choice, $"No choice starting with \"{textStart}\" is shown.");
        Assert.IsTrue(choice.IsInteractable, $"\"{textStart}\" should be pickable.");

        choice.Button.onClick.Invoke();
        yield return null;
    }

    // One Advance per frame (the dialogue ignores input on the frame something was picked or started).
    // Frames spent in a fade or a title card don't count as steps.
    private static IEnumerator AdvanceUntil(Func<bool> condition, string what, int maxSteps = 400)
    {
        int steps = 0;
        float deadline = Time.realtimeSinceStartup + 60f;

        while (!condition())
        {
            if (steps >= maxSteps || Time.realtimeSinceStartup > deadline)
                Assert.Fail("Never reached " + what + ". Screen text: " + ShownText);

            if (!GameManager.Instance.IsTransitioning)
            {
                DialogueManager.Instance.Advance();
                steps++;
            }

            yield return null;
        }
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

    private static void AssertCast(params string[] speakerAssetNames) =>
        CollectionAssert.AreEquivalent(speakerAssetNames, Stage.actors.Select(actor => actor.speaker.name));

    private static StageScreen.Actor Actor(string speakerAssetName) => Stage.actors.Single(actor => actor.speaker.name == speakerAssetName);

    private static int Get(string variableName)
    {
#if UNITY_EDITOR
        Story_VariableSO variable = UnityEditor.AssetDatabase.LoadAssetAtPath<Story_VariableSO>($"Assets/Data/Story Data/Variables/Var - {variableName}.asset");
        Assert.IsNotNull(variable, "Missing story variable " + variableName);
        return StoryManager.Instance.Get(variable);
#else
        return 0;
#endif
    }

    private static string SlotPath(string key) => SaveManager.Instance.GetSavePath(key);

    private static T Find<T>(Transform root, string name) where T : Component =>
        root.GetComponentsInChildren<T>(true).First(component => component.name == name);

    private static void SetField(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    private static void DeleteTestSaves()
    {
        if (Directory.Exists(SaveDirectory))
            Directory.Delete(SaveDirectory, true);
    }
}
