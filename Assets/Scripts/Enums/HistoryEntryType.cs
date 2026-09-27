// Saved as ints in save files, so only ever append new values
public enum HistoryEntryType
{
    Line = 0,       // A line from a conversation
    Reaction = 1,   // A choice's reaction line
    ChoiceOnly = 2  // Choices that appeared without a new line (they borrow the previous line's text)
}
