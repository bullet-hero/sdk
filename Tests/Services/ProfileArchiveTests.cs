using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Models;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Profile;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;
using BH.SDK.Services.Profile;
using BH.SDK.Versions;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    /// <summary> A profile archive: the manifest first, the category whitelist on both sides, the
    /// raised name cap that only zip honours, and every refusal a peek can answer. </summary>
    public class ProfileArchiveTests
    {
        private static readonly SerializationService Serialization = new SerializationService(new SerializationSettings());

        private static readonly DateTime When = new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);

        private static ProfileManifest Manifest(ProfileCategory categories)
        {
            var manifest = new ProfileManifest
            {
                CreatedUtc = When,
                GameVersion = "1.1.0",
                ModelGeneration = ModelGenerations.Current,
                Platform = "WindowsPlayer",
                Categories = categories,
            };
            manifest.Levels.Add(new ProfileLevelEntry(LevelId.NewId(), "my level", When));
            return manifest;
        }

        private static Dictionary<ProfileCategory, IContentStore> Folders()
        {
            var levels = new MemoryContentStore("levels");
            levels.Write("my level/level.json", Encoding.UTF8.GetBytes("{}"));
            levels.Write("my level/metadata.json", Encoding.UTF8.GetBytes("{}"));

            var stats = new MemoryContentStore("stats");
            stats.Write("statistics.json", Encoding.UTF8.GetBytes("{}"));

            var library = new MemoryContentStore("resources");
            library.Write("themes/a.json", Encoding.UTF8.GetBytes("{}"));

            var reports = new MemoryContentStore("reports");
            reports.Write("crash.txt", Encoding.UTF8.GetBytes("boom"));

            return new Dictionary<ProfileCategory, IContentStore>
            {
                [ProfileCategory.Levels] = levels,
                [ProfileCategory.Statistics] = stats,
                [ProfileCategory.Library] = library,
                [ProfileCategory.Reports] = reports,
            };
        }

        private static async Task<byte[]> Pack(ProfileManifest manifest,
            IReadOnlyDictionary<ProfileCategory, IContentStore> folders = null, byte[] settings = null)
        {
            using var buffer = new MemoryStream();
            await ProfileArchiveWriter.PackAsync(manifest, folders ?? Folders(),
                settings ?? Encoding.UTF8.GetBytes("{\"settings\":1}"), Serialization, buffer, null,
                CancellationToken.None);
            return buffer.ToArray();
        }

        private static async Task<ProfilePeek> Peek(byte[] archive)
        {
            using var input = new MemoryStream(archive);
            return await ProfileArchiveReader.PeekAsync(input, Serialization, CancellationToken.None);
        }

        private static IReadOnlyList<string> Names(byte[] archive)
        {
            using var input = new MemoryStream(archive);
            return ZipService.ListEntries(input).Select(e => e.Name).ToList();
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Pack_PutsTheManifestFirst_AndMirrorsTheProfileRoot()
        {
            var names = Names(await Pack(Manifest(ProfileCategory.All)));

            Assert.AreEqual(FileNames.ProfileManifestFileName, names[0]);
            CollectionAssert.Contains(names, FileNames.SettingsFileName);
            CollectionAssert.Contains(names, "levels/my level/level.json");
            CollectionAssert.Contains(names, "stats/statistics.json");
            CollectionAssert.Contains(names, "resources/themes/a.json");
            CollectionAssert.Contains(names, "reports/crash.txt");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Pack_LeavesOutEveryCategoryTheManifestDoesNotName()
        {
            var names = Names(await Pack(Manifest(ProfileCategory.Levels | ProfileCategory.Settings)));

            CollectionAssert.Contains(names, "levels/my level/level.json");
            CollectionAssert.Contains(names, FileNames.SettingsFileName);
            Assert.IsFalse(names.Any(n => n.StartsWith("stats/", StringComparison.Ordinal)));
            Assert.IsFalse(names.Any(n => n.StartsWith("reports/", StringComparison.Ordinal)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Pack_LeavesOutARecordingsScratchFolder()
        {
            var recordings = new MemoryContentStore("recordings");
            recordings.Write("a/level_20261002-210509.mp4", Encoding.UTF8.GetBytes("video"));
            recordings.Write("a/.tmp/job/video.mp4", Encoding.UTF8.GetBytes("partial"));

            var names = Names(await Pack(Manifest(ProfileCategory.Recordings),
                new Dictionary<ProfileCategory, IContentStore> { [ProfileCategory.Recordings] = recordings }));

            CollectionAssert.Contains(names, "recordings/a/level_20261002-210509.mp4");
            CollectionAssert.DoesNotContain(names, "recordings/a/.tmp/job/video.mp4");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Pack_IsDeterministic()
        {
            var manifest = Manifest(ProfileCategory.All);

            CollectionAssert.AreEqual(await Pack(manifest), await Pack(manifest));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Pack_WritesANameLongerThanTheSharedCap()
        {
            var longName = new string('a', 200) + ".ogg";
            var levels = new MemoryContentStore("levels");
            levels.Write("my level/media/" + longName, new byte[] { 1, 2, 3 });

            var archive = await Pack(Manifest(ProfileCategory.Levels),
                new Dictionary<ProfileCategory, IContentStore> { [ProfileCategory.Levels] = levels });

            CollectionAssert.Contains(Names(archive), "levels/my level/media/" + longName);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task TheDefaultPolicy_StillRefusesALongName_AndTarRefusesARaisedCap()
        {
            var entries = new[] { ArchiveEntrySource.FromBytes(new string('a', 150), new byte[] { 1 }) };

            await AsyncAssert.Throws<InvalidDataException>(async () =>
                await ZipService.PackAsync(entries, new MemoryStream(), ArchivePolicy.Default, CancellationToken.None));
            await AsyncAssert.Throws<ArgumentException>(async () =>
                await TarGzService.PackAsync(entries, new MemoryStream(), ProfileArchive.Policy, CancellationToken.None));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Peek_ReadsTheManifestAndCountsPerCategory()
        {
            var peek = await Peek(await Pack(Manifest(ProfileCategory.All)));

            Assert.AreEqual(ProfileReadResult.Ok, peek.Result);
            Assert.AreEqual(ProfileCategory.All, peek.Categories);
            Assert.AreEqual(1, peek.Manifest.Levels.Count);
            Assert.AreEqual(When, peek.Manifest.Levels[0].ModifiedUtc);
            Assert.AreEqual(2, peek.FilesByCategory[ProfileCategory.Levels]);
            Assert.AreEqual(1, peek.FilesByCategory[ProfileCategory.Settings]);
            Assert.IsFalse(peek.FilesByCategory.ContainsKey(ProfileCategory.Backups));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Unpack_WritesOnlyTheSelectedCategories_AndNeverTheManifest()
        {
            var archive = await Pack(Manifest(ProfileCategory.All));
            var peek = await Peek(archive);
            var into = new MemoryContentStore("staging");

            using (var input = new MemoryStream(archive))
                await ProfileArchiveReader.UnpackAsync(input, into, ProfileCategory.Levels | ProfileCategory.Settings,
                    peek, CancellationToken.None);

            var written = await into.ListAsync(string.Empty, CancellationToken.None);
            CollectionAssert.AreEquivalent(
                new[] { "levels/my level/level.json", "levels/my level/metadata.json", FileNames.SettingsFileName },
                written);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Unpack_IgnoresEntriesOutsideTheWhitelist()
        {
            var manifestBytes = Serialization.SerializeEnvelope(Manifest(ProfileCategory.Levels), SerializationType.Json);
            var archive = RawZip.Build(
                new RawZip.Entry(FileNames.ProfileManifestFileName, manifestBytes),
                new RawZip.Entry("levels/x/level.json", "{}"),
                new RawZip.Entry("profile-backups/profile-backup.zip", "old"),
                new RawZip.Entry("elsewhere.txt", "?"));

            var peek = await Peek(archive);
            var into = new MemoryContentStore("staging");
            using (var input = new MemoryStream(archive))
                await ProfileArchiveReader.UnpackAsync(input, into, ProfileCategory.All, peek, CancellationToken.None);

            CollectionAssert.AreEquivalent(new[] { "levels/x/level.json" },
                await into.ListAsync(string.Empty, CancellationToken.None));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Unpack_RefusesAnEntryInflatingPastWhatTheDirectoryDeclared()
        {
            var manifestBytes = Serialization.SerializeEnvelope(Manifest(ProfileCategory.Levels), SerializationType.Json);
            var archive = RawZip.Build(
                new RawZip.Entry(FileNames.ProfileManifestFileName, manifestBytes),
                new RawZip.Entry("levels/x/bomb.bin", new string('a', 64 * 1024)));
            LieAbout("levels/x/bomb.bin", archive, declaredSize: 16);

            var peek = await Peek(archive);
            Assert.AreEqual(ProfileReadResult.Ok, peek.Result);

            using var input = new MemoryStream(archive);
            await AsyncAssert.Throws<InvalidDataException>(async () =>
                await ProfileArchiveReader.UnpackAsync(input, new MemoryContentStore("staging"),
                    ProfileCategory.All, peek, CancellationToken.None));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Peek_AZipWithNoManifest_IsNotAProfile()
        {
            var peek = await Peek(RawZip.Build(new RawZip.Entry("levels/x/level.json", "{}")));

            Assert.AreEqual(ProfileReadResult.NotAProfile, peek.Result);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Peek_SomethingThatIsNotAZip_IsNotAZip()
        {
            var peek = await Peek(Encoding.UTF8.GetBytes("this is not an archive at all"));

            Assert.AreEqual(ProfileReadResult.NotAZip, peek.Result);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Peek_AnEnvelopeOfANewerGeneration_IsRefused()
        {
            var text = Encoding.UTF8.GetString(
                Serialization.SerializeEnvelope(Manifest(ProfileCategory.Levels), SerializationType.Json));
            var spliced = new Regex("\"g\":" + ModelGenerations.Current).Replace(text,
                "\"g\":" + MockData.FabricatedGeneration, 1);
            Assert.AreNotEqual(text, spliced, "the envelope's generation was not found to splice");

            var peek = await Peek(RawZip.Build(new RawZip.Entry(FileNames.ProfileManifestFileName, spliced)));

            Assert.AreEqual(ProfileReadResult.NewerGeneration, peek.Result);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Peek_AManifestFromANewerBuild_IsRefused_ButStillShown()
        {
            var manifest = Manifest(ProfileCategory.Levels);
            manifest.ModelGeneration = ModelGenerations.Current + 1;
            manifest.GameVersion = "9.9.9";

            var peek = await Peek(await Pack(manifest));

            Assert.AreEqual(ProfileReadResult.NewerGeneration, peek.Result);
            Assert.AreEqual("9.9.9", peek.Manifest.GameVersion);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Layout_MapsEveryPathToItsCategory()
        {
            Assert.AreEqual(ProfileCategory.Settings, ProfileLayout.CategoryOf("settings.json"));
            Assert.AreEqual(ProfileCategory.Levels, ProfileLayout.CategoryOf("levels/a/level.json"));
            Assert.AreEqual(ProfileCategory.Statistics, ProfileLayout.CategoryOf("stats/statistics.json"));
            Assert.AreEqual(ProfileCategory.Library, ProfileLayout.CategoryOf("resources/collections/x/collection.json"));
            Assert.AreEqual(ProfileCategory.Backups, ProfileLayout.CategoryOf("backups/a/backup_level_1.json"));
            Assert.AreEqual(ProfileCategory.Reports, ProfileLayout.CategoryOf("reports/r.txt"));
            Assert.AreEqual(ProfileCategory.Recordings, ProfileLayout.CategoryOf("recordings/a/level_20261002-210509.mp4"));
            Assert.AreEqual(ProfileCategory.None, ProfileLayout.CategoryOf("recordings/a/.tmp/job/video.mp4"));
            Assert.AreEqual(ProfileCategory.Library, ProfileLayout.CategoryOf("resources/.tmp/a.json"));
            Assert.AreEqual(ProfileCategory.None, ProfileLayout.CategoryOf("levels"));
            Assert.AreEqual(ProfileCategory.None, ProfileLayout.CategoryOf("levelsx/a.json"));
            Assert.AreEqual(ProfileCategory.None, ProfileLayout.CategoryOf("profile-backups/profile-backup.zip"));
            Assert.AreEqual(ProfileCategory.None, ProfileLayout.CategoryOf(FileNames.ProfileManifestFileName));
        }

        // Rewrites both size fields of the one named entry - what a bomb does to its own directory.
        private static void LieAbout(string name, byte[] archive, int declaredSize)
        {
            var nameBytes = Encoding.UTF8.GetBytes(name);

            for (var i = 0; i + 4 <= archive.Length; i++)
            {
                var signature = BitConverter.ToInt32(archive, i);
                int sizeOffset, nameOffset;
                if (signature == 0x04034b50) { sizeOffset = i + 22; nameOffset = i + 30; }
                else if (signature == 0x02014b50) { sizeOffset = i + 24; nameOffset = i + 46; }
                else continue;

                if (nameOffset + nameBytes.Length > archive.Length) continue;
                if (!archive.Skip(nameOffset).Take(nameBytes.Length).SequenceEqual(nameBytes)) continue;

                Array.Copy(BitConverter.GetBytes(declaredSize), 0, archive, sizeOffset, 4);
            }
        }
    }
}
