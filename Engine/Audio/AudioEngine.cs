using System;
using System.Collections.Generic;
using System.Threading;

namespace ElementEngine;

internal interface IAudioOutput : IDisposable
{
    void Write(short[] samples, CancellationToken cancellationToken);
}

internal static class AudioEngine
{
    internal const int SampleRate = 48000;
    internal const int Channels = 2;
    internal const int BufferFrameCount = 1024;
    internal const int BufferSampleCount = BufferFrameCount * Channels;

    private static readonly object Sync = new();
    private static Func<IAudioOutput> _outputFactory = NativeAudioOutput.Create;
    private static CancellationTokenSource _cancellation;
    private static Thread _thread;

    public static void EnsureStarted()
    {
        lock (Sync)
        {
            if (_thread?.IsAlive == true)
                return;

            _cancellation?.Dispose();
            var cancellation = new CancellationTokenSource();
            _cancellation = cancellation;
            _thread = new Thread(() => Run(cancellation.Token))
            {
                IsBackground = true,
                Name = "ElementEngine Audio",
                Priority = ThreadPriority.AboveNormal,
            };
            _thread.Start();
        }
    }

    public static void Shutdown()
    {
        CancellationTokenSource cancellation;
        Thread thread;

        lock (Sync)
        {
            cancellation = _cancellation;
            thread = _thread;
            _cancellation = null;
            _thread = null;
        }

        cancellation?.Cancel();

        var stopped = thread is null || thread == Thread.CurrentThread || thread.Join(TimeSpan.FromSeconds(2));
        if (!stopped)
        {
            // The thread owns its output and disposes it when it exits; the token
            // source is left to the finalizer because disposing it now would race
            // the thread's waits on its handle.
            Logging.Warning("The audio thread did not stop in time; it will clean up when it exits.");
            return;
        }

        cancellation?.Dispose();
    }

    internal static void MixToPcm16(Span<short> destination, Span<float> mixBuffer, List<AudioInstance> activeInstances)
    {
        if (destination.Length % Channels != 0)
            throw new ArgumentException("The output buffer must contain complete stereo frames.", nameof(destination));
        if (mixBuffer.Length != destination.Length)
            throw new ArgumentException("The mix buffer must match the output buffer size.", nameof(mixBuffer));

        mixBuffer.Clear();

        SoundManager.CopyActiveInstances(activeInstances);
        foreach (var instance in activeInstances)
            instance.Mix(mixBuffer, SampleRate);

        for (var i = 0; i < destination.Length; i++)
        {
            var sample = Math.Clamp(mixBuffer[i], -1f, 1f);
            destination[i] = sample <= -1f
                ? short.MinValue
                : (short)MathF.Round(sample * short.MaxValue);
        }
    }

    internal static void SetOutputFactoryForTests(Func<IAudioOutput> outputFactory)
    {
        Shutdown();
        lock (Sync)
            _outputFactory = outputFactory;
    }

    internal static void ResetOutputFactoryForTests() =>
        SetOutputFactoryForTests(NativeAudioOutput.Create);

    // The device is opened, replaced after failures, and disposed entirely on this
    // thread, so the game thread never blocks on device init and a device error at
    // any point falls back to silence and retries instead of losing audio for good.
    private static void Run(CancellationToken cancellationToken)
    {
        var retryInterval = Math.Max(1, SampleRate / BufferFrameCount);
        var buffer = new short[BufferSampleCount];
        var mixBuffer = new float[buffer.Length];
        var activeInstances = new List<AudioInstance>();
        IAudioOutput output = null;
        var writesUntilRetry = 0;
        var failureLogged = false;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (output is null || (output is SilentAudioOutput && --writesUntilRetry <= 0))
                {
                    var device = TryCreateOutput(ref failureLogged);
                    if (device is not null)
                    {
                        output?.Dispose();
                        output = device;
                    }

                    output ??= new SilentAudioOutput();
                    writesUntilRetry = retryInterval;
                }

                MixToPcm16(buffer, mixBuffer, activeInstances);

                try
                {
                    output.Write(buffer, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    Logging.Error("The audio device failed, continuing without sound until it recovers: {exception}", exception.ToString());
                    output.Dispose();
                    output = new SilentAudioOutput();
                    writesUntilRetry = retryInterval;
                    failureLogged = true;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Logging.Error("The audio output thread stopped unexpectedly: {exception}", exception.ToString());
        }
        finally
        {
            output?.Dispose();
        }
    }

    private static IAudioOutput TryCreateOutput(ref bool failureLogged)
    {
        Func<IAudioOutput> outputFactory;
        lock (Sync)
            outputFactory = _outputFactory;

        try
        {
            var output = outputFactory();
            if (failureLogged)
                Logging.Information("Audio output recovered.");
            failureLogged = false;
            return output;
        }
        catch (Exception exception)
        {
            // Log the first failure only; the ~1s retry cadence would otherwise
            // flood the log on machines with no audio device at all.
            if (!failureLogged)
                Logging.Error("Audio output is unavailable, continuing without sound: {exception}", exception.ToString());
            failureLogged = true;
            return null;
        }
    }

    // Keeps the mixer advancing in real time when no device is available, so
    // playback state still progresses and finished instances are reaped.
    private sealed class SilentAudioOutput : IAudioOutput
    {
        public void Write(short[] samples, CancellationToken cancellationToken)
        {
            var duration = new AudioFormat(SampleRate, Channels).GetDuration(samples.Length);
            if (cancellationToken.WaitHandle.WaitOne(duration))
                cancellationToken.ThrowIfCancellationRequested();
        }

        public void Dispose()
        {
        }
    }
}
