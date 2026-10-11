using System;
using BH.SDK.Models.Resources;

namespace BH.SDK.Agent.Imaging
{
    // Image space: x right, y DOWN, row 0 on top, pixel (x, y) centred on (x + 0.5, y + 0.5). Fills sample
    // pixel centres; triangles follow the top-left rule, so two triangles sharing an edge paint it once.

    /// <summary> An RGBA8 image in memory and the few primitives the agent draws with. Every draw call
    /// blends source-over and clips to the image. </summary>
    public sealed class Raster
    {
        /// <summary> Bytes per pixel: R, G, B, A in that order. </summary>
        public const int BytesPerPixel = 4;

        /// <summary> Width in pixels. </summary>
        public int Width { get; }
        /// <summary> Height in pixels. </summary>
        public int Height { get; }
        /// <summary> The pixels, row by row from the top, <see cref="BytesPerPixel"/> bytes each. </summary>
        public byte[] Pixels { get; }

        /// <summary> A transparent black image. </summary>
        public Raster(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Must be positive.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Must be positive.");

            Width = width;
            Height = height;
            Pixels = new byte[checked(width * height * BytesPerPixel)];
        }

        /// <summary> An image over an existing RGBA8 buffer, which it keeps rather than copies. </summary>
        public Raster(int width, int height, byte[] pixels)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Must be positive.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Must be positive.");
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            if (pixels.Length != checked(width * height * BytesPerPixel))
                throw new ArgumentException($"Expected {width * height * BytesPerPixel} bytes, got {pixels.Length}.",
                    nameof(pixels));

            Width = width;
            Height = height;
            Pixels = pixels;
        }

