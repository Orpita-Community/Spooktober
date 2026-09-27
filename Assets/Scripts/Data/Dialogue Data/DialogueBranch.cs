using System;
using System.Collections.Generic;

[Serializable]
public class DialogueBranch
{
    public List<StoryCondition> conditions = new List<StoryCondition>();
    public Dialogue_ConversationSO next;
}
