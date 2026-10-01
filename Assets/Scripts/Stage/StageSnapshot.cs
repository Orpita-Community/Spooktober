using System;
using System.Collections.Generic;

// What is on stage for one screen, stored as ids with every history entry.
// Rollback, the backlog and loading a save all read it back, so they show exactly the stage the player saw.
// The music and ambience playing are part of it too, so a loaded save sounds like the moment it was made.
// Snapshots are never changed after they are stored: With() returns a new one.
[Serializable]
public class StageSnapshot
{
    public string backgroundID = "";
    public string closeUpID = "";
    public List<StageActor> actors = new List<StageActor>();
    public string musicID = "";
    public string ambienceID = "";

    public StageActor FindActor(string speakerID)
    {
        if (string.IsNullOrEmpty(speakerID))
            return null;

        foreach (StageActor actor in actors)
        {
            if (actor != null && actor.speakerID == speakerID)
                return actor;
        }

        return null;
    }

    public bool HasActor(string speakerID) => FindActor(speakerID) != null;

    public StageSnapshot Clone()
    {
        StageSnapshot copy = new StageSnapshot
        {
            backgroundID = backgroundID ?? "",
            closeUpID = closeUpID ?? "",
            musicID = musicID ?? "",
            ambienceID = ambienceID ?? ""
        };

        foreach (StageActor actor in actors)
        {
            if (actor != null)
                copy.actors.Add(actor.Clone());
        }

        return copy;
    }

    // This stage with a line's direction applied, then the speaker's expression (a character on stage shows the face of their line)
    public StageSnapshot With(StageDirection direction, Dialogue_SpeakerSO speaker, PortraitExpression expression)
    {
        StageSnapshot next = Clone();

        if (direction != null)
        {
            if (direction.background != null)
                next.backgroundID = direction.background.saveID;

            if (direction.characters == StageChange.Clear)
                next.actors.Clear();
            else if (direction.characters == StageChange.Set)
                next.SetCast(direction.cast);

            next.closeUpID = Changed(next.closeUpID, direction.closeUp, direction.closeUpImage != null ? direction.closeUpImage.saveID : "");
            next.musicID = Changed(next.musicID, direction.music, direction.musicTrack != null ? direction.musicTrack.saveID : "");
            next.ambienceID = Changed(next.ambienceID, direction.ambience, direction.ambienceTrack != null ? direction.ambienceTrack.saveID : "");
        }

        if (speaker != null)
        {
            StageActor actor = next.FindActor(speaker.saveID);
            if (actor != null)
                actor.expression = expression;
        }

        return next;
    }

    private static string Changed(string current, StageChange change, string setTo)
    {
        switch (change)
        {
            case StageChange.Set: return setTo;
            case StageChange.Clear: return "";
            default: return current;
        }
    }

    private void SetCast(List<StageCharacter> cast)
    {
        actors.Clear();

        if (cast == null)
            return;

        foreach (StageCharacter character in cast)
        {
            if (character == null || character.character == null)
                continue;

            // One character per slot and one slot per character: the later entry wins
            actors.RemoveAll(a => a.slot == character.slot || a.speakerID == character.character.saveID);
            actors.Add(new StageActor
            {
                speakerID = character.character.saveID,
                slot = character.slot,
                expression = character.expression,
                flip = character.flip
            });
        }
    }
}

[Serializable]
public class StageActor
{
    public string speakerID = "";
    public StageSlot slot;
    public PortraitExpression expression;
    public bool flip;

    public StageActor Clone() => new StageActor { speakerID = speakerID, slot = slot, expression = expression, flip = flip };
}
