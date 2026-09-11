using System;
using System.Buffers.Binary;
using System.Text;

namespace BH.SDK.Serialization.Blob
{
    // EVERY READ IS BOUNDS-CHECKED AND EVERY FAILURE IS THE SAME FAILURE. A .blob comes off a disk
    // that can lie - a truncated download, a half-written save, a file from a future build - and the
    // reader's whole job on a bad one is to stop, not to guess. So it throws BlobFormatException and
    // exactly one place catches it: the envelope, which turns it into "this file could not be read"
    // and lets the caller fall back to the .json beside it. That is why the generated bodies below
    // carry no error handling at all - there is nothing for them to do about it.
    //
    // A HOSTILE COUNT IS THE ONE ATTACK THIS FORMAT HAS. `new List<T>(count)` on a length the file
    // chose is an allocation of the file's choosing, which the level cache could ignore because its
    // payload was self-produced and a format cannot. Remaining is what every collection checks
    // against before it allocates.

    /// <summary> A .blob payload could not be read. Always caught at the envelope. </summary>
    public sealed class BlobFormatException : Exception
    {
        /// <summary> Takes what to tell the caller; the envelope turns it into "this file could not be read". </summary>
        public BlobFormatException(string message) : base(message)
        {
        }

        /// <summary> The same, keeping the refusal a degraded read ran into before deciding it was damage. </summary>
        public BlobFormatException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    /// <summary> Reads a .blob payload out of a buffer. </summary>
    public ref struct BlobReader
    {
        private readonly byte[] _buffer;
        private readonly int _end;
        private int _position;

        /// <summary> Reads a window of a buffer, so an envelope can hand over its payload without copying it. </summary>
        public BlobReader(byte[] buffer, int offset, int length)
        {
            _buffer = buffer ?? throw new BlobFormatException("no payload");
            if (offset < 0 || length < 0 || offset + length > buffer.Length)
                throw new BlobFormatException("payload does not fit its buffer");
            _position = offset;
            _end = offset + length;
        }

        /// <summary> Reads a whole buffer. </summary>
        public BlobReader(byte[] buffer) : this(buffer, 0, buffer?.Length ?? 0)
        {
        }

        /// <summary> Bytes left. Every collection compares its own count against this BEFORE
        /// allocating, so a corrupt length costs an exception rather than a gigabyte. </summary>
        public int Remaining => _end - _position;

        /// <summary> How far into the window the reader has got. </summary>
        public int Position => _position;

        /// <summary> Reads a single byte. </summary>
        public byte ReadByte()
        {
            Ensure(1);
            return _buffer[_position++];
        }

        /// <summary> Reads one byte, where anything but zero is true. </summary>
        public bool ReadBool() => ReadByte() != 0;

        /// <summary> Reads a little-endian 16-bit integer. </summary>
        public short ReadShort()
        {
            Ensure(2);
            var value = BinaryPrimitives.ReadInt16LittleEndian(_buffer.AsSpan(_position));
            _position += 2;
            return value;
        }

        /// <summary> Reads a little-endian unsigned 16-bit integer. </summary>
        public ushort ReadUShort()
        {
            Ensure(2);
            var value = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.AsSpan(_position));
            _position += 2;
            return value;
        }

        /// <summary> Reads a little-endian 32-bit integer. </summary>
        public int ReadInt()
        {
            Ensure(4);
            var value = BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(_position));
            _position += 4;
            return value;
        }

        /// <summary> Reads a little-endian unsigned 32-bit integer. </summary>
        public uint ReadUInt()
        {
            Ensure(4);
            var value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(_position));
            _position += 4;
            return value;
        }

        /// <summary> Reads a little-endian 64-bit integer. </summary>
        public long ReadLong()
        {
            Ensure(8);
            var value = BinaryPrimitives.ReadInt64LittleEndian(_buffer.AsSpan(_position));
            _position += 8;
            return value;
        }

        /// <summary> Reads a little-endian unsigned 64-bit integer. </summary>
        public ulong ReadULong()
        {
            Ensure(8);
            var value = BinaryPrimitives.ReadUInt64LittleEndian(_buffer.AsSpan(_position));
            _position += 8;
            return value;
        }

        /// <summary> Reads a float as the 32-bit pattern it is, so no decimal formatting is involved. </summary>
        public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt());

        /// <summary> Reads a double as its 64-bit pattern. </summary>
        public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadLong());

        /// <summary> Read back as ticks plus its kind, so a UTC stamp does not come back as local time. </summary>
        public DateTime ReadDateTime()
        {
            var ticks = ReadLong();
            if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
                throw new BlobFormatException("timestamp out of range");
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        /// <summary> Sixteen raw bytes, never the textual form. </summary>
        public Guid ReadGuid()
        {
            Ensure(16);
            var span = _buffer.AsSpan(_position, 16);
            _position += 16;
#if NETSTANDARD2_1_OR_GREATER || NET
            return new Guid(span);
#else
            return new Guid(span.ToArray());
#endif
        }

        /// <summary> UTF-8 behind a length prefix, where <see cref="BlobWriter.NullLength"/> means null rather than empty. </summary>
        public string ReadString()
        {
            var count = ReadInt();
            if (count == BlobWriter.NullLength) return null;
            if (count < 0) throw new BlobFormatException("negative string length");
            Ensure(count);
            var value = Encoding.UTF8.GetString(_buffer, _position, count);
            _position += count;
            return value;
        }

        /// <summary> A collection's length prefix, refused before it can be believed. Returns
        /// NullLength for a null collection. `stride` is the smallest number of bytes one element
        /// can occupy, which is what makes "this file claims a million items" cheap to disprove. </summary>
        public int ReadCount(int stride)
        {
            var count = ReadInt();
            if (count == BlobWriter.NullLength) return BlobWriter.NullLength;
            if (count < 0) throw new BlobFormatException("negative collection length");
            if ((long)count * stride > Remaining)
                throw new BlobFormatException($"collection of {count} does not fit in {Remaining} bytes");
            return count;
        }

        /// <summary> A fresh array of exactly <paramref name="count"/> bytes, refused when the window is shorter. </summary>
        public byte[] ReadBytes(int count)
        {
            Ensure(count);
            var value = new byte[count];
            Buffer.BlockCopy(_buffer, _position, value, 0, count);
            _position += count;
            return value;
        }

        /// <summary> Steps over a payload whose shape this build does not know - the whole point of
        /// length-prefixing an envelope. </summary>
        public void Skip(int count)
        {
            Ensure(count);
            _position += count;
        }

        private void Ensure(int count)
        {
            if (count < 0 || _position + count > _end)
                throw new BlobFormatException($"read of {count} past the end of the payload");
        }
    }
}
