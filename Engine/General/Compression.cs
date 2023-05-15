using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ElementEngine
{
    public static class Compression
    {
        public static byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            return Zip(bytes);
        }

        public static byte[] Zip(byte[] bytes)
        {
            using var streamBytes = new MemoryStream(bytes);
            using var streamOutput = new MemoryStream();
            using (var gs = new GZipStream(streamOutput, CompressionMode.Compress))
                CopyTo(streamBytes, gs);

            return streamOutput.ToArray();
        }

        public static byte[] Zip(MemoryStream ms)
        {
            using var streamOutput = new MemoryStream();
            using (var gs = new GZipStream(streamOutput, CompressionMode.Compress))
                CopyTo(ms, gs);

            return streamOutput.ToArray();
        }

        public static MemoryStream ZipToMS(MemoryStream ms)
        {
            using var streamOutput = new MemoryStream();
            using (var gs = new GZipStream(streamOutput, CompressionMode.Compress))
                CopyTo(ms, gs);

            return streamOutput;
        }

        public static string UnzipToString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(Unzip(bytes));
        }

        public static byte[] Unzip(byte[] bytes)
        {
            using var streamBytes = new MemoryStream(bytes);
            using var streamOutput = new MemoryStream();
            using (var gs = new GZipStream(streamBytes, CompressionMode.Decompress))
                CopyTo(gs, streamOutput);

            return streamOutput.ToArray();
        }

        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];
            int count;

            while ((count = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, count);
            }
        }
    }
}
