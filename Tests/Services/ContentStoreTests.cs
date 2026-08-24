using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Services.Content;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    // ONE CASE SET, BOTH IMPLEMENTATIONS, and that is the whole point of the fixture rather than a
    // way to write fewer lines. MemoryContentStore is what every pipeline test above this runs on,
    // so anything it answers differently from a real directory is a test suite agreeing with itself
    // about a product that behaves otherwise. Running the same asserts through both is what makes
    // "the test double IS a store" a checked claim.
    //
    // The escape case is the load-bearing one. "Rooted by construction" is the property the archive
    // layer's tar-slip defence rests on: it validates entry names, but what actually stops a
    // traversal is that no store can address anything outside its root, whatever name it is handed.
    public class ContentStoreTests
    {
        private const string Memory = "memory";
        private const string Directory = "directory";

        private static readonly string[] StoreKinds = { Memory, Directory };

        private DirectoryInfo _tempDirectory;

        [SetUp]
        public void SetUp()
        {
            _tempDirectory = new DirectoryInfo(
                Path.Combine(Path.GetTempPath(), "BH_ContentStoreTests_" + Guid.NewGuid()));
            _tempDirectory.Create();
        }

        [TearDown]
        public void TearDown()
        {
            if (_tempDirectory.Exists)
                _tempDirectory.Delete(recursive: true);
        }

        private IContentStore CreateStore(string kind) => kind == Memory
            ? (IContentStore)new MemoryContentStore()
            : new DirectoryContentStore(_tempDirectory);

        private static async Task WriteText(IContentStore store, string path, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            using var stream = await store.OpenWriteAsync(path, CancellationToken.None);
            await stream.WriteAsync(bytes, 0, bytes.Length, CancellationToken.None);
        }

        private static async Task<string> ReadText(IContentStore store, string path)
        {
            using var stream = await store.OpenReadAsync(path, CancellationToken.None);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        #region Shared contract

        [TestCaseSource(nameof(StoreKinds))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Write_ThenRead_RoundTrips(string kind)
        {
            var store = CreateStore(kind);
            await WriteText(store, "metadata.json", "{}");

            Assert.IsTrue(await store.ExistsAsync("metadata.json", CancellationToken.None));
            Assert.AreEqual("{}", await ReadText(store, "metadata.json"));
            Assert.AreEqual(2, await store.GetLengthAsync("metadata.json", CancellationToken.None));
        }

        [TestCaseSource(nameof(StoreKinds))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Write_NestedPath_RoundTrips(string kind)
        {
            var store = CreateStore(kind);
            await WriteText(store, "audio/song.ogg", "not really a song");

            Assert.IsTrue(await store.ExistsAsync("audio/song.ogg", CancellationToken.None));
            Assert.AreEqual("not really a song", await ReadText(store, "audio/song.ogg"));
        }

        [TestCaseSource(nameof(StoreKinds))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Write_Twice_Replaces(string kind)
        {
            var store = CreateStore(kind);
            await WriteText(store, "level.json", "first version, longer");
            await WriteText(store, "level.json", "second");

            Assert.AreEqual("second", await ReadText(store, "level.json"));
            Assert.AreEqual(6, await store.GetLengthAsync("level.json", CancellationToken.None));
        }

        [TestCaseSource(nameof(StoreKinds))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Exists_IsFalse_ForNothingWritten(string kind)
        {
            var store = CreateStore(kind);
            Assert.IsFalse(await store.ExistsAsync("level.json", CancellationToken.None));
        }

        [TestCaseSource(nameof(StoreKinds))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task OpenRead_Missing_Throws(string kind)
        {
            var store = CreateStore(kind);
            await AsyncAssert.Throws<FileNotFoundException>(
                () => store.OpenReadAsync("level.json", CancellationToken.None).AsTask());
        }

        [TestCaseSource(nameof(StoreKinds))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task GetLength_Missing_Throws(string kind)
        {
            var store = CreateStore(kind);
            await AsyncAssert.Throws<FileNotFoundException>(
                () => store.GetLengthAsync("level.json", CancellationToken.None).AsTask());
        }

        [TestCaseSource(nameof(StoreKinds))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Delete_Removes_AndIsNoOpWhenAbsent(string kind)
        {
            var store = CreateStore(kind);
            await store.DeleteAsync("level.json", CancellationToken.None);

            await WriteText(store, "level.json", "{}");
            await store.DeleteAsync("level.json", CancellationToken.None);

            Assert.IsFalse(await store.ExistsAsync("level.json", CancellationToken.None));
        }

        [TestCaseSource(nameof(StoreKinds))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task List_IsOrdinalSorted(string kind)
        {
            var store = CreateStore(kind);
            await WriteText(store, "metadata.json", "{}");
            await WriteText(store, "audio/song.ogg", "s");
            await WriteText(store, "level.json", "{}");

            var listing = await store.ListAsync(string.Empty, CancellationToken.None);

            Assert.AreEqual(new[] { "audio/song.ogg", "level.json", "metadata.json" }, listing);
        }

        // A prefix names a FOLDER, not a string. "audio" must not drag in "audio-backup.ogg", which
        // is the failure that turns "pack the level's audio" into "pack somebody's backup too".
        [TestCaseSource(nameof(StoreKinds))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task List_MatchesPrefix_OnSegmentBoundaries(string kind)
        {
            var store = CreateStore(kind);
            await WriteText(store, "audio/song.ogg", "s");
            await WriteText(store, "audio-backup.ogg", "s");

            var listing = await store.ListAsync("audio", CancellationToken.None);

            Assert.AreEqual(new[] { "audio/song.ogg" }, listing);
        }

        [TestCaseSource(nameof(StoreKinds))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Write_OutsideTheRoot_IsRefused(string kind)
        {
            var store = CreateStore(kind);

            await AsyncAssert.Throws<ArgumentException>(
                () => store.OpenWriteAsync("../escaped.txt", CancellationToken.None).AsTask());
            await AsyncAssert.Throws<ArgumentException>(
                () => store.OpenWriteAsync("audio/../../escaped.txt", CancellationToken.None).AsTask());
            await AsyncAssert.Throws<ArgumentException>(
                () => store.OpenWriteAsync("/rooted.txt", CancellationToken.None).AsTask());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task DirectoryStore_WritesNothingOutsideItsRoot()
        {
            var store = new DirectoryContentStore(_tempDirectory);
            var sibling = Path.Combine(_tempDirectory.Parent.FullName, "escaped.txt");

            await AsyncAssert.Throws<ArgumentException>(
                () => store.OpenWriteAsync("../escaped.txt", CancellationToken.None).AsTask());

            Assert.IsFalse(File.Exists(sibling));
        }

        #endregion

        #region MemoryContentStore

        // The reason the interface is written in ValueTask at all: on this implementation every
        // operation is already finished when it is handed back, so awaiting one allocates nothing.
        // A change that makes any of these genuinely asynchronous is a change to that bargain, and
        // it should have to edit this test to happen.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task MemoryStore_CompletesSynchronously()
        {
            var store = new MemoryContentStore();
            store.Write("level.json", Encoding.UTF8.GetBytes("{}"));

            Assert.IsTrue(store.ExistsAsync("level.json", CancellationToken.None).IsCompleted);
            Assert.IsTrue(store.ListAsync(string.Empty, CancellationToken.None).IsCompleted);
            Assert.IsTrue(store.GetLengthAsync("level.json", CancellationToken.None).IsCompleted);
            Assert.IsTrue(store.DeleteAsync("level.json", CancellationToken.None).IsCompleted);

            var write = store.OpenWriteAsync("other.json", CancellationToken.None);
            Assert.IsTrue(write.IsCompleted);
            (await write).Dispose();
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void MemoryStore_RefusesAWriteOverItsCap()
        {
            var store = new MemoryContentStore("capped", maxTotalBytes: 16);
            var oversized = new byte[64];

            Assert.Throws<IOException>(() => store.Write("big.bin", oversized));
            Assert.AreEqual(0, store.Count);
            Assert.AreEqual(0, store.TotalBytes);
        }

        // The cap has to bite while the bytes are being written, not when the blob is committed: by
        // commit time a bomb has already been allocated, which is the whole thing being defended
        // against.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task MemoryStore_RefusesAStreamedWriteOverItsCap()
        {
            var store = new MemoryContentStore("capped", maxTotalBytes: 16);
            using var stream = await store.OpenWriteAsync("big.bin", CancellationToken.None);

            Assert.Throws<IOException>(() => stream.Write(new byte[64], 0, 64));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task MemoryStore_OverwritingDoesNotAccumulateAgainstTheCap()
        {
            var store = new MemoryContentStore("capped", maxTotalBytes: 32);

            for (var i = 0; i < 8; i++)
                await WriteText(store, "level.json", "0123456789012345");

            Assert.AreEqual(16, store.TotalBytes);
            Assert.AreEqual(1, store.Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task MemoryStore_ReadStreamIsNotWritable()
        {
            var store = new MemoryContentStore();
            store.Write("level.json", Encoding.UTF8.GetBytes("{}"));

            using var stream = await store.OpenReadAsync("level.json", CancellationToken.None);
            Assert.IsFalse(stream.CanWrite);
        }

        #endregion

        #region ContentPath

        [TestCase("level.json")]
        [TestCase("audio/song.ogg")]
        [TestCase("a/b/c/d.bin")]
        [TestCase("name with spaces.png")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ContentPath_Accepts(string path)
        {
            Assert.IsTrue(ContentPath.IsValid(path), path);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("../escaped.txt")]
        [TestCase("audio/../../escaped.txt")]
        [TestCase("..")]
        [TestCase("/rooted.txt")]
        [TestCase("C:/absolute.txt")]
        [TestCase("C:relative.txt")]
        [TestCase("audio\\song.ogg")]
        [TestCase("./level.json")]
        [TestCase("audio//song.ogg")]
        [TestCase("audio/")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ContentPath_Refuses(string path)
        {
            Assert.IsFalse(ContentPath.IsValid(path), path);
        }

        // Refused with a REASON, because every one of these reaches a person: an author whose
        // export named a file something the format cannot carry, or a moderator looking at why an
        // upload was rejected.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ContentPath_NamesWhatIsWrong()
        {
            Assert.IsFalse(ContentPath.TryValidate("../escaped.txt", out var error));
            Assert.IsNotEmpty(error);

            var exception = Assert.Throws<ArgumentException>(
                () => ContentPath.Require("../escaped.txt", "path"));
            StringAssert.Contains("..", exception.Message);
        }

        [TestCase("audio/song.ogg", "audio", ExpectedResult = true)]
        [TestCase("audio/song.ogg", "audio/", ExpectedResult = true)]
        [TestCase("audio/song.ogg", "", ExpectedResult = true)]
        [TestCase("audio-backup.ogg", "audio", ExpectedResult = false)]
        [TestCase("audio", "audio", ExpectedResult = false)]
        [TestCase("level.json", "audio", ExpectedResult = false)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public bool ContentPath_HasPrefix(string path, string prefix) => ContentPath.HasPrefix(path, prefix);

        #endregion
    }
}
