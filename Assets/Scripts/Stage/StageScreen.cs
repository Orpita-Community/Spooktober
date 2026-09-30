using System;
using System.Collections.Generic;
using UnityEngine;

// What the stage draws for one screen: a StageSnapshot's ids resolved to assets.
// Built the same way for the live screen and for rollback, like DialogueScreen.
public class StageScreen
{
    public class Actor
    {
        public Dialogue_SpeakerSO speaker;
        public Sprite sprite;
        public StageSlot slot;
        public bool flip;
        public bool speaking;
    }

    public Stage_ImageSO background;
    public Stage_ImageSO closeUp;
    public List<Actor> actors = new List<Actor>();

    public bool SomeoneSpeaking { get; private set; }

    public Actor GetActor(StageSlot slot) => actors.Find(actor => actor.slot == slot);

    // Ids that no longer resolve (content deleted since the save) are simply left off the stage
    public static StageScreen Build(StageSnapshot snapshot, Dialogue_SpeakerSO activeSpeaker,
        Func<string, Dialogue_SpeakerSO> findSpeaker, Func<string, Stage_ImageSO> findImage)
    {
        StageScreen screen = new StageScreen();

        if (snapshot == null)
            return screen;

        screen.background = findImage?.Invoke(snapshot.backgroundID);
        screen.closeUp = findImage?.Invoke(snapshot.closeUpID);

        foreach (StageActor stageActor in snapshot.actors)
        {
            Dialogue_SpeakerSO speaker = stageActor != null ? findSpeaker?.Invoke(stageActor.speakerID) : null;
            if (speaker == null)
                continue;

            bool speaking = activeSpeaker != null && activeSpeaker == speaker;
            screen.SomeoneSpeaking |= speaking;

            screen.actors.Add(new Actor
            {
                speaker = speaker,
                sprite = speaker.GetPortrait(stageActor.expression),
                slot = stageActor.slot,
                flip = stageActor.flip,
                speaking = speaking
            });
        }

        return screen;
    }
}
