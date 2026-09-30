using System;
using UnityEngine;

[Serializable]
public class StageCharacter
{
    public Dialogue_SpeakerSO character;
    public StageSlot slot;
    [Tooltip("Their portrait until they speak. When they speak, the line's expression takes over.")]
    public PortraitExpression expression;
    [Tooltip("Mirror the portrait so the character faces the other way.")]
    public bool flip;
}
