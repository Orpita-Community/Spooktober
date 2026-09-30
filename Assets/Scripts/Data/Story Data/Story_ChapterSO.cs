using UnityEngine;

// Where the story is, e.g. "Act 1 — The Man in the Rain". Shown on save slots.
[CreateAssetMenu(menuName = "Spooktober/Story Data/New Chapter", fileName = "Chapter - ")]
public class Story_ChapterSO : ScriptableObject
{
    public string saveID;

    [Header("Chapter Details")]
    [TextArea] public string label;
    [Tooltip("Shown on a black screen when the story enters this chapter (not when loading a save). Leave empty for no card.")]
    public TitleCard titleCard = new TitleCard();

    private void OnValidate()
    {
#if UNITY_EDITOR
        SaveIDUtility.StampAssetGUID(this, ref saveID);
#endif
    }
}
