using System;
using System.Collections.Generic;
using UnityEngine;

// Everything stored here is ids and plain values, never ScriptableObject references.
// Every field has an initializer, so saves made before a field existed still load with sensible defaults.
[Serializable]
public class GameData
{
    public const int CurrentVersion = 1;

    public int saveVersion = CurrentVersion;
    public SaveMetadata metadata = new SaveMetadata();

    // World
    public string sceneName = "";
    public bool hasPlayerPosition;
    public Vector3 playerPosition;

    // Story
    public SerializableDictionary<string, int> storyValues = new SerializableDictionary<string, int>(); // variable saveID => value
    public string chapterID = "";

    // Dialogue
    public DialogueSaveState dialogue = new DialogueSaveState();
    public List<HistoryEntry> history = new List<HistoryEntry>();
}

// Shown on the save slot, so the load menu doesn't need to understand the rest of the save
[Serializable]
public class SaveMetadata
{
    public string timestampUtc = ""; // ISO 8601 ("o"), JsonUtility can't serialize DateTime
    public float playTimeSeconds;
    public string chapterText = "";
    public string previewSpeaker = "";
    public string previewText = "";
}
