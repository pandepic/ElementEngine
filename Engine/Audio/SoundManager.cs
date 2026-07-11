using System;
using System.Collections.Generic;

namespace ElementEngine;

public enum AudioSourceType
{
    Auto,
    Ogg,
    WAV
}

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused
}

public readonly record struct AudioFormat(int SampleRate, int Channels)
{
    public TimeSpan GetDuration(int sampleCount) =>
        TimeSpan.FromSeconds((double)sampleCount / Channels / SampleRate);
}

public sealed class AudioSource : IDisposable
{
    private float[] _samples;
    private bool _disposed;

    public AudioFormat Format { get; }
    public ReadOnlyMemory<float> Samples => _samples;
    public TimeSpan Duration => Format.GetDuration(_samples.Length);
    public string AssetName { get; set; } = "";

    public AudioSource(AudioFormat format, float[] samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        if (format.SampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(format), "The sample rate must be positive.");
        if (format.Channels <= 0)
            throw new ArgumentOutOfRangeException(nameof(format), "The channel count must be positive.");
        if (samples.Length % format.Channels != 0)
            throw new ArgumentException("Interleaved sample data must contain complete frames.", nameof(samples));

        Format = format;
        _samples = samples;
    }

    // Instances share this buffer instead of copying it; nothing mutates it, and a
    // captured reference stays valid even if the source is disposed mid-playback.
    internal float[] GetSampleBuffer()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _samples;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _samples = [];
        _disposed = true;
    }
}

public sealed class AudioInstance : IDisposable
{
    private readonly object _sync = new();
    private readonly float[] _samples;
    private readonly AudioFormat _format;
    private double _framePosition;
    private float _volume = 1f;
    private PlaybackState _playbackState = PlaybackState.Stopped;
    // Volatile because the game thread writes it without a lock while the mixer
    // thread reads it inside Mix.
    private volatile bool _looping;
    private bool _disposed;

    public int InstanceID { get; set; }
    public int Type { get; set; }
    public string AssetName { get; set; }

    public bool Looping
    {
        get => _looping;
        set => _looping = value;
    }

    // Managed instances register with the mixer when played; tests opt out so
    // they can drive Mix deterministically without the engine running.
    internal bool Managed { get; init; } = true;

    public float Volume
    {
        get
        {
            lock (_sync)
                return _volume;
        }
        set
        {
            lock (_sync)
                _volume = Math.Clamp(value, 0f, 1f);
        }
    }

    public PlaybackState PlaybackState
    {
        get
        {
            lock (_sync)
                return _playbackState;
        }
    }

    public AudioInstance(AudioSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        AssetName = source.AssetName;
        _format = source.Format;
        _samples = source.GetSampleBuffer();
    }

    public void Play()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_playbackState != PlaybackState.Paused)
                _framePosition = 0;

            _playbackState = PlaybackState.Playing;
        }

        // Outside the instance lock: every other path nests manager -> instance,
        // so re-registering under the instance lock could deadlock against the mixer.
        if (Managed)
        {
            AudioEngine.EnsureStarted();
            SoundManager.Register(this);
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            _framePosition = 0;
            _playbackState = PlaybackState.Stopped;
        }
    }

    public void Pause()
    {
        lock (_sync)
        {
            if (_playbackState == PlaybackState.Playing)
                _playbackState = PlaybackState.Paused;
        }
    }

    internal void Mix(Span<float> destination, int outputSampleRate)
    {
        if (destination.Length % 2 != 0)
            throw new ArgumentException("The destination buffer must contain complete interleaved stereo frames.", nameof(destination));

        lock (_sync)
        {
            if (_disposed || _playbackState != PlaybackState.Playing)
                return;

            var sourceFrameCount = _samples.Length / _format.Channels;
            if (sourceFrameCount == 0)
            {
                _playbackState = PlaybackState.Stopped;
                return;
            }

            // Hoisted so one mix pass sees a consistent looping/volume snapshot.
            var looping = _looping;
            var volume = _volume;
            var frameStep = (double)_format.SampleRate / outputSampleRate;
            for (var outputIndex = 0; outputIndex < destination.Length; outputIndex += 2)
            {
                if (_framePosition >= sourceFrameCount)
                {
                    if (!looping)
                    {
                        _playbackState = PlaybackState.Stopped;
                        break;
                    }

                    _framePosition %= sourceFrameCount;
                }

                var firstFrame = (int)_framePosition;
                var secondFrame = firstFrame + 1;
                if (secondFrame >= sourceFrameCount)
                    secondFrame = looping ? 0 : firstFrame;

                var fraction = (float)(_framePosition - firstFrame);
                ReadStereoFrame(firstFrame, out var firstLeft, out var firstRight);
                ReadStereoFrame(secondFrame, out var secondLeft, out var secondRight);
                destination[outputIndex] += (firstLeft + ((secondLeft - firstLeft) * fraction)) * volume;
                destination[outputIndex + 1] += (firstRight + ((secondRight - firstRight) * fraction)) * volume;
                _framePosition += frameStep;
            }

            if (!looping && _framePosition >= sourceFrameCount)
                _playbackState = PlaybackState.Stopped;
        }
    }

    private void ReadStereoFrame(int frame, out float left, out float right)
    {
        var offset = frame * _format.Channels;
        left = _samples[offset];
        right = _format.Channels == 1 ? left : _samples[offset + 1];
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _playbackState = PlaybackState.Stopped;
            _disposed = true;
        }
    }
}