        /// <summary> Whether the pixel lies inside the image. </summary>
        public bool Contains(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;

        /// <summary> The pixel at (x, y); throws outside the image. </summary>
        public Pixel GetPixel(int x, int y)
        {
            if (!Contains(x, y)) throw new ArgumentOutOfRangeException(nameof(x), $"({x}, {y}) is outside the image.");
            var i = (y * Width + x) * BytesPerPixel;
            return new Pixel(Pixels[i], Pixels[i + 1], Pixels[i + 2], Pixels[i + 3]);
        }

        /// <summary> Overwrites the pixel, alpha included; nothing outside the image. </summary>
        public void SetPixel(int x, int y, Pixel color)
        {
            if (!Contains(x, y)) return;
            Write(Pixels, (y * Width + x) * BytesPerPixel, color);
        }

        /// <summary> Blends the colour over the pixel; nothing outside the image. </summary>
        public void BlendPixel(int x, int y, Pixel color)
        {
            if (!Contains(x, y)) return;
            Blend(Pixels, (y * Width + x) * BytesPerPixel, color);
        }

        /// <summary> Overwrites every pixel. </summary>
        public void Clear(Pixel color)
        {
            for (var i = 0; i < Pixels.Length; i += BytesPerPixel)
                Write(Pixels, i, color);
        }

        /// <summary> How many pixels hold exactly this colour. </summary>
        public int CountPixels(Pixel color)
        {
            var count = 0;
            for (var i = 0; i < Pixels.Length; i += BytesPerPixel)
            {
                if (Pixels[i] == color.r && Pixels[i + 1] == color.g && Pixels[i + 2] == color.b &&
                    Pixels[i + 3] == color.a)
                    count++;
            }
            return count;
        }

        /// <summary> Fills every pixel whose centre lies in [x, x + width) by [y, y + height). </summary>
        public void FillRect(float x, float y, float width, float height, Pixel color)
        {
            if (!TryCentreRange(x, width, Width, out var x0, out var x1)) return;
            if (!TryCentreRange(y, height, Height, out var y0, out var y1)) return;

            for (var py = y0; py <= y1; py++)
            for (var px = x0; px <= x1; px++)
                Blend(Pixels, (py * Width + px) * BytesPerPixel, color);
        }

        /// <summary> A one-pixel outline around every pixel the rect touches, each pixel painted once.
        /// </summary>
        public void DrawRect(float x, float y, float width, float height, Pixel color)
        {
            if (!IsFinite(x) || !IsFinite(y) || !IsFinite(width) || !IsFinite(height)) return;

            TouchedRange(x, width, out var x0, out var x1);
            TouchedRange(y, height, out var y0, out var y1);

            var cx0 = Math.Max(x0, 0);
            var cx1 = Math.Min(x1, Width - 1);
            for (var px = cx0; px <= cx1; px++)
            {
                BlendPixel(px, y0, color);
                if (y1 != y0) BlendPixel(px, y1, color);
            }

            var cy0 = Math.Max(y0 + 1, 0);
            var cy1 = Math.Min(y1 - 1, Height - 1);
            for (var py = cy0; py <= cy1; py++)
            {
                BlendPixel(x0, py, color);
                if (x1 != x0) BlendPixel(x1, py, color);
            }
        }

        /// <summary> A one-pixel line between the pixels holding both ends, each pixel painted once.
        /// </summary>
        public void DrawLine(float x0, float y0, float x1, float y1, Pixel color)
        {
            if (!ClipSegment(ref x0, ref y0, ref x1, ref y1)) return;

            int ax = FloorClamped(x0, Width), ay = FloorClamped(y0, Height);
            int bx = FloorClamped(x1, Width), by = FloorClamped(y1, Height);
            int dx = Math.Abs(bx - ax), dy = -Math.Abs(by - ay);
            int sx = ax < bx ? 1 : -1, sy = ay < by ? 1 : -1;
            var error = dx + dy;

            while (true)
            {
                BlendPixel(ax, ay, color);
                if (ax == bx && ay == by) return;

                var doubled = 2 * error;
                if (doubled >= dy)
                {
                    error += dy;
                    ax += sx;
                }
                if (doubled <= dx)
                {
                    error += dx;
                    ay += sy;
                }
            }
        }

        /// <summary> Fills the triangle, whichever way it is wound; a degenerate one paints nothing.
        /// </summary>
        public void FillTriangle(float x0, float y0, float x1, float y1, float x2, float y2, Pixel color)
        {
            var area = Edge(x0, y0, x1, y1, x2, y2);
            if (area == 0f || !IsFinite(area)) return;
            if (area < 0f)
            {
                (x1, x2) = (x2, x1);
                (y1, y2) = (y2, y1);
            }

            var minX = Math.Max(0, (int)Math.Floor(Math.Min(x0, Math.Min(x1, x2))));
            var maxX = Math.Min(Width - 1, (int)Math.Ceiling(Math.Max(x0, Math.Max(x1, x2))));
            var minY = Math.Max(0, (int)Math.Floor(Math.Min(y0, Math.Min(y1, y2))));
            var maxY = Math.Min(Height - 1, (int)Math.Ceiling(Math.Max(y0, Math.Max(y1, y2))));

            for (var py = minY; py <= maxY; py++)
            {
                var cy = py + 0.5f;
                for (var px = minX; px <= maxX; px++)
                {
                    var cx = px + 0.5f;
                    if (!Inside(x1, y1, x2, y2, cx, cy) || !Inside(x2, y2, x0, y0, cx, cy) ||
                        !Inside(x0, y0, x1, y1, cx, cy))
                        continue;
                    Blend(Pixels, (py * Width + px) * BytesPerPixel, color);
                }
            }
        }

        /// <summary> Copies another image in with its top-left at (x, y), overwriting; clipped. </summary>
        public void Blit(Raster source, int x, int y)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var sx0 = Math.Max(0, -x);
            var sy0 = Math.Max(0, -y);
            var sx1 = Math.Min(source.Width, Width - x);
            var sy1 = Math.Min(source.Height, Height - y);
            if (sx0 >= sx1 || sy0 >= sy1) return;

            var rowBytes = (sx1 - sx0) * BytesPerPixel;
            for (var sy = sy0; sy < sy1; sy++)
            {
                var from = (sy * source.Width + sx0) * BytesPerPixel;
                var to = ((sy + y) * Width + sx0 + x) * BytesPerPixel;
                Buffer.BlockCopy(source.Pixels, from, Pixels, to, rowBytes);
            }
        }

