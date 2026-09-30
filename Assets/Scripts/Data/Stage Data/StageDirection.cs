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
}
