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

    // Bytes and a store, NOT a Level: deserializing is the caller's step (an editor migrates, a server
    // stores bytes). The formats travel beside the bytes: a document's format is its stored NAME.

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

        // The container travels with the verdict so a refusal can say "7z is not read yet". A folder
        // answers Unknown.

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
