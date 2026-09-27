using System.Collections.Generic;
using NUnit.Framework;

public class DialogueScreenTests
{
    private TestContent content;
    private StoryState story;
    private DialogueHistory history;
    private DialogueRunner runner;
    private Dialogue_SpeakerSO vance;
    private Story_VariableSO humanity;

    private Dialogue_ConversationSO intro;
    private DialogueChoice give;
    private DialogueChoice lockedChoice;

    [SetUp]
    public void SetUp()
    {
        content = new TestContent();
        story = new StoryState();
        history = new DialogueHistory();
        runner = new DialogueRunner(story, history, content.Resolver);
        vance = content.Speaker("Vance");
        humanity = content.Variable("humanity");

        Dialogue_ConversationSO after = content.Conversation("after");
        content.Line(after, "Thank you, <b>boy</b>.", vance);

        intro = content.Conversation("intro", DialogueEndType.Choices);
        content.Line(intro, "Give it to me.", vance);
        give = content.Choice(intro, "Give him the costume", after);
        lockedChoice = content.Choice(intro, "Tell him the truth", after);
        lockedChoice.requirements.Add(TestContent.Condition(humanity, ComparisonType.GreaterOrEqual, 3));
        lockedChoice.showWhenLocked = true;
    }

    [TearDown]
    public void TearDown() => content.Dispose();

    private DialogueScreen Build(int index, bool live) => DialogueScreen.Build(history.Entries, index, live, content.Resolver, story);

    [Test]
    public void LiveChoicesAreInteractableUnlessLocked()
    {
        runner.Start(intro);

        DialogueScreen screen = Build(0, true);

        Assert.AreEqual("Give it to me.", screen.text);
        Assert.AreEqual("Vance", screen.speakerName);
        Assert.AreEqual(2, screen.choices.Count);
        Assert.IsTrue(screen.choices[0].interactable);
        Assert.IsFalse(screen.choices[1].interactable);
        Assert.IsTrue(screen.choices[1].locked);
    }

    [Test]
    public void PastChoicesAreLockedWithThePickHighlighted()
    {
        runner.Start(intro);
        runner.Choose(give.id);

        DialogueScreen past = Build(0, false);

        Assert.IsFalse(past.isLive);
        Assert.AreEqual(2, past.choices.Count);
        Assert.IsFalse(past.choices[0].interactable);
        Assert.IsFalse(past.choices[1].interactable);
        Assert.IsFalse(past.choices[1].locked); // "Locked" only matters for choices still pending
        Assert.IsTrue(past.choices[0].picked);
        Assert.IsFalse(past.choices[1].picked);
    }

    [Test]
    public void NarratorLinesHaveNoNameOrPortrait()
    {
        Dialogue_ConversationSO narration = content.Conversation("narration");
        content.Line(narration, "Rain hammers the shop window.");
        runner.Start(narration);

        DialogueScreen screen = Build(0, true);

        Assert.AreEqual("", screen.speakerName);
        Assert.IsNull(screen.portrait);
        Assert.IsTrue(screen.isResolved);
    }

    [Test]
    public void DeletedContentShowsAPlaceholder()
    {
        runner.Start(intro);
        intro.lines[0].id = "renamed";

        DialogueScreen screen = Build(0, false);

        Assert.IsFalse(screen.isResolved);
        Assert.AreEqual(DialogueText.Missing, screen.text);
    }

    [Test]
    public void BacklogListsLinesAndPicksAndSkipsMissingContent()
    {
        runner.Start(intro);
        runner.Choose(give.id);
        runner.Advance();

        Dialogue_ConversationSO gone = content.Conversation("gone");
        content.Line(gone, "This will be deleted.");
        runner.Start(gone);
        gone.lines[0].id = "renamed";

        List<DialogueScreen.BacklogItem> items = DialogueScreen.BuildBacklog(history.Entries, content.Resolver);

        Assert.AreEqual(3, items.Count);
        Assert.AreEqual("Give it to me.", items[0].text);
        Assert.IsTrue(items[1].isChoice);
        Assert.AreEqual("Give him the costume", items[1].text);
        Assert.AreEqual("Thank you, <b>boy</b>.", items[2].text);
    }

    [Test]
    public void StripTagsRemovesRichText()
    {
        Assert.AreEqual("Thank you, boy.", DialogueText.StripTags("Thank you, <b>boy</b>."));
    }
}
