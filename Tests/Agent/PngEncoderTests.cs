using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using BH.SDK.Agent.Imaging;
using BH.SDK.Models.Resources;
using NUnit.Framework;

namespace BH.SDK.Tests.Agent
{
    /// <summary> The agent's PNG writer: a file any decoder opens, pinned to the byte. </summary>
    [TestFixture]
    public class PngEncoderTests
    {
        // Computed once, checked by an independent zlib decoder, then pinned.
        private const string Golden4x4 =
            "89504e470d0a1a0a0000000d4948445200000004000000040806000000a9f19e7e0000003d49444154789c0dc841010030" +
            "080331a44cca49414aa54c4a1d4c021290c0c833b18638ada01cf9223803b4c832da1083b2259575379c83d5f22ddbef" +
            "03aa292071fa037a040000000049454e44ae426082";

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Encode_4x4_DecodesBackToItsScanlines()
        {
            var raster = Create4x4();
            var png = PngEncoder.Encode(raster);

            CollectionAssert.AreEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, Slice(png, 0, 8));

            var offset = 8;
            var ihdr = ReadChunk(png, ref offset, out var type);
            Assert.AreEqual("IHDR", type);
            CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 4, 0, 0, 0, 4, 8, 6, 0, 0, 0 }, ihdr);

            var idat = ReadChunk(png, ref offset, out type);
            Assert.AreEqual("IDAT", type);
            var iend = ReadChunk(png, ref offset, out type);
            Assert.AreEqual("IEND", type);
            Assert.AreEqual(0, iend.Length);
            Assert.AreEqual(png.Length, offset, "nothing after IEND");

            Assert.AreEqual(0x78, idat[0], "a zlib stream with a 32K window");
            Assert.AreEqual(0, (idat[0] * 256 + idat[1]) % 31, "zlib header check bits");

            var scanlines = Inflate(idat);
            var expected = new byte[4 * (1 + 4 * 4)];
            for (var y = 0; y < 4; y++)
            {
                expected[y * 17] = 0;
                Buffer.BlockCopy(raster.Pixels, y * 16, expected, y * 17 + 1, 16);
            }
            CollectionAssert.AreEqual(expected, scanlines);
            Assert.AreEqual(Adler32(expected), ReadBigEndian(idat, idat.Length - 4), "zlib trailer");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Encode_4x4_IsPinnedToTheByte()
        {
            Assert.AreEqual(Golden4x4, ToHex(PngEncoder.Encode(Create4x4())));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Encode_WrongBufferLength_Throws()
        {
            Assert.Throws<ArgumentException>(() => PngEncoder.Encode(new byte[15], 2, 2));
        }

        private static Raster Create4x4()
        {
            var raster = new Raster(4, 4);
            for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
                raster.SetPixel(x, y, new Pixel((byte)(x * 64), (byte)(y * 64), (byte)((x + y) * 32), (byte)(255 - x * 16)));
            return raster;
        }

        private static byte[] ReadChunk(byte[] png, ref int offset, out string type)
        {
            var length = (int)ReadBigEndian(png, offset);
            type = Encoding.ASCII.GetString(png, offset + 4, 4);
            var data = Slice(png, offset + 8, length);
            var crc = ReadBigEndian(png, offset + 8 + length);
            Assert.AreEqual(Crc32(Slice(png, offset + 4, 4 + length)), crc, $"CRC of {type}");
            offset += 12 + length;
            return data;
        }

        private static byte[] Inflate(byte[] zlib)
        {
            using var input = new MemoryStream(zlib, 2, zlib.Length - 6);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            deflate.CopyTo(output);
            return output.ToArray();
        }

        private static uint Crc32(byte[] data)
        {
            var crc = 0xFFFFFFFFu;
            foreach (var b in data)
            {
                crc ^= b;
                for (var k = 0; k < 8; k++)
                    crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
            }
            return crc ^ 0xFFFFFFFFu;
        }

        private static uint Adler32(byte[] data)
        {
            uint a = 1, b = 0;
            foreach (var value in data)
            {
                a = (a + value) % 65521;
                b = (b + a) % 65521;
            }
            return (b << 16) | a;
        }

        private static uint ReadBigEndian(byte[] data, int offset)
            => (uint)(data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3]);

        private static byte[] Slice(byte[] data, int offset, int length)
        {
            var slice = new byte[length];
            Buffer.BlockCopy(data, offset, slice, 0, length);
            return slice;
        }

        private static string ToHex(byte[] data)
        {
            var text = new StringBuilder(data.Length * 2);
            foreach (var b in data) text.Append(b.ToString("x2"));
            return text.ToString();
        }
    }
}
