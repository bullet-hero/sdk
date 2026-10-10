using System;
using BH.SDK.Utils;

namespace BH.SDK.Serialization.Blob
{
    // The header is checked in ONE order and the order is the design: magic, codec generation,
    // declared length against the real one, hash - nothing is decoded or allocated before that.
    // Four refusals, so the UI can tell "damaged" from "from a newer build". Docs/SERIALIZATION.md.

    /// <summary> The .blob file header. </summary>
    public static class BlobFormat
    {
        /// <summary> 'B','H','B','O'. </summary>
        public const uint Magic = 0x4F42_4842;

        // The CODEC generation: which byte layout a .blob uses. NOT ModelGenerationAttribute.Generation
        // (a domain's shape, inside each envelope); moving this invalidates every .blob. It seeds the
        // hash, so a file from another codec is refused by the header.

        /// <summary> The CODEC's generation, not any domain's. It moves when the encoding itself
        /// changes shape - which per-domain envelopes are designed to make unnecessary. </summary>
        public const ushort Generation = 1;

        /// <summary> Bit 0: the payload hash is present. Every other bit is reserved and must be
        /// zero, so a future flag makes an old reader refuse rather than misread. </summary>
        public const ushort FlagHashed = 1;

        /// <summary> magic + generation + flags + length + hash. </summary>
        public const int HeaderLength = 4 + 2 + 2 + 8 + 8;

        /// <summary> Lays the header down in the order a reader checks it. </summary>
        public static void WriteHeader(ref BlobWriter writer, int payloadLength, ulong hash)
        {
            writer.WriteUInt(Magic);
            writer.WriteUShort(Generation);
            writer.WriteUShort(FlagHashed);
            writer.WriteLong(payloadLength);
            writer.WriteULong(hash);
        }

        /// <summary> Validates a file's header and hands back where its payload starts. Every
        /// failure is a BlobFormatException naming which of the four checks it failed. </summary>
        public static int ReadHeader(byte[] data, out int payloadLength)
        {
            payloadLength = 0;
            if (data is null || data.Length < HeaderLength)
                throw new BlobFormatException("not a .blob: shorter than its own header");

            var reader = new BlobReader(data, 0, HeaderLength);
            if (reader.ReadUInt() != Magic)
                throw new BlobFormatException("not a .blob: wrong magic");

            var generation = reader.ReadUShort();
            if (generation != Generation)
                throw new BlobFormatException(
                    $"this .blob was written by codec generation {generation}, this build reads {Generation}");

            var flags = reader.ReadUShort();
            if ((flags & ~FlagHashed) != 0)
                throw new BlobFormatException($"this .blob sets flags {flags} this build does not know");

            var length = reader.ReadLong();
            if (length < 0 || HeaderLength + length != data.Length)
                throw new BlobFormatException(
                    $"this .blob declares {length} payload bytes and carries {data.Length - HeaderLength}");

            var hash = reader.ReadULong();
            if ((flags & FlagHashed) != 0)
            {
                var actual = XxHash64.Compute(data, HeaderLength, (int)length, Generation);
                if (actual != hash)
                    throw new BlobFormatException("this .blob is damaged: its contents do not match its hash");
            }

            payloadLength = (int)length;
            return HeaderLength;
        }

        /// <summary> The hash a payload should carry. Seeded with the generation so a payload can
        /// never validate against a header from a different codec. </summary>
        public static ulong Hash(ReadOnlySpan<byte> payload) => XxHash64.Compute(payload, Generation);
    }
}