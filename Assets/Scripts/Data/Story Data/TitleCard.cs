using System;
using UnityEngine;

// Text shown on a black screen, e.g. "ACT 1" / "The Man in the Rain" or "THE END".
[Serializable]
public class TitleCard
{
    public string title = "";
    public string subtitle = "";
    [Tooltip("Optional. Shown on its own after the title, before the scene fades in, e.g. a hint about what the act asks of the player.")]
    [TextArea(2, 4)] public string note = "";

    public bool IsEmpty => string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(subtitle) && string.IsNullOrWhiteSpace(note);
}
