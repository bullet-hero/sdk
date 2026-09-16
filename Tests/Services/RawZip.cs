using System.IO;
using System.Text;

namespace BH.SDK.Tests.Services
{
    // AN ARCHIVE BUILT FIELD BY FIELD, because a hostile zip is one no writer here will produce -
    // asking ZipService for a traversal name proves only that it refuses twice. It is also the only
    // way this assembly can write a zip at all: BH.SDK.Tests.asmdef sets overrideReferences and
    // lists nunit and Newtonsoft, so SharpZipLib is not visible here and a fixture built on
    // ZipEntry would compile under `dotnet test` and then fail to compile inside Unity.
    //
    // It stands in for a foreign writer as well as a hostile one - Explorer, `zip -r`, a server -
    // which is what the import direction needs: bytes this project did not produce, read by the
    // reader a player's file actually goes through.

    /// <summary> A zip written by hand, so a test can claim anything about it that an arriving file
    /// could claim about itself. </summary>
    internal static class RawZip
    {
        /// <summary> Uncompressed, so a fixture needs no compressor. </summary>
        public const short Stored = 0;

        /// <summary> One entry, with every field a check reads left to the caller. </summary>
        public readonly struct Entry
        {
            public readonly string Name;
            public readonly byte[] Content;
            public readonly short Method;
            public readonly int ExternalAttributes;

            public Entry(string name, string content, short method = Stored, int externalAttributes = 0)
                : this(name, Encoding.UTF8.GetBytes(content), method, externalAttributes)
            {
            }

            public Entry(string name, byte[] content, short method = Stored, int externalAttributes = 0)
            {
                Name = name;
                Content = content;
                Method = method;
                ExternalAttributes = externalAttributes;
            }
        }

        // The smallest file the format allows, per entry: a local header, the bytes stored
        // uncompressed, a central directory and an end record.
        public static byte[] Build(params Entry[] entries)
        {
            using var buffer = new MemoryStream();
            using var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true);

            var offsets = new int[entries.Length];
            var names = new byte[entries.Length][];

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                names[i] = Encoding.UTF8.GetBytes(entry.Name);
                offsets[i] = (int)buffer.Position;

                writer.Write(0x04034b50);
                writer.Write((short)20);
                writer.Write((short)0);
                writer.Write(entry.Method);
                writer.Write((short)0); // time
                writer.Write((short)0x0021); // date: 1980-01-01
                writer.Write(Crc32Of(entry.Content));
                writer.Write(entry.Content.Length);
                writer.Write(entry.Content.Length);
                writer.Write((short)names[i].Length);
                writer.Write((short)0);
                writer.Write(names[i]);
                writer.Write(entry.Content);
            }

            var centralOffset = (int)buffer.Position;

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];

                writer.Write(0x02014b50);
                writer.Write((short)(3 << 8 | 20)); // made by unix
                writer.Write((short)20);
                writer.Write((short)0);
                writer.Write(entry.Method);
                writer.Write((short)0);
                writer.Write((short)0x0021);
                writer.Write(Crc32Of(entry.Content));
                writer.Write(entry.Content.Length);
                writer.Write(entry.Content.Length);
                writer.Write((short)names[i].Length);
                writer.Write((short)0); // extra
                writer.Write((short)0); // comment
                writer.Write((short)0); // disk
                writer.Write((short)0); // internal attributes
                writer.Write(entry.ExternalAttributes);
                writer.Write(offsets[i]);
                writer.Write(names[i]);
            }

            var centralSize = (int)buffer.Position - centralOffset;

            writer.Write(0x06054b50);
            writer.Write((short)0);
            writer.Write((short)0);
            writer.Write((short)entries.Length);
            writer.Write((short)entries.Length);
            writer.Write(centralSize);
            writer.Write(centralOffset);
            writer.Write((short)0);

            writer.Flush();
            return buffer.ToArray();
        }

        private static int Crc32Of(byte[] content)
        {
            var crc = 0xFFFFFFFFu;
            foreach (var value in content)
            {
                crc ^= value;
                for (var bit = 0; bit < 8; bit++)
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }

            return unchecked((int)(crc ^ 0xFFFFFFFFu));
        }
    }
}
