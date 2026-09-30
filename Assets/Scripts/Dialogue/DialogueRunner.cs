using System;
using System.Collections.Generic;
using UnityEngine;

// The dialogue flow, with no UI. This is the only place story effects, chapter changes and history entries happen,
// so rollback and loading (which only re-display history) can never apply an effect twice.
// Each history entry also stores the stage behind it (see StageSnapshot), worked out here from the lines' stage directions.
public class DialogueRunner
{
    public const int MaxJumpsWithoutContent = 64;

    private readonly StoryState story;
    private readonly DialogueHistory history;
    private readonly Func<string, Dialogue_ConversationSO> findConversation;
    private bool resuming;

    public DialoguePhase Phase { get; private set; }
    public Dialogue_ConversationSO Conversation { get; private set; }
    public int LineIndex { get; private set; } = -1;
    public DialogueChoice PendingReaction { get; private set; }
    public Dialogue_ConversationSO GameEnding { get; private set; } // The End Game conversation that just ended the dialogue, if any

    public bool IsActive => Phase != DialoguePhase.Inactive;
    public DialogueLine CurrentLine => Conversation != null ? Conversation.GetLine(LineIndex) : null;

    // The stage on screen right now. A new session starts with an empty stage.
    private StageSnapshot CurrentStage => history.Present?.stage ?? new StageSnapshot();

    public event Action<Dialogue_ConversationSO> ConversationEntered; // After its first screen is up (not raised when resuming)
    public event Action Ended;

    public DialogueRunner(StoryState story, DialogueHistory history, Func<string, Dialogue_ConversationSO> findConversation)
    {
        this.story = story;
        this.history = history;
        this.findConversation = findConversation;
    }

    public bool Start(Dialogue_ConversationSO conversation)
    {
        if (conversation == null || IsActive)
            return false;

        GameEnding = null;
        history.BeginSession();
        Enter(conversation, 0);
        return IsActive;
    }

    public void Advance()
    {
        switch (Phase)
        {
            case DialoguePhase.Line:
                if (LineIndex < Conversation.LastLineIndex)
                    ShowLine(LineIndex + 1);
                else
                    HandleEnd(0);
                break;

            case DialoguePhase.Reaction:
                DialogueChoice reactedChoice = PendingReaction;
                PendingReaction = null;
                Enter(reactedChoice != null ? reactedChoice.next : null, 0);
                break;
        }
    }

    public bool CanChoose(DialogueChoice choice) => choice != null && story.Evaluate(choice.requirements);

    public bool Choose(string choiceID)
    {
        if (Phase != DialoguePhase.Choices)
            return false;

        HistoryEntry present = history.Present;
        DialogueChoice choice = Conversation.GetChoice(choiceID);

        if (choice == null || present == null || !present.shownChoiceIDs.Contains(choiceID) || present.HasPick)
            return false;

        if (!CanChoose(choice))
            return false; // Locked

        present.pickedChoiceID = choiceID;
        story.Apply(choice.effects);

        if (choice.HasReaction)
        {
            PendingReaction = choice;
            Phase = DialoguePhase.Reaction;
            history.Append(new HistoryEntry
            {
                type = HistoryEntryType.Reaction,
                conversationID = Conversation.saveID,
                itemID = choice.id,
                stage = StageAfterReaction(choice)
            });
        }
        else
        {
            Enter(choice.next, 0);
        }

        return true;
    }

    public void Stop()
    {
        bool wasActive = IsActive;

        Phase = DialoguePhase.Inactive;
        Conversation = null;
        LineIndex = -1;
        PendingReaction = null;
        history.EndSession();

        // A resume that falls through to the end never showed anything, so there's nothing to close
        if (wasActive && !resuming)
            Ended?.Invoke();
    }

