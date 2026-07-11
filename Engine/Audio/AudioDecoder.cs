using NVorbis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ElementEngine;

internal static class AudioDecoder
{
    private const ushort PcmFormat = 1;
    private const ushort IeeeFloatFormat = 3;
    private const ushort ExtensibleFormat = 0xfffe;

    public static AudioSource DecodeWav(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanSeek)
        {
            using var bufferedStream = new MemoryStream();
            stream.CopyTo(bufferedStream);
            bufferedStream.Position = 0;
            return DecodeWav(bufferedStream);
        }

        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var riffStart = stream.Position;
        if (ReadFourCc(reader) != "RIFF")
            throw new InvalidDataException("The stream is not a little-endian RIFF WAV file.");

        var riffSize = reader.ReadUInt32();
        if (ReadFourCc(reader) != "WAVE")
            throw new InvalidDataException("The RIFF stream does not contain WAVE audio.");

        // Bytes past the declared RIFF size are not chunk data (tools append tags
        // there), and a non-finalized writer can declare more than is present, so
        // the walk stops at whichever bound comes first.
        var riffEnd = Math.Min(stream.Length, riffStart + 8 + riffSize);

        WavFormat? format = null;
        byte[] data = null;

        while (stream.Position + 8 <= riffEnd && (format is null || data is null))
        {
            var chunkId = ReadFourCc(reader);
            var chunkSize = (long)reader.ReadUInt32();
            var availableBytes = riffEnd - stream.Position;
            var chunkEnd = stream.Position + chunkSize;

            if (chunkId == "fmt ")
            {
                if (chunkSize > availableBytes)
                    throw new InvalidDataException("The WAV fmt chunk is truncated.");
                format ??= ReadWavFormat(reader, (uint)chunkSize);
            }
            else if (chunkId == "data" && data is null)
            {
                // A placeholder or oversized declared length is clamped to the
                // bytes actually present so the available audio still loads.
                var dataLength = (int)Math.Min(chunkSize, Math.Min(availableBytes, int.MaxValue));
                data = reader.ReadBytes(dataLength);
            }

            stream.Position = Math.Min(riffEnd, chunkEnd + (chunkSize & 1));
        }

        if (format is null)
            throw new InvalidDataException("The WAV stream has no format chunk.");
        if (data is null)
            throw new InvalidDataException("The WAV stream has no data chunk.");

