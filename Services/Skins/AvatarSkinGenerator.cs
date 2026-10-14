using System;
using System.Collections.Generic;
using System.Text;
using BH.SDK.Models;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Utils;
using BH.SDK.Validations;

namespace BH.SDK.Services.Skins
{
    // Identicon-style: a random left half mirrored to the right, so every result is symmetric and, once
    // accepted, one piece. Its own SplitMix generator: a seed means the same skin on every runtime.

    /// <summary> Builds avatar skin geometry presets from a seed: pure and deterministic. </summary>
    public static class AvatarSkinGenerator
    {
        /// <summary> Every style a preset can take, by name. </summary>
        public static readonly string[] Styles = { "blocks", "dots", "hex", "gems", "rings", "frame", "smooth", "tri" };

        private const int MaxTries = 32;
        private const float SquareGridChance = 0.7f;
        private const float VerticalMirrorChance = 0.3f;
        private const float MinFill = 0.55f;
        private const float MaxFill = 0.85f;
        private const float MinFilledShare = 0.4f;

        private const int Rim = 1;
        private const int Inner = 2;
        private const int Core = 3;

        private const string Square = "Square";
        private const string Circle = "Circle";
        private const string CircleRing = "Circle_T4";
        private const string SquareRing = "Square_T4";
        private const string Hexagon = "Hexagon";
        private const string Triangle = "Triangle";
        private const string RightTriangle = "RightTriangle";

        /// <summary> The preset seed <paramref name="seed"/> names. </summary>
        public static AvatarSkin Generate(uint seed)
        {
            var random = new SplitMix(seed);
            var style = Styles[random.NextInt(Styles.Length)];
            var filled = DrawMask(ref random, out var rows, out var columns);
            var bands = Bands(filled, rows, columns);

            var skin = new AvatarSkin
            {
                Id = PresetId(seed),
                Name = style + " " + seed,
                Columns = columns,
                Rows = rows,
                Mask = 0UL,
                Parts = new List<AvatarSkinPart>(),
            };

            // filled[] reads top row first, as it is drawn; the mask counts rows from the bottom.
            var parts = new Dictionary<string, int>(StringComparer.Ordinal);
            var colorCount = 1;
            for (var y = 0; y < rows; y++)
            for (var column = 0; column < columns; column++)
            {
                var row = rows - 1 - y;
                if (!filled[row, column]) continue;

                var part = PartFor(style, bands[row, column], filled, rows, columns, row, column);
                var key = part.Shape.value + "|" + part.Color + "|" + part.Rotation + "|" + part.Scale;
                if (!parts.TryGetValue(key, out var index))
                {
                    index = skin.Parts.Count;
                    parts[key] = index;
                    skin.Parts.Add(part);
                }

                skin.Mask |= AvatarSkinMask.Bit(column, y);
                skin.Cells.Add(index);
                colorCount = Math.Max(colorCount, part.Color + 1);
            }

            skin.Colors = new List<Color4Value>(colorCount);
            for (var i = 0; i < colorCount; i++) skin.Colors.Add(AvatarSkinDefaults.Body());
            return skin;
        }

        /// <summary> The id preset <paramref name="seed"/> carries: built-in, so never a file's. </summary>
        public static AvatarSkinId PresetId(uint seed)
        {
            var random = new SplitMix(seed ^ 0xA5A5_5A5Au);
            var bytes = new byte[16];
            for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)random.NextInt(256);
            bytes[7] &= 0x0F;
            bytes[15] |= 0x80;
            return new AvatarSkinId(new Guid(bytes));
        }

