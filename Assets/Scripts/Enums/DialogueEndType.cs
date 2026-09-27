// Saved as ints in assets, so only ever append new values
public enum DialogueEndType
{
    End = 0,     // Close the dialogue after the last line
    Choices = 1, // Show the choices under the last line
    Jump = 2     // Continue to the first branch whose conditions pass, or the default next conversation
}
