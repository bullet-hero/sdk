using System;

namespace BH.SDK.Services.Archive
{
    // What stands between an upload and the disk. A gzip bomb is a few kilobytes that expands to as
    // much as the reader will accept, and a tar of a million empty entries costs nothing to make -
    // neither is caught by validating names, because both are made of perfectly legal ones.
    //
    // The caps are checked WHILE UNPACKING, entry by entry, and the unpack aborts the moment one is
    // passed. Checking afterwards would mean the damage is already done, which is the whole of what
    // these exist to prevent. The store underneath has its own cap for the same reason; this one is
    // what reports the failure in the archive's own terms.
    //
    // Defaults are sized for a LEVEL, not for an arbitrary archive: a level's own folder is a
    // handful of documents and a song, and anything that needs a thousand times that is not one.

    /// <summary> Bounds an unpack refuses to exceed. </summary>
    public sealed class ArchiveLimits
    {
        /// <summary> What an unpack accepts when the caller states nothing of its own. </summary>
        public static ArchiveLimits Default { get; } = new ArchiveLimits();

        /// <summary> How many entries an archive may hold. </summary>
        public int MaxEntries { get; set; } = 4096;

        /// <summary> How much one entry may weigh, uncompressed. </summary>
        public long MaxEntryBytes { get; set; } = 512L * 1024 * 1024;

        /// <summary> How much the whole archive may weigh, uncompressed. </summary>
        public long MaxTotalBytes { get; set; } = 1024L * 1024 * 1024;

        /// <summary> Throws when this instance describes bounds nothing can satisfy. </summary>
        public void Validate()
        {
            if (MaxEntries <= 0) throw new ArgumentOutOfRangeException(nameof(MaxEntries));
            if (MaxEntryBytes <= 0) throw new ArgumentOutOfRangeException(nameof(MaxEntryBytes));
            if (MaxTotalBytes <= 0) throw new ArgumentOutOfRangeException(nameof(MaxTotalBytes));
        }
    }
}