public static class SoundManager
{
    private static readonly object Sync = new();
    private static int _nextID;

    public static int UISoundType = -1;
    public static float DefaultVolume { get; set; } = 0.5f;
    public static float MasterVolume { get; set; } = 1f;
    public static Dictionary<int, AudioInstance> AudioInstances { get; } = [];
    public static Dictionary<int, float> VolumeSettings { get; } = [];

    public static void Update()
    {
        lock (Sync)
        {
            foreach (var (id, instance) in AudioInstances)
            {
                if (instance.PlaybackState == PlaybackState.Stopped)
                    AudioInstances.Remove(id);
            }
        }
    }

    public static AudioInstance Play(
        string assetName,
        int type,
        AudioSourceType sourceType = AudioSourceType.Auto,
        bool loop = false,
        bool allowDuplicates = false)
    {
        return sourceType switch
        {
            AudioSourceType.Auto => Play(
                AssetManager.Instance.LoadAudioSourceByExtension(assetName),
                type,
                loop,
                allowDuplicates),
            AudioSourceType.Ogg => Play(
                AssetManager.Instance.LoadAudioSourceOggVorbis(assetName),
                type,
                loop,
                allowDuplicates),
            AudioSourceType.WAV => Play(
                AssetManager.Instance.LoadAudioSourceWAV(assetName),
                type,
                loop,
                allowDuplicates),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceType)),
        };
    }

    public static AudioInstance Play(
        AudioSource source,
        int type,
        bool loop = false,
        bool allowDuplicates = false)
    {
        ArgumentNullException.ThrowIfNull(source);

        lock (Sync)
        {
            // Started before the duplicate check so that a faulted engine is
            // revived even when this call is suppressed as a duplicate.
            AudioEngine.EnsureStarted();

            if (!allowDuplicates)
            {
                foreach (var existingInstance in AudioInstances.Values)
                {
                    if (existingInstance.AssetName == source.AssetName)
                        return null;
                }
            }

            if (!VolumeSettings.TryGetValue(type, out var typeVolume))
            {
                typeVolume = DefaultVolume;
                VolumeSettings.Add(type, typeVolume);
            }

            var instance = new AudioInstance(source)
            {
                Type = type,
                Looping = loop,
                InstanceID = GetNextID(),
                Volume = typeVolume * MasterVolume,
            };

            AudioInstances.Add(instance.InstanceID, instance);
            instance.Play();
            return instance;
        }
    }

    public static void SetMasterVolume(float volume)
    {
        volume = Math.Clamp(volume, 0f, 1f);

        lock (Sync)
        {
            if (MasterVolume == volume)
                return;

            MasterVolume = volume;
            foreach (var instance in AudioInstances.Values)
                instance.Volume = GetTypeVolume(instance.Type) * MasterVolume;
        }
    }

    public static void SetVolume(int type, float volume)
    {
        volume = Math.Clamp(volume, 0f, 1f);

        lock (Sync)
        {
            VolumeSettings[type] = volume;
            foreach (var instance in AudioInstances.Values)
            {
                if (instance.Type == type)
                    instance.Volume = volume * MasterVolume;
            }
        }
    }

    public static void StopByInstance(AudioInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        StopByID(instance.InstanceID);
    }

    public static void StopByID(int id)
    {
        lock (Sync)
        {
            if (AudioInstances.Remove(id, out var instance))
                instance.Stop();
        }
    }

    public static void StopByType(int type)
    {
        lock (Sync)
        {
            foreach (var (id, instance) in AudioInstances)
            {
                if (instance.Type == type)
                {
                    AudioInstances.Remove(id);
                    instance.Stop();
                }
            }
        }
    }

    public static void StopAll()
    {
        lock (Sync)
        {
            foreach (var instance in AudioInstances.Values)
                instance.Stop();

            AudioInstances.Clear();
        }
    }

    public static void Shutdown()
    {
        StopAll();
        AudioEngine.Shutdown();
    }

    // The manager stops tracking finished or stopped instances, but callers may
    // retain the handle and replay it; Play() re-registers it here.
    internal static void Register(AudioInstance instance)
    {
        lock (Sync)
        {
            if (AudioInstances.TryGetValue(instance.InstanceID, out var existing) && ReferenceEquals(existing, instance))
                return;

            if (AudioInstances.ContainsKey(instance.InstanceID))
                instance.InstanceID = GetNextID();

            AudioInstances.Add(instance.InstanceID, instance);
        }
    }

    internal static void CopyActiveInstances(List<AudioInstance> destination)
    {
        lock (Sync)
        {
            destination.Clear();
            destination.AddRange(AudioInstances.Values);
        }
    }

    internal static void ResetForTests()
    {
        lock (Sync)
        {
            StopAll();
            VolumeSettings.Clear();
            DefaultVolume = 0.5f;
            MasterVolume = 1f;
            _nextID = 0;
        }
    }

    private static float GetTypeVolume(int type) =>
        VolumeSettings.TryGetValue(type, out var volume) ? volume : DefaultVolume;

    private static int GetNextID()
    {
        while (AudioInstances.ContainsKey(_nextID))
            _nextID = _nextID == int.MaxValue ? 0 : _nextID + 1;

        var id = _nextID;
        _nextID = _nextID == int.MaxValue ? 0 : _nextID + 1;
        return id;
    }
}