    public DialogueSaveState GetSaveState()
    {
        if (!IsActive)
            return new DialogueSaveState();

        DialogueLine line = CurrentLine;

        return new DialogueSaveState
        {
            isActive = true,
            conversationID = Conversation.saveID,
            phase = Phase,
            lineID = line != null ? line.id : "",
            lineIndex = LineIndex,
            reactionChoiceID = PendingReaction != null ? PendingReaction.id : "",
            session = history.CurrentSession
        };
    }

    // Puts the dialogue back where a save left it. Effects and chapter changes are NOT re-applied,
    // the loaded story state already contains them.
    public bool Resume(DialogueSaveState state)
    {
        if (state == null || !state.isActive || IsActive)
            return false;

        Dialogue_ConversationSO conversation = findConversation?.Invoke(state.conversationID);
        if (conversation == null)
        {
            Debug.LogWarning($"Can't resume dialogue: conversation '{state.conversationID}' no longer exists.");
            return false;
        }

        resuming = true;
        GameEnding = null;
        history.ResumeSession(state.session);
        Conversation = conversation;
        PendingReaction = null;
        Phase = DialoguePhase.Line; // Marks the runner active while restoring

        switch (state.phase)
        {
            case DialoguePhase.Reaction:
                ResumeReaction(state);
                break;

            case DialoguePhase.Choices when conversation.endType == DialogueEndType.Choices:
                ResumeChoices();
                break;

            default:
                ResumeLine(state);
                break;
        }

        resuming = false;
        return IsActive;
    }

    private void ResumeLine(DialogueSaveState state)
    {
        if (Conversation.lines.Count == 0)
        {
            HandleEnd(0);
            return;
        }

        int index = Conversation.IndexOfLine(state.lineID);
        if (index < 0)
            index = Mathf.Clamp(state.lineIndex, 0, Conversation.LastLineIndex);

        LineIndex = index;
        Phase = DialoguePhase.Line;
        EnsurePresent(HistoryEntryType.Line, Conversation.lines[index]);

        if (index == Conversation.LastLineIndex && Conversation.endType == DialogueEndType.Choices)
            ResumeChoices();
    }

    private void ResumeChoices()
    {
        LineIndex = Conversation.LastLineIndex;
        HistoryEntry present = history.Present;

        if (present != null && present.choicesConversationID == Conversation.saveID && present.HasChoices && !present.HasPick)
        {
            Phase = DialoguePhase.Choices;
            return;
        }

        if (LineIndex >= 0)
            EnsurePresent(HistoryEntryType.Line, Conversation.lines[LineIndex]);

        Phase = DialoguePhase.Line;

        if (!PresentChoices() && LineIndex < 0)
        {
            Debug.LogWarning($"'{Conversation.name}' has no choices available, ending the dialogue.");
            Stop();
        }
    }

    private void ResumeReaction(DialogueSaveState state)
    {
        DialogueChoice choice = Conversation.GetChoice(state.reactionChoiceID);

        if (choice == null || !choice.HasReaction)
        {
            Enter(choice != null ? choice.next : null, 0);
            return;
        }

        LineIndex = Conversation.LastLineIndex;
        PendingReaction = choice;
        Phase = DialoguePhase.Reaction;
        EnsurePresent(HistoryEntryType.Reaction, choice.id, StageAfterReaction(choice));
    }

    private void EnsurePresent(HistoryEntryType type, DialogueLine line) =>
        EnsurePresent(type, line != null ? line.id : "", StageAfterLine(line));

    // Keeps "the present is the last history entry" true after a load. A saved entry keeps its saved stage.
    private void EnsurePresent(HistoryEntryType type, string itemID, StageSnapshot stageIfAdded)
    {
        HistoryEntry present = history.Present;

        if (present != null && present.type == type && present.conversationID == Conversation.saveID && present.itemID == itemID)
            return;

        history.Append(new HistoryEntry { type = type, conversationID = Conversation.saveID, itemID = itemID, stage = stageIfAdded });
    }

    private StageSnapshot StageAfterLine(DialogueLine line) =>
        line != null ? CurrentStage.With(line.stage, line.speaker, line.expression) : CurrentStage.Clone();

