using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    [Tooltip("Empty = narrator (no name or portrait).")]
    public Dialogue_SpeakerSO speaker;
    public PortraitExpression expression;
    [TextArea(2, 5)] public string text;
    [Tooltip("Box = the dialogue box. Centered = large text on a darkened screen, for captions and inscriptions.")]
    public DialogueLineStyle style;

    [Tooltip("Applied once when this line is shown, e.g. advance the clock or raise the curse.")]
    public List<StoryEffect> effects = new List<StoryEffect>();

    [Tooltip("Background, characters and close-up changes that happen when this line shows.")]
    public StageDirection stage = new StageDirection();

    [HideInInspector] public string id; // Stable id for saves and localization, generated automatically
}
