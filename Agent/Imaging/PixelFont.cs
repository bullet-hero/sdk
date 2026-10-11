using System;
using System.Collections.Generic;
using BH.SDK.Models.Resources;

namespace BH.SDK.Agent.Imaging
{
    /// <summary> A 5x7 bitmap font for labels on agent images: digits, A-Z, space and <c># - . : +</c>.
    /// Lower case draws as upper case; any other character draws as a space. </summary>
    public static class PixelFont
    {
        /// <summary> Glyph width in font pixels. </summary>
        public const int GlyphWidth = 5;
        /// <summary> Glyph height in font pixels. </summary>
        public const int GlyphHeight = 7;
        /// <summary> Blank columns between two glyphs, in font pixels. </summary>
        public const int Spacing = 1;
        /// <summary> Horizontal step from one glyph to the next, in font pixels. </summary>
        public const int Advance = GlyphWidth + Spacing;

        // One row per word, top to bottom, '1' = lit; the leftmost character is the leftmost column.
        private static readonly Dictionary<char, byte[]> Glyphs = Build(new[]
        {
            ('0', "01110 10001 10011 10101 11001 10001 01110"),
            ('1', "00100 01100 00100 00100 00100 00100 01110"),
            ('2', "01110 10001 00001 00010 00100 01000 11111"),
            ('3', "11111 00010 00100 00010 00001 10001 01110"),
            ('4', "00010 00110 01010 10010 11111 00010 00010"),
            ('5', "11111 10000 11110 00001 00001 10001 01110"),
            ('6', "00110 01000 10000 11110 10001 10001 01110"),
            ('7', "11111 00001 00010 00100 01000 01000 01000"),
            ('8', "01110 10001 10001 01110 10001 10001 01110"),
            ('9', "01110 10001 10001 01111 00001 00010 01100"),
            ('A', "01110 10001 10001 11111 10001 10001 10001"),
            ('B', "11110 10001 10001 11110 10001 10001 11110"),
            ('C', "01110 10001 10000 10000 10000 10001 01110"),
            ('D', "11100 10010 10001 10001 10001 10010 11100"),
            ('E', "11111 10000 10000 11110 10000 10000 11111"),
            ('F', "11111 10000 10000 11110 10000 10000 10000"),
            ('G', "01110 10001 10000 10111 10001 10001 01111"),
            ('H', "10001 10001 10001 11111 10001 10001 10001"),
            ('I', "01110 00100 00100 00100 00100 00100 01110"),
            ('J', "00111 00010 00010 00010 00010 10010 01100"),
            ('K', "10001 10010 10100 11000 10100 10010 10001"),
            ('L', "10000 10000 10000 10000 10000 10000 11111"),
            ('M', "10001 11011 10101 10101 10001 10001 10001"),
            ('N', "10001 10001 11001 10101 10011 10001 10001"),
            ('O', "01110 10001 10001 10001 10001 10001 01110"),
            ('P', "11110 10001 10001 11110 10000 10000 10000"),
            ('Q', "01110 10001 10001 10001 10101 10010 01101"),
            ('R', "11110 10001 10001 11110 10100 10010 10001"),
            ('S', "01111 10000 10000 01110 00001 00001 11110"),
            ('T', "11111 00100 00100 00100 00100 00100 00100"),
            ('U', "10001 10001 10001 10001 10001 10001 01110"),
            ('V', "10001 10001 10001 10001 10001 01010 00100"),
            ('W', "10001 10001 10001 10101 10101 10101 01010"),
            ('X', "10001 10001 01010 00100 01010 10001 10001"),
            ('Y', "10001 10001 10001 01010 00100 00100 00100"),
            ('Z', "11111 00001 00010 00100 01000 10000 11111"),
            ('#', "01010 01010 11111 01010 11111 01010 01010"),
            ('-', "00000 00000 00000 11111 00000 00000 00000"),
            ('.', "00000 00000 00000 00000 00000 01100 01100"),
            (':', "00000 01100 01100 00000 01100 01100 00000"),
            ('+', "00000 00100 00100 11111 00100 00100 00000"),
            (' ', "00000 00000 00000 00000 00000 00000 00000"),
        });

        /// <summary> Whether the character has a glyph of its own (case-insensitive). </summary>
        public static bool Supports(char c) => Glyphs.ContainsKey(char.ToUpperInvariant(c));

        /// <summary> Width of the text in image pixels at this scale; zero for an empty string. </summary>
        public static int MeasureWidth(string text, int scale = 1)
        {
            ValidateScale(scale);
            if (string.IsNullOrEmpty(text)) return 0;
            return (text.Length * Advance - Spacing) * scale;
        }

        /// <summary> Height of one line in image pixels at this scale. </summary>
        public static int MeasureHeight(int scale = 1)
        {
            ValidateScale(scale);
            return GlyphHeight * scale;
        }

        /// <summary> How many characters fit in <paramref name="width"/> image pixels at this scale.
        /// </summary>
        public static int FitCharacters(int width, int scale = 1)
        {
            ValidateScale(scale);
            return Math.Max(0, (width + Spacing * scale) / (Advance * scale));
        }

        /// <summary> Draws the text with its top-left at (x, y), each font pixel a scale x scale square,
        /// blended and clipped to the image. </summary>
        public static void Draw(Raster raster, string text, int x, int y, Pixel color, int scale = 1)
        {
            if (raster == null) throw new ArgumentNullException(nameof(raster));
            ValidateScale(scale);
            if (string.IsNullOrEmpty(text)) return;

            for (var c = 0; c < text.Length; c++)
            {
                var originX = x + c * Advance * scale;
                if (!Glyphs.TryGetValue(char.ToUpperInvariant(text[c]), out var rows)) continue;

                for (var row = 0; row < GlyphHeight; row++)
                {
                    var bits = rows[row];
                    if (bits == 0) continue;

                    for (var column = 0; column < GlyphWidth; column++)
                    {
                        if ((bits & (1 << (GlyphWidth - 1 - column))) == 0) continue;
                        FillCell(raster, originX + column * scale, y + row * scale, scale, color);
                    }
                }
            }
        }

        private static void FillCell(Raster raster, int x, int y, int scale, Pixel color)
        {
            for (var py = y; py < y + scale; py++)
            for (var px = x; px < x + scale; px++)
                raster.BlendPixel(px, py, color);
        }

        private static void ValidateScale(int scale)
        {
            if (scale < 1) throw new ArgumentOutOfRangeException(nameof(scale), scale, "Must be at least 1.");
        }

        private static Dictionary<char, byte[]> Build((char Glyph, string Rows)[] table)
        {
            var glyphs = new Dictionary<char, byte[]>(table.Length);
            foreach (var (glyph, text) in table)
            {
                var words = text.Split(' ');
                var rows = new byte[GlyphHeight];
                for (var row = 0; row < GlyphHeight; row++)
                    rows[row] = Convert.ToByte(words[row], 2);
                glyphs.Add(glyph, rows);
            }
            return glyphs;
        }
    }
}
