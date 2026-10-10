using BH.SDK.Services.Crypto;

namespace BH.SDK.Services.Archive
{
    // The order of the checks is the whole file: OpenPGP has no magic number, only a loose packet-tag
    // test, so it is asked LAST. A buffer shorter than MagicBytes is not an error; too short for any
    // format is Unknown.

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

        // Four zip signatures, not one (ordinary, empty, two spanned forms), so the level layer can say
        // "this zip holds no level" instead of "not an archive".
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
