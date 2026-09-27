using System;
using System.Collections.Generic;
using UnityEngine;

// Finds content by saveID when loading a save. Right-click the asset > "Auto-Fill & Validate" after adding content.
[CreateAssetMenu(menuName = "Spooktober/Dialogue Data/Dialogue Database", fileName = "Dialogue Database")]
public class Dialogue_DatabaseSO : ScriptableObject
{
    public List<Dialogue_ConversationSO> conversations = new List<Dialogue_ConversationSO>();
    public List<Dialogue_SpeakerSO> speakers = new List<Dialogue_SpeakerSO>();
    public List<Story_ChapterSO> chapters = new List<Story_ChapterSO>();
    public List<Story_VariableSO> variables = new List<Story_VariableSO>();

    [NonSerialized] private Dictionary<string, Dialogue_ConversationSO> conversationLookup;
    [NonSerialized] private Dictionary<string, Story_ChapterSO> chapterLookup;

    public Dialogue_ConversationSO GetConversation(string saveID) => Find(conversations, saveID, c => c.saveID, ref conversationLookup);
    public Story_ChapterSO GetChapter(string saveID) => Find(chapters, saveID, c => c.saveID, ref chapterLookup);

    private void OnEnable() => ClearCache();
    private void OnValidate() => ClearCache();

    private void ClearCache()
    {
        conversationLookup = null;
        chapterLookup = null;
    }

    private static T Find<T>(List<T> items, string saveID, Func<T, string> getId, ref Dictionary<string, T> lookup) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(saveID))
            return null;

        if (lookup == null)
        {
            lookup = new Dictionary<string, T>();

            foreach (T item in items)
            {
                if (item == null || string.IsNullOrEmpty(getId(item)))
                    continue;

                lookup[getId(item)] = item;
            }
        }

        lookup.TryGetValue(saveID, out T result);
        return result;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto-Fill & Validate")]
    public void AutoFillAndValidate()
    {
        conversations = FindAllAssets<Dialogue_ConversationSO>();
        speakers = FindAllAssets<Dialogue_SpeakerSO>();
        chapters = FindAllAssets<Story_ChapterSO>();
        variables = FindAllAssets<Story_VariableSO>();

        // New assets have an empty saveID until they're re-validated, so stamp everything here
        foreach (Dialogue_ConversationSO conversation in conversations)
        {
            bool changed = SaveIDUtility.StampAssetGUID(conversation, ref conversation.saveID);
            changed |= conversation.EnsureIds();

            if (changed)
                UnityEditor.EditorUtility.SetDirty(conversation);

            ValidateConversation(conversation);
        }

        foreach (Dialogue_SpeakerSO speaker in speakers)
            SaveIDUtility.StampAssetGUID(speaker, ref speaker.saveID);

        foreach (Story_ChapterSO chapter in chapters)
            SaveIDUtility.StampAssetGUID(chapter, ref chapter.saveID);

        foreach (Story_VariableSO variable in variables)
            SaveIDUtility.StampAssetGUID(variable, ref variable.saveID);

        ClearCache();
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();

        Debug.Log($"Dialogue Database: {conversations.Count} conversations, {speakers.Count} speakers, {chapters.Count} chapters, {variables.Count} variables.", this);
    }

    private static void ValidateConversation(Dialogue_ConversationSO conversation)
    {
        if (conversation.endType == DialogueEndType.Choices && conversation.choices.Count == 0)
            Debug.LogWarning($"'{conversation.name}' ends in Choices but has no choices.", conversation);

        if (conversation.endType != DialogueEndType.Choices && conversation.choices.Count > 0)
            Debug.LogWarning($"'{conversation.name}' has choices but its End Type is {conversation.endType}, so they will never show.", conversation);

        for (int i = 0; i < conversation.lines.Count; i++)
        {
            if (conversation.lines[i] != null && string.IsNullOrWhiteSpace(conversation.lines[i].text))
                Debug.LogWarning($"'{conversation.name}' line {i} has no text.", conversation);
        }

        foreach (DialogueChoice choice in conversation.choices)
        {
            if (choice == null)
                continue;

            if (string.IsNullOrWhiteSpace(choice.text))
                Debug.LogWarning($"'{conversation.name}' has a choice with no text.", conversation);

            if (choice.next == null && !choice.HasReaction)
                Debug.Log($"'{conversation.name}' choice \"{choice.text}\" ends the dialogue (no reaction, no next).", conversation);
        }
    }

    private static List<T> FindAllAssets<T>() where T : ScriptableObject
    {
        List<T> result = new List<T>();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(T).Name);

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset != null)
                result.Add(asset);
        }

        result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return result;
    }
#endif
}
