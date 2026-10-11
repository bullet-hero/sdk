using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace BH.SDK.Agent.Imaging
{
    // Truecolour with alpha, 8 bits, no interlace, filter 0 on every row: the smallest valid PNG writer.
    // Golden-tested to the byte, so the zlib level and the chunk set are part of the contract.

    /// <summary> Encodes an RGBA8 image as a PNG file. </summary>
    public static class PngEncoder
    {
        private const byte BitDepth = 8;
        private const byte ColorTypeRgba = 6;

        private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

        /// <summary> The raster as PNG bytes. </summary>
        public static byte[] Encode(Raster raster)
        {
            if (raster == null) throw new ArgumentNullException(nameof(raster));
            return Encode(raster.Pixels, raster.Width, raster.Height);
        }

        /// <summary> A top-down RGBA8 buffer as PNG bytes. </summary>
        public static byte[] Encode(byte[] rgba, int width, int height)
        {
            if (rgba == null) throw new ArgumentNullException(nameof(rgba));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Must be positive.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Must be positive.");
            if (rgba.Length != checked(width * height * Raster.BytesPerPixel))
                throw new ArgumentException($"Expected {width * height * Raster.BytesPerPixel} bytes, got {rgba.Length}.",
                    nameof(rgba));

            var header = new byte[13];
            WriteBigEndian(header, 0, (uint)width);
            WriteBigEndian(header, 4, (uint)height);
            header[8] = BitDepth;
            header[9] = ColorTypeRgba;

            using var output = new MemoryStream();
            output.Write(Signature, 0, Signature.Length);
            WriteChunk(output, "IHDR", header);
            WriteChunk(output, "IDAT", Compress(rgba, width, height));
            WriteChunk(output, "IEND", Array.Empty<byte>());
            return output.ToArray();
        }

        private static byte[] Compress(byte[] rgba, int width, int height)
        {
            var stride = width * Raster.BytesPerPixel;
            using var buffer = new MemoryStream();
            using (var zlib = new DeflaterOutputStream(buffer, new Deflater(Deflater.DEFAULT_COMPRESSION, false)))
            {
                zlib.IsStreamOwner = false;
                for (var y = 0; y < height; y++)
                {
                    zlib.WriteByte(0);
                    zlib.Write(rgba, y * stride, stride);
                }
                zlib.Finish();
            }
            return buffer.ToArray();
        }

        private static void WriteChunk(Stream output, string type, byte[] data)
        {
            var typeBytes = Encoding.ASCII.GetBytes(type);
            var word = new byte[4];

            WriteBigEndian(word, 0, (uint)data.Length);
            output.Write(word, 0, 4);
            output.Write(typeBytes, 0, typeBytes.Length);
            output.Write(data, 0, data.Length);

            var crc = new Crc32();
            crc.Update(typeBytes);
            crc.Update(data);
            WriteBigEndian(word, 0, (uint)crc.Value);
            output.Write(word, 0, 4);
        }

        private static void WriteBigEndian(byte[] target, int offset, uint value)
        {
            target[offset] = (byte)(value >> 24);
            target[offset + 1] = (byte)(value >> 16);
            target[offset + 2] = (byte)(value >> 8);
            target[offset + 3] = (byte)value;
        }
    }
}
