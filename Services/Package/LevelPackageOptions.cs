using BH.SDK.Serialization.Serializers;

namespace BH.SDK.Services.Package
{
    /// <summary> How a package's two documents are written. </summary>
    public sealed class LevelPackageOptions
    {
        /// <summary> What a caller gets by asking for nothing in particular. </summary>
        public static LevelPackageOptions Default { get; } = new LevelPackageOptions();

        // Defaulting to the level's OWN formats is the host's job, not this class's: a level saved
        // as Bson should export as Bson, and only the host knows which it is (LevelMetaInfo). What
        // this class supplies when asked nothing is the format a level is created in.

        /// <summary> Format the level document is written in. </summary>
        public SerializationType LevelFormat { get; set; } = SerializationType.Json;

        /// <summary> Format the metadata document is written in. </summary>
        public SerializationType MetaFormat { get; set; } = SerializationType.Json;
    }
}
