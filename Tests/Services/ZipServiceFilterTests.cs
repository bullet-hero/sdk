using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    /// <summary> ZipService's filtered unpack and its directory listing: a skipped entry is never
    /// inflated, yet every entry is still judged. </summary>
    public class ZipServiceFilterTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Unpack_WithAFilter_WritesOnlyWhatItAccepts()
        {
            var archive = RawZip.Build(
                new RawZip.Entry("keep/a.txt", "a"),
                new RawZip.Entry("skip/b.txt", "b"),
                new RawZip.Entry("keep/c.txt", "c"));
            var into = new MemoryContentStore("into");

            using var input = new MemoryStream(archive);
            var written = await ZipService.UnpackAsync(input, into, path => path.StartsWith("keep/"),
                ArchiveLimits.Default, null, CancellationToken.None);

            CollectionAssert.AreEqual(new[] { "keep/a.txt", "keep/c.txt" }, written);
            Assert.IsFalse(await into.ExistsAsync("skip/b.txt", CancellationToken.None));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Unpack_WithAFilter_StillRefusesADuplicateItSkips()
        {
            var archive = RawZip.Build(
                new RawZip.Entry("skip/b.txt", "one"),
                new RawZip.Entry("skip/b.txt", "two"));

            using var input = new MemoryStream(archive);
            await AsyncAssert.Throws<InvalidDataException>(async () =>
                await ZipService.UnpackAsync(input, new MemoryContentStore("into"), path => false,
                    ArchiveLimits.Default, null, CancellationToken.None));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Unpack_WithAFilter_CountsOnlyKeptEntriesAgainstTheTotal()
        {
            var archive = RawZip.Build(
                new RawZip.Entry("keep/a.txt", "a"),
                new RawZip.Entry("skip/large.bin", new string('x', 64 * 1024)));

            using var input = new MemoryStream(archive);
            var written = await ZipService.UnpackAsync(input, new MemoryContentStore("into"),
                path => path.StartsWith("keep/"), new ArchiveLimits { MaxTotalBytes = 1024 }, null,
                CancellationToken.None);

            CollectionAssert.AreEqual(new[] { "keep/a.txt" }, written);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ListEntries_ReportsDeclaredSizes()
        {
            var archive = RawZip.Build(
                new RawZip.Entry("a.txt", "abc"),
                new RawZip.Entry("dir/b.txt", "hello"));

            using var input = new MemoryStream(archive);
            var entries = ZipService.ListEntries(input);

            CollectionAssert.AreEqual(new[] { "a.txt", "dir/b.txt" }, entries.Select(e => e.Name));
            CollectionAssert.AreEqual(new[] { 3L, 5L }, entries.Select(e => e.DeclaredBytes));
            Assert.IsFalse(entries.Any(e => e.Encrypted));
        }
    }
}
