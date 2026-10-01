using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

// The game's audio files, and the Sound assets (Audio_SoundSO) the story and the AudioManager play them through.
// The volumes balance the files against each other (the importer normalizes them all to full peak, which makes the
// music much louder than the rest). The player's Master/Music/SFX settings (.6 by default) apply on top.
public static class StoryAudio
{
    public const string SoundFolder = "Assets/Data/Audio Data";
    public const string MixerPath = "Assets/Settings/AudioMixer.mixer";

    public const string Theme = "Assets/Audio/a BGM.wav";
    public const string RainOutside = "Assets/Audio/rain outside shop_.wav";
    public const string RainInside = "Assets/Audio/rain inside shop.wav";
    public const string Thunder = "Assets/Audio/thunder.wav";
    public const string Door = "Assets/Audio/door open then close.wav";
    public const string MaskBoxOpen = "Assets/Audio/mask box open.wav";
    public const string AgreementPaper = "Assets/Audio/agreement paper.wav";
    public const string Signature = "Assets/Audio/signature_.wav";
    public const string ButtonClick = "Assets/Audio/menu button click.wav";
    public const string ButtonHover = "Assets/Audio/menu button hover.wav";

    public static Audio_SoundSO MainTheme() => Sound("Music", "Music - Main Theme", Theme, .45f);
    public static Audio_SoundSO RainOutsideLoop() => Sound("Ambience", "Ambience - Rain Outside", RainOutside, .6f);
    public static Audio_SoundSO RainInsideLoop() => Sound("Ambience", "Ambience - Rain Inside", RainInside, .5f);
    public static Audio_SoundSO ThunderClap() => Sound("SFX", "SFX - Thunder", Thunder, .8f);
    public static Audio_SoundSO DoorOpenClose() => Sound("SFX", "SFX - Door Open and Close", Door, .8f);
    public static Audio_SoundSO MaskBox() => Sound("SFX", "SFX - Mask Box Open", MaskBoxOpen, .9f);
    public static Audio_SoundSO Paper() => Sound("SFX", "SFX - Agreement Paper", AgreementPaper, 1f);
    public static Audio_SoundSO Pen() => Sound("SFX", "SFX - Signature", Signature, 1f);
    public static Audio_SoundSO Click() => Sound("UI", "UI - Button Click", ButtonClick, .7f);
    public static Audio_SoundSO Hover() => Sound("UI", "UI - Button Hover", ButtonHover, .5f);

    public static AudioMixer Mixer()
    {
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null)
            throw new System.Exception($"No AudioMixer at {MixerPath}.");

        return mixer;
    }

    public static AudioMixerGroup Group(string name)
    {
        AudioMixerGroup[] groups = Mixer().FindMatchingGroups(name);
        if (groups.Length == 0)
            throw new System.Exception($"The AudioMixer has no '{name}' group.");

        return groups[0];
    }

    // Loads or creates the Sound asset and points it at its file. Running it again keeps the asset (and its saveID).
    private static Audio_SoundSO Sound(string folder, string name, string clipPath, float volume)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        if (clip == null)
            throw new System.Exception($"No audio file at {clipPath}.");

        Audio_SoundSO sound = StoryBuilder.LoadOrCreate<Audio_SoundSO>($"{SoundFolder}/{folder}/{name}.asset");
        sound.clips.Clear();
        sound.clips.Add(clip);
        sound.volume = volume;
        EditorUtility.SetDirty(sound);
        return sound;
    }
}
