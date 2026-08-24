using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    // THE ADVERSARIAL ARCHIVES HERE ARE BUILT BY HAND, out of raw 512-byte headers, and that is the
    // point rather than an inconvenience. A traversal entry or a symlink entry is not something the
    // writer under test can produce - it refuses to - so asking it to build the attack would prove
    // only that it refuses twice. A hostile archive arrives from somewhere else, and so does this
    // one: the fixture below writes the ustar fields directly, exactly as `tar` would.
    //
    // What the caps are for is stated where they are checked: a gzip bomb is a few kilobytes that
    // expands to whatever the reader will hold, and on the server this reader is what stands between
    // an upload and the disk.
    public class TarGzServiceTests
    {
        private static async Task<MemoryContentStore> CreateSourceStore()
        {
            var store = new MemoryContentStore("source");
            store.Write("metadata.json", Encoding.UTF8.GetBytes("{\"name\":\"a level\"}"));
            store.Write("level.json", Encoding.UTF8.GetBytes("{\"objects\":[]}"));
            store.Write("audio/song.ogg", Encoding.UTF8.GetBytes("not really a song"));

            await Task.CompletedTask;
            return store;
        }

        private static IReadOnlyList<ArchiveEntrySource> Entries(IContentStore store, params string[] paths)
        {
            var entries = new List<ArchiveEntrySource>(paths.Length);
            foreach (var path in paths) entries.Add(ArchiveEntrySource.FromStore(path, store));
            return entries;
        }

        private static async Task<byte[]> Pack(IReadOnlyList<ArchiveEntrySource> entries)
        {
            using var buffer = new MemoryStream();
            await TarGzService.PackAsync(entries, buffer, ArchivePolicy.Default, CancellationToken.None);
            return buffer.ToArray();
        }

        private static async Task<string> ReadText(IContentStore store, string path)
        {
            using var stream = await store.OpenReadAsync(path, CancellationToken.None);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        #region Round trip

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Pack_ThenUnpack_RoundTrips()
        {
            var source = await CreateSourceStore();
            var packed = await Pack(Entries(source, "metadata.json", "level.json", "audio/song.ogg"));

            var destination = new MemoryContentStore("destination");
            using var input = new MemoryStream(packed);
            var written = await TarGzService.UnpackAsync(input, destination, ArchiveLimits.Default,
                CancellationToken.None);

            Assert.AreEqual(new[] { "metadata.json", "level.json", "audio/song.ogg" }, written);
            Assert.AreEqual("{\"name\":\"a level\"}", await ReadText(destination, "metadata.json"));
            Assert.AreEqual("{\"objects\":[]}", await ReadText(destination, "level.json"));
            Assert.AreEqual("not really a song", await ReadText(destination, "audio/song.ogg"));
        }

        // The order the caller gave is the order the archive holds, and that is the whole mitigation
        // for tar.gz having no directory to seek into: the writer puts the documents first, so a
        // reader after the level card decompresses a few kilobytes instead of the song.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Pack_PreservesTheOrderItWasGiven()
        {
            var source = await CreateSourceStore();
            var packed = await Pack(Entries(source, "audio/song.ogg", "metadata.json", "level.json"));

            var destination = new MemoryContentStore("destination");
            using var input = new MemoryStream(packed);
            var written = await TarGzService.UnpackAsync(input, destination, ArchiveLimits.Default,
                CancellationToken.None);

            Assert.AreEqual(new[] { "audio/song.ogg", "metadata.json", "level.json" }, written);
        }

        // Everything a tar header can take from the machine it ran on - a modification time, an
        // owner, a group - is pinned, so the same level packed twice hashes the same. A backend that
        // recognises a re-upload by digest depends on exactly this.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Pack_IsReproducible()
        {
            var source = await CreateSourceStore();
            var entries = Entries(source, "metadata.json", "level.json", "audio/song.ogg");

            var first = await Pack(entries);
            var second = await Pack(entries);

            Assert.AreEqual(first, second);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task ReadLeading_ReturnsOnlyWhatWasAskedFor()
        {
            var source = await CreateSourceStore();
            var packed = await Pack(Entries(source, "metadata.json", "level.json", "audio/song.ogg"));

            using var input = new MemoryStream(packed);
            var found = await TarGzService.ReadLeadingAsync(input, new[] { "metadata.json" },
                ArchiveLimits.Default, CancellationToken.None);

            Assert.AreEqual(1, found.Count);
            Assert.AreEqual("{\"name\":\"a level\"}", Encoding.UTF8.GetString(found["metadata.json"]));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Pack_RefusesANameLongerThanAHeaderHolds()
        {
            var source = new MemoryContentStore("source");
            var longName = new string('a', ArchivePolicy.MaxNameBytes) + ".json";
            source.Write(longName, Encoding.UTF8.GetBytes("{}"));

            var entries = Entries(source, longName);

            using var buffer = new MemoryStream();
            await AsyncAssert.Throws<InvalidDataException>(
                () => TarGzService.PackAsync(entries, buffer, ArchivePolicy.Default,
                    CancellationToken.None));
        }

        #endregion

        #region Hostile archives

        // Tar slip. The name is the attack and nothing about the bytes is malformed, so the only
        // thing that catches it is refusing the name - and, behind that, a store that cannot address
        // anything outside its own root however it is asked.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Unpack_RefusesATraversalEntry()
        {
            var hostile = TarFixture.Build(
                TarFixture.File("../escaped.txt", "owned"));

            var destination = new MemoryContentStore("destination");
            using var input = new MemoryStream(hostile);

            await AsyncAssert.Throws<InvalidDataException>(
                () => TarGzService.UnpackAsync(input, destination, ArchiveLimits.Default,
                    CancellationToken.None));
            Assert.AreEqual(0, destination.Count);
        }

        // The check ZIP never needed. The entry's NAME is innocent here - it is the link target that
        // points outward, and no amount of path validation looks at that.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Unpack_RefusesASymlinkEntry()
        {
            var hostile = TarFixture.Build(
                TarFixture.Symlink("innocent.json", "../../../etc/passwd"));

            var destination = new MemoryContentStore("destination");
            using var input = new MemoryStream(hostile);

            await AsyncAssert.Throws<InvalidDataException>(
                () => TarGzService.UnpackAsync(input, destination, ArchiveLimits.Default,
                    CancellationToken.None));
            Assert.AreEqual(0, destination.Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Unpack_RefusesMoreEntriesThanAllowed()
        {
            var source = await CreateSourceStore();
            var packed = await Pack(Entries(source, "metadata.json", "level.json", "audio/song.ogg"));

            var limits = new ArchiveLimits { MaxEntries = 2 };
            var destination = new MemoryContentStore("destination");
            using var input = new MemoryStream(packed);

            await AsyncAssert.Throws<InvalidDataException>(
                () => TarGzService.UnpackAsync(input, destination, limits, CancellationToken.None));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Unpack_RefusesMoreBytesThanAllowed()
        {
            var source = await CreateSourceStore();
            var packed = await Pack(Entries(source, "metadata.json", "level.json", "audio/song.ogg"));

            var limits = new ArchiveLimits { MaxTotalBytes = 20 };
            var destination = new MemoryContentStore("destination");
            using var input = new MemoryStream(packed);

            await AsyncAssert.Throws<InvalidDataException>(
                () => TarGzService.UnpackAsync(input, destination, limits, CancellationToken.None));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Unpack_RefusesAnEntryBiggerThanAllowed()
        {
            var source = await CreateSourceStore();
            var packed = await Pack(Entries(source, "audio/song.ogg"));

            var limits = new ArchiveLimits { MaxEntryBytes = 4 };
            var destination = new MemoryContentStore("destination");
            using var input = new MemoryStream(packed);

            await AsyncAssert.Throws<InvalidDataException>(
                () => TarGzService.UnpackAsync(input, destination, limits, CancellationToken.None));
        }

        #endregion

        // A minimal ustar writer. Deliberately independent of the library under test, so an archive
        // this produces is evidence about the READER rather than about the pair agreeing with each
        // other - which is what an adversarial fixture has to be.
        private static class TarFixture
        {
            private const int BlockSize = 512;

            public static byte[] File(string name, string content) =>
                Entry(name, Encoding.UTF8.GetBytes(content), '0', string.Empty);

            public static byte[] Symlink(string name, string target) =>
                Entry(name, Array.Empty<byte>(), '2', target);

            /// <summary> Gzips the given entry blocks plus the two zero blocks that end a tar. </summary>
            public static byte[] Build(params byte[][] entries)
            {
                using var raw = new MemoryStream();
                foreach (var entry in entries) raw.Write(entry, 0, entry.Length);
                raw.Write(new byte[BlockSize * 2], 0, BlockSize * 2);

                using var packed = new MemoryStream();
                using (var gzip = new GZipStream(packed, CompressionLevel.Optimal, leaveOpen: true))
                {
                    var bytes = raw.ToArray();
                    gzip.Write(bytes, 0, bytes.Length);
                }

                return packed.ToArray();
            }

            private static byte[] Entry(string name, byte[] content, char typeFlag, string linkName)
            {
                var padded = (content.Length + BlockSize - 1) / BlockSize * BlockSize;
                var block = new byte[BlockSize + padded];

                WriteAscii(block, 0, name, 100);
                WriteOctal(block, 100, 420, 8);            // mode
                WriteOctal(block, 108, 0, 8);              // uid
                WriteOctal(block, 116, 0, 8);              // gid
                WriteOctal(block, 124, content.Length, 12);
                WriteOctal(block, 136, 0, 12);             // mtime
                block[156] = (byte)typeFlag;
                WriteAscii(block, 157, linkName, 100);
                WriteAscii(block, 257, "ustar", 6);
                block[263] = (byte)'0';
                block[264] = (byte)'0';

                // The checksum is computed with its own field read as spaces - the one field of a
                // tar header that cannot include itself.
                for (var i = 148; i < 156; i++) block[i] = (byte)' ';

                var sum = 0;
                for (var i = 0; i < BlockSize; i++) sum += block[i];

                WriteOctal(block, 148, sum, 7);
                block[154] = 0;
                block[155] = (byte)' ';

                Array.Copy(content, 0, block, BlockSize, content.Length);
                return block;
            }

            private static void WriteAscii(byte[] block, int offset, string value, int length)
            {
                var bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);
                var count = Math.Min(bytes.Length, length - 1);
                Array.Copy(bytes, 0, block, offset, count);
            }

            private static void WriteOctal(byte[] block, int offset, long value, int length)
            {
                var text = Convert.ToString(value, 8).PadLeft(length - 1, '0');
                var bytes = Encoding.ASCII.GetBytes(text);
                Array.Copy(bytes, 0, block, offset, Math.Min(bytes.Length, length - 1));
            }
        }
    }
}
