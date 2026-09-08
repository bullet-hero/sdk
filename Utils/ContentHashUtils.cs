using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BH.SDK.Utils
{
    // Identity of the bytes behind a resource, for ResourceMeta.ResourceHashes. A takedown names a
    // work and answering it means finding every level that carries it - by title that is a guess,
    // by digest it is a lookup.
    //
    // The algorithm is part of the stored value ("sha256:ab12...") rather than assumed, so a future
    // digest can be introduced without invalidating what is already written: an old hash keeps
    // saying what it is, and a comparison of two different algorithms fails as unequal instead of
    // silently matching. Lowercase hex with no separators, because that is what every other tool a
    // moderator might reach for prints.
    //
    // Not cryptography in the CryptographyService sense - nothing here protects anything. SHA-256
    // is chosen for being collision-resistant enough that two different tracks never collide by
    // accident, and universally available outside this codebase.

    /// <summary> Content digests for resource files. </summary>
    public static class ContentHashUtils
    {
        /// <summary> What every digest this project writes begins with, so the algorithm is stated in the value. </summary>
        public const string Sha256Prefix = "sha256:";

        /// <summary> Length of a "sha256:" value - the prefix plus 64 hex characters. </summary>
        public const int Sha256Length = 7 + 64;

        /// <summary> Digest of a byte buffer, as "sha256:&lt;hex&gt;". </summary>
        public static string Sha256(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            using var algorithm = SHA256.Create();
            return Format(algorithm.ComputeHash(data));
        }

        /// <summary> Digest of a stream, read from its current position to the end. Streams rather
        /// than a byte[] overload alone, so hashing a file never loads the whole track into memory
        /// just to read it once. </summary>
        public static string Sha256(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using var algorithm = SHA256.Create();
            return Format(algorithm.ComputeHash(stream));
        }

        /// <summary> True when a stored string is a well-formed sha256 value. Cheap enough to run on
        /// data that arrived from a file somebody else wrote. </summary>
        public static bool IsSha256(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            if (value.Length != Sha256Length) return false;
            if (!value.StartsWith(Sha256Prefix, StringComparison.Ordinal)) return false;

            for (var i = Sha256Prefix.Length; i < value.Length; i++)
            {
                var symbol = value[i];
                var isHex = (symbol >= '0' && symbol <= '9') || (symbol >= 'a' && symbol <= 'f');
                if (!isHex) return false;
            }
            return true;
        }

        /// <summary> Whether two stored digests describe the same bytes. Case-insensitive, since a
        /// value can arrive from a tool that prints uppercase hex. </summary>
        public static bool Matches(string left, string right)
            => !string.IsNullOrEmpty(left)
               && !string.IsNullOrEmpty(right)
               && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        private static string Format(byte[] hash)
        {
            var builder = new StringBuilder(Sha256Length);
            builder.Append(Sha256Prefix);
            foreach (var value in hash) builder.Append(value.ToString("x2"));
            return builder.ToString();
        }
    }
}
