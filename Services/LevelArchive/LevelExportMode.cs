using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Services.Archive;

namespace BH.SDK.Services.LevelArchive
{
    // Three independent choices flattened into one enum (an author picks ONE thing from a dropdown).
    // THE NUMBER IS IDENTITY, DisplayOrder IS THE DROPDOWN: a number never moves. ZipEncrypted (zip AES)
    // is not a second spelling of ZipProtected (OpenPGP around the archive). Docs/ARCHIVE.md.

    /// <summary> How a level archive is written out. </summary>
    public enum LevelExportMode
    {
        /// <summary> A plain folder - the same shape a level already has on disk, so it can be
        /// zipped, sent and unzipped by anyone. </summary>
        Folder = 0,

        /// <summary> A folder whose level document is encrypted. The metadata, the cover and the
        /// media stay readable, so a browser still renders the card. </summary>
        FolderProtectedLevel = 1,

        /// <summary> One .tar.gz. </summary>
        TarGz = 2,

        /// <summary> One .tar.gz.gpg - the whole archive behind a passphrase. </summary>
        TarGzProtected = 3,

        /// <summary> One .zip, the shape a desktop opens with no tool installed. </summary>
        Zip = 4,

        /// <summary> One .zip.gpg - the same zip behind an OpenPGP passphrase instead, for
        /// somebody who wants a zip and already has gpg. </summary>
        ZipProtected = 5,

        /// <summary> One .zip whose entries carry the zip's own AES-256, which an archiver opens by
        /// asking for the password - what a password on an archive means by default. </summary>
        ZipEncrypted = 6,
    }

    /// <summary> What each <see cref="LevelExportMode"/> implies. </summary>
    public static class LevelExportModeExtensions
    {
        /// <summary> Whether the mode writes one file rather than a folder. </summary>
        public static bool IsArchive(this LevelExportMode mode) => mode >= LevelExportMode.TarGz;

        /// <summary> Whether the mode needs a passphrase. </summary>
        public static bool IsProtected(this LevelExportMode mode) =>
            mode == LevelExportMode.FolderProtectedLevel || mode == LevelExportMode.TarGzProtected ||
            mode == LevelExportMode.ZipProtected || mode == LevelExportMode.ZipEncrypted;

        /// <summary> Which container the mode writes. Unknown for the folder shapes, which are not
        /// a container at all. </summary>
        public static ArchiveFormat Format(this LevelExportMode mode)
        {
            switch (mode)
            {
                case LevelExportMode.TarGz:
                case LevelExportMode.TarGzProtected:
                    return ArchiveFormat.TarGz;
                case LevelExportMode.Zip:
                case LevelExportMode.ZipProtected:
                case LevelExportMode.ZipEncrypted:
                    return ArchiveFormat.Zip;
                default:
                    return ArchiveFormat.Unknown;
            }
        }

        /// <summary> Which scheme protects what the mode writes. A protected FOLDER answers OpenPgp
        /// too - there the scheme covers the level document alone. </summary>
        public static ArchiveProtection Protection(this LevelExportMode mode)
        {
            switch (mode)
            {
                case LevelExportMode.FolderProtectedLevel:
                case LevelExportMode.TarGzProtected:
                case LevelExportMode.ZipProtected:
                    return ArchiveProtection.OpenPgp;
                case LevelExportMode.ZipEncrypted:
                    return ArchiveProtection.ZipAes256;
                default:
                    return ArchiveProtection.None;
            }
        }

        /// <summary> What the written file is called, extension included. Empty for a folder. </summary>
        public static string Extension(this LevelExportMode mode)
        {
            switch (mode)
            {
                case LevelExportMode.TarGz:
                    return FileNames.TarGzExtension;
                case LevelExportMode.TarGzProtected:
                    return FileNames.TarGzExtension + FileNames.EncryptedExtension;
                case LevelExportMode.Zip:
                case LevelExportMode.ZipEncrypted:
                    return FileNames.ZipExtension;
                case LevelExportMode.ZipProtected:
                    return FileNames.ZipExtension + FileNames.EncryptedExtension;
                default:
                    return string.Empty;
            }
        }

        // Ranked by use, not grouped by container: zip AES, then .tar.gz.gpg, then the rare .zip.gpg.
        // Every mode appears exactly once (LevelExportModeTests).
        private static readonly LevelExportMode[] Order =
        {
            LevelExportMode.Folder,
            LevelExportMode.FolderProtectedLevel,
            LevelExportMode.Zip,
            LevelExportMode.TarGz,
            LevelExportMode.ZipEncrypted,
            LevelExportMode.TarGzProtected,
            LevelExportMode.ZipProtected,
        };

        /// <summary> Every mode in the order a dropdown shows them. </summary>
        public static IReadOnlyList<LevelExportMode> DisplayOrder => Order;

        // A dropdown hands back a position, and a position out of range is a UI that drifted from
        // this table rather than a choice anybody made. Answering Folder is the safe reading of it:
        // it is the one mode that writes nothing a passphrase was needed for.

        /// <summary> The mode at a dropdown position, or Folder when the position means nothing. </summary>
        public static LevelExportMode FromIndex(int index) =>
            index >= 0 && index < Order.Length ? Order[index] : LevelExportMode.Folder;

        /// <summary> Where a mode sits in the dropdown, or 0 for one the table forgot. </summary>
        public static int ToIndex(this LevelExportMode mode)
        {
            for (var i = 0; i < Order.Length; i++)
                if (Order[i] == mode)
                    return i;

            return 0;
        }
    }
}