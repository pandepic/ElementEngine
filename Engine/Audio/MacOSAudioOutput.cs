using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace ElementEngine;

internal sealed class MacOSAudioOutput : IAudioOutput
{
    private const string AudioToolbox = "/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox";
    private const int BufferCount = 3;
    private const uint LinearPcmFormat = 0x6c70636d;
    private const uint SignedIntegerFlag = 1 << 2;
    private const uint PackedFlag = 1 << 3;

    // The completion callback resolves its target here by queue handle instead of
    // via a GCHandle in userData: a callback still in flight during Dispose then
    // finds no entry and returns, rather than dereferencing a freed handle.
    private static readonly ConcurrentDictionary<nint, MacOSAudioOutput> LiveOutputs = new();

    private static readonly int AudioDataOffset = (int)Marshal.OffsetOf<AudioQueueBuffer>(nameof(AudioQueueBuffer.AudioData));
    private static readonly int AudioDataByteSizeOffset = (int)Marshal.OffsetOf<AudioQueueBuffer>(nameof(AudioQueueBuffer.AudioDataByteSize));

    private readonly object _sync = new();
    private readonly Queue<int> _availableBuffers = new();
    private readonly SemaphoreSlim _bufferAvailable = new(BufferCount, BufferCount);
    private readonly nint[] _buffers = new nint[BufferCount];
    private readonly AudioQueueCallback _callback;
    private nint _queue;
    private bool _started;
    private bool _disposed;

    public MacOSAudioOutput()
    {
        _callback = OnBufferComplete;
        var format = new AudioStreamBasicDescription
        {
            SampleRate = AudioEngine.SampleRate,
            FormatId = LinearPcmFormat,
            FormatFlags = SignedIntegerFlag | PackedFlag,
            BytesPerPacket = AudioEngine.Channels * sizeof(short),
            FramesPerPacket = 1,
            BytesPerFrame = AudioEngine.Channels * sizeof(short),
            ChannelsPerFrame = AudioEngine.Channels,
            BitsPerChannel = sizeof(short) * 8,
        };

        var result = AudioQueueNewOutput(
            ref format,
            _callback,
            0,
            0,
            0,
            0,
            out _queue);
        if (result != 0)
            ThrowCoreAudioError(result, "open the default macOS audio device");

        LiveOutputs[_queue] = this;

        try
        {
            var byteLength = AudioEngine.BufferSampleCount * sizeof(short);
            for (var i = 0; i < _buffers.Length; i++)
            {
                ThrowCoreAudioError(
                    AudioQueueAllocateBuffer(_queue, (uint)byteLength, out _buffers[i]),
                    "allocate a macOS audio buffer");
                _availableBuffers.Enqueue(i);
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
            throw new ArgumentException("The macOS audio buffer has an unexpected size.", nameof(samples));

        _bufferAvailable.Wait(cancellationToken);

        int index;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            index = _availableBuffers.Dequeue();
        }

        var bufferPointer = _buffers[index];
        Marshal.Copy(samples, 0, Marshal.ReadIntPtr(bufferPointer + AudioDataOffset), samples.Length);
        Marshal.WriteInt32(bufferPointer + AudioDataByteSizeOffset, samples.Length * sizeof(short));

        var result = AudioQueueEnqueueBuffer(_queue, bufferPointer, 0, 0);
        if (result != 0)
        {
            ReturnBuffer(index);
            ThrowCoreAudioError(result, "queue a macOS audio buffer");
        }

        if (_started)
            return;

        result = AudioQueueStart(_queue, 0);
        if (result != 0)
            ThrowCoreAudioError(result, "start the default macOS audio device");
        _started = true;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;
            _disposed = true;
        }

        if (_queue != 0)
        {
            // Unregister before the native queue is freed: once freed, a new output
            // can be handed the recycled handle, and removing the key afterwards
            // would delete that new output's registration.
            LiveOutputs.TryRemove(_queue, out _);
            _ = AudioQueueStop(_queue, 1);
            _ = AudioQueueDispose(_queue, 1);
            _queue = 0;
        }

        // The SemaphoreSlim is deliberately not disposed: it holds no unmanaged
        // resources, and disposing it would race a late completion callback's Release.
    }

    private static void OnBufferComplete(nint userData, nint queue, nint buffer)
    {
        if (!LiveOutputs.TryGetValue(queue, out var output))
            return;

        for (var i = 0; i < output._buffers.Length; i++)
        {
            if (output._buffers[i] == buffer)
            {
                output.ReturnBuffer(i);
                return;
            }
        }
    }

    private void ReturnBuffer(int index)
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _availableBuffers.Enqueue(index);
            _bufferAvailable.Release();
        }
    }

    private static void ThrowCoreAudioError(int result, string operation)
    {
        if (result == 0)
            return;

        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, (uint)result);
        var fourCc = System.Text.Encoding.ASCII.GetString(bytes);
        throw new InvalidOperationException($"Could not {operation}: CoreAudio error {result} ('{fourCc}').");
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void AudioQueueCallback(nint userData, nint queue, nint buffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioStreamBasicDescription
    {
        public double SampleRate;
        public uint FormatId;
        public uint FormatFlags;
        public uint BytesPerPacket;
        public uint FramesPerPacket;
        public uint BytesPerFrame;
        public uint ChannelsPerFrame;
        public uint BitsPerChannel;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioQueueBuffer
    {
        public uint AudioDataBytesCapacity;
        public nint AudioData;
        public uint AudioDataByteSize;
        public nint UserData;
        public uint PacketDescriptionCapacity;
        public nint PacketDescriptions;
        public uint PacketDescriptionCount;
    }

    [DllImport(AudioToolbox, CallingConvention = CallingConvention.Cdecl)]
    private static extern int AudioQueueNewOutput(
        ref AudioStreamBasicDescription format,
        AudioQueueCallback callback,
        nint userData,
        nint callbackRunLoop,
        nint callbackRunLoopMode,
        uint flags,
        out nint queue);

    [DllImport(AudioToolbox, CallingConvention = CallingConvention.Cdecl)]
    private static extern int AudioQueueAllocateBuffer(nint queue, uint bufferSize, out nint buffer);

    [DllImport(AudioToolbox, CallingConvention = CallingConvention.Cdecl)]
    private static extern int AudioQueueEnqueueBuffer(
        nint queue,
        nint buffer,
        uint packetDescriptionCount,
        nint packetDescriptions);

    [DllImport(AudioToolbox, CallingConvention = CallingConvention.Cdecl)]
    private static extern int AudioQueueStart(nint queue, nint startTime);

    [DllImport(AudioToolbox, CallingConvention = CallingConvention.Cdecl)]
    private static extern int AudioQueueStop(nint queue, byte immediate);

    [DllImport(AudioToolbox, CallingConvention = CallingConvention.Cdecl)]
    private static extern int AudioQueueDispose(nint queue, byte immediate);
}
