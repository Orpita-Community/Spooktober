using System;
using System.Collections.Generic;
using UnityEngine;

// Builds throwaway dialogue/story assets in memory for tests. Dispose() destroys everything it created.
public class TestContent : IDisposable
{
    private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
    private readonly Dictionary<string, Dialogue_ConversationSO> conversations = new Dictionary<string, Dialogue_ConversationSO>();
    private readonly Dictionary<string, Dialogue_SpeakerSO> speakers = new Dictionary<string, Dialogue_SpeakerSO>();
    private readonly Dictionary<string, Stage_ImageSO> images = new Dictionary<string, Stage_ImageSO>();

    public Func<string, Dialogue_ConversationSO> Resolver => Find;

    public Dialogue_ConversationSO Find(string saveID)
    {
        if (saveID == null)
            return null;

        conversations.TryGetValue(saveID, out Dialogue_ConversationSO conversation);
        return conversation;
    }

    public Dialogue_SpeakerSO FindSpeaker(string saveID) => saveID != null && speakers.TryGetValue(saveID, out Dialogue_SpeakerSO speaker) ? speaker : null;
    public Stage_ImageSO FindImage(string saveID) => saveID != null && images.TryGetValue(saveID, out Stage_ImageSO image) ? image : null;

    public Story_VariableSO Variable(string saveID, int defaultValue = 0)
    {
        Story_VariableSO variable = Create<Story_VariableSO>(saveID);
        variable.saveID = saveID;
        variable.defaultValue = defaultValue;
        return variable;
    }

    public Story_ChapterSO Chapter(string saveID, string label)
    {
        Story_ChapterSO chapter = Create<Story_ChapterSO>(saveID);
        chapter.saveID = saveID;
        chapter.label = label;
        return chapter;
    }

    public Dialogue_SpeakerSO Speaker(string name, params PortraitExpression[] expressions)
    {
        Dialogue_SpeakerSO speaker = Create<Dialogue_SpeakerSO>(name);
        speaker.saveID = "speaker-" + name;
        speaker.speakerName = name;
        speaker.defaultPortrait = MakeSprite(name + " default");

        foreach (PortraitExpression expression in expressions)
            speaker.portraits.Add(new SpeakerPortrait { expression = expression, sprite = MakeSprite($"{name} {expression}") });

        speakers[speaker.saveID] = speaker;
        return speaker;
    }

    public Stage_ImageSO Image(string saveID)
    {
        Stage_ImageSO image = Create<Stage_ImageSO>(saveID);
        image.saveID = saveID;
        image.sprite = MakeSprite(saveID);
        images[saveID] = image;
        return image;
    }

    public static StageCharacter On(Dialogue_SpeakerSO character, StageSlot slot, PortraitExpression expression = PortraitExpression.Normal) =>
        new StageCharacter { character = character, slot = slot, expression = expression };

    public static StageDirection Cast(params StageCharacter[] cast) =>
        new StageDirection { characters = StageChange.Set, cast = new List<StageCharacter>(cast) };

    public Dialogue_ConversationSO Conversation(string saveID, DialogueEndType endType = DialogueEndType.End)
    {
        Dialogue_ConversationSO conversation = Create<Dialogue_ConversationSO>(saveID);
        conversation.saveID = saveID;
        conversation.endType = endType;
        conversations[saveID] = conversation;
        return conversation;
    }

    public DialogueLine Line(Dialogue_ConversationSO conversation, string text, Dialogue_SpeakerSO speaker = null,
        PortraitExpression expression = PortraitExpression.Normal)
    {
        DialogueLine line = new DialogueLine
        {
            id = $"{conversation.saveID}-line{conversation.lines.Count}",
            text = text,
            speaker = speaker,
            expression = expression
        };

        conversation.lines.Add(line);
        return line;
    }

    public DialogueChoice Choice(Dialogue_ConversationSO conversation, string text, Dialogue_ConversationSO next = null)
    {
        DialogueChoice choice = new DialogueChoice
        {
            id = $"{conversation.saveID}-choice{conversation.choices.Count}",
            text = text,
            next = next
        };

        conversation.choices.Add(choice);
        return choice;
    }

    public static StoryEffect Effect(Story_VariableSO variable, EffectOperation operation, int value) =>
        new StoryEffect { variable = variable, operation = operation, value = value };

    public static StoryCondition Condition(Story_VariableSO variable, ComparisonType comparison, int constant) =>
        new StoryCondition { variable = variable, comparison = comparison, compareTo = CompareTarget.Constant, constant = constant };

    public static StoryCondition Condition(Story_VariableSO variable, ComparisonType comparison, Story_VariableSO other) =>
        new StoryCondition { variable = variable, comparison = comparison, compareTo = CompareTarget.Variable, otherVariable = other };

    private Sprite MakeSprite(string name)
    {
        Texture2D texture = new Texture2D(1, 1);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero);
        sprite.name = name;
        created.Add(texture);
        created.Add(sprite);
        return sprite;
    }

    private T Create<T>(string name) where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        asset.name = name;
        created.Add(asset);
        return asset;
    }

    public void Dispose()
    {
        foreach (UnityEngine.Object obj in created)
        {
            if (obj != null)
                UnityEngine.Object.DestroyImmediate(obj);
        }

        created.Clear();
        conversations.Clear();
        speakers.Clear();
        images.Clear();
    }
}
