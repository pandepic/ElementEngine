using System.Buffers.Binary;
using System.Text;
using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class AudioDecoderTests
{
    [Fact]
    public void DecodesPcm16WavWithUnknownOddSizedChunk()
    {
        var samples = new short[] { short.MinValue, -16384, 0, short.MaxValue };
        using var wav = CreatePcm16Wav(24000, 2, samples, includeOddSizedChunk: true);

        using var source = AudioDecoder.DecodeWav(wav);

        Assert.Equal(new AudioFormat(24000, 2), source.Format);
        Assert.Equal(TimeSpan.FromSeconds(2d / 24000), source.Duration);
        Assert.Equal([-1f, -0.5f, 0f, short.MaxValue / 32768f], source.Samples.ToArray());
    }

    [Fact]
    public void DecodesIeeeFloatWav()
    {
        using var wav = CreateWav(formatTag: 3, 48000, channels: 1, bitsPerSample: 32, sampleCount: 2, writer =>
        {
            writer.Write(-0.25f);
            writer.Write(0.75f);
        });

        using var source = AudioDecoder.DecodeWav(wav);

        Assert.Equal([-0.25f, 0.75f], source.Samples.ToArray());
    }

    [Fact]
    public void ZeroesNonFiniteSamplesInIeeeFloatWav()
    {
        using var wav = CreateWav(formatTag: 3, 48000, channels: 1, bitsPerSample: 32, sampleCount: 3, writer =>
        {
            writer.Write(float.NaN);
            writer.Write(0.5f);
            writer.Write(float.PositiveInfinity);
        });

        using var source = AudioDecoder.DecodeWav(wav);

        Assert.Equal([0f, 0.5f, 0f], source.Samples.ToArray());
    }

    [Fact]
    public void RejectsTruncatedWavChunks()
    {
        using var wav = new MemoryStream(Encoding.ASCII.GetBytes("RIFF\x20\0\0\0WAVEfmt \x10\0\0\0"));

        Assert.Throws<InvalidDataException>(() => AudioDecoder.DecodeWav(wav));
    }

    [Fact]
    public void RejectsWavWithMoreThanTwoChannels()
    {
        using var wav = CreateWav(formatTag: 1, 48000, channels: 6, bitsPerSample: 16, sampleCount: 6, writer =>
        {
            for (var i = 0; i < 6; i++)
                writer.Write((short)0);
        });

        Assert.Throws<NotSupportedException>(() => AudioDecoder.DecodeWav(wav));
    }

    [Fact]
    public void IgnoresBytesAppendedAfterTheRiffChunk()
    {
        using var wav = CreatePcm16Wav(48000, 1, [1000, -1000], includeOddSizedChunk: false);
        wav.Seek(0, SeekOrigin.End);
        wav.Write(Encoding.ASCII.GetBytes("TAGtrailing tag data outside the container"));
        wav.Position = 0;

        using var source = AudioDecoder.DecodeWav(wav);

        Assert.Equal([1000 / 32768f, -1000 / 32768f], source.Samples.ToArray());
    }

    [Fact]
    public void ClampsPlaceholderChunkSizesFromNonFinalizedWriters()
    {
        using var wav = CreatePcm16Wav(48000, 1, [1000, -1000], includeOddSizedChunk: false);
        var bytes = wav.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), uint.MaxValue);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(40), uint.MaxValue);
        using var placeholderWav = new MemoryStream(bytes);

        using var source = AudioDecoder.DecodeWav(placeholderWav);

        Assert.Equal([1000 / 32768f, -1000 / 32768f], source.Samples.ToArray());
    }

    private static MemoryStream CreatePcm16Wav(
        int sampleRate,
        short channels,
        short[] samples,
        bool includeOddSizedChunk)
    {
        return CreateWav(formatTag: 1, sampleRate, channels, bitsPerSample: 16, samples.Length, writer =>
        {
            foreach (var sample in samples)
                writer.Write(sample);
        }, includeOddSizedChunk);
    }

    private static MemoryStream CreateWav(
        ushort formatTag,
        int sampleRate,
        short channels,
        ushort bitsPerSample,
        int sampleCount,
        Action<BinaryWriter> writeSamples,
        bool includeOddSizedChunk = false)
    {
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        var unknownChunkLength = includeOddSizedChunk ? 10 : 0;
        var bytesPerSample = bitsPerSample / 8;
        var dataLength = sampleCount * bytesPerSample;

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write((uint)(36 + unknownChunkLength + dataLength));
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        if (includeOddSizedChunk)
        {
            writer.Write(Encoding.ASCII.GetBytes("JUNK"));
            writer.Write(1u);
            writer.Write((byte)42);
            writer.Write((byte)0);
        }

        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16u);
        writer.Write(formatTag);
        writer.Write((ushort)channels);
        writer.Write((uint)sampleRate);
        writer.Write((uint)(sampleRate * channels * bytesPerSample));
        writer.Write((ushort)(channels * bytesPerSample));
        writer.Write(bitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write((uint)dataLength);
        writeSamples(writer);

        stream.Position = 0;
        return stream;
    }
}