        return DecodeWavSamples(format.Value, data);
    }

    public static AudioSource DecodeOggVorbis(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // NVorbis's forward-only path throws from TotalSamples on non-seekable
        // streams, so buffer them the same way DecodeWav does.
        if (!stream.CanSeek)
        {
            using var bufferedStream = new MemoryStream();
            stream.CopyTo(bufferedStream);
            bufferedStream.Position = 0;
            return DecodeOggVorbis(bufferedStream);
        }

        using var reader = new VorbisReader(stream, closeOnDispose: false);
        if (reader.Channels <= 0 || reader.SampleRate <= 0)
            throw new InvalidDataException("The Ogg Vorbis channel count and sample rate must be positive.");
        if (reader.Channels > 2)
            throw new NotSupportedException($"{reader.Channels}-channel Ogg Vorbis audio is not supported; use mono or stereo.");

        var format = new AudioFormat(reader.SampleRate, reader.Channels);
        var totalSamples = reader.TotalSamples;

        if (totalSamples > 0 && totalSamples <= int.MaxValue / reader.Channels)
        {
            var samples = new float[(int)(totalSamples * reader.Channels)];
            var offset = 0;
            int read;
            while (offset < samples.Length
                && (read = reader.ReadSamples(samples, offset, samples.Length - offset)) > 0)
            {
                offset += read;
            }

            if (offset != samples.Length)
                Array.Resize(ref samples, offset);

            return new AudioSource(format, samples);
        }

        var readBuffer = new float[4096 - (4096 % reader.Channels)];
        var sampleList = new List<float>();
        int samplesRead;
        while ((samplesRead = reader.ReadSamples(readBuffer, 0, readBuffer.Length)) > 0)
            sampleList.AddRange(readBuffer.AsSpan(0, samplesRead));

        return new AudioSource(format, sampleList.ToArray());
    }

    private static WavFormat ReadWavFormat(BinaryReader reader, uint chunkSize)
    {
        if (chunkSize < 16)
            throw new InvalidDataException("The WAV format chunk is too short.");

        var formatTag = reader.ReadUInt16();
        var channels = reader.ReadUInt16();
        var sampleRate = reader.ReadUInt32();
        _ = reader.ReadUInt32();
        var blockAlign = reader.ReadUInt16();
        var bitsPerSample = reader.ReadUInt16();

        if (formatTag == ExtensibleFormat)
        {
            if (chunkSize < 40)
                throw new InvalidDataException("The extensible WAV format chunk is too short.");

            var extensionSize = reader.ReadUInt16();
            if (extensionSize < 22)
                throw new InvalidDataException("The extensible WAV format data is incomplete.");

            _ = reader.ReadUInt16();
            _ = reader.ReadUInt32();
            formatTag = reader.ReadUInt16();
            _ = reader.ReadBytes(14);
        }

        if (formatTag is not PcmFormat and not IeeeFloatFormat)
            throw new NotSupportedException($"WAV format tag {formatTag} is not supported.");
        if (channels == 0 || sampleRate == 0)
            throw new InvalidDataException("The WAV channel count and sample rate must be positive.");
        if (channels > 2)
            throw new NotSupportedException($"{channels}-channel WAV audio is not supported; use mono or stereo.");

        var bytesPerSample = (bitsPerSample + 7) / 8;
        if (blockAlign != channels * bytesPerSample)
            throw new InvalidDataException("The WAV block alignment does not match its channel and sample sizes.");

        var supported = formatTag == PcmFormat
            ? bitsPerSample is 8 or 16 or 24 or 32
            : bitsPerSample is 32 or 64;
        if (!supported)
            throw new NotSupportedException($"{bitsPerSample}-bit WAV format tag {formatTag} is not supported.");

        return new WavFormat(formatTag, (int)sampleRate, channels, bitsPerSample, blockAlign);
    }

    private static AudioSource DecodeWavSamples(WavFormat format, byte[] data)
    {
        // A trailing partial frame (clamped or truncated data chunk) is dropped
        // rather than rejecting the audio that is present.
        var bytesPerSample = format.BitsPerSample / 8;
        var sampleData = data.AsSpan(0, data.Length - (data.Length % format.BlockAlign));
        var samples = new float[sampleData.Length / bytesPerSample];

        // WAV data is little-endian; the reinterpret casts read native-endian,
        // which matches on every platform this engine supports.
        switch (format.FormatTag, format.BitsPerSample)
        {
            case (PcmFormat, 8):
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = (sampleData[i] - 128) / 128f;
                break;
            case (PcmFormat, 16):
                var shorts = MemoryMarshal.Cast<byte, short>(sampleData);
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = shorts[i] / 32768f;
                break;
            case (PcmFormat, 24):
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = ReadInt24(sampleData.Slice(i * 3, 3)) / 8388608f;
                break;
            case (PcmFormat, 32):
                var ints = MemoryMarshal.Cast<byte, int>(sampleData);
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = ints[i] / 2147483648f;
                break;
            case (IeeeFloatFormat, 32):
                var floats = MemoryMarshal.Cast<byte, float>(sampleData);
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = SanitizeSample(floats[i]);
                break;
            case (IeeeFloatFormat, 64):
                var doubles = MemoryMarshal.Cast<byte, double>(sampleData);
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = SanitizeSample((float)doubles[i]);
                break;
            default:
                throw new UnreachableException();
        }

        return new AudioSource(new AudioFormat(format.SampleRate, format.Channels), samples);
    }

    private static int ReadInt24(ReadOnlySpan<byte> source)
    {
        var value = source[0] | (source[1] << 8) | (source[2] << 16);
        return (value & 0x800000) == 0 ? value : value | unchecked((int)0xff000000);
    }

    // A NaN from a corrupt float WAV would poison the mixer's accumulated sum and
    // mute every concurrently playing sound, so non-finite samples become silence.
    private static float SanitizeSample(float value) => float.IsFinite(value) ? value : 0f;

    // Decode the ID as raw bytes: a multi-byte text decoding could consume more
    // than four bytes on non-ASCII input and silently corrupt chunk parsing.
    private static string ReadFourCc(BinaryReader reader) =>
        System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4));

    private readonly record struct WavFormat(
        ushort FormatTag,
        int SampleRate,
        int Channels,
        int BitsPerSample,
        int BlockAlign);
}
