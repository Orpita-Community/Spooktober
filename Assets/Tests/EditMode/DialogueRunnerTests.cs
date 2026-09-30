using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class DialogueRunnerTests
{
    private TestContent content;
    private StoryState story;
    private DialogueHistory history;
    private DialogueRunner runner;

    private Dialogue_SpeakerSO vance;
    private Dialogue_SpeakerSO adam;
    private Story_VariableSO humanity;
    private Story_VariableSO cynicism;

    [SetUp]
    public void SetUp()
    {
        content = new TestContent();
        story = new StoryState();
        history = new DialogueHistory();
        runner = new DialogueRunner(story, history, content.Resolver);

        vance = content.Speaker("Vance", PortraitExpression.Angry, PortraitExpression.Smirk);
        adam = content.Speaker("Adam", PortraitExpression.Scared);
        humanity = content.Variable("humanity");
        cynicism = content.Variable("cynicism");
    }

    [TearDown]
    public void TearDown() => content.Dispose();

    private HistoryEntry Last => history.Entries[history.Entries.Count - 1];

    private DialogueScreen PresentScreen() =>
        DialogueScreen.Build(history.Entries, history.Entries.Count - 1, true, content.Resolver, story);

    // Vance: "I believe you have something that belongs to me." -> 3 choices
    private Dialogue_ConversationSO VanceIntro(out DialogueChoice give, out DialogueChoice deposit, out DialogueChoice refuse)
    {
        Dialogue_ConversationSO intro = content.Conversation("intro", DialogueEndType.Choices);
        content.Line(intro, "It's late.", adam, PortraitExpression.Scared);
        content.Line(intro, "I believe you have something that belongs to me.", vance, PortraitExpression.Smirk);

        give = content.Choice(intro, "Give him the costume", Linear("give", "Thank you."));
        deposit = content.Choice(intro, "Ask him about the deposit", Linear("deposit", "Money? Tonight?"));
        refuse = content.Choice(intro, "Refuse", Linear("refuse", "Pity."));
        return intro;
    }

    private Dialogue_ConversationSO Linear(string id, params string[] lines)
    {
        Dialogue_ConversationSO conversation = content.Conversation(id);
        foreach (string text in lines)
            content.Line(conversation, text, vance);
        return conversation;
    }

    #region Flow

    [Test]
    public void LinearConversationShowsEachLineThenEnds()
    {
        int ended = 0;
        runner.Ended += () => ended++;

        Assert.IsTrue(runner.Start(Linear("a", "one", "two")));
        Assert.AreEqual(DialoguePhase.Line, runner.Phase);
        Assert.AreEqual(0, runner.LineIndex);

        runner.Advance();
        Assert.AreEqual(1, runner.LineIndex);

        runner.Advance();
        Assert.IsFalse(runner.IsActive);
        Assert.AreEqual(1, ended);
        Assert.AreEqual(2, history.Entries.Count);
    }

    [Test]
    public void StartingAConversationThatShowsNothingReturnsFalse()
    {
        Assert.IsFalse(runner.Start(content.Conversation("empty")));
        Assert.IsFalse(runner.IsActive);
    }

    [Test]
    public void LineEffectsApplyExactlyOnce()
    {
        Dialogue_ConversationSO conversation = Linear("a", "one", "two");
        conversation.lines[0].effects.Add(TestContent.Effect(humanity, EffectOperation.Add, 1));

        runner.Start(conversation);
        runner.Advance();
        runner.Advance();

        Assert.AreEqual(1, story.Get(humanity));
    }

    [Test]
    public void ChoicesShareTheScreenWithTheLastLine()
    {
        runner.Start(VanceIntro(out _, out _, out _));
        runner.Advance();

        Assert.AreEqual(DialoguePhase.Choices, runner.Phase);
        Assert.AreEqual(2, history.Entries.Count);
        Assert.AreEqual(HistoryEntryType.Line, Last.type);
        Assert.AreEqual(3, Last.shownChoiceIDs.Count);
        Assert.AreEqual("intro", Last.choicesConversationID);
    }

    [Test]
    public void AdvanceDoesNothingWhileChoicesAreShown()
    {
        runner.Start(VanceIntro(out _, out _, out _));
        runner.Advance();
        runner.Advance();

        Assert.AreEqual(DialoguePhase.Choices, runner.Phase);
        Assert.AreEqual(2, history.Entries.Count);
    }

    [Test]
    public void HiddenAndLockedChoicesCantBePicked()
    {
        Dialogue_ConversationSO conversation = content.Conversation("c", DialogueEndType.Choices);
        content.Line(conversation, "Pick.");
        DialogueChoice hidden = content.Choice(conversation, "hidden");
        DialogueChoice locked = content.Choice(conversation, "locked");
        DialogueChoice open = content.Choice(conversation, "open");
        hidden.requirements.Add(TestContent.Condition(humanity, ComparisonType.GreaterOrEqual, 5));
        locked.requirements.Add(TestContent.Condition(humanity, ComparisonType.GreaterOrEqual, 5));
        locked.showWhenLocked = true;

        runner.Start(conversation);

        CollectionAssert.AreEqual(new[] { locked.id, open.id }, Last.shownChoiceIDs);
        Assert.IsFalse(runner.Choose(hidden.id));
        Assert.IsFalse(runner.Choose(locked.id));
        Assert.IsTrue(runner.Choose(open.id));
    }

    [Test]
    public void PickingAChoiceAppliesItsEffectsAndContinuesToNext()
    {
        runner.Start(VanceIntro(out DialogueChoice give, out _, out _));
        give.effects.Add(TestContent.Effect(humanity, EffectOperation.Add, 1));
        runner.Advance();
        HistoryEntry choiceScreen = Last;

        Assert.IsTrue(runner.Choose(give.id));

        Assert.AreEqual(1, story.Get(humanity));
        Assert.AreEqual(give.id, choiceScreen.pickedChoiceID);
        Assert.AreEqual("give", runner.Conversation.saveID);
        Assert.AreEqual(DialoguePhase.Line, runner.Phase);
        Assert.IsFalse(runner.Choose(give.id)); // Can't pick twice
    }

    [Test]
    public void ChoiceWithoutNextEndsTheDialogue()
    {
        Dialogue_ConversationSO conversation = content.Conversation("c", DialogueEndType.Choices);
        content.Line(conversation, "Leave?");
        DialogueChoice leave = content.Choice(conversation, "Leave");

        runner.Start(conversation);
        runner.Choose(leave.id);

        Assert.IsFalse(runner.IsActive);
    }

    [Test]
    public void AllChoicesHiddenEndsInsteadOfSoftlocking()
    {
        Dialogue_ConversationSO conversation = content.Conversation("c", DialogueEndType.Choices);
        content.Line(conversation, "Nothing to say.");
        content.Choice(conversation, "hidden").requirements.Add(TestContent.Condition(humanity, ComparisonType.Equal, 9));

        runner.Start(conversation);
        Assert.AreEqual(DialoguePhase.Line, runner.Phase); // The line is still shown

        LogAssert.Expect(LogType.Warning, new Regex("no choices available"));
        runner.Advance();

        Assert.IsFalse(runner.IsActive);
    }

    #endregion

    #region Reactions

    [Test]
    public void ReactionLineDefaultsToTheSpeakerWhoAsked()
    {
        runner.Start(VanceIntro(out _, out _, out DialogueChoice refuse));
        refuse.reactionLine = "How dare you.";
        refuse.reactionExpression = PortraitExpression.Angry;
        runner.Advance();

        runner.Choose(refuse.id);

        Assert.AreEqual(DialoguePhase.Reaction, runner.Phase);
        Assert.AreEqual(HistoryEntryType.Reaction, Last.type);

        DialogueScreen screen = PresentScreen();
        Assert.AreEqual("How dare you.", screen.text);
        Assert.AreEqual(vance, screen.speaker);
        Assert.AreEqual(vance.GetPortrait(PortraitExpression.Angry), screen.portrait);
        Assert.IsFalse(screen.HasChoices);

        runner.Advance();
        Assert.AreEqual("refuse", runner.Conversation.saveID);
        Assert.AreEqual(PortraitExpression.Normal, runner.CurrentLine.expression); // next keeps its own expression
    }

    [Test]
    public void ReactionCanUseAnExplicitSpeaker()
    {
        runner.Start(VanceIntro(out DialogueChoice give, out _, out _));
        give.reactionLine = "...";
        give.reactionSpeaker = adam;
        give.reactionExpression = PortraitExpression.Scared;
        runner.Advance();

        runner.Choose(give.id);

        DialogueScreen screen = PresentScreen();
        Assert.AreEqual(adam, screen.speaker);
        Assert.AreEqual(adam.GetPortrait(PortraitExpression.Scared), screen.portrait);
    }

    [Test]
    public void ReactionFollowedByChoicesOnlyConversationShowsNewChoicesUnderTheReaction()
    {
        Dialogue_ConversationSO followUp = content.Conversation("followUp", DialogueEndType.Choices);
        DialogueChoice apologize = content.Choice(followUp, "Apologize");
        content.Choice(followUp, "Stand firm");

        runner.Start(VanceIntro(out _, out _, out DialogueChoice refuse));
        refuse.reactionLine = "You insult me?";
        refuse.reactionExpression = PortraitExpression.Angry;
        refuse.next = followUp;
        runner.Advance();
        runner.Choose(refuse.id);
        int entries = history.Entries.Count;

        runner.Advance();

        Assert.AreEqual(DialoguePhase.Choices, runner.Phase);
        Assert.AreEqual(entries, history.Entries.Count); // Same screen as the reaction
        Assert.AreEqual(HistoryEntryType.Reaction, Last.type);
        Assert.AreEqual("followUp", Last.choicesConversationID);

        DialogueScreen screen = PresentScreen();
        Assert.AreEqual("You insult me?", screen.text);
        Assert.AreEqual(2, screen.choices.Count);
        Assert.IsTrue(runner.Choose(apologize.id));
    }

    [Test]
    public void ChoicesOnlyConversationAfterAPlainPickGetsItsOwnScreen()
    {
        Dialogue_ConversationSO followUp = content.Conversation("followUp", DialogueEndType.Choices);
        content.Choice(followUp, "Go on");

        runner.Start(VanceIntro(out DialogueChoice give, out _, out _));
        give.next = followUp;
        runner.Advance();
        runner.Choose(give.id);

        Assert.AreEqual(HistoryEntryType.ChoiceOnly, Last.type);
        Assert.AreEqual(3, history.Entries.Count);

        // It borrows the text that was on screen
        Assert.AreEqual("I believe you have something that belongs to me.", PresentScreen().text);
    }

    #endregion

    #region Jumps and chapters

    [Test]
    public void JumpTakesTheFirstMatchingBranch()
    {
        Dialogue_ConversationSO ending = content.Conversation("ending", DialogueEndType.Jump);
        ending.branches.Add(new DialogueBranch
        {
            conditions = new List<StoryCondition> { TestContent.Condition(humanity, ComparisonType.Greater, cynicism) },
            next = Linear("humanityEnding", "You break the cycle.")
        });
        ending.defaultNext = Linear("cynicismEnding", "You take the contract.");

        story.Set(humanity, 2);
        runner.Start(ending);
        Assert.AreEqual("humanityEnding", runner.Conversation.saveID);

        runner.Stop();
        story.Set(cynicism, 3);
        runner.Start(ending);
        Assert.AreEqual("cynicismEnding", runner.Conversation.saveID);
    }

    [Test]
    public void JumpWithNowhereToGoEnds()
    {
        Dialogue_ConversationSO conversation = Linear("a", "last line");
        conversation.endType = DialogueEndType.Jump;

        runner.Start(conversation);
        runner.Advance();

        Assert.IsFalse(runner.IsActive);
    }

    [Test]
    public void JumpLoopIsStopped()
    {
        Dialogue_ConversationSO loop = content.Conversation("loop", DialogueEndType.Jump);
        loop.defaultNext = loop;

        LogAssert.Expect(LogType.Error, new Regex("jumped .* times without showing anything"));
        Assert.IsFalse(runner.Start(loop));
    }

    [Test]
    public void EnteringAConversationSetsItsChapterAndRaisesEntered()
    {
        Dialogue_ConversationSO conversation = Linear("a", "one");
        conversation.chapter = content.Chapter("act-2", "Act 2 — 11:00 PM");
        List<Dialogue_ConversationSO> entered = new List<Dialogue_ConversationSO>();
        runner.ConversationEntered += entered.Add;

        runner.Start(conversation);

        Assert.AreEqual("act-2", story.ChapterID);
        CollectionAssert.AreEqual(new[] { conversation }, entered);
    }

    [Test]
    public void EndGameStopsTheDialogueAndSaysWhichEndingItWas()
    {
        Dialogue_ConversationSO ending = Linear("ending", "The shop is silent.");
        ending.endType = DialogueEndType.EndGame;
        int ended = 0;
        runner.Ended += () => ended++;

        runner.Start(ending);
        Assert.IsNull(runner.GameEnding);

        runner.Advance();

        Assert.IsFalse(runner.IsActive);
        Assert.AreEqual(1, ended);
        Assert.AreEqual(ending, runner.GameEnding);

        runner.Start(Linear("again", "New game."));
        Assert.IsNull(runner.GameEnding, "A new dialogue forgets the last ending.");
    }

    [Test]
    public void APlainEndIsNotAGameEnding()
    {
        runner.Start(Linear("a", "one"));
        runner.Advance();

        Assert.IsFalse(runner.IsActive);
        Assert.IsNull(runner.GameEnding);
    }

    #endregion

    #region Stage

    [Test]
    public void EachLineRecordsTheStageAndItCarriesOver()
    {
        Stage_ImageSO shop = content.Image("bg-shop");
        Dialogue_ConversationSO conversation = content.Conversation("c");
        DialogueLine first = content.Line(conversation, "Vance is standing inside the shop.");
        first.stage = TestContent.Cast(TestContent.On(adam, StageSlot.Left), TestContent.On(vance, StageSlot.Right));
        first.stage.background = shop;
        content.Line(conversation, "The door was locked.", vance, PortraitExpression.Smirk);

        runner.Start(conversation);
        StageSnapshot firstStage = Last.stage;
        runner.Advance();
        StageSnapshot secondStage = Last.stage;

        Assert.AreEqual("bg-shop", firstStage.backgroundID);
        Assert.AreEqual(2, firstStage.actors.Count);
        Assert.AreEqual("bg-shop", secondStage.backgroundID);
        Assert.AreEqual(PortraitExpression.Smirk, secondStage.FindActor(vance.saveID).expression);
        Assert.AreEqual(PortraitExpression.Normal, firstStage.FindActor(vance.saveID).expression, "Earlier screens keep their own stage.");
    }

    [Test]
    public void ReactionsAndChoiceOnlyScreensKeepTheStage()
    {
        Dialogue_ConversationSO followUp = content.Conversation("followUp", DialogueEndType.Choices);
        content.Choice(followUp, "Go on");

        Dialogue_ConversationSO intro = VanceIntro(out DialogueChoice give, out _, out DialogueChoice refuse);
        intro.lines[0].stage = TestContent.Cast(TestContent.On(adam, StageSlot.Left), TestContent.On(vance, StageSlot.Right));
        refuse.reactionLine = "How dare you.";
        refuse.reactionExpression = PortraitExpression.Angry;
        give.next = followUp;

        runner.Start(intro);
        runner.Advance();
        runner.Choose(refuse.id);

        Assert.AreEqual(HistoryEntryType.Reaction, Last.type);
        Assert.AreEqual(PortraitExpression.Angry, Last.stage.FindActor(vance.saveID).expression);

        runner.Stop();
        runner.Start(intro);
        runner.Advance();
        runner.Choose(give.id);

        Assert.AreEqual(HistoryEntryType.ChoiceOnly, Last.type);
        Assert.AreEqual(2, Last.stage.actors.Count);
    }

    [Test]
    public void ANewDialogueStartsWithAnEmptyStage()
    {
        Dialogue_ConversationSO first = Linear("first", "one");
        first.lines[0].stage = TestContent.Cast(TestContent.On(adam, StageSlot.Center));
        first.lines[0].stage.background = content.Image("bg-shop");

        runner.Start(first);
        runner.Advance();
        runner.Start(Linear("second", "two"));

        Assert.AreEqual("", Last.stage.backgroundID);
        Assert.AreEqual(0, Last.stage.actors.Count);
    }

    [Test]
    public void ALoadedSaveKeepsTheStageItWasSavedWith()
    {
        Dialogue_ConversationSO conversation = Linear("a", "one", "two");
        conversation.lines[0].stage = TestContent.Cast(TestContent.On(adam, StageSlot.Left), TestContent.On(vance, StageSlot.Right));
        conversation.lines[0].stage.background = content.Image("bg-shop");
        conversation.lines[1].expression = PortraitExpression.Angry;
        runner.Start(conversation);
        runner.Advance();

        DialogueRunner resumed = SaveAndReload(out _, out DialogueHistory loadedHistory);
        HistoryEntry present = loadedHistory.Entries[loadedHistory.Entries.Count - 1];

        Assert.AreEqual(1, resumed.LineIndex);
        Assert.AreEqual("bg-shop", present.stage.backgroundID);
        Assert.AreEqual(PortraitExpression.Angry, present.stage.FindActor(vance.saveID).expression);
        Assert.AreEqual(StageSlot.Left, present.stage.FindActor(adam.saveID).slot);
    }

    #endregion

    #region Save and resume

    private DialogueRunner SaveAndReload(out StoryState loadedStory, out DialogueHistory loadedHistory,
        System.Action<DialogueRunner> beforeResume = null)
    {
        GameData data = new GameData { dialogue = runner.GetSaveState(), history = history.Snapshot() };
        story.SaveTo(data);
        GameData loaded = JsonUtility.FromJson<GameData>(JsonUtility.ToJson(data));

        loadedStory = new StoryState();
        loadedStory.LoadFrom(loaded);
        loadedHistory = new DialogueHistory();
        loadedHistory.Load(loaded.history);

        DialogueRunner loadedRunner = new DialogueRunner(loadedStory, loadedHistory, content.Resolver);
        beforeResume?.Invoke(loadedRunner);
        Assert.IsTrue(loadedRunner.Resume(loaded.dialogue));
        return loadedRunner;
    }

    [Test]
    public void ResumesOnTheSameLineWithoutReapplyingEffects()
    {
        Dialogue_ConversationSO conversation = Linear("a", "one", "two", "three");
        conversation.lines[1].effects.Add(TestContent.Effect(humanity, EffectOperation.Add, 1));
        conversation.chapter = content.Chapter("act-1", "Act 1");
        runner.Start(conversation);
        runner.Advance();

        bool entered = false;
        DialogueRunner resumed = SaveAndReload(out StoryState loadedStory, out DialogueHistory loadedHistory,
            r => r.ConversationEntered += c => entered = true);

        Assert.AreEqual(1, resumed.LineIndex);
        Assert.AreEqual(1, loadedStory.Get(humanity));
        Assert.AreEqual(2, loadedHistory.Entries.Count); // No duplicate entry for the present line
        Assert.IsFalse(entered);

        resumed.Advance();
        Assert.AreEqual(2, resumed.LineIndex);
        Assert.AreEqual(1, loadedStory.Get(humanity));
    }

    [Test]
    public void ResumeFallsBackToTheLineIndexWhenTheLineIdIsGone()
    {
        runner.Start(Linear("a", "one", "two", "three"));
        runner.Advance();
        DialogueSaveState state = runner.GetSaveState();
        state.lineID = "deleted-line";

        DialogueRunner resumed = new DialogueRunner(story, new DialogueHistory(), content.Resolver);

        Assert.IsTrue(resumed.Resume(state));
        Assert.AreEqual(1, resumed.LineIndex);
    }

    [Test]
    public void ResumeFailsWhenTheConversationIsGone()
    {
        runner.Start(Linear("a", "one"));
        DialogueSaveState state = runner.GetSaveState();
        state.conversationID = "deleted-conversation";

        LogAssert.Expect(LogType.Warning, new Regex("no longer exists"));
        Assert.IsFalse(new DialogueRunner(story, new DialogueHistory(), content.Resolver).Resume(state));
    }

    [Test]
    public void ResumesAtAChoicePointWithLiveChoices()
    {
        runner.Start(VanceIntro(out _, out DialogueChoice deposit, out _));
        runner.Advance();

        DialogueRunner resumed = SaveAndReload(out _, out DialogueHistory loadedHistory);

        Assert.AreEqual(DialoguePhase.Choices, resumed.Phase);
        Assert.AreEqual(2, loadedHistory.Entries.Count);
        Assert.IsTrue(resumed.Choose(deposit.id));
        Assert.AreEqual("deposit", resumed.Conversation.saveID);
    }

    [Test]
    public void ResumesOnAReactionAndContinuesToItsNext()
    {
        runner.Start(VanceIntro(out _, out _, out DialogueChoice refuse));
        refuse.reactionLine = "How dare you.";
        refuse.effects.Add(TestContent.Effect(cynicism, EffectOperation.Add, 1));
        runner.Advance();
        runner.Choose(refuse.id);

        DialogueRunner resumed = SaveAndReload(out StoryState loadedStory, out DialogueHistory loadedHistory);

        Assert.AreEqual(DialoguePhase.Reaction, resumed.Phase);
        Assert.AreEqual(3, loadedHistory.Entries.Count);
        Assert.AreEqual(1, loadedStory.Get(cynicism));

        resumed.Advance();
        Assert.AreEqual("refuse", resumed.Conversation.saveID);
    }

    [Test]
    public void InactiveDialogueSavesAsInactive()
    {
        Assert.IsFalse(runner.GetSaveState().isActive);
        Assert.IsFalse(new DialogueRunner(story, history, content.Resolver).Resume(runner.GetSaveState()));
    }

    #endregion
}
