using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Services.Archive;

namespace BH.SDK.Services.LevelArchive
{
    // THREE INDEPENDENT CHOICES FLATTENED INTO ONE ENUM, and flattening them is deliberate: an
    // author picks ONE thing from a dropdown, not a container, then a checkbox, then another
    // checkbox. The predicates below are what code reads instead of switching on all seven.
    //
    // THE NUMBER IS IDENTITY, DisplayOrder IS THE DROPDOWN. They were the same thing once, and the
    // day the order changed is the day that stopped being free: a value read back by position means
    // every reordering silently turns one export into another. So the number a mode carries never
    // moves again - 2 and 3 stay the tar.gz pair they have always been - and what an author sees is
    // the table below, which is a product decision and is expected to change.
    //
    // ZipEncrypted IS NOT A SECOND WAY TO SPELL ZipProtected. Encrypted means the zip's own AES-256,
    // which any archiver opens by asking for the password - that is the DEFAULT way to put a level
    // behind a password, because the person receiving it has an archiver and does not have gpg.
    // Protected means an OpenPGP message around the whole archive, which `gpg -d` opens and which
    // covers a document, a folder and an archive with one scheme; it stays for the people who
    // already live in that world, and for the folder shape, where nothing else can do the job.

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

        // THE ORDER THE AUTHOR READS, most reached for first, and the three passworded shapes are
        // ranked rather than grouped by container: the zip's own AES is what a password on a level
        // means now, .tar.gz.gpg is the shape that predates it, and .zip.gpg is the rare one that
        // exists for somebody who wants both a zip and gpg. Every mode appears exactly once, which
        // LevelExportModeTests checks - a dropdown missing a mode is a mode nobody can pick.
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