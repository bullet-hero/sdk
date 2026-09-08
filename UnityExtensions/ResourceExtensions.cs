using BH.SDK.Models.Resources;
using UnityEngine;

namespace BH.SDK
{
    /// <summary> Conversions between the SDK's engine-free image types and Unity's own. </summary>
    public static class ResourceExtensions
    {
        /// <summary> The SDK's own pixel as Unity's. </summary>
        public static Color32 ToColor32(this Pixel pixel) => new(pixel.r, pixel.g, pixel.b, pixel.a);

        /// <summary> The other way round. </summary>
        public static Pixel ToPixel(this Color32 color) => new(color.r, color.g, color.b, color.a);

        /// <summary> A Unity texture read back into the engine-free image the SDK works on. </summary>
        public static PixelTexture ToPixelTexture(this Texture2D texture)
        {
            var pixels = texture.GetPixelData<Pixel>(0);
            var pixelTexture = new PixelTexture(texture.width, texture.height, pixels.ToArray());
            pixels.Dispose();
            return pixelTexture;
        }

        /// <summary> The other way round. </summary>
        public static Texture2D ToTexture2D(this PixelTexture pixelTexture)
        {
            var texture = new Texture2D(pixelTexture.Width, pixelTexture.Height);
            texture.SetPixelData(pixelTexture.Pixels, 0);
            texture.Apply();
            return texture;
        }
    }
}