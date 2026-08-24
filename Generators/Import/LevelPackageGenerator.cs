using System;
using BH.SDK.Generators.External;
using BH.SDK.Interop;
using BH.SDK.Models;
using BH.SDK.Models.Primitives;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;

namespace BH.SDK.Generators.Import
{
    // A LEVEL GENERATOR RATHER THAN A MENU COMMAND, for the same reason the Afterbeat import is one:
    // "build a whole Level and LevelMeta from a few parameters" is exactly what the generator
    // contract already describes, and going through it costs the host no UI at all - the form, the
    // preset list, the estimate and the labels all come off the contract. Importing this project's
    // OWN format is authoring automation like any other.
    //
    // THE REVERSE DIRECTION IS DELIBERATELY NOT A GENERATOR. A generator produces content; an export
    // consumes a level and writes files, which is a host service (Services/Package's writer, driven
    // from the editor's Dangerous Zone).
    //
    // IT DESERIALIZES THROUGH THE ORDINARY SERVICE, which is the whole reason importing an older
    // package costs nothing: DeserializeEnvelope walks VersionedTypeRegistry and migrates the
    // document to the domain's current shape on the way in. A package written by a build from a year
    // ago opens as today's model, and this generator never learns that it was old.
    //
    // ExternalAnalysis for the reason every other one here needs it: the SDK does not open the file.
    // The host reads the package - decrypting it if it was protected - and fills the bytes in.
    // Handed nothing, this produces an empty level and says why, never a plausible-looking one.

    /// <summary> Builds a level out of a level package the host opened. </summary>
    public class LevelPackageGenerator : BaseLevelGenerator<LevelPackageGenerator.Parameters>
    {
        private const string CodeNoLevel = "package.no_level";
        private const string CodeUnreadable = "package.unreadable";
        private const string CodeNoMeta = "package.no_metadata";
        private const string CodeNewId = "package.new_level_id";

        // Its own instance rather than an injected one: a generator is constructed by a reflection
        // scan with no arguments, and this service is stateless configuration whose converter list
        // is resolved once per instance. Static, so importing twice does not build it twice.
        private static readonly SerializationService Serialization = new SerializationService();

        public override string NameKey => "gen_level_package";

        // Ahead of the foreign-format import at 10: this reads the project's own packages, which is
        // the commoner answer to "I was sent a level".
        public override int ListOrder => 9;

        public override GeneratorRequirements Requirements => GeneratorRequirements.ExternalAnalysis;

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
            catch (Exception exception)
            {
                // A package that opened and then would not deserialize is a real answer the author
                // needs: the archive was fine, the document inside it was not.
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

            return new GeneratedLevel(level, ReadMeta(parameters, report));
        }

        private LevelMeta ReadMeta(Parameters parameters, InteropReport report)
        {
            var meta = Deserialize(parameters, report);

            // Minted by default, and the default is the safe one: a package is usually a copy of a
            // level the author may well already have, and importing it under the same id would point
            // two entries at one folder. Keeping the id is the deliberate choice - restoring a level
            // this machine used to hold.
            if (parameters.NewLevelId)
            {
                meta.LevelId = LevelId.NewId();
                report.Info(CodeNewId,
                    "The imported level was given a new id, so it cannot overwrite one already here.");
            }

            // Clearing the authors is NOT anonymising somebody else's work - the level is still
            // theirs and its license still says so. It is for the case the author is importing their
            // own package as a starting point, where their name is already about to be added.
            if (!parameters.KeepAuthor) meta.LevelAuthors.Clear();

            return meta;
        }

        private LevelMeta Deserialize(Parameters parameters, InteropReport report)
        {
            if (parameters.MetaBytes == null || parameters.MetaBytes.Length == 0)
            {
                report.Approximated(CodeNoMeta,
                    "The package carried no metadata, so the level was given fresh metadata of its own.",
                    parameters.SourcePath);
                return new LevelMeta();
            }

            try
            {
                return Serialization.DeserializeEnvelope<LevelMeta>(parameters.MetaBytes, parameters.MetaFormat)
                       ?? new LevelMeta();
            }
            catch (Exception exception)
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
        // cheap next to showing the author a number that has nothing to do with their package.
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
                // An estimate is a readout, not a check - the run itself reports what went wrong.
                return GeneratorCost.Zero;
            }

            if (level?.Resources == null) return GeneratorCost.Zero;

            var resources = level.Resources.Textures.Count + level.Resources.Fonts.Count
                            + level.Resources.Audios.Count + level.Resources.CompositeShapes.Count
                            + level.Resources.Themes.Count + level.Resources.Effects.Count
                            + level.Resources.Prefabs.Count;

            return new GeneratorCost(level.Game?.Objects?.Count ?? 0, 0, resources);
        }

        private static GeneratedLevel Empty() => new GeneratedLevel(new Level(), new LevelMeta());

        public class Parameters : ILevelPackageInput
        {
            /// <summary> Give the imported level an id of its own, so it cannot overwrite a level
            /// already on this machine. </summary>
            public bool NewLevelId = true;

            /// <summary> Keep whoever the package says wrote it. </summary>
            public bool KeepAuthor = true;

            /// <summary> Copy the package's own files - the cover, the song, the textures - into the
            /// new level's folder. </summary>
            public bool ImportResources = true;

            public byte[] LevelBytes = Array.Empty<byte>();
            public SerializationType LevelFormat = SerializationType.Json;
            public byte[] MetaBytes;
            public SerializationType MetaFormat = SerializationType.Json;
            public string SourcePath = string.Empty;
            public string[] ResourceFileNames = Array.Empty<string>();

            byte[] ILevelPackageInput.LevelBytes
            {
                get => LevelBytes;
                set => LevelBytes = value;
            }

            SerializationType ILevelPackageInput.LevelFormat
            {
                get => LevelFormat;
                set => LevelFormat = value;
            }

            byte[] ILevelPackageInput.MetaBytes
            {
                get => MetaBytes;
                set => MetaBytes = value;
            }

            SerializationType ILevelPackageInput.MetaFormat
            {
                get => MetaFormat;
                set => MetaFormat = value;
            }

            string ILevelPackageInput.SourcePath
            {
                get => SourcePath;
                set => SourcePath = value;
            }

            string[] ILevelPackageInput.ResourceFileNames
            {
                get => ResourceFileNames;
                set => ResourceFileNames = value;
            }

            bool ILevelPackageInput.ImportResources => ImportResources;
        }
    }
}
