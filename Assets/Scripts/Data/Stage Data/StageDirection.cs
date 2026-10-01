using System;
using System.Collections.Generic;
using UnityEngine;

// How the stage changes when a line shows. Everything defaults to "keep", so an untouched direction changes nothing.
[Serializable]
public class StageDirection
{
    [Tooltip("None cross-fades the changes quickly, Fade goes through black, Flash flashes white.")]
    public StageTransition transition;

    [Tooltip("Empty = keep the current background.")]
    public Stage_ImageSO background;

    [Tooltip("Set = show exactly the characters listed below (and nobody else). Clear = empty the stage.")]
    public StageChange characters;
    public List<StageCharacter> cast = new List<StageCharacter>();

    [Tooltip("Set = show the close-up picture below over the stage. Clear = take it away.")]
    public StageChange closeUp;
    public Stage_ImageSO closeUpImage;

    [Header("Audio")]
    [Tooltip("Keep = the music carries on. Set = switch to the track below (it loops). Clear = fade the music out.")]
    public StageChange music;
    public Audio_SoundSO musicTrack;
    [Tooltip("A looping background sound such as rain. Keep, Set or Clear, like the music.")]
    public StageChange ambience;
    public Audio_SoundSO ambienceTrack;
    [Tooltip("Played once when the line first appears (not during rollback, skipping, or when a save loads).")]
    public Audio_SoundSO sound;
}
