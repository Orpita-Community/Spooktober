using System;

// Where the dialogue was when the game was saved. The rollback position is never saved: saving always saves the present.
[Serializable]
public class DialogueSaveState
{
    public bool isActive;
    public string conversationID = "";
    public DialoguePhase phase;
    public string lineID = "";
    public int lineIndex = -1;        // Fallback when the line id no longer exists after a content edit
    public string reactionChoiceID = "";
    public int session = -1;
}
