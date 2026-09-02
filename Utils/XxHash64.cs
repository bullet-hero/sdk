using System;

namespace BH.SDK.Utils
{
    // WHAT IS BEING CHECKED IS CORRUPTION, NOT FORGERY, and that decides everything about this
    // file. A .blob is a player's level: an interrupted download, a file half-written when the
    // process died, a failing SD card. None of those are adversarial, and the one thing that IS -
    // a level someone tampered with - is already answered where it belongs: a protected level goes
    // through OpenPGP SEIPD, which authenticates its own payload. A cryptographic digest here would
    // cost several times as much and buy a guarantee the format does not need.
    //
    // 64 BITS RATHER THAN CRC32's 32, for the same money. Levels reach 18 MB and sit on disk for
    // years, and a 32-bit check leaves a one-in-four-billion chance of accepting a damaged one -
    // small until it is multiplied by every level of every player over the life of the game.
    //
    // Written out by hand because netstandard2.1 has no System.IO.Hashing, and one algorithm in
    // eighty lines is not worth a NuGet dependency in a library that ships inside a game.

    /// <summary> xxHash64, the reference algorithm. Deterministic across platforms and runtimes. </summary>
    public static class XxHash64
    {
        private const ulong Prime1 = 11400714785074694791UL;
        private const ulong Prime2 = 14029467366897019727UL;
        private const ulong Prime3 = 1609587929392839161UL;
        private const ulong Prime4 = 9650029242287828579UL;
        private const ulong Prime5 = 2870177450012600261UL;

        public static ulong Compute(byte[] data, ulong seed = 0)
            => data is null ? Compute(ReadOnlySpan<byte>.Empty, seed) : Compute(data.AsSpan(), seed);

        public static ulong Compute(byte[] data, int offset, int length, ulong seed = 0)
            => Compute(data.AsSpan(offset, length), seed);

        public static ulong Compute(ReadOnlySpan<byte> data, ulong seed = 0)
        {
            var length = data.Length;
            ulong hash;
            var index = 0;

            if (length >= 32)
            {
                var v1 = seed + Prime1 + Prime2;
                var v2 = seed + Prime2;
                var v3 = seed;
                var v4 = seed - Prime1;

                do
                {
                    v1 = Round(v1, Read64(data, index)); index += 8;
                    v2 = Round(v2, Read64(data, index)); index += 8;
                    v3 = Round(v3, Read64(data, index)); index += 8;
                    v4 = Round(v4, Read64(data, index)); index += 8;
                }
                while (index <= length - 32);

                hash = RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
                hash = MergeRound(hash, v1);
                hash = MergeRound(hash, v2);
                hash = MergeRound(hash, v3);
                hash = MergeRound(hash, v4);
            }
            else
            {
                hash = seed + Prime5;
            }

            hash += (ulong)length;

            while (index <= length - 8)
            {
                hash ^= Round(0, Read64(data, index));
                hash = RotateLeft(hash, 27) * Prime1 + Prime4;
                index += 8;
            }

            if (index <= length - 4)
            {
                hash ^= Read32(data, index) * Prime1;
                hash = RotateLeft(hash, 23) * Prime2 + Prime3;
                index += 4;
            }

            while (index < length)
            {
                hash ^= data[index] * Prime5;
                hash = RotateLeft(hash, 11) * Prime1;
                index++;
            }

            hash ^= hash >> 33;
            hash *= Prime2;
            hash ^= hash >> 29;
            hash *= Prime3;
            hash ^= hash >> 32;
            return hash;
        }

        private static ulong Round(ulong accumulator, ulong input)
        {
            accumulator += input * Prime2;
            accumulator = RotateLeft(accumulator, 31);
            accumulator *= Prime1;
            return accumulator;
        }

        private static ulong MergeRound(ulong hash, ulong value)
        {
            hash ^= Round(0, value);
            hash = hash * Prime1 + Prime4;
            return hash;
        }

        private static ulong RotateLeft(ulong value, int count) => (value << count) | (value >> (64 - count));

        private static ulong Read64(ReadOnlySpan<byte> data, int index)
            => (ulong)data[index]
               | ((ulong)data[index + 1] << 8)
               | ((ulong)data[index + 2] << 16)
               | ((ulong)data[index + 3] << 24)
               | ((ulong)data[index + 4] << 32)
               | ((ulong)data[index + 5] << 40)
               | ((ulong)data[index + 6] << 48)
               | ((ulong)data[index + 7] << 56);

        private static ulong Read32(ReadOnlySpan<byte> data, int index)
            => data[index]
               | ((ulong)data[index + 1] << 8)
               | ((ulong)data[index + 2] << 16)
               | ((ulong)data[index + 3] << 24);
    }
}
