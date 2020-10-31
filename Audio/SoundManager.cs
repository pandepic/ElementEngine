using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Veldrid;

namespace PandaEngine
{
    public class AudioSource : IDisposable
    {
        public WaveFormat Format { get; set; }
        public MemoryStream Data { get; set; }

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
        public static readonly int AUDIO_ID_NONE = -1;
        public static float DefaultVolume { get; set; } = 0.2f;
        public static float MasterVolume { get; set; } = 1f;

        public static Dictionary<int, AudioInstance> AudioInstances { get; set; } = new Dictionary<int, AudioInstance>();
        public static Dictionary<int, float> VolumeSettings { get; set; } = new Dictionary<int, float>();

        private static List<int> _removeList = new List<int>();
        private static int _nextID = 0;

        public static void Update()
        {
            foreach (var kvp in AudioInstances)
            {
                if (kvp.Value.PlaybackState == PlaybackState.Stopped)
                {
                    if (kvp.Value.Looping)
                        kvp.Value.Play();
                    else
                        _removeList.Add(kvp.Key);
                }
            }

            foreach (var remove in _removeList)
                AudioInstances.Remove(remove);

            _removeList.Clear();
        }

        public static int Play(string assetName, int type, bool loop = false, bool allowDuplicates = false)
        {
            if (!allowDuplicates)
            {
                foreach (var kvp in AudioInstances)
                {
                    if (kvp.Value.AssetName == assetName)
                        return AUDIO_ID_NONE;
                }
            }

            if (!VolumeSettings.ContainsKey(type))
                VolumeSettings.Add(type, DefaultVolume);

            var source = AssetManager.LoadAudioSourceFromOggVorbis(assetName);
            var instance = new AudioInstance(source)
            {
                AssetName = assetName,
                Type = type,
                Looping = loop,
            };

            instance.Volume = VolumeSettings[type] * MasterVolume;
            instance.Play();

            var id = _nextID;
            _nextID += 1;

            if (_nextID >= (int.MaxValue - 1))
                _nextID = 0;

            AudioInstances.Add(id, instance);

            return id;
        } // Play

    } // SoundManager
}
