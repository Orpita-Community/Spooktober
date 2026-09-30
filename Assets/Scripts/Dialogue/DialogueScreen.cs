using System;
using System.Collections.Generic;
using UnityEngine;

// What the dialogue box shows for one history entry. The live screen and every rollback screen are built
// the same way, so rollback always looks exactly like the moment the player first saw it.
public class DialogueScreen
{
    public class ChoiceView
    {
        public string id;
        public string text;
        public bool interactable; // Can be picked right now
        public bool locked;       // Visible but its requirements fail
        public bool picked;       // The option the player chose (shown during rollback)
    }

    public class BacklogItem
    {
        public string speakerName;
        public Color nameColor;
        public string text;
        public bool isChoice;
    }

    public bool isLive;
    public bool isResolved;        // False when the content this entry points to no longer exists
    public HistoryEntryType type;
    public Dialogue_SpeakerSO speaker;
    public string speakerName = "";
    public Color nameColor = Color.white;
    public Sprite portrait;
    public bool speakerOnStage;    // The speaker is standing on the stage, so the box doesn't need their portrait
    public DialogueLineStyle style;
    public string text = "";
    public List<ChoiceView> choices = new List<ChoiceView>();
    public StageSnapshot stage = new StageSnapshot();

    public bool HasChoices => choices.Count > 0;

    public static DialogueScreen Build(IReadOnlyList<HistoryEntry> entries, int index, bool live,
        Func<string, Dialogue_ConversationSO> findConversation, StoryState story)
    {
        DialogueScreen screen = new DialogueScreen { isLive = live };

        if (entries == null || index < 0 || index >= entries.Count)
            return screen;

        HistoryEntry entry = entries[index];
        screen.type = entry.type;
        screen.stage = entry.stage ?? new StageSnapshot();

        // A choices-only screen shows the text that was on screen before it
        HistoryEntry textEntry = entry.type == HistoryEntryType.ChoiceOnly ? FindTextEntry(entries, index) : entry;
        screen.isResolved = textEntry == null || ResolveText(textEntry, findConversation, screen);
        screen.speakerOnStage = screen.speaker != null && screen.stage.HasActor(screen.speaker.saveID);

        if (entry.HasChoices)
            BuildChoices(entry, live, findConversation, story, screen);

        return screen;
    }

    public static List<BacklogItem> BuildBacklog(IReadOnlyList<HistoryEntry> entries, Func<string, Dialogue_ConversationSO> findConversation)
    {
        List<BacklogItem> items = new List<BacklogItem>();

        if (entries == null)
            return items;

        foreach (HistoryEntry entry in entries)
        {
            if (entry.type != HistoryEntryType.ChoiceOnly)
            {
                DialogueScreen screen = new DialogueScreen();

                // Entries whose content was removed are skipped instead of showing "…"
                if (ResolveText(entry, findConversation, screen) && !string.IsNullOrEmpty(screen.text))
                {
                    items.Add(new BacklogItem
                    {
                        speakerName = screen.speakerName,
                        nameColor = screen.nameColor,
                        text = screen.text
                    });
                }
            }

            if (entry.HasPick)
            {
                Dialogue_ConversationSO choicesConversation = findConversation?.Invoke(entry.choicesConversationID);
                DialogueChoice picked = choicesConversation != null ? choicesConversation.GetChoice(entry.pickedChoiceID) : null;

                if (picked != null)
                    items.Add(new BacklogItem { text = DialogueText.Choice(choicesConversation, picked), isChoice = true });
            }
        }

        return items;
    }

    private static HistoryEntry FindTextEntry(IReadOnlyList<HistoryEntry> entries, int index)
    {
        int session = entries[index].session;

        for (int i = index - 1; i >= 0 && entries[i].session == session; i--)
        {
            if (entries[i].type != HistoryEntryType.ChoiceOnly)
                return entries[i];
        }

        return null;
    }

    private static bool ResolveText(HistoryEntry entry, Func<string, Dialogue_ConversationSO> findConversation, DialogueScreen screen)
    {
        Dialogue_ConversationSO conversation = findConversation?.Invoke(entry.conversationID);

        Dialogue_SpeakerSO speaker = null;
        PortraitExpression expression = PortraitExpression.Normal;
        DialogueLineStyle style = DialogueLineStyle.Box;
        string text = null;

        if (conversation != null && entry.type == HistoryEntryType.Line)
        {
            DialogueLine line = conversation.GetLine(entry.itemID);
            if (line != null)
            {
                speaker = line.speaker;
                expression = line.expression;
                style = line.style;
                text = DialogueText.Line(conversation, line);
            }
        }
        else if (conversation != null && entry.type == HistoryEntryType.Reaction)
        {
            DialogueChoice choice = conversation.GetChoice(entry.itemID);
            if (choice != null)
            {
                speaker = choice.reactionSpeaker != null ? choice.reactionSpeaker : conversation.LastLineSpeaker;
                expression = choice.reactionExpression;
                text = DialogueText.Reaction(conversation, choice);
            }
        }

        if (text == null)
        {
            screen.text = DialogueText.Missing;
            return false;
        }

        screen.speaker = speaker;
        screen.speakerName = DialogueText.SpeakerName(speaker);
        screen.nameColor = speaker != null ? speaker.nameColor : Color.white;
        screen.portrait = speaker != null ? speaker.GetPortrait(expression) : null;
        screen.style = style;
        screen.text = text;
        return true;
    }

    private static void BuildChoices(HistoryEntry entry, bool live, Func<string, Dialogue_ConversationSO> findConversation,
        StoryState story, DialogueScreen screen)
    {
        Dialogue_ConversationSO conversation = findConversation?.Invoke(entry.choicesConversationID);
        if (conversation == null)
            return;

        bool pending = live && !entry.HasPick;

        foreach (string choiceID in entry.shownChoiceIDs)
        {
            DialogueChoice choice = conversation.GetChoice(choiceID);
            if (choice == null)
                continue;

            bool requirementsMet = story == null || story.Evaluate(choice.requirements);

            screen.choices.Add(new ChoiceView
            {
                id = choiceID,
                text = DialogueText.Choice(conversation, choice),
                interactable = pending && requirementsMet,
                locked = pending && !requirementsMet,
                picked = choiceID == entry.pickedChoiceID
            });
        }
    }
}