    private StageSnapshot StageAfterReaction(DialogueChoice choice)
    {
        Dialogue_SpeakerSO speaker = choice.reactionSpeaker != null ? choice.reactionSpeaker : Conversation.LastLineSpeaker;
        return CurrentStage.With(null, speaker, choice.reactionExpression);
    }

    private void Enter(Dialogue_ConversationSO conversation, int jumps)
    {
        if (conversation == null)
        {
            Stop();
            return;
        }

        if (jumps > MaxJumpsWithoutContent)
        {
            Debug.LogError($"Dialogue jumped {jumps} times without showing anything, stopping at '{conversation.name}'. Check for a Jump loop.");
            Stop();
            return;
        }

        Conversation = conversation;
        PendingReaction = null;
        LineIndex = -1;

        if (conversation.chapter != null)
            story.SetChapter(conversation.chapter.saveID);

        if (conversation.lines.Count > 0)
            ShowLine(0);
        else
            HandleEnd(jumps);

        // Only when this conversation's own screen is up (a jump may have moved on to another one)
        if (!resuming && IsActive && Conversation == conversation)
            ConversationEntered?.Invoke(conversation);
    }

    private void ShowLine(int index)
    {
        DialogueLine line = Conversation.lines[index];

        LineIndex = index;
        Phase = DialoguePhase.Line;

        if (line != null)
            story.Apply(line.effects);

        history.Append(new HistoryEntry
        {
            type = HistoryEntryType.Line,
            conversationID = Conversation.saveID,
            itemID = line != null ? line.id : "",
            stage = StageAfterLine(line) // Worked out before Append changes what the present entry is
        });

        // The last line and its choices are one screen
        if (index == Conversation.LastLineIndex && Conversation.endType == DialogueEndType.Choices)
            PresentChoices();
    }

    private void HandleEnd(int jumps)
    {
        switch (Conversation.endType)
        {
            case DialogueEndType.Choices:
                if (!PresentChoices())
                {
                    Debug.LogWarning($"'{Conversation.name}' has no choices available, ending the dialogue.");
                    Stop();
                }
                break;

            case DialogueEndType.Jump:
                Enter(ResolveJump(Conversation), jumps + 1);
                break;

            case DialogueEndType.EndGame:
                GameEnding = Conversation; // Read by whoever handles Ended
                Stop();
                break;

            default:
                Stop();
                break;
        }
    }

    private Dialogue_ConversationSO ResolveJump(Dialogue_ConversationSO conversation)
    {
        foreach (DialogueBranch branch in conversation.branches)
        {
            if (branch != null && story.Evaluate(branch.conditions))
                return branch.next;
        }

        return conversation.defaultNext;
    }

    private List<string> GetVisibleChoiceIDs()
    {
        List<string> visible = new List<string>();

        foreach (DialogueChoice choice in Conversation.choices)
        {
            if (choice == null || string.IsNullOrEmpty(choice.id))
                continue;

            if (choice.showWhenLocked || CanChoose(choice))
                visible.Add(choice.id);
        }

        return visible;
    }

    private bool PresentChoices()
    {
        List<string> visible = GetVisibleChoiceIDs();

        if (visible.Count == 0)
            return false;

        // Attach the choices to whatever is on screen (the last line, or a reaction) so they stay one screen.
        // If that screen already had choices, add a choices-only screen that borrows its text.
        HistoryEntry present = history.Present;
        bool canAttach = present != null && present.type != HistoryEntryType.ChoiceOnly && !present.HasChoices;

        HistoryEntry target = canAttach
            ? present
            : history.Append(new HistoryEntry { type = HistoryEntryType.ChoiceOnly, conversationID = Conversation.saveID, stage = CurrentStage.Clone() });

        target.choicesConversationID = Conversation.saveID;
        target.shownChoiceIDs = visible;
        target.pickedChoiceID = "";

        Phase = DialoguePhase.Choices;
        return true;
    }
}
