using NUnit.Framework;
using UnityEngine;

// The volume settings follow RPG2D: a 0-1 value (default .6) becomes Log10(value) * 25 decibels on the mixer.
public class AudioSettingsTests
{
    private const float Multiplier = 25f;

    [Test]
    public void EveryChannelStartsAtPointSix()
    {
        Assert.AreEqual(.6f, AudioManager.DefaultVolume);
    }

    [Test]
    public void VolumeBecomesDecibelsTheRPG2DWay()
    {
        Assert.AreEqual(0f, AudioManager.ToDecibels(1f, true, Multiplier), .001f);
        Assert.AreEqual(-5.546f, AudioManager.ToDecibels(.6f, true, Multiplier), .001f);
        Assert.AreEqual(-25f, AudioManager.ToDecibels(.1f, true, Multiplier), .001f);
    }

    [Test]
    public void ZeroOrSwitchedOffIsSilentAndNothingGoesBelowTheMixersFloor()
    {
        Assert.AreEqual(AudioManager.SilentDecibels, AudioManager.ToDecibels(0f, true, Multiplier));
        Assert.AreEqual(AudioManager.SilentDecibels, AudioManager.ToDecibels(1f, false, Multiplier));
        Assert.AreEqual(AudioManager.SilentDecibels, AudioManager.ToDecibels(.0002f, true, Multiplier)); // Log10 says -92.5
    }

    [Test]
    public void ASoundWithoutClipsGivesNoClip()
    {
        Audio_SoundSO sound = ScriptableObject.CreateInstance<Audio_SoundSO>();

        try
        {
            Assert.IsNull(sound.GetRandomClip());
        }
        finally
        {
            Object.DestroyImmediate(sound);
        }
    }
}
