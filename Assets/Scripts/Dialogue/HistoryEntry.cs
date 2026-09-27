using System;
using System.Collections.Generic;

// One screen the player saw. Only ids are stored; text, speaker and portrait come from the content,
// so the backlog and rollback follow text fixes and (later) the selected language.
[Serializable]
public class HistoryEntry
{
    public HistoryEntryType type;
    public int session;                                     // Which dialogue session this belongs to (rollback can't leave it)
    public string conversationID = "";
    public string itemID = "";                              // Line id (Line) or choice id (Reaction). Empty for ChoiceOnly.

    public string choicesConversationID = "";               // The conversation whose choices were shown on this screen
    public List<string> shownChoiceIDs = new List<string>();
    public string pickedChoiceID = "";

    public bool HasChoices => shownChoiceIDs != null && shownChoiceIDs.Count > 0;
    public bool HasPick => !string.IsNullOrEmpty(pickedChoiceID);
}
