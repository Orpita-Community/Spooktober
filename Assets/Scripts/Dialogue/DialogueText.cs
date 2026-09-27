using System.Text.RegularExpressions;

// The single place player-facing dialogue text comes from.
// Today it returns the text typed into the assets. When localization is added, only this class changes:
// look the key up in a String Table and fall back to the raw text when the entry is missing.
public static class DialogueText
{
    public const string Missing = "…";

    private static readonly Regex richTextTags = new Regex("<.*?>", RegexOptions.Compiled);

    public static string Line(Dialogue_ConversationSO conversation, DialogueLine line) => line != null ? line.text : Missing;
    public static string Choice(Dialogue_ConversationSO conversation, DialogueChoice choice) => choice != null ? choice.text : Missing;
    public static string Reaction(Dialogue_ConversationSO conversation, DialogueChoice choice) => choice != null ? choice.reactionLine : Missing;
    public static string SpeakerName(Dialogue_SpeakerSO speaker) => speaker != null ? speaker.speakerName : "";
    public static string Chapter(Story_ChapterSO chapter) => chapter != null ? chapter.label : "";

    // Future String Table keys
    public static string LineKey(Dialogue_ConversationSO conversation, DialogueLine line) => $"{conversation.saveID}.{line.id}";
    public static string ChoiceKey(Dialogue_ConversationSO conversation, DialogueChoice choice) => $"{conversation.saveID}.{choice.id}";
    public static string ReactionKey(Dialogue_ConversationSO conversation, DialogueChoice choice) => $"{conversation.saveID}.{choice.id}.reaction";
    public static string SpeakerKey(Dialogue_SpeakerSO speaker) => $"speaker.{speaker.saveID}";
    public static string ChapterKey(Story_ChapterSO chapter) => $"chapter.{chapter.saveID}";

    public static string StripTags(string richText) => string.IsNullOrEmpty(richText) ? "" : richTextTags.Replace(richText, "");

    public static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? "";

        return text.Substring(0, maxLength - 1).TrimEnd() + "…";
    }
}
