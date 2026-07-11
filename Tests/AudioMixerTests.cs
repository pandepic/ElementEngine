using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class AudioMixerTests : IDisposable
{
    public AudioMixerTests()
    {
        SoundManager.ResetForTests();
    }

    [Fact]
    public void MixesMonoIntoStereoAndResamplesLinearly()
    {
        using var source = new AudioSource(new AudioFormat(24000, 1), [0f, 1f]);
        using var instance = new AudioInstance(source) { Volume = 0.5f, Managed = false };
        instance.Play();
        var destination = new float[8];

        instance.Mix(destination, 48000);

        Assert.Equal([0f, 0f, 0.25f, 0.25f, 0.5f, 0.5f, 0.5f, 0.5f], destination);
        Assert.Equal(PlaybackState.Stopped, instance.PlaybackState);
    }

    [Fact]
    public void LoopingIsGaplessWithinAnOutputBuffer()
    {
        using var source = new AudioSource(new AudioFormat(48000, 1), [-1f, 1f]);
        using var instance = new AudioInstance(source) { Looping = true, Managed = false };
        instance.Play();
        var destination = new float[12];

        instance.Mix(destination, 48000);

        Assert.Equal(
            [-1f, -1f, 1f, 1f, -1f, -1f, 1f, 1f, -1f, -1f, 1f, 1f],
            destination);
        Assert.Equal(PlaybackState.Playing, instance.PlaybackState);
    }

    [Fact]
    public void PausedPlaybackResumesAndStoppedPlaybackRestarts()
    {
        using var source = new AudioSource(new AudioFormat(48000, 1), [0.25f, 0.5f, 0.75f]);
        using var instance = new AudioInstance(source) { Managed = false };
        instance.Play();
        var firstFrame = new float[2];
        instance.Mix(firstFrame, 48000);
        instance.Pause();
        var pausedFrame = new float[2];
        instance.Mix(pausedFrame, 48000);
        instance.Play();
        var resumedFrame = new float[2];
        instance.Mix(resumedFrame, 48000);
        instance.Stop();
        instance.Play();
        var restartedFrame = new float[2];
        instance.Mix(restartedFrame, 48000);

        Assert.Equal([0.25f, 0.25f], firstFrame);
        Assert.Equal([0f, 0f], pausedFrame);
        Assert.Equal([0.5f, 0.5f], resumedFrame);
        Assert.Equal([0.25f, 0.25f], restartedFrame);
    }

    [Fact]
    public void ManagerPreservesDuplicateAndVolumeBehavior()
    {
        AudioEngine.SetOutputFactoryForTests(() => new BlockingAudioOutput());
        using var source = new AudioSource(new AudioFormat(48000, 1), [0.5f])
        {
            AssetName = "click.wav",
        };

        var first = SoundManager.Play(source, 7, loop: true);
        var duplicate = SoundManager.Play(source, 7, loop: true);
        SoundManager.SetMasterVolume(0.5f);
        SoundManager.SetVolume(7, 0.25f);

        Assert.NotNull(first);
        Assert.Null(duplicate);
        Assert.Equal(0.125f, first.Volume);
        Assert.Single(SoundManager.AudioInstances);
    }

    [Fact]
    public void DirectlyConstructedInstanceIsManagedByDefault()
    {
        AudioEngine.SetOutputFactoryForTests(() => new BlockingAudioOutput());
        using var source = new AudioSource(new AudioFormat(48000, 1), [0.5f])
        {
            AssetName = "direct.wav",
        };
        using var instance = new AudioInstance(source) { Looping = true };

        instance.Play();

        Assert.Equal(PlaybackState.Playing, instance.PlaybackState);
        Assert.Same(instance, SoundManager.AudioInstances[instance.InstanceID]);
    }

    [Fact]
    public void RetainedHandleCanReplayAfterTheManagerReapsIt()
    {
        AudioEngine.SetOutputFactoryForTests(() => new BlockingAudioOutput());
        using var source = new AudioSource(new AudioFormat(48000, 1), [0.5f])
        {
            AssetName = "music.ogg",
        };

        var instance = SoundManager.Play(source, 2, loop: true);
        instance.Stop();
        SoundManager.Update();
        Assert.Empty(SoundManager.AudioInstances);

        instance.Play();

        Assert.Equal(PlaybackState.Playing, instance.PlaybackState);
        Assert.Same(instance, SoundManager.AudioInstances[instance.InstanceID]);
    }

    public void Dispose()
    {
        SoundManager.Shutdown();
        AudioEngine.ResetOutputFactoryForTests();
    }

    private sealed class BlockingAudioOutput : IAudioOutput
    {
        public void Write(short[] samples, CancellationToken cancellationToken) =>
            cancellationToken.WaitHandle.WaitOne();

        public void Dispose()
        {
        }
    }
}
