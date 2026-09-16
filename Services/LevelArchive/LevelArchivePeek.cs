using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Archive;

namespace BH.SDK.Services.LevelArchive
{
    // WHAT A HOST CAN SAY ABOUT A FILE BEFORE COMMITTING TO IT. An import control that has been
    // handed a path can now name the container and the level inside it, which is the difference
    // between "12 file(s)" and "ZIP - <the level's name> - 12 files" on the one screen where an
    // author finds out whether they picked the file they meant to.
    //
    // It carries a RESULT rather than throwing, like everything else on this reader, and the result
    // is the same enum a full read answers with - so a host maps one set of outcomes, not two.

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
