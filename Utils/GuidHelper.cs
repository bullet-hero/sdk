using System;
using System.Buffers.Binary;

namespace BH.SDK.Utils
{
    public static class GuidHelper
    {
        /// <summary>
        /// Creates a Guid with the given int placed in the last 4 bytes. The first 12 bytes are zero.
        /// </summary>
        public static Guid FromIntAtEnd(int value) => new(0, 0, 0,0, 0, 0, 0,
                (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);
        
        /// <summary>
        /// Extracts the int back from the last 4 bytes of the Guid.
        /// </summary>
        public static int ToIntFromEnd(Guid guid)
        {
            Span<byte> bytes = stackalloc byte[16];
            guid.TryWriteBytes(bytes);
            return BinaryPrimitives.ReadInt32BigEndian(bytes.Slice(12, 4));
        }
    }
}