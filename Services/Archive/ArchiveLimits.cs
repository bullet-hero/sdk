using System;

namespace BH.SDK.Services.Archive
{
    // What stands between an upload and the disk (a gzip bomb or a million empty entries are made of
    // legal names). Checked WHILE unpacking, aborting at the first cap passed. Sized for a LEVEL.

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
