// Copy of UnityEngine.Color32 for no Unity uses

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BH.SDK.Models.Interfaces;
using BH.SDK.Utils;

namespace BH.SDK.Models.Resources
{
    /// <summary>
    /// One 32-bit RGBA pixel - a Unity-free stand-in for Color32, since the core SDK must not
    /// reference UnityEngine. Byte channels here, unlike the float-based Color4Value used for
    /// authored colors: this is image data, not a value someone edits.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct Pixel : IEquatable<Pixel>, IFormattable, ICopyable<Pixel>
    {
        /// <summary> All four channels as one int - the same four bytes seen whole, thanks to the
        /// explicit layout. Lets a pixel be compared or copied in a single operation. </summary>
        [FieldOffset(0)] public int rgba;

        /// <summary> Red channel, 0-255. </summary>
        [FieldOffset(0)] public byte r;
        /// <summary> Green channel, 0-255. </summary>
        [FieldOffset(1)] public byte g;
        /// <summary> Blue channel, 0-255. </summary>
        [FieldOffset(2)] public byte b;
        /// <summary> Alpha channel, 0-255. </summary>
        [FieldOffset(3)] public byte a;

        /// <summary> Built from its r, g, b and a. </summary>
        public Pixel(byte r, byte g, byte b, byte a)
        {
            this = default;
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
        /// <summary> Built from its rgba. </summary>
        public Pixel(int rgba)
        {
            this = default;
            this.rgba = rgba;
        }

        /// <summary> Blends two pixels, clamping the factor to 0-1. </summary>
        public static Pixel Lerp(in Pixel a, in Pixel b, float t)
        {
            t = BHSDKMath.Clamp01(t);
            return new Pixel
            {
                r = (byte)(a.r + (b.r - a.r) * t),
                g = (byte)(a.g + (b.g - a.g) * t),
                b = (byte)(a.b + (b.b - a.b) * t),
                a = (byte)(a.a + (b.a - a.a) * t)
            };
        }
        /// <summary> The same without that clamp. </summary>
        public static Pixel LerpUnclamped(Pixel a, Pixel b, float t)
        {
            return new Pixel
            {
                r = (byte)(a.r + (b.r - a.r) * t),
                g = (byte)(a.g + (b.g - a.g) * t),
                b = (byte)(a.b + (b.b - a.b) * t),
                a = (byte)(a.a + (b.a - a.a) * t)
            };
        }

        /// <summary> One channel by index, in R, G, B, A order. </summary>
        public byte this[int index]
        {
            readonly get
            {
                return index switch
                {
                    0 => r,
                    1 => g,
                    2 => b,
                    3 => a,
                    _ => throw new IndexOutOfRangeException($"Invalid Color32 index({index.ToString()})!")
                };
            }
            set
            {
                switch (index)
                {
                    case 0: r = value; break;
                    case 1: g = value; break;
                    case 2: b = value; break;
                    case 3: a = value; break;
                    default: throw new IndexOutOfRangeException($"Invalid Color32 index({index.ToString()})!");
                }
            }
        }

        /// <summary> Matches the equality above. </summary>
        public readonly override int GetHashCode() => rgba.GetHashCode();

        /// <summary> The untyped spelling of <c>Copy</c>. </summary>
        public object Clone() => Copy();
        /// <summary> A deep copy, sharing nothing mutable with this one. </summary>
        public Pixel Copy() => new(r, g, b, a);

        /// <summary> The same, boxed. </summary>
        public readonly override bool Equals(object other) => other is Pixel value && Equals(value);
        /// <summary> Member by member. </summary>
        public readonly bool Equals(Pixel other) => rgba == other.rgba;
        
        /// <summary> One line, for a log. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString() => ToString(null, null);
        
        /// <summary> One line, with the channels formatted as asked. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string ToString(string format) => ToString(format, null);
        
        /// <summary> The same, honouring a culture. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            formatProvider ??= CultureInfo.InvariantCulture.NumberFormat;
            return $"RGBA({r.ToString(format, formatProvider)}, {g.ToString(format, formatProvider)}, {b.ToString(format, formatProvider)}, {a.ToString(format, formatProvider)})";
        }
    }
}