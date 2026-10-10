using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Archive;

namespace BH.SDK.Services.LevelArchive
{
    /// <summary> How an archive's two documents are written. </summary>
    public sealed class LevelArchiveOptions
    {
        /// <summary> What a caller gets by asking for nothing in particular. </summary>
        public static LevelArchiveOptions Default { get; } = new LevelArchiveOptions();

        // Defaulting to the level's OWN formats is the host's job, not this class's: a level saved
        // as Blob should export as Blob, and only the host knows which it is (LevelMetaInfo). What
        // this class supplies when asked nothing is the format a level is created in.

        /// <summary> Format the level document is written in. </summary>
        public SerializationType LevelFormat { get; set; } = SerializationType.Json;

        /// <summary> Format the metadata document is written in. </summary>
        public SerializationType MetaFormat { get; set; } = SerializationType.Json;

        // Container and passphrase scheme live here, not in WriteArchiveAsync's signature: this is
        // already the one place a LevelExportMode becomes writer input, so the fact has one home.

        /// <summary> Which container an archive export writes. </summary>
        public ArchiveFormat Format { get; set; } = ArchiveFormat.TarGz;

        /// <summary> Which scheme protects it. </summary>
        public ArchiveProtection Protection { get; set; } = ArchiveProtection.None;
    }
}