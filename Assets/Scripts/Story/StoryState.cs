using System;
using System.Collections.Generic;
using UnityEngine;

// All story values for the current playthrough. Only values that were actually set are stored,
// everything else falls back to the variable's default. That makes New Game a simple Clear(),
// and variables added later work with old saves without any migration.
public class StoryState
{
    private readonly SerializableDictionary<string, int> values = new SerializableDictionary<string, int>(); // variable saveID => value

    public string ChapterID { get; private set; } = "";

    public event Action<Story_VariableSO, int> OnValueChanged;
    public event Action<string> OnChapterChanged;

    public int Get(Story_VariableSO variable)
    {
        if (variable == null)
            return 0;

        if (!string.IsNullOrEmpty(variable.saveID) && values.TryGetValue(variable.saveID, out int value))
            return value;

        return variable.defaultValue;
    }

    public bool GetBool(Story_VariableSO variable) => Get(variable) != 0;

    public void Set(Story_VariableSO variable, int value)
    {
        if (!IsValid(variable))
            return;

        if (values.TryGetValue(variable.saveID, out int current) && current == value)
            return;

        values[variable.saveID] = value;
        OnValueChanged?.Invoke(variable, value);
    }

    public void Add(Story_VariableSO variable, int amount) => Set(variable, Get(variable) + amount);

    public void Apply(StoryEffect effect)
    {
        if (effect == null)
            return;

        switch (effect.operation)
        {
            case EffectOperation.Set:
                Set(effect.variable, effect.value);
                break;
            case EffectOperation.Add:
                Add(effect.variable, effect.value);
                break;
        }
    }

    public void Apply(IList<StoryEffect> effects)
    {
        if (effects == null)
            return;

        foreach (StoryEffect effect in effects)
            Apply(effect);
    }

    public bool Evaluate(StoryCondition condition)
    {
        if (condition == null)
            return true;

        if (condition.variable == null)
        {
            Debug.LogWarning("Story condition has no variable assigned, treating it as failed.");
            return false;
        }

        int left = Get(condition.variable);
        int right;

        if (condition.compareTo == CompareTarget.Variable)
        {
            if (condition.otherVariable == null)
            {
                Debug.LogWarning($"Story condition on '{condition.variable.name}' compares to a variable but none is assigned, treating it as failed.");
                return false;
            }

            right = Get(condition.otherVariable);
        }
        else
        {
            right = condition.constant;
        }

        switch (condition.comparison)
        {
            case ComparisonType.Equal: return left == right;
            case ComparisonType.NotEqual: return left != right;
            case ComparisonType.Greater: return left > right;
            case ComparisonType.GreaterOrEqual: return left >= right;
            case ComparisonType.Less: return left < right;
            case ComparisonType.LessOrEqual: return left <= right;
            default: return false;
        }
    }

    // All conditions must pass. No conditions means it always passes.
    public bool Evaluate(IList<StoryCondition> conditions)
    {
        if (conditions == null)
            return true;

        foreach (StoryCondition condition in conditions)
        {
            if (!Evaluate(condition))
                return false;
        }

        return true;
    }

    public void SetChapter(string chapterID)
    {
        chapterID ??= "";

        if (ChapterID == chapterID)
            return;

        ChapterID = chapterID;
        OnChapterChanged?.Invoke(chapterID);
    }

    public void Clear()
    {
        values.Clear();
        ChapterID = "";
    }

    public void SaveTo(GameData data)
    {
        data.storyValues = new SerializableDictionary<string, int>();
        foreach (KeyValuePair<string, int> pair in values)
            data.storyValues[pair.Key] = pair.Value;

        data.chapterID = ChapterID;
    }

    public void LoadFrom(GameData data)
    {
        values.Clear();

        if (data.storyValues != null)
        {
            foreach (KeyValuePair<string, int> pair in data.storyValues)
                values[pair.Key] = pair.Value;
        }

        ChapterID = data.chapterID ?? "";
    }

    private static bool IsValid(Story_VariableSO variable)
    {
        if (variable == null)
        {
            Debug.LogWarning("Tried to change a story variable, but none is assigned.");
            return false;
        }

        if (string.IsNullOrEmpty(variable.saveID))
        {
            Debug.LogWarning($"Story variable '{variable.name}' has no saveID. Run 'Auto-Fill & Validate' on the Dialogue Database.");
            return false;
        }

        return true;
    }
}
