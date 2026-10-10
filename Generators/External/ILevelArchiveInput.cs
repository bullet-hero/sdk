using BH.SDK.Serialization.Serializers;

namespace BH.SDK.Generators.External
{
    // BYTES, not a store or a path: parameters are SERIALIZED INTO A PRESET FILE, so a handle or a
    // passphrase would be written to disk as a field. The passphrase is absent entirely: the host
    // decrypts. The format travels beside the bytes (a document's format is its stored name).

    /// <summary> A generator that builds a level out of an archive the host already opened. </summary>
    public interface ILevelArchiveInput
    {
        /// <summary> The level document, decrypted if it needed to be. Empty means the host found
        /// nothing, and the generator must then produce nothing rather than an empty level. </summary>
        byte[] LevelBytes { get; set; }

        /// <summary> Which format <see cref="LevelBytes"/> is in. </summary>
        SerializationType LevelFormat { get; set; }

        /// <summary> The metadata document, or null when the archive carried none. </summary>
        byte[] MetaBytes { get; set; }

        /// <summary> Which format <see cref="MetaBytes"/> is in. </summary>
        SerializationType MetaFormat { get; set; }

        /// <summary> Where the archive came from, for the report to name. Never opened by the SDK. </summary>
        string SourcePath { get; set; }

        /// <summary> What the archive carries besides its two documents - the cover, the song, the
        /// textures. Names only: the host is what copies the files. </summary>
        string[] ResourceFileNames { get; set; }

        // The one member here the host READS rather than fills. It is an authored choice, so it
        // lives with the other parameters and is answered by the form; the host needs it because
        // copying the files is the host's half of the import.

        /// <summary> Whether the host should copy the archive's files into the new level's folder. </summary>
        bool ImportResources { get; }
    }
}
