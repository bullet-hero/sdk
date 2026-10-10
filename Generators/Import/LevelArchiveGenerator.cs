using System;
using BH.SDK.Generators.External;
using BH.SDK.Interop;
using BH.SDK.Models;
using BH.SDK.Models.Primitives;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Utils;
using BH.SDK.Versions;

namespace BH.SDK.Generators.Import
{
    // A LEVEL generator, not a menu command: the form, presets, estimate and labels come off the contract.
    // Export is NOT a generator (it consumes a level; a host service). It deserializes through the
    // ordinary service, so an old archive migrates on the way in. Handed nothing: an EMPTY level, with why.

    /// <summary> Builds a level out of a level archive the host opened. </summary>
    public class LevelArchiveGenerator : BaseLevelGenerator<LevelArchiveGenerator.Parameters>
    {
        private const string CodeNoLevel = "archive.no_level";
        private const string CodeUnreadable = "archive.unreadable";
        private const string CodeNoMeta = "archive.no_metadata";
        private const string CodeNewId = "archive.new_level_id";
        private const string CodePlacementsIncomplete = "archive.placements_incomplete";

        // Its own instance rather than an injected one: a generator is constructed by a reflection
        // scan with no arguments, and this service is stateless configuration whose converter list
        // is resolved once per instance. Static, so importing twice does not build it twice.
        private static readonly SerializationService Serialization = new SerializationService();

        /// <summary> <c>"gen_level_archive"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "gen_level_archive";

        // Behind every generator that builds a level from nothing: reading an archive someone sent is
        // the answer to "I was sent a level", not to "make a level", so it is nobody's first pick.

        /// <summary> Where this sits in a host's list; lower comes first. </summary>
        public override int ListOrder => 9;

        /// <summary> What must be true before a host offers this run. </summary>
        public override GeneratorRequirements Requirements => GeneratorRequirements.ExternalAnalysis;

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
        public override GeneratorHints Hints => HintsValue;

        private static readonly GeneratorHints HintsValue = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, nameof(Parameters.NewLevelId), nameof(Parameters.KeepAuthor),
                nameof(Parameters.ImportResources))
            // Host-filled fields are listed like any other - Hidden decides whether a row is SHOWN,
            // not whether the field is accounted for, and a field in no section still renders, at
            // the bottom, where nobody would look for it.
            .Section(GeneratorSections.Additional, nameof(Parameters.LevelBytes),
                nameof(Parameters.LevelFormat), nameof(Parameters.MetaBytes),
                nameof(Parameters.MetaFormat), nameof(Parameters.SourcePath),
                nameof(Parameters.ResourceFileNames))
            .Hidden(nameof(Parameters.LevelBytes))
            .Hidden(nameof(Parameters.LevelFormat))
            .Hidden(nameof(Parameters.MetaBytes))
            .Hidden(nameof(Parameters.MetaFormat))
            .Hidden(nameof(Parameters.SourcePath))
            .Hidden(nameof(Parameters.ResourceFileNames))
            .Build();

        /// <summary> The report from the last run, for a host to show once the level exists. It is
        /// not part of GeneratedLevel because that struct is the format's, not this import's. </summary>
        public InteropReport LastReport { get; private set; }

        /// <summary> Builds the level and its metadata together, so the two cannot disagree. </summary>
        protected override GeneratedLevel CreateTyped(Parameters parameters)
        {
            var report = new InteropReport();
            LastReport = report;

            if (parameters.LevelBytes == null || parameters.LevelBytes.Length == 0)
            {
                report.Failed(CodeNoLevel,
                    "No level document was read, so there is nothing to import.", parameters.SourcePath);
                return Empty();
            }

            Level level;
            try
            {
                level = Serialization.DeserializeEnvelope<Level>(parameters.LevelBytes, parameters.LevelFormat);
            }
            catch (Exception exception) when (exception is not NewerGenerationException)
            {
                // An archive that opened but would not deserialize is its own answer. A NEWER document is
                // refused unwrapped, so the host can say "update", not "broken archive".
                report.Failed(CodeUnreadable,
                    $"The level document could not be read: {exception.Message}", parameters.SourcePath);
                return Empty();
            }

            if (level == null)
            {
                report.Failed(CodeUnreadable,
                    "The level document read as nothing at all.", parameters.SourcePath);
                return Empty();
            }

            // A PACKAGE IS A LEVEL FILE, so it carries the same virtualized placements a level.json
            // does and needs the same step on the way in. The host's own loader does this for a
            // level it reads off disk; a generator reads its bytes itself, so it does it itself.
            var expansion = PrefabVirtualizationUtils.Expand(level);
            if (!expansion.IsEmpty)
                report.Dropped(CodePlacementsIncomplete,
                    $"{expansion.Count} prefab placement issue(s): part of the level's prefab content " +
                    "could not be rebuilt and is missing.", parameters.SourcePath);

            return new GeneratedLevel(level, ReadMeta(parameters, report));
        }

