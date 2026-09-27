using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class StoryManager : MonoBehaviour, ISaveable
{
    public static StoryManager Instance { get; private set; }

    [SerializeField] private Dialogue_DatabaseSO database;

    public StoryState State { get; } = new StoryState();

    public string CurrentChapterLabel => DialogueText.Chapter(database != null ? database.GetChapter(State.ChapterID) : null);

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int Get(Story_VariableSO variable) => State.Get(variable);
    public bool GetBool(Story_VariableSO variable) => State.GetBool(variable);
    public void Set(Story_VariableSO variable, int value) => State.Set(variable, value);
    public void Add(Story_VariableSO variable, int amount) => State.Add(variable, amount);
    public bool Evaluate(IList<StoryCondition> conditions) => State.Evaluate(conditions);

    public void ResetToDefaults() => State.Clear();

    public void LoadData(GameData data) => State.LoadFrom(data);

    public void SaveData(ref GameData data) => State.SaveTo(data);
}
