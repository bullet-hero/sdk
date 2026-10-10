using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Archive;

namespace BH.SDK.Services.LevelArchive
{
    // What a host can say about a file before committing: container and level name. A RESULT using the
    // same enum a full read answers with, so a host maps one set of outcomes.

    /// <summary> What a cheap look at an archive could tell. </summary>
    public readonly struct LevelArchivePeek
    {
        /// <summary> Which container it turned out to be. </summary>
        public readonly ArchiveFormat Format;

        /// <summary> How far the look got. </summary>
        public readonly LevelArchiveOpenResult Result;

        /// <summary> The metadata document, or null when the archive carries none. </summary>
        public readonly byte[] MetaBytes;

        /// <summary> Which format that document is in. </summary>
        public readonly SerializationType MetaFormat;

        /// <summary> How many files the archive holds, where that is knowable without unpacking -
        /// which is to say, for a zip. Zero otherwise. </summary>
        public readonly int FileCount;

        /// <summary> Built from what the look found. </summary>
        public LevelArchivePeek(ArchiveFormat format, LevelArchiveOpenResult result, byte[] metaBytes,
            SerializationType metaFormat, int fileCount)
        {
            Format = format;
            Result = result;
            MetaBytes = metaBytes;
            MetaFormat = metaFormat;
            FileCount = fileCount;
        }

        /// <summary> Whether the look succeeded - which does not promise a metadata document. </summary>
        public bool IsOk => Result == LevelArchiveOpenResult.Ok;

        /// <summary> Whether a metadata document came back. </summary>
        public bool HasMeta => MetaBytes != null && MetaBytes.Length > 0;
    }
}
