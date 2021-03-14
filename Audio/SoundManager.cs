using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Veldrid;

namespace ElementEngine
{
    public enum AudioSourceType
    {
        Auto,
        Ogg,
        WAV
    }

    public class AudioSource : IDisposable
    {
        public WaveFormat Format { get; set; }
        public MemoryStream Data { get; set; }
        public string AssetName { get; set; }

        #region IDisposable
        protected bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Data?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public AudioSource(IWaveProvider provider)
        {
            Data = new MemoryStream();
            Format = provider.WaveFormat;
            WaveFileWriter.WriteWavFileToStream(Data, provider);
        }

        ~AudioSource()
        {
            Dispose(false);
        }
    } // AudioSource

    public class AudioInstance : IDisposable
    {
        public WaveOutEvent WaveOut { get; set; }
        public MemoryStream Data { get; set; }
        public RawSourceWaveStream WaveStream { get; set; }
        public long StreamStartPosition { get; set; }

        public int InstanceID { get; set; }
        public bool Looping { get; set; } = false;
        public int Type { get; set; }
        public string AssetName { get; set; }

        public float Volume
        {
            get => WaveOut.Volume;
            set => WaveOut.Volume = value;
        }

        public PlaybackState PlaybackState { get => WaveOut.PlaybackState; }

        #region IDisposable
        protected bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    WaveOut?.Dispose();
                    WaveStream?.Dispose();
                    Data?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public AudioInstance(AudioSource source)
        {
            AssetName = source.AssetName;

            Data?.Dispose();
            Data = new MemoryStream(source.Data.ToArray());
            StreamStartPosition = source.Data.Position;

            WaveStream = new RawSourceWaveStream(Data, source.Format);
            WaveOut = new WaveOutEvent();
            WaveOut.Init(WaveStream);
        }

        ~AudioInstance()
        {
            Dispose(false);
        }

        public void Play()
        {
            Data.Position = StreamStartPosition;
            WaveOut.Play();
        }

        public void Stop() => WaveOut.Stop();
        public void Pause() => WaveOut.Pause();
    }

    public static class SoundManager
    {
        public static int UISoundType = -1;
        public static float DefaultVolume { get; set; } = 0.5f;
        public static float MasterVolume { get; set; } = 1f;

        public static Dictionary<int, AudioInstance> AudioInstances { get; set; } = new Dictionary<int, AudioInstance>();
        public static Dictionary<int, float> VolumeSettings { get; set; } = new Dictionary<int, float>();

        private static List<int> _removeList = new List<int>();
        private static int _nextID = 0;

        public static void Update()
        {
            foreach (var instance in AudioInstances)
            {
                if (instance.Value.PlaybackState == PlaybackState.Stopped)
                {
                    if (instance.Value.Looping)
                        instance.Value.Play();
                    else
                        _removeList.Add(instance.Key);
                }
            }

            for (var i = 0; i < _removeList.Count; i++)
                AudioInstances.Remove(_removeList[i]);

            _removeList.Clear();
        } // Update

        public static AudioInstance Play(string assetName, int type, AudioSourceType sourceType = AudioSourceType.Auto, bool loop = false, bool allowDuplicates = false)
        {
            switch (sourceType)
            {
                case AudioSourceType.Auto:
                    return Play(AssetManager.LoadAudioSourceByExtension(assetName), type, loop, allowDuplicates);

                case AudioSourceType.Ogg:
                    return Play(AssetManager.LoadAudioSourceOggVorbis(assetName), type, loop, allowDuplicates);

                case AudioSourceType.WAV:
                    return Play(AssetManager.LoadAudioSourceWAV(assetName), type, loop, allowDuplicates);
            }

            return null;
        }

        public static AudioInstance Play(AudioSource source, int type, bool loop = false, bool allowDuplicates = false)
        {
            if (source == null)
                throw new ArgumentException("Can't play sound from a null audio source.", "source");

            if (!allowDuplicates)
            {
                foreach (var kvp in AudioInstances)
                {
                    if (kvp.Value.AssetName == source.AssetName)
                        return null;
                }
            }

            if (!VolumeSettings.ContainsKey(type))
                VolumeSettings.Add(type, DefaultVolume);

            var instance = new AudioInstance(source)
            {
                Type = type,
                Looping = loop,
                InstanceID = _nextID,
            };

            _nextID += 1;

            if (_nextID >= (int.MaxValue - 1))
                _nextID = 0;

            instance.Volume = VolumeSettings[type] * MasterVolume;
            instance.Play();

            AudioInstances.Add(instance.InstanceID, instance);
            return instance;

        } // Play

        public static void SetMasterVolume(float volume)
        {
            if (MasterVolume == volume)
                return;

            MasterVolume = volume;

            foreach (var instance in AudioInstances)
                instance.Value.Volume = VolumeSettings[instance.Value.Type] * MasterVolume;

        } // SetMasterVolume

        public static void SetVolume(int type, float volume)
        {
            if (!VolumeSettings.ContainsKey(type))
                VolumeSettings.Add(type, volume);
            else
                VolumeSettings[type] = volume;

            foreach (var instance in AudioInstances)
            {
                if (instance.Value.Type == type)
                    instance.Value.Volume = VolumeSettings[type] * MasterVolume;
            }
        } // SetVolume

        public static void StopByInstance(AudioInstance instance) => StopByID(instance.InstanceID);

        public static void StopByID(int id)
        {
            if (AudioInstances.TryGetValue(id, out var instance))
            {
                instance.Stop();
                AudioInstances.Remove(id);
            }
        } // StopByID

        public static void StopByType(int type)
        {
            foreach (var instance in AudioInstances)
            {
                instance.Value.Stop();
                _removeList.Add(instance.Key);
            }

            for (var i = 0; i < _removeList.Count; i++)
                AudioInstances.Remove(_removeList[i]);

            _removeList.Clear();

        } // StopByType

        public static void StopAll()
        {
            foreach (var instance in AudioInstances)
                instance.Value.Stop();

            AudioInstances.Clear();
        } // StopAll

    } // SoundManager
}
