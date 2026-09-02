using System;
using System.Buffers.Binary;
using System.Text;

namespace BH.SDK.Serialization.Blob
{
    // A REF STRUCT PASSED BY REF, and both halves of that are deliberate. Ref struct: it never
    // escapes to the heap, so writing a value costs a bounds check and a store, with no allocation
    // and no virtual call - which is the entire reason this exists rather than BinaryWriter, whose
    // every Write goes through a Stream. Passed by ref: the buffer grows, and a by-value copy would
    // leave the caller writing into the old array.
    //
    // LITTLE-ENDIAN AND FIXED WIDTH, no varints. A level's numbers are mostly floats and ids, which
    // a varint does not shrink, and the format has to be explainable to whoever writes the second
    // reader for it one day. Size is measured after; it is not what this is optimizing.

    /// <summary> Writes a .blob payload into a growable buffer. </summary>
    public ref struct BlobWriter
    {
        /// <summary> Length prefix for a null collection or string. Empty is a different value and
        /// has to stay one: an object's keyframe list is legitimately empty, and a round trip that
        /// confuses the two stops being Equals. </summary>
        public const int NullLength = -1;

        private byte[] _buffer;
        private int _position;

        public BlobWriter(int capacity)
        {
            _buffer = new byte[capacity < 16 ? 16 : capacity];
            _position = 0;
        }

        public int Length => _position;

        public void WriteByte(byte value)
        {
            Ensure(1);
            _buffer[_position++] = value;
        }

        public void WriteBool(bool value) => WriteByte(value ? (byte)1 : (byte)0);

        public void WriteShort(short value)
        {
            Ensure(2);
            BinaryPrimitives.WriteInt16LittleEndian(_buffer.AsSpan(_position), value);
            _position += 2;
        }

        public void WriteUShort(ushort value)
        {
            Ensure(2);
            BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(_position), value);
            _position += 2;
        }

        public void WriteInt(int value)
        {
            Ensure(4);
            BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_position), value);
            _position += 4;
        }

        public void WriteUInt(uint value)
        {
            Ensure(4);
            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_position), value);
            _position += 4;
        }

        public void WriteLong(long value)
        {
            Ensure(8);
            BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan(_position), value);
            _position += 8;
        }

        public void WriteULong(ulong value)
        {
            Ensure(8);
            BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(_position), value);
            _position += 8;
        }

        public void WriteFloat(float value) => WriteInt(BitConverter.SingleToInt32Bits(value));

        public void WriteDouble(double value) => WriteLong(BitConverter.DoubleToInt64Bits(value));

        /// <summary> UTC ticks. A statistics file travels between machines, so an instant is stored
        /// rather than a local time; the JSON side stores the same instant readably. </summary>
        public void WriteDateTime(DateTime value) => WriteLong(value.ToUniversalTime().Ticks);

        public void WriteGuid(Guid value)
        {
            Ensure(16);
            var span = _buffer.AsSpan(_position, 16);
#if NETSTANDARD2_1_OR_GREATER || NET
            value.TryWriteBytes(span);
#else
            value.ToByteArray().CopyTo(span);
#endif
            _position += 16;
        }

        /// <summary> A length prefix and UTF-8, with NullLength for a null string - which is not the
        /// same as an empty one anywhere in this format. </summary>
        public void WriteString(string value)
        {
            if (value is null)
            {
                WriteInt(NullLength);
                return;
            }

            var count = Encoding.UTF8.GetByteCount(value);
            WriteInt(count);
            Ensure(count);
            Encoding.UTF8.GetBytes(value, 0, value.Length, _buffer, _position);
            _position += count;
        }

        public void WriteBytes(byte[] value, int offset, int count)
        {
            Ensure(count);
            Buffer.BlockCopy(value, offset, _buffer, _position, count);
            _position += count;
        }

        /// <summary> Overwrites four bytes already written - how a length prefix is filled in once
        /// the thing it measures has been written. </summary>
        public void PatchInt(int position, int value)
            => BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(position), value);

        /// <summary> Reserves four bytes for a length and returns where they are. </summary>
        public int ReserveInt()
        {
            var position = _position;
            WriteInt(0);
            return position;
        }

        public byte[] ToArray()
        {
            var result = new byte[_position];
            Buffer.BlockCopy(_buffer, 0, result, 0, _position);
            return result;
        }

        /// <summary> The written bytes without copying them. Only valid while this writer lives. </summary>
        public ReadOnlySpan<byte> AsSpan() => _buffer.AsSpan(0, _position);

        private void Ensure(int count)
        {
            var required = _position + count;
            if (required <= _buffer.Length) return;

            var capacity = _buffer.Length;
            while (capacity < required) capacity *= 2;
            Array.Resize(ref _buffer, capacity);
        }
    }
}
