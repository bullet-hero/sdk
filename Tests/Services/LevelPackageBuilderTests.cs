using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Interop;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;
using BH.SDK.Services.Package;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    // FOUR ROUTES OUT OF ONE FIELD, and every one of them is a different promise to the author:
    // a file in the level folder is packed, a file outside it is COPIED IN and the level's own key
    // rewritten to match, a URL is left exactly as it is, and a reference to something that is not
    // there any more is reported rather than packed as a hole.
    //
    // The collecting case is the one that matters most in practice and looks like an edge case:
    // the editor creates a level around a song picked from disk, with UriType = AbsolutePath, so
    // the single most common way to START a level produces one whose music is outside its folder.
    // An export that only packed the folder would ship those levels silently mute.
    //
    // And the live model must come out untouched. An export is not an edit, and the rewriting above
    // is exactly the kind of thing that is invisible until somebody saves afterwards.
    public class LevelPackageBuilderTests
    {
        private DirectoryInfo _tempDirectory;

        [SetUp]
        public void SetUp()
        {
            _tempDirectory = new DirectoryInfo(
                Path.Combine(Path.GetTempPath(), "BH_LevelPackageBuilderTests_" + Guid.NewGuid()));
            _tempDirectory.Create();
        }

        [TearDown]
        public void TearDown()
        {
            if (_tempDirectory.Exists)
                _tempDirectory.Delete(recursive: true);
        }

        private string WriteExternalFile(string name, string content = "external bytes")
        {
            var path = Path.Combine(_tempDirectory.FullName, name);
            File.WriteAllText(path, content);
            return path;
        }

        private static MemoryContentStore CreateLevelStore()
        {
            var store = new MemoryContentStore("level");
            store.Write("level.json", Encoding.UTF8.GetBytes("{}"));
            store.Write("metadata.json", Encoding.UTF8.GetBytes("{}"));
            store.Write(FileNames.LogoFileNamePng, Encoding.UTF8.GetBytes("a cover"));
            store.Write("texture.png", Encoding.UTF8.GetBytes("a texture"));
            return store;
        }

        private static void SetOnlySource(Resource resource, ResourceUriType type, string uri)
        {
            resource.Sources.Clear();
            resource.Sources.Add(new ResourceKey(type, uri));
        }

        private static Resource Texture(Level level) => level.Resources.Textures.Values.First();
        private static Resource Font(Level level) => level.Resources.Fonts.Values.First();
        private static Resource Audio(Level level) => level.Resources.Audios.Values.First();

        private static bool HasCode(InteropReport report, string code) =>
            report.Issues.Any(issue => issue.Code == code);

        private static PackageFile? Find(LevelPackagePlan plan, string packagePath) =>
            plan.Files.Cast<PackageFile?>().FirstOrDefault(file => file.Value.PackagePath == packagePath);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Build_PacksWhatTheLevelReferencesFromItsOwnFolder()
        {
            var level = MockData.CreateTestLevel();
            var meta = MockData.CreateTestLevelMeta();
            var store = CreateLevelStore();

            meta.LevelLogo = new ResourceKey(ResourceUriType.LevelPath, FileNames.LogoFileNamePng);
            SetOnlySource(Texture(level), ResourceUriType.LevelPath, "texture.png");

            var plan = await LevelPackageBuilder.BuildAsync(level, meta, store, CancellationToken.None);

            Assert.IsNotNull(Find(plan, FileNames.LogoFileNamePng));
            Assert.IsNotNull(Find(plan, "texture.png"));
            Assert.IsFalse(Find(plan, "texture.png").Value.IsExternal);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Build_CollectsAFileFromOutsideTheLevelFolder()
        {
            var level = MockData.CreateTestLevel();
            var meta = MockData.CreateTestLevelMeta();
            var store = CreateLevelStore();

            var songPath = WriteExternalFile("spider-dance.ogg");
            SetOnlySource(Audio(level), ResourceUriType.AbsolutePath, songPath);

            var plan = await LevelPackageBuilder.BuildAsync(level, meta, store, CancellationToken.None);

            var collected = Find(plan, "spider-dance.ogg");
            Assert.IsNotNull(collected, "the song outside the folder should have been collected");
            Assert.IsTrue(collected.Value.IsExternal);
            Assert.AreEqual(songPath, collected.Value.SourcePath);

            // Rewritten in the EXPORTED copy, so the package is self-contained...
            var exported = Audio(plan.Level).Sources[0];
            Assert.AreEqual(ResourceUriType.LevelPath, exported.UriType);
            Assert.AreEqual("spider-dance.ogg", exported.Uri);

            // ...and untouched in the level the author is working on.
            var live = Audio(level).Sources[0];
            Assert.AreEqual(ResourceUriType.AbsolutePath, live.UriType);
            Assert.AreEqual(songPath, live.Uri);
        }

        // Two different songs that happen to share a file name are two files, and the second one
        // must not overwrite the first inside the package.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Build_GivesTwoCollectedFilesOfOneNameTwoNames()
        {
            var level = MockData.CreateTestLevel();
            var meta = MockData.CreateTestLevelMeta();
            var store = CreateLevelStore();

            var first = WriteExternalFile("song.ogg", "first");
            var secondDirectory = _tempDirectory.CreateSubdirectory("other");
            var second = Path.Combine(secondDirectory.FullName, "song.ogg");
            File.WriteAllText(second, "second");

            SetOnlySource(Audio(level), ResourceUriType.AbsolutePath, first);
            SetOnlySource(Texture(level), ResourceUriType.AbsolutePath, second);

            var plan = await LevelPackageBuilder.BuildAsync(level, meta, store, CancellationToken.None);

            Assert.IsNotNull(Find(plan, "song.ogg"));
            Assert.IsNotNull(Find(plan, "song_1.ogg"));
            Assert.AreNotEqual(Audio(plan.Level).Sources[0].Uri, Texture(plan.Level).Sources[0].Uri);
        }

        // One file referenced twice is one file. A resource's Sources are FALLBACKS for the same
        // asset, so a level listing the same path twice must not pay for it twice.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Build_PacksOneSourceOnce()
        {
            var level = MockData.CreateTestLevel();
            var meta = MockData.CreateTestLevelMeta();
            var store = CreateLevelStore();

            SetOnlySource(Texture(level), ResourceUriType.LevelPath, "texture.png");
            SetOnlySource(Font(level), ResourceUriType.LevelPath, "texture.png");

            var plan = await LevelPackageBuilder.BuildAsync(level, meta, store, CancellationToken.None);

            Assert.AreEqual(1, plan.Files.Count(file => file.PackagePath == "texture.png"));
            Assert.AreEqual("texture.png", Font(plan.Level).Sources[0].Uri);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Build_ReportsAReferenceToSomethingThatIsNotThere()
        {
            var level = MockData.CreateTestLevel();
            var meta = MockData.CreateTestLevelMeta();
            var store = CreateLevelStore();

            SetOnlySource(Texture(level), ResourceUriType.LevelPath, "gone.png");
            SetOnlySource(Audio(level), ResourceUriType.AbsolutePath,
                Path.Combine(_tempDirectory.FullName, "never-existed.ogg"));

            var plan = await LevelPackageBuilder.BuildAsync(level, meta, store, CancellationToken.None);

            Assert.IsTrue(HasCode(plan.Report, "package.resource_missing"));
            Assert.IsNull(Find(plan, "gone.png"));
            Assert.IsFalse(plan.Report.IsClean);
        }

        // A URL is not a loss and not a file: it will resolve on the other machine exactly as well
        // as it does on this one. Saying so is the whole of what an export can do about it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Build_LeavesAUrlAlone()
        {
            var level = MockData.CreateTestLevel();
            var meta = MockData.CreateTestLevelMeta();
            var store = CreateLevelStore();

            const string url = "https://example.com/song.ogg";
            SetOnlySource(Audio(level), ResourceUriType.DirectUrl, url);

            var plan = await LevelPackageBuilder.BuildAsync(level, meta, store, CancellationToken.None);

            Assert.IsTrue(HasCode(plan.Report, "package.resource_unreachable"));
            Assert.AreEqual(ResourceUriType.DirectUrl, Audio(plan.Level).Sources[0].UriType);
            Assert.AreEqual(url, Audio(plan.Level).Sources[0].Uri);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Build_CountsWhatNothingReferences()
        {
            var level = MockData.CreateTestLevel();
            var meta = MockData.CreateTestLevelMeta();
            var store = CreateLevelStore();

            store.Write("leftover.psd", Encoding.UTF8.GetBytes("somebody's working file"));
            meta.LevelLogo = new ResourceKey(ResourceUriType.LevelPath, FileNames.LogoFileNamePng);
            SetOnlySource(Texture(level), ResourceUriType.LevelPath, "texture.png");

            var plan = await LevelPackageBuilder.BuildAsync(level, meta, store, CancellationToken.None);

            // level.json and metadata.json are regenerated rather than copied, so they are not
            // "unreferenced" - only the stray file is.
            Assert.AreEqual(1, plan.DroppedFileCount);
            Assert.IsTrue(HasCode(plan.Report, "package.unreferenced_dropped"));
            Assert.IsNull(Find(plan, "leftover.psd"));
        }

        // A tar header holds 100 bytes of name, so a longer one cannot travel as it is. Renaming is
        // done HERE, where a folder export and an archive export both see it, because two exports
        // that disagree about what a file is called are two different levels.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Build_RenamesANameThatCannotTravel()
        {
            var level = MockData.CreateTestLevel();
            var meta = MockData.CreateTestLevelMeta();
            var store = CreateLevelStore();

            var longName = new string('t', ArchivePolicy.MaxNameBytes + 40) + ".png";
            store.Write(longName, Encoding.UTF8.GetBytes("a texture"));
            SetOnlySource(Texture(level), ResourceUriType.LevelPath, longName);

            var plan = await LevelPackageBuilder.BuildAsync(level, meta, store, CancellationToken.None);

            var packed = Texture(plan.Level).Sources[0].Uri;
            Assert.AreNotEqual(longName, packed);
            Assert.IsTrue(ArchivePolicy.FitsName(packed));
            Assert.IsTrue(packed.EndsWith(".png", StringComparison.Ordinal), "the extension has to survive");
            Assert.IsTrue(HasCode(plan.Report, "package.resource_renamed"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Build_RefusesAPathThatEscapesTheLevelFolder()
        {
            var level = MockData.CreateTestLevel();
            var meta = MockData.CreateTestLevelMeta();
            var store = CreateLevelStore();

            SetOnlySource(Texture(level), ResourceUriType.LevelPath, "../../elsewhere.png");

            var plan = await LevelPackageBuilder.BuildAsync(level, meta, store, CancellationToken.None);

            Assert.IsTrue(HasCode(plan.Report, "package.resource_bad_path"));
            Assert.AreEqual(0, plan.Files.Count(file => file.PackagePath.Contains("elsewhere")));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Build_OrdersFilesTheSameWayEveryTime()
        {
            var level = MockData.CreateTestLevel();
            var meta = MockData.CreateTestLevelMeta();

            meta.LevelLogo = new ResourceKey(ResourceUriType.LevelPath, FileNames.LogoFileNamePng);
            SetOnlySource(Texture(level), ResourceUriType.LevelPath, "texture.png");

            var first = await LevelPackageBuilder.BuildAsync(level, meta, CreateLevelStore(), CancellationToken.None);
            var second = await LevelPackageBuilder.BuildAsync(level, meta, CreateLevelStore(), CancellationToken.None);

            Assert.AreEqual(Names(first), Names(second));
        }

        private static List<string> Names(LevelPackagePlan plan) =>
            plan.Files.Select(file => file.PackagePath).ToList();
    }
}
