using System;
using System.Globalization;

namespace BH.SDK.Utils
{
    // Sizes are plain longs of BYTES everywhere in the format and in publish profiles. .NET has no
    // size type to use instead - there is no ByteSize/DataSize in the BCL, only third-party ones -
    // and inventing a struct here would buy nothing over an integer while making every stored limit
    // a custom-serialized value. What a number that big does need is a way to be READ, which is all
    // this file is: the refusal a moderator sees says "48.2 MB", not "50529730".
    //
    // Binary units (1 KiB = 1024 B) printed with the decimal abbreviations everyone actually uses -
    // matching what a file manager on any of the target platforms shows for the same file, which is
    // the number an author will compare the message against.

    /// <summary> Human-readable byte counts. </summary>
    public static class ByteSizeUtils
    {
        /// <summary> Binary kilobyte - 1024 bytes, as a file browser counts. </summary>
        public const long Kilobyte = 1024L;

        /// <summary> Binary megabyte. </summary>
        public const long Megabyte = Kilobyte * 1024L;

        /// <summary> Binary gigabyte. </summary>
        public const long Gigabyte = Megabyte * 1024L;

        /// <summary> "12.4 MB", "900 KB", "512 B". Negative counts read as zero - a size that could
        /// not be measured is not a negative size. </summary>
        public static string Format(long bytes)
        {
            if (bytes <= 0) return "0 B";

            if (bytes >= Gigabyte) return Format(bytes, Gigabyte, "GB");
            if (bytes >= Megabyte) return Format(bytes, Megabyte, "MB");
            if (bytes >= Kilobyte) return Format(bytes, Kilobyte, "KB");
            return bytes.ToString(CultureInfo.InvariantCulture) + " B";
        }

        private static string Format(long bytes, long unit, string suffix)
        {
            var value = (double)bytes / unit;
            var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
            return rounded.ToString("0.#", CultureInfo.InvariantCulture) + " " + suffix;
        }
    }
}
