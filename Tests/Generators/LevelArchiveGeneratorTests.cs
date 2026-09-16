using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Generators.External;
using BH.SDK.Generators.Import;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Meta;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Content;
using BH.SDK.Services.LevelArchive;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    // THE IMPORT'S OWN HALF, fed by the writer rather than by hand-built bytes: what is under test
    // is that an archive this project WROTE comes back as the level that went in, so producing the
    // input any other way would only prove the generator agrees with a fixture.
    //
    // Two of the three parameters are about not damaging what the author already has. An archive is
    // usually a copy of a level they may well hold already, so importing under the same id would
    // point two entries at one folder - which is why NewLevelId defaults ON, and why the case that
    // needs testing is the one where it is turned off.
    //
    // Everything the generator cannot read still has to produce a level the author can see is empty,
    // never a plausible-looking one - the same rule the Afterbeat import already states.

    /// <summary> The import half, fed by this project's own writer - so what is proven is that an archive we WROTE
    /// comes back as the level that went in, rather than that the reader agrees with a fixture. </summary>
    public class LevelArchiveGeneratorTests
    {
        private static SerializationService Serialization => new SerializationService();

        private static (Level Level, LevelMeta Meta) CreateLevel()
        {
            var level = MockData.CreateTestLevel();
            var meta = MockData.CreateTestLevelMeta();

            meta.LevelLogo = new ResourceKey(ResourceUriType.LevelPath, FileNames.LogoFileNamePng);
            return (level, meta);
        }

        private static MemoryContentStore CreateLevelStore()
        {
            var store = new MemoryContentStore("level");
            store.Write(FileNames.LogoFileNamePng, Encoding.UTF8.GetBytes("a cover"));
            return store;
        }

        // Writer -> reader -> the generator's own input, which is exactly the chain the editor runs.
        private static async Task<LevelArchiveGenerator.Parameters> ImportedAsync(Level level, LevelMeta meta)
        {
            var source = CreateLevelStore();
            var plan = await LevelArchiveBuilder.BuildAsync(level, meta, source, CancellationToken.None);

            using var archive = new System.IO.MemoryStream();
            await LevelArchiveWriter.WriteArchiveAsync(plan, source, archive, Serialization,
                token: CancellationToken.None);

            archive.Position = 0;
            var content = await LevelArchiveReader.ReadAsync(archive, token: CancellationToken.None);
            Assert.AreEqual(LevelArchiveOpenResult.Ok, content.Result, "the archive should have opened");

            var parameters = new LevelArchiveGenerator.Parameters();
            ILevelArchiveInput input = parameters;

            input.LevelBytes = content.LevelBytes;
            input.LevelFormat = content.LevelFormat;
            input.MetaBytes = content.MetaBytes;
            input.MetaFormat = content.MetaFormat;
            input.SourcePath = "archive.tar.gz";
            input.ResourceFileNames = content.ResourceFileNames.ToArray();

            return parameters;
        }

        private static bool HasCode(LevelArchiveGenerator generator, string code) =>
            generator.LastReport != null && generator.LastReport.Issues.Any(issue => issue.Code == code);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Create_RoundTripsTheLevelThroughAPackage()
        {
            var (level, meta) = CreateLevel();
            var parameters = await ImportedAsync(level, meta);

            var generator = new LevelArchiveGenerator();
            var (imported, importedMeta) = generator.Create(parameters);

            Assert.AreEqual(level, imported);
            Assert.AreEqual(meta.LevelName, importedMeta.LevelName);
            Assert.IsTrue(generator.LastReport.IsClean, "an ordinary import loses nothing");
        }

        // The default, and the one that protects a library: two levels must never claim one folder.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Create_MintsANewLevelIdByDefault()
        {
            var (level, meta) = CreateLevel();
            var parameters = await ImportedAsync(level, meta);

            Assert.IsTrue(parameters.NewLevelId, "importing must not overwrite by default");

            var generator = new LevelArchiveGenerator();
            var (_, importedMeta) = generator.Create(parameters);

            Assert.AreNotEqual(meta.LevelId, importedMeta.LevelId);
            Assert.IsTrue(HasCode(generator, "archive.new_level_id"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Create_KeepsTheIdWhenAsked()
        {
            var (level, meta) = CreateLevel();
            var parameters = await ImportedAsync(level, meta);
            parameters.NewLevelId = false;

            var (_, importedMeta) = new LevelArchiveGenerator().Create(parameters);

            Assert.AreEqual(meta.LevelId, importedMeta.LevelId);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Create_DropsTheAuthorsWhenAsked()
        {
            var (level, meta) = CreateLevel();
            meta.LevelAuthors.Add(new Author(new StringValue("somebody"), "example.com"));

            var parameters = await ImportedAsync(level, meta);
            parameters.KeepAuthor = false;

            var (_, importedMeta) = new LevelArchiveGenerator().Create(parameters);

            Assert.IsEmpty(importedMeta.LevelAuthors);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Estimate_CountsWhatThePackageHolds()
        {
            var (level, meta) = CreateLevel();
            var parameters = await ImportedAsync(level, meta);

            var cost = new LevelArchiveGenerator().Estimate(null, parameters);

            Assert.AreEqual(level.Game.Objects.Count, cost.Objects);
        }

        // Handed nothing, produce nothing usable - and say so, rather than an empty level that reads
        // as a conversion of the author's content having gone wrong.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Create_WithNoLevelDocument_SaysSo()
        {
            var generator = new LevelArchiveGenerator();
            var (level, _) = generator.Create(new LevelArchiveGenerator.Parameters());

            Assert.IsNotNull(level);
            Assert.IsEmpty(level.Game.Objects);
            Assert.IsTrue(HasCode(generator, "archive.no_level"));
            Assert.IsTrue(generator.LastReport.HasFailure);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Create_WithUnreadableBytes_SaysSo()
        {
            var generator = new LevelArchiveGenerator();
            var parameters = new LevelArchiveGenerator.Parameters
            {
                LevelBytes = Encoding.UTF8.GetBytes("not a level document at all"),
                LevelFormat = SerializationType.Json,
            };

            var (level, _) = generator.Create(parameters);

            Assert.IsNotNull(level);
            Assert.IsTrue(HasCode(generator, "archive.unreadable"));
            Assert.IsTrue(generator.LastReport.HasFailure);
        }

        // An archive with no metadata still imports: a name and a cover can be retyped, and refusing
        // the whole thing over them would throw away the half that matters.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Create_WithNoMetadata_StillImportsTheLevel()
        {
            var (level, meta) = CreateLevel();
            var parameters = await ImportedAsync(level, meta);
            parameters.MetaBytes = null;

            var generator = new LevelArchiveGenerator();
            var (imported, importedMeta) = generator.Create(parameters);

            Assert.AreEqual(level, imported);
            Assert.IsNotNull(importedMeta);
            Assert.IsTrue(HasCode(generator, "archive.no_metadata"));
        }
    }
}