        private LevelMeta ReadMeta(Parameters parameters, InteropReport report)
        {
            var meta = Deserialize(parameters, report);

            // Minted by default: an archive is often a copy of a level the author already has, and the
            // same id would point two entries at one folder. Keeping it is the deliberate restore case.
            if (parameters.NewLevelId)
            {
                meta.LevelId = LevelId.NewId();
                report.Info(CodeNewId,
                    "The imported level was given a new id, so it cannot overwrite one already here.");
            }

            // Clearing the authors is NOT anonymising somebody else's work - the level is still
            // theirs and its license still says so. It is for the case the author is importing their
            // own archive as a starting point, where their name is already about to be added.
            if (!parameters.KeepAuthor) meta.LevelAuthors.Clear();

            return meta;
        }

        private LevelMeta Deserialize(Parameters parameters, InteropReport report)
        {
            if (parameters.MetaBytes == null || parameters.MetaBytes.Length == 0)
            {
                report.Approximated(CodeNoMeta,
                    "The archive carried no metadata, so the level was given fresh metadata of its own.",
                    parameters.SourcePath);
                return new LevelMeta();
            }

            try
            {
                return Serialization.DeserializeEnvelope<LevelMeta>(parameters.MetaBytes, parameters.MetaFormat)
                       ?? new LevelMeta();
            }
            catch (Exception exception) when (exception is not NewerGenerationException)
            {
                // The metadata failing is not the level failing: a name and a cover can be retyped,
                // and refusing the whole import over them would throw away the part that matters.
                report.Approximated(CodeNoMeta,
                    $"The metadata could not be read ({exception.Message}), so the level was given fresh metadata.",
                    parameters.SourcePath);
                return new LevelMeta();
            }
        }

        // The level's real cost is only known once its document is read, and reading it twice is
        // cheap next to showing the author a number that has nothing to do with their archive.

        /// <summary> What this run would add, answered before it runs. </summary>
        protected override GeneratorCost EstimateTyped(Parameters parameters)
        {
            if (parameters.LevelBytes == null || parameters.LevelBytes.Length == 0) return GeneratorCost.Zero;

            Level level;
            try
            {
                level = Serialization.DeserializeEnvelope<Level>(parameters.LevelBytes, parameters.LevelFormat);
            }
            catch (Exception)
            {
                // A newer document included: the run itself refuses it, and an estimate that threw
                // would take the form down with it.
                // An estimate is a readout, not a check - the run itself reports what went wrong.
                return GeneratorCost.Zero;
            }

            if (level?.Resources == null) return GeneratorCost.Zero;

            // Expanded before it is counted, or the number under-reports by every prefab copy in the
            // archive - which on a prefab-heavy level is most of it.
            PrefabVirtualizationUtils.Expand(level);

            var resources = level.Resources.Textures.Count + level.Resources.Fonts.Count
                                                           + level.Resources.Audios.Count +
                                                           level.Resources.CompositeShapes.Count
                                                           + level.Resources.Themes.Count +
                                                           level.Resources.Effects.Count
                                                           + level.Resources.Prefabs.Count;

            return new GeneratorCost(level.Game?.Objects?.Count ?? 0, 0, resources);
        }

        private static GeneratedLevel Empty() => new GeneratedLevel(new Level(), new LevelMeta());

        /// <summary> Which archive the host opened, and the passphrase when it is protected. Public mutable fields,
        /// like every parameters class here - a form binds to them and a preset serializes from them. </summary>
        public class Parameters : ILevelArchiveInput
        {
            /// <summary> Give the imported level an id of its own, so it cannot overwrite a level
            /// already on this machine. </summary>
            public bool NewLevelId = true;

            /// <summary> Keep whoever the archive says wrote it. </summary>
            public bool KeepAuthor = true;

            /// <summary> Copy the archive's own files - the cover, the song, the textures - into the
            /// new level's folder. </summary>
            public bool ImportResources = true;

            /// <summary> The level document as it came out of the archive. </summary>
            public byte[] LevelBytes = Array.Empty<byte>();

            /// <summary> Which format those bytes are in. </summary>
            public SerializationType LevelFormat = SerializationType.Json;

            /// <summary> The metadata document, where the archive carried one. </summary>
            public byte[] MetaBytes;

            /// <summary> Which format that one is in. </summary>
            public SerializationType MetaFormat = SerializationType.Json;

            /// <summary> Where the archive came from, for the report. </summary>
            public string SourcePath = string.Empty;

            /// <summary> What else the archive held, so the import can tell a missing resource from one it never had. </summary>
            public string[] ResourceFileNames = Array.Empty<string>();

            byte[] ILevelArchiveInput.LevelBytes
            {
                get => LevelBytes;
                set => LevelBytes = value;
            }

            SerializationType ILevelArchiveInput.LevelFormat
            {
                get => LevelFormat;
                set => LevelFormat = value;
            }

            byte[] ILevelArchiveInput.MetaBytes
            {
                get => MetaBytes;
                set => MetaBytes = value;
            }

            SerializationType ILevelArchiveInput.MetaFormat
            {
                get => MetaFormat;
                set => MetaFormat = value;
            }

            string ILevelArchiveInput.SourcePath
            {
                get => SourcePath;
                set => SourcePath = value;
            }

            string[] ILevelArchiveInput.ResourceFileNames
            {
                get => ResourceFileNames;
                set => ResourceFileNames = value;
            }

            bool ILevelArchiveInput.ImportResources => ImportResources;
        }
    }
}