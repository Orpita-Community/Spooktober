using System.Collections.Generic;
using UnityEngine;

// One stretch of dialogue: an ordered list of lines, then choices, a jump to another conversation, or the end.
[CreateAssetMenu(menuName = "Spooktober/Dialogue Data/New Conversation", fileName = "Conv - ")]
public class Dialogue_ConversationSO : ScriptableObject
{
    public string saveID;

    [Header("Conversation Info")]
    [Tooltip("Optional. Becomes the current chapter (shown on save slots) when this conversation starts.")]
    public Story_ChapterSO chapter;
    [Tooltip("Write the autosave slot once the first line of this conversation is shown.")]
    public bool autosaveOnStart;

    [Header("Lines")]
    public List<DialogueLine> lines = new List<DialogueLine>();

    [Header("Ending")]
    public DialogueEndType endType;
    [Tooltip("Used when End Type is Choices.")]
    public List<DialogueChoice> choices = new List<DialogueChoice>();
    [Tooltip("Used when End Type is Jump. The first branch whose conditions all pass wins.")]
    public List<DialogueBranch> branches = new List<DialogueBranch>();
    [Tooltip("Used when End Type is Jump and no branch passes. Empty = end the dialogue.")]
    public Dialogue_ConversationSO defaultNext;

    public int LastLineIndex => lines.Count - 1;
    public Dialogue_SpeakerSO LastLineSpeaker => lines.Count > 0 ? lines[lines.Count - 1]?.speaker : null;

    public DialogueLine GetLine(int index) => index >= 0 && index < lines.Count ? lines[index] : null;

    public int IndexOfLine(string lineID)
    {
        if (string.IsNullOrEmpty(lineID))
            return -1;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i] != null && lines[i].id == lineID)
                return i;
        }

        return -1;
    }

    public DialogueLine GetLine(string lineID) => GetLine(IndexOfLine(lineID));

    public DialogueChoice GetChoice(string choiceID)
    {
        if (string.IsNullOrEmpty(choiceID))
            return null;

        foreach (DialogueChoice choice in choices)
        {
            if (choice != null && choice.id == choiceID)
                return choice;
        }

        return null;
    }

    public bool EnsureIds()
    {
        HashSet<string> usedIds = new HashSet<string>();
        bool changed = SaveIDUtility.EnsureUniqueIds(lines, line => line.id, (line, id) => line.id = id, usedIds);
        changed |= SaveIDUtility.EnsureUniqueIds(choices, choice => choice.id, (choice, id) => choice.id = id, usedIds);
        return changed;
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        SaveIDUtility.StampAssetGUID(this, ref saveID);

        if (EnsureIds())
            UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
