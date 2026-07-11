using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ElementEngine;

internal sealed class LinuxAudioOutput : IAudioOutput
{
    private const string AlsaLibrary = "libasound.so.2";
    private const int PlaybackStream = 0;
    private const int ReadWriteInterleavedAccess = 3;
    private const int Signed16LittleEndianFormat = 2;

    private nint _device;

    public LinuxAudioOutput()
    {
        var result = snd_pcm_open(out _device, "default", PlaybackStream, 0);
        ThrowIfError(result, "open the default ALSA device");

        result = snd_pcm_set_params(
            _device,
            Signed16LittleEndianFormat,
            ReadWriteInterleavedAccess,
            AudioEngine.Channels,
            AudioEngine.SampleRate,
            1,
            100_000);
        if (result < 0)
        {
            Dispose();
            ThrowIfError(result, "configure the default ALSA device");
        }
    }

    public unsafe void Write(short[] samples, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        fixed (short* samplePointer = samples)
        {
            var framesRemaining = samples.Length / AudioEngine.Channels;
            var frameOffset = 0;
            while (framesRemaining > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = snd_pcm_writei(
                    _device,
                    samplePointer + (frameOffset * AudioEngine.Channels),
                    (nuint)framesRemaining);
                if (result < 0)
                    result = snd_pcm_recover(_device, (int)result, 1);
                ThrowIfError(result, "write to the default ALSA device");

                if (result == 0)
                    continue;

                frameOffset += (int)result;
                framesRemaining -= (int)result;
            }
        }
    }

    public void Dispose()
    {
        if (_device == 0)
            return;

        _ = snd_pcm_drop(_device);
        _ = snd_pcm_close(_device);
        _device = 0;
    }

    private static void ThrowIfError(long result, string operation)
    {
        if (result >= 0)
            return;

        var error = Marshal.PtrToStringAnsi(snd_strerror((int)result));
        throw new InvalidOperationException($"Could not {operation}: {error ?? $"ALSA error {result}"}.");
    }

    [DllImport(AlsaLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern int snd_pcm_open(out nint pcm, string name, int stream, int mode);

    [DllImport(AlsaLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern int snd_pcm_set_params(
        nint pcm,
        int format,
        int access,
        uint channels,
        uint rate,
        int softResample,
        uint latency);

    [DllImport(AlsaLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe nint snd_pcm_writei(nint pcm, short* buffer, nuint size);

    [DllImport(AlsaLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern int snd_pcm_recover(nint pcm, int error, int silent);

    [DllImport(AlsaLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern int snd_pcm_drop(nint pcm);

    [DllImport(AlsaLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern int snd_pcm_close(nint pcm);

    [DllImport(AlsaLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint snd_strerror(int error);
}
