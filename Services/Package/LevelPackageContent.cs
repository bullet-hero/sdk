using System.Collections.Generic;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Content;

namespace BH.SDK.Services.Package
{
    /// <summary> Why opening a level package ended the way it did. </summary>
    public enum LevelPackageOpenResult
    {
        /// <summary> Opened. </summary>
        Ok = 0,

        /// <summary> It is protected and no passphrase was given. A distinct answer from a wrong
        /// one: the host asks, rather than telling the player they got it wrong. </summary>
        PassphraseRequired = 1,

        /// <summary> The passphrase does not open it. </summary>
        WrongPassphrase = 2,

        /// <summary> It opened and failed its integrity check - altered or truncated. </summary>
        Damaged = 3,

        /// <summary> Not a level package: not an archive, not an OpenPGP message, or an archive
        /// with no level document in it. </summary>
        NotAPackage = 4,

        /// <summary> A real package this build cannot read - an encryption shape it does not
        /// implement, or a document format it does not know. </summary>
        Unsupported = 5,
    }

    // WHAT COMES OUT IS BYTES AND A STORE, NOT A Level. Deserializing is the caller's step, and
    // keeping it out of here is what lets the same reader serve two callers that want different
    // things from the same file: the editor's import turns the bytes into a model and migrates it,
    // while a server writes them into a column having never constructed one.
    //
    // The formats travel beside the bytes because nothing inside them says which they are - a level
    // document is Json or Bson according to the NAME it was stored under, which is the same
    // convention a level folder on disk uses.

    /// <summary> Everything a level package held. </summary>
    public sealed class LevelPackageContent
    {
        private LevelPackageContent(LevelPackageOpenResult result)
        {
            Result = result;
        }

        public LevelPackageContent(byte[] levelBytes, SerializationType levelFormat, bool levelWasProtected,
            byte[] metaBytes, SerializationType metaFormat, IContentStore payload,
            IReadOnlyList<string> resourceFileNames)
        {
            Result = LevelPackageOpenResult.Ok;
            LevelBytes = levelBytes;
            LevelFormat = levelFormat;
            LevelWasProtected = levelWasProtected;
            MetaBytes = metaBytes;
            MetaFormat = metaFormat;
            Payload = payload;
            ResourceFileNames = resourceFileNames;
        }

        /// <summary> Why it ended the way it did. </summary>
        public LevelPackageOpenResult Result { get; }

        /// <summary> Whether it opened. </summary>
        public bool IsOk => Result == LevelPackageOpenResult.Ok;

        /// <summary> The level document, ready to deserialize. </summary>
        public byte[] LevelBytes { get; }

        /// <summary> Which format <see cref="LevelBytes"/> is in. </summary>
        public SerializationType LevelFormat { get; }

        /// <summary> Whether the level document was encrypted inside the package. </summary>
        public bool LevelWasProtected { get; }

        /// <summary> The metadata document, ready to deserialize. Never encrypted. </summary>
        public byte[] MetaBytes { get; }

        /// <summary> Which format <see cref="MetaBytes"/> is in. </summary>
        public SerializationType MetaFormat { get; }

        /// <summary> Everything else the package carried - the cover, the song, the textures. </summary>
        public IContentStore Payload { get; }

        /// <summary> Names of the files in <see cref="Payload"/> that are not documents. </summary>
        public IReadOnlyList<string> ResourceFileNames { get; }

        public static LevelPackageContent Failed(LevelPackageOpenResult result) =>
            new LevelPackageContent(result);

        public override string ToString() => IsOk
            ? $"Ok ({LevelFormat}, {ResourceFileNames?.Count ?? 0} file(s))"
            : Result.ToString();
    }
}
