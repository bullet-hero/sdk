using System;
using System.Collections.Generic;
using System.Linq;
using BH.SDK.Models;
using BH.SDK.Services.Archive;
using BH.SDK.Services.LevelArchive;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    // THE DROPDOWN IS THE ONLY PLACE THE SEVEN MODES ARE A LIST, and until this file existed that
    // list lived in the UI layer keyed by position - so a mode could be dropped, duplicated or
    // silently swapped for its neighbour and nothing would say so. The order is a product decision
    // and is expected to move again; what may not move is the number a mode carries, because a
    // value read back by position turns one export into another the day the order changes.

    /// <summary> What each export mode implies, and the order an author reads them in. </summary>
    public class LevelExportModeTests
    {
        private static IEnumerable<LevelExportMode> All =>
            Enum.GetValues(typeof(LevelExportMode)).Cast<LevelExportMode>();

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void DisplayOrder_HoldsEveryModeExactlyOnce()
        {
            CollectionAssert.AreEquivalent(All.ToArray(), LevelExportModeExtensions.DisplayOrder);
        }

        // The two directions of the same table, and a mode the table forgot would answer 0 from
        // ToIndex and Folder from FromIndex - which is why the case above is the one that fails
        // first rather than this one.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void FromIndex_AndToIndex_AreTheSameTable()
        {
            foreach (var mode in All)
                Assert.AreEqual(mode, LevelExportModeExtensions.FromIndex(mode.ToIndex()));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void FromIndex_AnswersFolderForAPositionThatMeansNothing()
        {
            Assert.AreEqual(LevelExportMode.Folder, LevelExportModeExtensions.FromIndex(-1));
            Assert.AreEqual(LevelExportMode.Folder,
                LevelExportModeExtensions.FromIndex(LevelExportModeExtensions.DisplayOrder.Count));
        }

        // THE RANKING THE AUTHOR ASKED FOR, written down where it can fail. A password on a level
        // means the zip's own AES first, then the .tar.gz.gpg that predates it, then the .zip.gpg
        // that exists for somebody who wants both a zip and gpg - and they sit together, so the
        // three read as one choice rather than as three scattered ones.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void DisplayOrder_RanksThePasswordedArchives()
        {
            var order = LevelExportModeExtensions.DisplayOrder;

            var aes = order.ToList().IndexOf(LevelExportMode.ZipEncrypted);

            Assert.AreEqual(LevelExportMode.ZipEncrypted, order[aes]);
            Assert.AreEqual(LevelExportMode.TarGzProtected, order[aes + 1]);
            Assert.AreEqual(LevelExportMode.ZipProtected, order[aes + 2]);
            Assert.AreEqual(LevelExportMode.Folder, order[0]);
        }

        [Test]
        [TestCase(LevelExportMode.Folder, ArchiveFormat.Unknown, ArchiveProtection.None, "")]
        [TestCase(LevelExportMode.FolderProtectedLevel, ArchiveFormat.Unknown, ArchiveProtection.OpenPgp, "")]
        [TestCase(LevelExportMode.TarGz, ArchiveFormat.TarGz, ArchiveProtection.None, ".tar.gz")]
        [TestCase(LevelExportMode.TarGzProtected, ArchiveFormat.TarGz, ArchiveProtection.OpenPgp, ".tar.gz.gpg")]
        [TestCase(LevelExportMode.Zip, ArchiveFormat.Zip, ArchiveProtection.None, ".zip")]
        [TestCase(LevelExportMode.ZipProtected, ArchiveFormat.Zip, ArchiveProtection.OpenPgp, ".zip.gpg")]
        [TestCase(LevelExportMode.ZipEncrypted, ArchiveFormat.Zip, ArchiveProtection.ZipAes256, ".zip")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryMode_NamesItsContainerItsSchemeAndItsExtension(LevelExportMode mode,
            ArchiveFormat format, ArchiveProtection protection, string extension)
        {
            Assert.AreEqual(format, mode.Format());
            Assert.AreEqual(protection, mode.Protection());
            Assert.AreEqual(extension, mode.Extension());
        }

        // The two predicates the UI reads, checked against the table above rather than restated:
        // IsProtected is what shows the passphrase row, and a mode needing a passphrase while
        // claiming no scheme - or the reverse - is a row that asks for nothing or drops what it was
        // given. IsArchive is what decides whether a file or a folder is picked.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ThePredicates_AgreeWithTheScheme()
        {
            foreach (var mode in All)
            {
                Assert.AreEqual(mode.Protection() != ArchiveProtection.None, mode.IsProtected(),
                    mode.ToString());
                Assert.AreEqual(mode.Format() != ArchiveFormat.Unknown, mode.IsArchive(),
                    mode.ToString());
                Assert.AreEqual(mode.IsArchive(), mode.Extension().Length > 0, mode.ToString());
            }
        }

        // The extension is what the save dialog writes, so it has to end in the container's own
        // name - a .zip called .tar.gz is a file no archiver opens by double click.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryArchiveExtension_EndsInItsContainerOrInGpg()
        {
            foreach (var mode in All.Where(mode => mode.IsArchive()))
            {
                var expected = mode.Format() == ArchiveFormat.Zip
                    ? FileNames.ZipExtension
                    : FileNames.TarGzExtension;

                var extension = mode.Extension();
                if (mode.Protection() == ArchiveProtection.OpenPgp)
                    expected += FileNames.EncryptedExtension;

                Assert.AreEqual(expected, extension, mode.ToString());
            }
        }
    }
}
