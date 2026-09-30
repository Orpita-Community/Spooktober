using System;

// Text shown on a black screen, e.g. "ACT 1" / "The Man in the Rain" or "THE END".
[Serializable]
public class TitleCard
{
    public string title = "";
    public string subtitle = "";

    public bool IsEmpty => string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(subtitle);
}
