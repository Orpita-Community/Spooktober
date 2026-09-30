// Saved as ints in assets, so only ever append new values
public enum StageTransition
{
    None = 0,  // Stage changes cross-fade quickly
    Fade = 1,  // Fade to black, change the stage, fade back in
    Flash = 2  // A white flash with the new stage underneath
}
