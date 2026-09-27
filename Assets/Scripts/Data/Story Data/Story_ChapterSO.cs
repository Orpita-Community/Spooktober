using UnityEngine;

// Where the story is, e.g. "Act 1 — 10:30 PM". Shown on save slots.
[CreateAssetMenu(menuName = "Spooktober/Story Data/New Chapter", fileName = "Chapter - ")]
public class Story_ChapterSO : ScriptableObject
{
    public string saveID;

    [Header("Chapter Details")]
    [TextArea] public string label;

    private void OnValidate()
    {
#if UNITY_EDITOR
        SaveIDUtility.StampAssetGUID(this, ref saveID);
#endif
    }
}