        private static float Edge(float ax, float ay, float bx, float by, float px, float py)
            => (bx - ax) * (py - ay) - (by - ay) * (px - ax);

        // With y down and positive area, a top edge runs right along a row and a left edge runs up.
        private static bool Inside(float ax, float ay, float bx, float by, float px, float py)
        {
            var w = Edge(ax, ay, bx, by, px, py);
            if (w != 0f) return w > 0f;
            var dy = by - ay;
            return dy < 0f || (dy == 0f && bx > ax);
        }

        private static bool TryCentreRange(float start, float length, int limit, out int first, out int last)
        {
            first = last = 0;
            if (!IsFinite(start) || !IsFinite(length)) return false;
            if (length < 0f)
            {
                start += length;
                length = -length;
            }

            var from = Math.Ceiling(start - 0.5);
            var to = Math.Ceiling(start + length - 0.5) - 1.0;
            first = (int)Math.Max(from, 0.0);
            last = (int)Math.Min(to, limit - 1.0);
            return first <= last;
        }

        private static void TouchedRange(float start, float length, out int first, out int last)
        {
            if (length < 0f)
            {
                start += length;
                length = -length;
            }

            first = ToIntSaturated(Math.Floor(start));
            last = Math.Max(first, ToIntSaturated(Math.Ceiling((double)start + length) - 1.0));
        }

        private static int ToIntSaturated(double value)
            => value <= int.MinValue ? int.MinValue : value >= int.MaxValue ? int.MaxValue : (int)value;

        private static int FloorClamped(float value, int limit)
            => Math.Min(Math.Max((int)Math.Floor(value), 0), limit - 1);

        // Liang-Barsky against [0, Width] x [0, Height]; false when nothing of the segment is inside.
        private bool ClipSegment(ref float x0, ref float y0, ref float x1, ref float y1)
        {
            if (!IsFinite(x0) || !IsFinite(y0) || !IsFinite(x1) || !IsFinite(y1)) return false;

            float dx = x1 - x0, dy = y1 - y0;
            float t0 = 0f, t1 = 1f;
            if (!ClipEdge(-dx, x0, ref t0, ref t1) || !ClipEdge(dx, Width - x0, ref t0, ref t1) ||
                !ClipEdge(-dy, y0, ref t0, ref t1) || !ClipEdge(dy, Height - y0, ref t0, ref t1))
                return false;

            var sx = x0;
            var sy = y0;
            x0 = sx + t0 * dx;
            y0 = sy + t0 * dy;
            x1 = sx + t1 * dx;
            y1 = sy + t1 * dy;
            return true;
        }

        private static bool ClipEdge(float p, float q, ref float t0, ref float t1)
        {
            if (p == 0f) return q >= 0f;

            var t = q / p;
            if (p < 0f)
            {
                if (t > t1) return false;
                if (t > t0) t0 = t;
            }
            else
            {
                if (t < t0) return false;
                if (t < t1) t1 = t;
            }
            return true;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static void Write(byte[] pixels, int i, Pixel color)
        {
            pixels[i] = color.r;
            pixels[i + 1] = color.g;
            pixels[i + 2] = color.b;
            pixels[i + 3] = color.a;
        }

        private static void Blend(byte[] pixels, int i, Pixel color)
        {
            int sa = color.a;
            if (sa == 0) return;
            if (sa == 255)
            {
                Write(pixels, i, color);
                return;
            }

            int da = pixels[i + 3];
            var srcWeight = sa * 255;
            var dstWeight = da * (255 - sa);
            var total = srcWeight + dstWeight;
            var half = total / 2;

            pixels[i] = (byte)((color.r * srcWeight + pixels[i] * dstWeight + half) / total);
            pixels[i + 1] = (byte)((color.g * srcWeight + pixels[i + 1] * dstWeight + half) / total);
            pixels[i + 2] = (byte)((color.b * srcWeight + pixels[i + 2] * dstWeight + half) / total);
            pixels[i + 3] = (byte)((total + 127) / 255);
        }
    }
}
