using UnityEngine;

// A single story value: Humanity, Cynicism, the clock, curse intensity, or a flag (0 = false, 1 = true).
[CreateAssetMenu(menuName = "Spooktober/Story Data/New Variable", fileName = "Var - ")]
public class Story_VariableSO : ScriptableObject
{
    public string saveID;

    [Header("Variable Details")]
    public int defaultValue;
    [TextArea] public string description;

    private void OnValidate()
    {
#if UNITY_EDITOR
        SaveIDUtility.StampAssetGUID(this, ref saveID);
#endif
    }
}
