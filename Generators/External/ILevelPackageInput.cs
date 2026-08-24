using BH.SDK.Serialization.Serializers;

namespace BH.SDK.Generators.External
{
    // BYTES, NOT A STORE AND NOT A PATH, and that is the one decision worth stating here. Generator
    // parameters are bound by a reflection-built form and SERIALIZED INTO A PRESET FILE - so an open
    // store handle would be a live resource written to disk as a field, and a passphrase would be a
    // password written to disk as a field. Neither belongs in a preset, and the type system is where
    // that gets settled rather than in a convention nobody reads.
    //
    // Which is also why THE PASSPHRASE IS ABSENT FROM THIS INTERFACE ENTIRELY. The host opens the
    // package, asks for the passphrase if the package wants one, and hands over plaintext; the
    // generator never learns that the package was protected at all.
    //
    // The format travels beside the bytes because nothing inside them says which they are: a level
    // document is Json or Bson according to the NAME it was stored under, exactly as in a level
    // folder on disk.

    /// <summary> A generator that builds a level out of a package the host already opened. </summary>
    public interface ILevelPackageInput
    {
        /// <summary> The level document, decrypted if it needed to be. Empty means the host found
        /// nothing, and the generator must then produce nothing rather than an empty level. </summary>
        byte[] LevelBytes { get; set; }

        /// <summary> Which format <see cref="LevelBytes"/> is in. </summary>
        SerializationType LevelFormat { get; set; }

        /// <summary> The metadata document, or null when the package carried none. </summary>
        byte[] MetaBytes { get; set; }

        /// <summary> Which format <see cref="MetaBytes"/> is in. </summary>
        SerializationType MetaFormat { get; set; }

        /// <summary> Where the package came from, for the report to name. Never opened by the SDK. </summary>
        string SourcePath { get; set; }

        /// <summary> What the package carries besides its two documents - the cover, the song, the
        /// textures. Names only: the host is what copies the files. </summary>
        string[] ResourceFileNames { get; set; }

        // The one member here the host READS rather than fills. It is an authored choice, so it
        // lives with the other parameters and is answered by the form; the host needs it because
        // copying the files is the host's half of the import.

        /// <summary> Whether the host should copy the package's files into the new level's folder. </summary>
        bool ImportResources { get; }
    }
}
