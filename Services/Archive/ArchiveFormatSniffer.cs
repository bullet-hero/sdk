using BH.SDK.Services.Crypto;

namespace BH.SDK.Services.Archive
{
    // THE ORDER OF THE CHECKS IS THE WHOLE FILE. Three of the four formats begin with a magic
    // number - a constant somebody would have to write deliberately - while OpenPGP has none: its
    // first byte is a PACKET TAG, and the test for one is "the high bit is set and the tag is one
    // of six". That is loose enough to accept a byte from an unrelated binary, so it is asked LAST,
    // after every exact answer has already failed.
    //
    // MagicBytes is how much a caller has to peek. Six, for the 7z signature - the longest of the
    // three - and a shorter buffer is not an error: a two-byte read can still answer TarGz, and a
    // file too short to be any of them is Unknown, which is the truth about it.

    /// <summary> Deciding which container a stream holds by looking at its first bytes. </summary>
    public static class ArchiveFormatSniffer
    {
        /// <summary> How many leading bytes <see cref="Detect"/> wants to see. </summary>
        public const int MagicBytes = 8;

        private static readonly byte[] GzipMagic = { 0x1f, 0x8b };
        private static readonly byte[] SevenZipMagic = { 0x37, 0x7a, 0xbc, 0xaf, 0x27, 0x1c };

        /// <summary> Which format the leading bytes belong to, or Unknown. </summary>
        public static ArchiveFormat Detect(byte[] leading)
        {
            if (leading == null || leading.Length == 0) return ArchiveFormat.Unknown;

            if (StartsWith(leading, GzipMagic)) return ArchiveFormat.TarGz;
            if (IsZip(leading)) return ArchiveFormat.Zip;
            if (StartsWith(leading, SevenZipMagic)) return ArchiveFormat.SevenZip;

            return PgpSymmetricService.LooksLikeOpenPgp(leading)
                ? ArchiveFormat.OpenPgp
                : ArchiveFormat.Unknown;
        }

        // FOUR ZIP SIGNATURES, NOT ONE. "PK\x03\x04" begins the ordinary case, an archive that
        // holds something. The other three are real files a person can hand over: "PK\x05\x06" is
        // an empty zip - nothing but an end-of-central-directory record - and "PK\x07\x08" and
        // "PK00" begin the spanned forms. Recognising all four is what lets the LEVEL layer answer
        // "this zip holds no level" instead of "this is not an archive", which are different facts
        // and deserve different words.
        private static bool IsZip(byte[] leading)
        {
            if (leading.Length < 4 || leading[0] != 0x50 || leading[1] != 0x4b) return false;

            var third = leading[2];
            var fourth = leading[3];

            return (third == 0x03 && fourth == 0x04)
                   || (third == 0x05 && fourth == 0x06)
                   || (third == 0x07 && fourth == 0x08)
                   || (third == 0x30 && fourth == 0x30);
        }

        private static bool StartsWith(byte[] leading, byte[] magic)
        {
            if (leading.Length < magic.Length) return false;

            for (var i = 0; i < magic.Length; i++)
                if (leading[i] != magic[i])
                    return false;

            return true;
        }
    }
}
