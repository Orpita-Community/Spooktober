using UnityEngine;

// A picture the dialogue can put on the stage: a background, or a close-up shown over it (a letter, a box...).
// Saves store its saveID, so it has to be in the Dialogue Database (Auto-Fill & Validate picks it up).
[CreateAssetMenu(menuName = "Spooktober/Stage Data/New Image", fileName = "Image - ")]
public class Stage_ImageSO : ScriptableObject
{
    public string saveID;

    [Header("Image")]
    public Sprite sprite;
    [Tooltip("Multiplies the picture's colours, e.g. to darken it or warm it up.")]
    public Color tint = Color.white;
    [Tooltip("1 = original colours, 0 = black and white. Lower it for memories.")]
    [Range(0f, 1f)] public float saturation = 1f;

    private void OnValidate()
    {
#if UNITY_EDITOR
        SaveIDUtility.StampAssetGUID(this, ref saveID);
#endif
    }
}
