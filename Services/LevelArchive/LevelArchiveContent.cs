using System.Collections.Generic;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;

namespace BH.SDK.Services.LevelArchive
{
    /// <summary> Why opening a level archive ended the way it did. </summary>
    public enum LevelArchiveOpenResult
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

        /// <summary> Not a level archive: not an archive, not an OpenPGP message, or an archive
        /// with no level document in it. </summary>
        NotAnArchive = 4,

        /// <summary> A real archive this build cannot read - an encryption shape it does not
        /// implement, or a document format it does not know. </summary>
        Unsupported = 5,
    }

    // WHAT COMES OUT IS BYTES AND A STORE, NOT A Level. Deserializing is the caller's step, and
    // keeping it out of here is what lets the same reader serve two callers that want different
    // things from the same file: the editor's import turns the bytes into a model and migrates it,
    // while a server writes them into a column having never constructed one.
    //
    // The formats travel beside the bytes because nothing inside them says which they are - a level
    // document is Json or Blob according to the NAME it was stored under, which is the same
    // convention a level folder on disk uses.

    /// <summary> Everything a level archive held. </summary>
    public sealed class LevelArchiveContent
    {
        private LevelArchiveContent(LevelArchiveOpenResult result, ArchiveFormat format)
        {
            Result = result;
            Format = format;
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public LevelArchiveContent(byte[] levelBytes, SerializationType levelFormat, bool levelWasProtected,
            byte[] metaBytes, SerializationType metaFormat, IContentStore payload,
            IReadOnlyList<string> resourceFileNames, ArchiveFormat format = ArchiveFormat.Unknown)
        {
            Result = LevelArchiveOpenResult.Ok;
            Format = format;
            LevelBytes = levelBytes;
            LevelFormat = levelFormat;
            LevelWasProtected = levelWasProtected;
            MetaBytes = metaBytes;
            MetaFormat = metaFormat;
            Payload = payload;
            ResourceFileNames = resourceFileNames;
        }

        /// <summary> Why it ended the way it did. </summary>
        public LevelArchiveOpenResult Result { get; }

        // THE CONTAINER TRAVELS WITH THE VERDICT, and a refusal is where it earns its place: a
        // host that can say "this build does not read 7z yet" sends a person to re-zip their file,
        // while one that can only say "unsupported" sends them looking for a fault in it. A folder
        // answers Unknown, which is the truth - a folder is not a container.

        /// <summary> Which container it turned out to be. </summary>
        public ArchiveFormat Format { get; }

        /// <summary> Whether it opened. </summary>
        public bool IsOk => Result == LevelArchiveOpenResult.Ok;

        /// <summary> The level document, ready to deserialize. </summary>
        public byte[] LevelBytes { get; }

        /// <summary> Which format <see cref="LevelBytes"/> is in. </summary>
        public SerializationType LevelFormat { get; }

        /// <summary> Whether the level document was encrypted inside the archive. </summary>
        public bool LevelWasProtected { get; }

        /// <summary> The metadata document, ready to deserialize. Never encrypted. </summary>
        public byte[] MetaBytes { get; }

        /// <summary> Which format <see cref="MetaBytes"/> is in. </summary>
        public SerializationType MetaFormat { get; }

        /// <summary> Everything else the archive carried - the cover, the song, the textures. </summary>
        public IContentStore Payload { get; }

        /// <summary> Names of the files in <see cref="Payload"/> that are not documents. </summary>
        public IReadOnlyList<string> ResourceFileNames { get; }

        /// <summary> Nothing was opened, and this is why - naming the container where one was
        /// recognised, since a refusal that can name the format is one a person can act on. </summary>
        public static LevelArchiveContent Failed(LevelArchiveOpenResult result,
            ArchiveFormat format = ArchiveFormat.Unknown) =>
            new LevelArchiveContent(result, format);

        /// <summary> One line, for a log. </summary>
        public override string ToString() => IsOk
            ? $"Ok ({LevelFormat}, {ResourceFileNames?.Count ?? 0} file(s))"
            : Result.ToString();
    }
}
