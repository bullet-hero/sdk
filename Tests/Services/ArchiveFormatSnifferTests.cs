using System.Text;
using BH.SDK.Services.Archive;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    // THE ORDER OF THE CHECKS IS WHAT THESE PIN. Three magic numbers are exact; the OpenPGP test is
    // a packet tag, which is loose enough to match a byte from anything, so it has to be asked
    // last. A case that begins with a gzip magic AND could pass the packet-tag test must still come
    // back TarGz - that is the whole reason the order is written down rather than left to
    // whichever branch happens to be first.

    /// <summary> Which container the leading bytes say a stream is. </summary>
    public class ArchiveFormatSnifferTests
    {
        [Test]
        [TestCase(new byte[] { 0x1f, 0x8b, 0x08, 0x00 }, ArchiveFormat.TarGz)]
        [TestCase(new byte[] { 0x50, 0x4b, 0x03, 0x04 }, ArchiveFormat.Zip)]
        [TestCase(new byte[] { 0x50, 0x4b, 0x05, 0x06 }, ArchiveFormat.Zip)]
        [TestCase(new byte[] { 0x50, 0x4b, 0x07, 0x08 }, ArchiveFormat.Zip)]
        [TestCase(new byte[] { 0x50, 0x4b, 0x30, 0x30 }, ArchiveFormat.Zip)]
        [TestCase(new byte[] { 0x37, 0x7a, 0xbc, 0xaf, 0x27, 0x1c }, ArchiveFormat.SevenZip)]
        [TestCase(new byte[] { 0xc3, 0x0d, 0x04, 0x09 }, ArchiveFormat.OpenPgp)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Detect_NamesTheFormatItsMagicBelongsTo(byte[] leading, ArchiveFormat expected) =>
            Assert.AreEqual(expected, ArchiveFormatSniffer.Detect(leading));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Detect_AnswersUnknownForNothingInParticular()
        {
            Assert.AreEqual(ArchiveFormat.Unknown, ArchiveFormatSniffer.Detect(null));
            Assert.AreEqual(ArchiveFormat.Unknown, ArchiveFormatSniffer.Detect(new byte[0]));
            Assert.AreEqual(ArchiveFormat.Unknown,
                ArchiveFormatSniffer.Detect(Encoding.UTF8.GetBytes("{\"objects\":[]}")));
        }

        // A truncated read is not an error, and a two-byte buffer can still answer: gzip's magic
        // fits in it, 7z's does not, and saying Unknown about six bytes nobody has seen is the
        // truth rather than a guess.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Detect_WorksOnABufferShorterThanItAskedFor()
        {
            Assert.AreEqual(ArchiveFormat.TarGz, ArchiveFormatSniffer.Detect(new byte[] { 0x1f, 0x8b }));
            Assert.AreEqual(ArchiveFormat.Unknown, ArchiveFormatSniffer.Detect(new byte[] { 0x37, 0x7a }));
            Assert.AreEqual(ArchiveFormat.Unknown, ArchiveFormatSniffer.Detect(new byte[] { 0x50, 0x4b }));
        }
    }
}