        /// <summary> <paramref name="count"/> presets from consecutive seeds starting at
        /// <paramref name="startSeed"/>, skipping any whose geometry an earlier one already has. </summary>
        public static List<AvatarSkin> GenerateMany(int count, uint startSeed)
        {
            var result = new List<AvatarSkin>(count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var seed = startSeed;

            for (var attempt = 0; result.Count < count && attempt < count * 20; attempt++, seed++)
            {
                var skin = Generate(seed);
                if (seen.Add(GeometryKey(skin))) result.Add(skin);
            }

            return result;
        }

        /// <summary> A string equal for two skins exactly when their cells and parts are. </summary>
        public static string GeometryKey(AvatarSkin skin)
        {
            var builder = new StringBuilder();
            builder.Append(skin.Columns).Append('x').Append(skin.Rows).Append(':').Append(skin.Mask).Append('/');
            foreach (var cell in skin.Cells) builder.Append(cell).Append(',');
            builder.Append('/');
            foreach (var part in skin.Parts)
                builder.Append(part.Shape.value).Append(',').Append(part.Color).Append(',')
                    .Append(part.Rotation).Append(',').Append(part.Mirror).Append(',').Append(part.Scale).Append(';');
            return builder.ToString();
        }

        private static bool[,] DrawMask(ref SplitMix random, out int rows, out int columns)
        {
            var span = AvatarSkinRules.MaxGridSide - AvatarSkinRules.MinGridSide + 1;
            columns = AvatarSkinRules.MinGridSide + random.NextInt(span);
            rows = random.NextFloat() < SquareGridChance ? columns : AvatarSkinRules.MinGridSide + random.NextInt(span);
            var mirrorVertically = random.NextFloat() < VerticalMirrorChance;
            var fill = MinFill + (MaxFill - MinFill) * random.NextFloat();
            var minFilled = Math.Max(AvatarSkinRules.MinFilledCells, (int)Math.Ceiling(MinFilledShare * rows * columns));

            for (var attempt = 0; attempt < MaxTries; attempt++)
            {
                var mask = new bool[rows, columns];
                var halfColumns = (columns + 1) / 2;
                var halfRows = mirrorVertically ? (rows + 1) / 2 : rows;

                for (var row = 0; row < halfRows; row++)
                for (var column = 0; column < halfColumns; column++)
                {
                    var on = random.NextFloat() < fill;
                    mask[row, column] = on;
                    mask[row, columns - 1 - column] = on;
                    if (!mirrorVertically) continue;
                    mask[rows - 1 - row, column] = on;
                    mask[rows - 1 - row, columns - 1 - column] = on;
                }

                if (IsAcceptable(mask, rows, columns, minFilled)) return mask;
            }

            var full = new bool[rows, columns];
            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
                full[row, column] = true;
            return full;
        }

        private static bool IsAcceptable(bool[,] mask, int rows, int columns, int minFilled)
        {
            var count = 0;
            bool top = false, bottom = false, left = false, right = false;
            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
            {
                if (!mask[row, column]) continue;
                count++;
                top |= row == 0;
                bottom |= row == rows - 1;
                left |= column == 0;
                right |= column == columns - 1;
            }

            if (count < minFilled || !(top && bottom && left && right)) return false;
            if (LooksLikeAFace(mask, rows, columns)) return false;

            var probe = new AvatarSkin { Id = PresetId(0), Columns = columns, Rows = rows, Mask = 0UL };
            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
                if (mask[row, column])
                    probe.Mask |= AvatarSkinMask.Bit(column, rows - 1 - row);
            return AvatarSkinValidation.Validate(probe).Count == 0;
        }

        // A skin is an abstract body: the avatar draws its own eyes. Two enclosed holes read as eyes, a
        // crooked or wide one as a mouth, one off the middle row as either. Only a centred box hole is a ring.

        /// <summary> Whether <paramref name="filled"/> (top row first) has holes that read as a face.
        /// </summary>
        public static bool LooksLikeAFace(bool[,] filled, int rows, int columns)
        {
            var outside = new bool[rows, columns];
            var stack = new Stack<(int Row, int Column)>();
            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
                if ((row == 0 || row == rows - 1 || column == 0 || column == columns - 1) && !filled[row, column])
                    Mark(outside, row, column);

            while (stack.Count > 0)
            {
                var (row, column) = stack.Pop();
                Spread(outside, row - 1, column);
                Spread(outside, row + 1, column);
                Spread(outside, row, column - 1);
                Spread(outside, row, column + 1);
            }

            var holes = 0;
            int top = rows, bottom = -1, left = columns, right = -1, cells = 0, rowSum = 0;
            var seen = new bool[rows, columns];
            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
            {
                if (filled[row, column] || outside[row, column] || seen[row, column]) continue;
                if (++holes > 1) return true;

                Mark(seen, row, column);
                while (stack.Count > 0)
                {
                    var (r, c) = stack.Pop();
                    top = Math.Min(top, r);
                    bottom = Math.Max(bottom, r);
                    left = Math.Min(left, c);
                    right = Math.Max(right, c);
                    cells++;
                    rowSum += r;
                    Spread(seen, r - 1, c);
                    Spread(seen, r + 1, c);
                    Spread(seen, r, c - 1);
                    Spread(seen, r, c + 1);
                }
            }

            if (holes == 0) return false;
            var centreRow = (float)rowSum / cells;
            var rectangle = cells == (right - left + 1) * (bottom - top + 1);
            return !rectangle || right - left > bottom - top || Math.Abs(centreRow - (rows - 1) * 0.5f) > 0.5f;

            void Mark(bool[,] marks, int row, int column)
            {
                marks[row, column] = true;
                stack.Push((row, column));
            }

            void Spread(bool[,] marks, int row, int column)
            {
                if (row < 0 || row >= rows || column < 0 || column >= columns) return;
                if (filled[row, column] || marks[row, column] || (marks != outside && outside[row, column])) return;
                Mark(marks, row, column);
            }
        }

        // Chebyshev distance to the nearest empty cell or the border: 1 is the rim, 2 inner, 3+ core.

        private static int[,] Bands(bool[,] filled, int rows, int columns)
        {
            var bands = new int[rows, columns];
            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
            {
                if (!filled[row, column]) continue;

                var distance = Math.Min(Math.Min(row + 1, column + 1), Math.Min(rows - row, columns - column));
                for (var r = 0; r < rows; r++)
                for (var c = 0; c < columns; c++)
                    if (!filled[r, c])
                        distance = Math.Min(distance, Math.Max(Math.Abs(r - row), Math.Abs(c - column)));

                bands[row, column] = Math.Min(distance, Core);
            }

            return bands;
        }

        private static AvatarSkinPart PartFor(string style, int band, bool[,] filled, int rows, int columns,
            int row, int column)
        {
            switch (style)
            {
                case "dots": return Part(Circle, band, scale: 0.85f);
                case "hex": return band == Core ? Part(Circle, band, scale: 0.7f) : Part(Hexagon, band);
                case "gems": return Part(Square, band, 45f, 0.72f);
                case "rings":
                    return band == Rim ? Part(CircleRing, band) : Part(Circle, band, scale: band == Core ? 0.6f : 1f);
                case "frame":
                    return band == Rim ? Part(Square, band) : band == Inner ? Part(SquareRing, band) : Part(Square, band, scale: 0.6f);
                case "smooth":
                    return band == Rim && TryCornerRotation(filled, rows, columns, row, column, out var turn)
                        ? Part(RightTriangle, band, turn)
                        : Part(Square, band);
                case "tri":
                    return band == Core ? Part(Square, band) : Part(Triangle, band, column % 2 == 0 ? 0f : 180f);
                default: return Part(Square, band);
            }
        }

        // RightTriangle's right angle sits bottom-left; each 90 degrees counter-clockwise moves it one
        // corner on. The right angle points inward, away from the two empty neighbours.

        private static bool TryCornerRotation(bool[,] filled, int rows, int columns, int row, int column,
            out float rotation)
        {
            bool Empty(int r, int c) => r < 0 || r >= rows || c < 0 || c >= columns || !filled[r, c];

            var up = Empty(row - 1, column);
            var down = Empty(row + 1, column);
            var left = Empty(row, column - 1);
            var right = Empty(row, column + 1);

            rotation = 0f;
            if (up && down || left && right) return false;
            if (up && left) rotation = 90f;
            else if (up && right) rotation = 0f;
            else if (down && left) rotation = 180f;
            else if (down && right) rotation = 270f;
            else return false;
            return true;
        }

        private static AvatarSkinPart Part(string shape, int band, float rotation = 0f, float scale = 1f)
        {
            AvatarSkinShapes.TryGet(shape, out var shapeId);
            return new AvatarSkinPart(shapeId, band - 1) { Rotation = rotation, Scale = scale };
        }

        private struct SplitMix
        {
            private ulong _state;

            public SplitMix(uint seed) => _state = seed * 0x9E3779B97F4A7C15UL + 0x632BE59BD9B4E019UL;

            private ulong Next()
            {
                var z = _state += 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }

            public float NextFloat() => (Next() >> 40) / (float)(1UL << 24);

            public int NextInt(int exclusiveMax) => (int)(Next() % (ulong)exclusiveMax);
        }
    }
}
