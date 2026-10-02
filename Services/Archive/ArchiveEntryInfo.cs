namespace BH.SDK.Services.Archive
{
    /// <summary> What an archive's directory says about one file entry, read without inflating it.
    /// The size is a CLAIM the archive makes - an unpack enforces it again while copying. </summary>
    public sealed class ArchiveEntryInfo
    {
        public ArchiveEntryInfo(string name, long declaredBytes, bool encrypted)
        {
            Name = name;
            DeclaredBytes = declaredBytes;
            Encrypted = encrypted;
        }

        /// <summary> The entry's name inside the archive. </summary>
        public string Name { get; }

        /// <summary> The uncompressed size the directory declares. </summary>
        public long DeclaredBytes { get; }

        /// <summary> Whether opening it needs a passphrase. </summary>
        public bool Encrypted { get; }
    }
}
