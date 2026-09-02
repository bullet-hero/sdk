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

        public PixelTexture(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new Pixel[width * height];
        }
        public PixelTexture(int width, int height, Pixel[] pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
        }

        public object Clone() => Copy();
        public PixelTexture Copy() => new(Width, Height, Pixels.CopyArrayUnmanaged());

        public override bool Equals(object obj) => obj is PixelTexture value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Width, Height, Pixels.GetArrayHashCode());

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