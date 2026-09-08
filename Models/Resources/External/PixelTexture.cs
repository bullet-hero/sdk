using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Utils;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Resources
{
    /// <summary>
    /// A raw image in memory - the Unity-free counterpart of Texture2D, used by tooling that
    /// generates or reads image data (see Generators/, gen_texture_objects). Not part of the level
    /// format: a level stores a TextureResource pointing at a file, never pixels.
    /// </summary>
    public class PixelTexture : ICopyable<PixelTexture>, IEquatable<PixelTexture>
    {
        /// <summary> Row length in pixels. </summary>
        public int Width;
        /// <summary> Number of rows. </summary>
        public int Height;
        /// <summary> Pixels in one flat row-major array of Width*Height - indexing is the caller's
        /// job (see DimensionalIndexer2). </summary>
        public Pixel[] Pixels;

        /// <summary> Built from its width and height. </summary>
        public PixelTexture(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new Pixel[width * height];
        }
        /// <summary> Built from its width, height and pixels. </summary>
        public PixelTexture(int width, int height, Pixel[] pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
        }

        /// <summary> The untyped spelling of <c>Copy</c>. </summary>
        public object Clone() => Copy();
        /// <summary> A deep copy, sharing nothing mutable with this one. </summary>
        public PixelTexture Copy() => new(Width, Height, Pixels.CopyArrayUnmanaged());

        /// <summary> The same, boxed. </summary>
        public override bool Equals(object obj) => obj is PixelTexture value && Equals(value);
        /// <summary> Matches the equality above. </summary>
        public override int GetHashCode() => HashCode.Combine(Width, Height, Pixels.GetArrayHashCode());

        /// <summary> Member by member. </summary>
        public bool Equals(PixelTexture other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Width.Equals(other.Width)
                         && Height.Equals(other.Height)
                         && Pixels.ArrayEquals(other.Pixels);
            return result;
        }
    }
}