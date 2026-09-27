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

    [Tooltip("Applied once when this line is shown, e.g. advance the clock or raise the curse.")]
    public List<StoryEffect> effects = new List<StoryEffect>();

    [HideInInspector] public string id; // Stable id for saves and localization, generated automatically
}
