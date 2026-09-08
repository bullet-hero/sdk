using System;
using BH.SDK.Utils;

namespace BH.SDK.Serialization.Blob
{
    // THE FILE HEADER, AND THE ORDER IT IS CHECKED IN IS THE DESIGN. magic, then generation, then
    // the declared length against the real one, then the hash - and only then is a single byte
    // decoded. Nothing is allocated on the strength of anything the file says until the file has
    // been shown to be whole, which is the difference between a format and the level cache this
    // took its encoding from: a cache's payload was its own, a format's payload is a stranger's.
    //
    // FOUR REFUSALS, NOT ONE, and they are worth telling apart on screen: "this file is damaged"
    // and "this file is from a newer build" ask completely different things of a player.

    /// <summary> The .blob file header. </summary>
    public static class BlobFormat
    {
        /// <summary> 'B','H','B','L'. </summary>
        public const uint Magic = 0x4C42_4842;

        // TWO DIFFERENT THINGS ARE CALLED A GENERATION IN THIS SUBSYSTEM, and only one of them is
        // here. THIS one belongs to the ENCODING: which byte layout a .blob is written in. The other
        // is ModelGenerationAttribute.Generation, which belongs to a DOMAIN - what shape its model
        // was when the file was written - and travels inside each envelope. A domain moving does not
        // move this; this moving invalidates every .blob whatever its domains say.
        //
        // It went 1 -> 2 when a domain envelope stopped carrying two ushorts (major, minor) and
        // started carrying one int. Since it also seeds the hash below, an older file is refused by
        // the header rather than misread deeper in.

        /// <summary> The CODEC's generation, not any domain's. It moves when the encoding itself
        /// changes shape - which per-domain envelopes are designed to make unnecessary. </summary>
        public const ushort Generation = 2;

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
