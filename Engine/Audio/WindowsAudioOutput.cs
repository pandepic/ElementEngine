using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ElementEngine;

// Uses CALLBACK_EVENT rather than a function callback: WinMM invokes function
// callbacks on a driver thread that must not run arbitrary code (locks, GC, JIT),
// while an event handle is explicitly sanctioned and leaves no native-to-managed
// callback to race against Dispose.
internal sealed class WindowsAudioOutput : IAudioOutput
{
    private const int BufferCount = 3;
    private const uint WaveMapper = uint.MaxValue;
    private const uint CallbackEvent = 0x00050000;
    private const int InQueueFlag = 0x00000010;

    private static readonly int FlagsOffset = (int)Marshal.OffsetOf<WaveHeader>(nameof(WaveHeader.Flags));

    private readonly AutoResetEvent _bufferDone = new(false);
    private readonly WaitHandle[] _writeWaitHandles = new WaitHandle[2];
    private readonly Buffer[] _buffers = new Buffer[BufferCount];
    private nint _device;
    private volatile bool _disposed;

    public WindowsAudioOutput()
    {
        var format = new WaveFormatEx
        {
            FormatTag = 1,
            Channels = AudioEngine.Channels,
            SamplesPerSecond = AudioEngine.SampleRate,
            AverageBytesPerSecond = AudioEngine.SampleRate * AudioEngine.Channels * sizeof(short),
            BlockAlign = AudioEngine.Channels * sizeof(short),
            BitsPerSample = sizeof(short) * 8,
        };

        var result = waveOutOpen(
            out _device,
            WaveMapper,
            ref format,
            _bufferDone.SafeWaitHandle.DangerousGetHandle(),
            0,
            CallbackEvent);
        if (result != 0)
        {
            _bufferDone.Dispose();
            ThrowWaveOutError(result, "open the default Windows audio device");
        }

        _writeWaitHandles[0] = _bufferDone;

        try
        {
            var byteLength = AudioEngine.BufferSampleCount * sizeof(short);
            for (var i = 0; i < _buffers.Length; i++)
            {
                var data = Marshal.AllocHGlobal(byteLength);
                var header = Marshal.AllocHGlobal(Marshal.SizeOf<WaveHeader>());
                Marshal.StructureToPtr(new WaveHeader
                {
                    Data = data,
                    BufferLength = (uint)byteLength,
                }, header, false);

                _buffers[i] = new Buffer(data, header);
                ThrowWaveOutError(
                    waveOutPrepareHeader(_device, header, Marshal.SizeOf<WaveHeader>()),
                    "prepare a Windows audio buffer");
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Write(short[] samples, CancellationToken cancellationToken)
    {
        if (samples.Length != AudioEngine.BufferSampleCount)
            throw new ArgumentException("The Windows audio buffer has an unexpected size.", nameof(samples));

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(_disposed, this);

            foreach (var buffer in _buffers)
            {
                if ((Marshal.ReadInt32(buffer.Header + FlagsOffset) & InQueueFlag) != 0)
                    continue;

                Marshal.Copy(samples, 0, buffer.Data, samples.Length);
                ThrowWaveOutError(
                    waveOutWrite(_device, buffer.Header, Marshal.SizeOf<WaveHeader>()),
                    "write to the default Windows audio device");
                return;
            }

            _writeWaitHandles[1] = cancellationToken.WaitHandle;
            WaitHandle.WaitAny(_writeWaitHandles);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_device != 0)
        {
            _ = waveOutReset(_device);
            foreach (var buffer in _buffers)
            {
                if (buffer.Header == 0)
                    continue;

                _ = waveOutUnprepareHeader(_device, buffer.Header, Marshal.SizeOf<WaveHeader>());
                Marshal.FreeHGlobal(buffer.Header);
                Marshal.FreeHGlobal(buffer.Data);
            }

            _ = waveOutClose(_device);
            _device = 0;
        }

        _bufferDone.Dispose();
    }

    private static void ThrowWaveOutError(uint result, string operation)
    {
        if (result == 0)
            return;

        var message = new StringBuilder(256);
        _ = waveOutGetErrorText(result, message, message.Capacity);
        throw new InvalidOperationException($"Could not {operation}: {message} (WinMM error {result}).");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveFormatEx
    {
        public ushort FormatTag;
        public ushort Channels;
        public uint SamplesPerSecond;
        public uint AverageBytesPerSecond;
        public ushort BlockAlign;
        public ushort BitsPerSample;
        public ushort ExtraSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveHeader
    {
        public nint Data;
        public uint BufferLength;
        public uint BytesRecorded;
        public nint UserData;
        public uint Flags;
        public uint Loops;
        public nint Next;
        public nint Reserved;
    }

    private readonly record struct Buffer(nint Data, nint Header);

    [DllImport("winmm.dll", CallingConvention = CallingConvention.Winapi)]
    private static extern uint waveOutOpen(
        out nint waveOut,
        uint deviceId,
        ref WaveFormatEx format,
        nint callbackEvent,
        nint instance,
        uint flags);

    [DllImport("winmm.dll", CallingConvention = CallingConvention.Winapi)]
    private static extern uint waveOutPrepareHeader(nint waveOut, nint header, int headerSize);

    [DllImport("winmm.dll", CallingConvention = CallingConvention.Winapi)]
    private static extern uint waveOutUnprepareHeader(nint waveOut, nint header, int headerSize);

    [DllImport("winmm.dll", CallingConvention = CallingConvention.Winapi)]
    private static extern uint waveOutWrite(nint waveOut, nint header, int headerSize);

    [DllImport("winmm.dll", CallingConvention = CallingConvention.Winapi)]
    private static extern uint waveOutReset(nint waveOut);

    [DllImport("winmm.dll", CallingConvention = CallingConvention.Winapi)]
    private static extern uint waveOutClose(nint waveOut);

    [DllImport("winmm.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)]
    private static extern uint waveOutGetErrorText(uint error, StringBuilder message, int messageLength);
}
