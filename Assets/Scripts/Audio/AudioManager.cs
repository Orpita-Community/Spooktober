using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// Music, a looping ambience (the rain) and sound effects, played through the AudioMixer's Music and SFX groups.
// Volume settings work like RPG2D's options: a 0-1 value per mixer group, kept in PlayerPrefs (default .6) and applied
// as Log10(value) * mixerMultiplier decibels. Each group can also be switched off, which silences it but keeps the value.
[DefaultExecutionOrder(-100)]
public class AudioManager : MonoBehaviour
{
    public const float DefaultVolume = .6f;
    public const float SilentDecibels = -80f;

    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [Tooltip("Loops the music. Output: the Music group.")]
    [SerializeField] private AudioSource musicSource;
    [Tooltip("Loops the ambience, e.g. rain. Output: the SFX group.")]
    [SerializeField] private AudioSource ambienceSource;
    [Tooltip("Plays one-shot effects. Output: the SFX group.")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [Tooltip("Turns a 0-1 setting into decibels: Log10(value) * this.")]
    [SerializeField] private float mixerMultiplier = 25f;
    [SerializeField] private string masterParameter = "masterVolume";
    [SerializeField] private string musicParameter = "musicVolume";
    [SerializeField] private string sfxParameter = "sfxVolume";

    [Header("Sounds")]
    [SerializeField] private Audio_SoundSO menuMusic;
    [SerializeField] private Audio_SoundSO buttonHover;
    [SerializeField] private Audio_SoundSO buttonClick;

    [Header("Fades")]
    [SerializeField] private float musicFadeTime = 1.2f;
    [SerializeField] private float ambienceFadeTime = 1.5f;

    private string prefsPrefix = ""; // Tests keep their settings apart from the player's
    private Coroutine musicFade;
    private Coroutine ambienceFade;

    public Audio_SoundSO CurrentMusic { get; private set; }
    public Audio_SoundSO CurrentAmbience { get; private set; }

    // Each sound effect as it plays (the smoke test listens to it; captions could too)
    public event Action<Audio_SoundSO> OnSoundPlayed;

    private void Awake()
    {
        Instance = this;
    }

    // AudioMixer.SetFloat is ignored before the first frame, so the saved volumes go in here
    private void Start() => ApplyAllVolumes();

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #region Playing

    // The same track keeps playing (e.g. the menu theme carrying on into the prologue). Null fades the music out.
    public void PlayMusic(Audio_SoundSO track, float fadeTime = -1f)
    {
        if (track == CurrentMusic)
            return;

        CurrentMusic = track;
        Restart(ref musicFade, SwitchLoopCo(musicSource, track, fadeTime >= 0f ? fadeTime : musicFadeTime));
    }

    public void PlayAmbience(Audio_SoundSO track, float fadeTime = -1f)
    {
        if (track == CurrentAmbience)
            return;

        CurrentAmbience = track;
        Restart(ref ambienceFade, SwitchLoopCo(ambienceSource, track, fadeTime >= 0f ? fadeTime : ambienceFadeTime));
    }

    // Back at the main menu: its theme, and no rain
    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
        PlayAmbience(null);
    }

    public void StopAll(float fadeTime)
    {
        PlayMusic(null, fadeTime);
        PlayAmbience(null, fadeTime);
    }

    public void PlaySFX(Audio_SoundSO sound)
    {
        AudioClip clip = sound != null ? sound.GetRandomClip() : null;

        if (clip == null)
        {
            if (sound != null)
                Debug.LogWarning($"AudioManager: '{sound.name}' has no clips.", sound);

            return;
        }

        sfxSource.PlayOneShot(clip, sound.volume);
        OnSoundPlayed?.Invoke(sound);
    }

    public void PlayButtonHover() => PlaySFX(buttonHover);
    public void PlayButtonClick() => PlaySFX(buttonClick);

    private void Restart(ref Coroutine running, IEnumerator routine)
    {
        if (running != null)
            StopCoroutine(running);

        running = StartCoroutine(routine);
    }

    // Fades the old loop out, then the new one in (half the time each)
    private IEnumerator SwitchLoopCo(AudioSource source, Audio_SoundSO track, float fadeTime)
    {
        if (source.isPlaying)
            yield return FadeVolumeCo(source, 0f, fadeTime * .5f);

        source.Stop();

        AudioClip clip = track != null ? track.GetRandomClip() : null;
        if (clip == null)
            yield break;

        source.clip = clip;
        source.loop = true;
        source.volume = 0f;
        source.Play();

        yield return FadeVolumeCo(source, track.volume, fadeTime * .5f);
    }

    // Unscaled time, so fades carry on while the game is paused
    private static IEnumerator FadeVolumeCo(AudioSource source, float targetVolume, float duration)
    {
        float startVolume = source.volume;

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(startVolume, targetVolume, t / duration);
            yield return null;
        }

        source.volume = targetVolume;
    }

    #endregion

    #region Volume settings

    public float GetVolume(AudioChannel channel) => PlayerPrefs.GetFloat(VolumeKey(channel), DefaultVolume);
    public bool IsOn(AudioChannel channel) => PlayerPrefs.GetInt(OnKey(channel), 1) == 1;

    public void SetVolume(AudioChannel channel, float volume)
    {
        PlayerPrefs.SetFloat(VolumeKey(channel), Mathf.Clamp01(volume));
        ApplyVolume(channel);
    }

    public void SetOn(AudioChannel channel, bool on)
    {
        PlayerPrefs.SetInt(OnKey(channel), on ? 1 : 0);
        ApplyVolume(channel);
    }

    public static float ToDecibels(float volume, bool on, float multiplier) =>
        on && volume > .0001f ? Mathf.Max(SilentDecibels, Mathf.Log10(volume) * multiplier) : SilentDecibels;

    public float GetMixerDecibels(AudioChannel channel) =>
        audioMixer != null && audioMixer.GetFloat(Parameter(channel), out float decibels) ? decibels : 0f;

    private void ApplyAllVolumes()
    {
        ApplyVolume(AudioChannel.Master);
        ApplyVolume(AudioChannel.Music);
        ApplyVolume(AudioChannel.SFX);
    }

    private void ApplyVolume(AudioChannel channel)
    {
        if (audioMixer != null)
            audioMixer.SetFloat(Parameter(channel), ToDecibels(GetVolume(channel), IsOn(channel), mixerMultiplier));
    }

    private string Parameter(AudioChannel channel)
    {
        switch (channel)
        {
            case AudioChannel.Music: return musicParameter;
            case AudioChannel.SFX: return sfxParameter;
            default: return masterParameter;
        }
    }

    // Keyed by the mixer parameter like RPG2D ("musicVolume"), plus "musicVolumeOn" for the switch
    private string VolumeKey(AudioChannel channel) => prefsPrefix + Parameter(channel);
    private string OnKey(AudioChannel channel) => prefsPrefix + Parameter(channel) + "On";

    #endregion
}
