using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueChoice
{
    [TextArea(1, 3)] public string text;

    [Header("Requirements")]
    public List<StoryCondition> requirements = new List<StoryCondition>();
    [Tooltip("When the requirements fail, show the choice greyed out instead of hiding it.")]
    public bool showWhenLocked;

    [Header("Effects")]
    [Tooltip("Applied when the choice is picked, e.g. Humanity +1. Never shown to the player.")]
    public List<StoryEffect> effects = new List<StoryEffect>();

    [Header("Reaction (optional)")]
    [Tooltip("Who reacts to this choice. Empty = the speaker of the last line before the choices.")]
    public Dialogue_SpeakerSO reactionSpeaker;
    public PortraitExpression reactionExpression;
    [Tooltip("Shown right after picking, with the reaction portrait. Leave empty for no reaction.")]
    [TextArea(2, 5)] public string reactionLine;

    [Header("Next")]
    [Tooltip("Where the dialogue continues. Empty = end the dialogue. A conversation with no lines that ends in Choices shows its choices right under this reaction.")]
    public Dialogue_ConversationSO next;

    [HideInInspector] public string id; // Stable id for saves and localization, generated automatically

    public bool HasReaction => !string.IsNullOrWhiteSpace(reactionLine);
}
