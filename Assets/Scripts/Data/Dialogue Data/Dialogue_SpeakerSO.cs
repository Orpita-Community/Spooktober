using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Spooktober/Dialogue Data/New Speaker Data", fileName = "Speaker - ")]
public class Dialogue_SpeakerSO : ScriptableObject
{
    public string saveID;

    [Header("Speaker Info")]
    public string speakerName;
    public Color nameColor = Color.white;

    [Header("Portraits")]
    [Tooltip("Used when the requested expression has no sprite.")]
    public Sprite defaultPortrait;
    public List<SpeakerPortrait> portraits = new List<SpeakerPortrait>();

    [Header("Stage")]
    [Tooltip("Height of the portrait on stage, in canvas pixels (the screen is 1080 tall). Full-body art can be taller than the screen.")]
    public float stageHeight = 1560f;
    [Tooltip("Where the bottom of the portrait sits, measured from the bottom of the screen. Negative values push the legs off screen.")]
    public float stageOffsetY = -540f;

    public Sprite GetPortrait(PortraitExpression expression)
    {
        foreach (SpeakerPortrait portrait in portraits)
        {
            if (portrait != null && portrait.expression == expression && portrait.sprite != null)
                return portrait.sprite;
        }

        return defaultPortrait;
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        SaveIDUtility.StampAssetGUID(this, ref saveID);
#endif
    }
}
