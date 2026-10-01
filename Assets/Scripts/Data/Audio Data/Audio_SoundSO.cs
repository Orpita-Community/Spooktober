using System.Collections.Generic;
using UnityEngine;

// A sound the game can play: music, a looping ambience such as rain, or a one-shot effect.
// Like RPG2D's AudioClipData: with several clips one is picked at random, and the volume balances it against the others.
// Saves store its saveID (for the music and ambience playing), so it has to be in the Dialogue Database.
[CreateAssetMenu(menuName = "Spooktober/Audio Data/New Sound", fileName = "Sound - ")]
public class Audio_SoundSO : ScriptableObject
{
    public string saveID;

    [Header("Sound")]
    public List<AudioClip> clips = new List<AudioClip>();
    [Tooltip("Balances this sound against the others. The player's Master, Music and SFX volumes apply on top.")]
    [Range(0f, 1f)] public float volume = 1f;

    public AudioClip GetRandomClip() => clips.Count > 0 ? clips[Random.Range(0, clips.Count)] : null;

    private void OnValidate()
    {
#if UNITY_EDITOR
        SaveIDUtility.StampAssetGUID(this, ref saveID);
#endif
    }
}
