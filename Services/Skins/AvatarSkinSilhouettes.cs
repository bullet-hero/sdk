using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BH.SDK.Models;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Utils;

namespace BH.SDK.Services.Skins
{
    // Body space is [-0.5, 0.5] on both axes, row 0 at the bottom. A slice is stored once and every
    // other cell reaches it by a quarter turn and a mirror, the transforms a part already has.

    /// <summary> Cuts a convex outline - a circle, a polygon - over a square body grid: cells inside are
    /// squares, cells on the edge are the skin's own meshes, so the outline stays smooth and the body still
    /// shatters cell by cell. Pure and deterministic. </summary>
    public static class AvatarSkinSilhouettes
    {
        /// <summary> The least share of a cell the outline must cover for the cell to be filled. </summary>
        public const double MinCoverage = 0.002;

        /// <summary> Every outline <see cref="Outline"/> knows, by name. </summary>
        public static readonly string[] Outlines =
            { "circle", "triangle", "triangle-down", "diamond", "pentagon", "hexagon", "octagon", "dome", "rounded",
              "shield", "drop" };

        private const double FullCoverage = 0.995;
        private const double Epsilon = 1e-7;
        private const int KeyScale = 1000;
        private const string Square = "Square";

        /// <summary> The outline called <paramref name="name"/>: convex, counter-clockwise, centred and scaled
        /// so its longer side spans the body. Null for a name <see cref="Outlines"/> does not hold. </summary>
        public static List<Vector2Value> Outline(string name)
        {
            var points = new List<(double X, double Y)>();
            switch (name)
            {
                case "circle":
                    Arc(points, 0, 360, 64, 0, 0, 1, closed: true);
                    break;
                case "triangle":
                    Regular(points, 3, 90);
                    break;
                case "triangle-down":
                    Regular(points, 3, 270);
                    break;
                case "diamond":
                    Regular(points, 4, 0);
                    break;
                case "pentagon":
                    Regular(points, 5, 90);
                    break;
                case "hexagon":
                    Regular(points, 6, 0);
                    break;
                case "octagon":
                    Regular(points, 8, 22.5);
                    break;
                case "dome":
                    Arc(points, 0, 180, 32, 0, 0, 1, closed: false);
                    points.Add((-1, -1));
                    points.Add((1, -1));
                    break;
                case "rounded":
                    const double radius = 0.4;
                    Arc(points, 0, 90, 8, 1 - radius, 1 - radius, radius, closed: false);
                    Arc(points, 90, 180, 8, radius - 1, 1 - radius, radius, closed: false);
                    Arc(points, 180, 270, 8, radius - 1, radius - 1, radius, closed: false);
                    Arc(points, 270, 360, 8, 1 - radius, radius - 1, radius, closed: false);
                    break;
                case "shield":
                    points.Add((1, 1));
                    points.Add((-1, 1));
                    points.Add((-1, -0.15));
                    points.Add((0, -1.15));
                    points.Add((1, -0.15));
                    break;
                case "drop":
                    Arc(points, 180, 360, 32, 0, 0, 1, closed: false);
                    points.Add((0, 1.7));
                    break;
                default:
                    return null;
            }

            return Fit(points);
        }

        /// <summary> The skin <paramref name="outline"/> (see <see cref="Outline"/>) makes over a
        /// <paramref name="side"/> x <paramref name="side"/> grid: colour 1 on the edge, colour 0 inside, the
        /// classic parts on. Its meshes take ids derived from <paramref name="meshSeed"/>; the caller sets its id. </summary>
        public static AvatarSkin Build(IReadOnlyList<Vector2Value> outline, int side, string name, uint meshSeed)
        {
            if (side < AvatarSkinRules.MinGridSide || side > AvatarSkinRules.MaxGridSide)
                throw new ArgumentOutOfRangeException(nameof(side));

            var subject = new List<(double X, double Y)>(outline.Count);
            foreach (var point in outline) subject.Add((point.X, point.Y));

            var cellSize = 1.0 / side;
            var looks = new (ShapeId Shape, float Rotation, bool Mirror, bool Slice)[side, side];
            var filled = new bool[side, side];
            var meshes = new List<AvatarSkinShape>();
            var meshKeys = new Dictionary<string, int>(StringComparer.Ordinal);
            AvatarSkinShapes.TryGet(Square, out var square);

            for (var row = 0; row < side; row++)
            for (var column = 0; column < side; column++)
            {
                var x0 = -0.5 + column * cellSize;
                var y0 = -0.5 + row * cellSize;
                var clipped = Clip(subject, x0, y0, x0 + cellSize, y0 + cellSize);
                var coverage = Area(clipped) / (cellSize * cellSize);
                if (coverage < MinCoverage) continue;

                filled[row, column] = true;
                if (coverage >= FullCoverage)
                {
                    looks[row, column] = (square, 0f, false, false);
                    continue;
                }

                var local = new List<(double X, double Y)>(clipped.Count);
                foreach (var (x, y) in clipped)
                    local.Add(((x - x0) / cellSize - 0.5, (y - y0) / cellSize - 0.5));
                local = Clean(local);

                looks[row, column] = Reuse(local, meshes, meshKeys, out var look)
                    ? look
                    : AddMesh(local, meshes, meshKeys, meshSeed);
            }

            var skin = new AvatarSkin
            {
                Name = name,
                Columns = side,
                Rows = side,
                Mask = 0UL,
                Parts = new List<AvatarSkinPart>(),
                Meshes = meshes,
                Colors = new List<Color4Value> { AvatarSkinDefaults.Body(), AvatarSkinDefaults.Body() },
                Elements = AvatarSkinElements.Classic,
            };

            var parts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var row = 0; row < side; row++)
            for (var column = 0; column < side; column++)
            {
                if (!filled[row, column]) continue;

                var look = looks[row, column];
                var color = look.Slice || IsEdge(filled, side, row, column) ? 1 : 0;
                var key = look.Shape.value + "|" + color + "|" + look.Rotation + "|" + look.Mirror;
                if (!parts.TryGetValue(key, out var index))
                {
                    index = skin.Parts.Count;
                    parts[key] = index;
                    skin.Parts.Add(new AvatarSkinPart(look.Shape, color) { Rotation = look.Rotation, Mirror = look.Mirror });
                }

                skin.Mask |= AvatarSkinMask.Bit(column, row);
                skin.Cells.Add(index);
            }

            return skin;
        }

        /// <summary> The skin the outline called <paramref name="outline"/> makes; see
        /// <see cref="Build(IReadOnlyList{Vector2Value}, int, string, uint)"/>. </summary>
        public static AvatarSkin Build(string outline, int side, uint meshSeed) =>
            Build(Outline(outline) ?? throw new ArgumentException(outline, nameof(outline)), side,
                outline + " " + side.ToString(CultureInfo.InvariantCulture), meshSeed);

        // The drawn slice is R(turn) * M^mirror * stored, so the stored one is M^mirror * R(-turn) * slice.

        private static bool Reuse(List<(double X, double Y)> slice, List<AvatarSkinShape> meshes,
            Dictionary<string, int> keys, out (ShapeId, float, bool, bool) look)
        {
            for (var mirror = 0; mirror < 2; mirror++)
            for (var turn = 0; turn < 4; turn++)
            {
                var candidate = new List<(double X, double Y)>(slice.Count);
                foreach (var point in slice)
                {
                    var (x, y) = point;
                    for (var i = 0; i < turn; i++) (x, y) = (y, -x);
                    candidate.Add(mirror == 1 ? (-x, y) : (x, y));
                }

                if (!keys.TryGetValue(Key(candidate), out var index)) continue;
                look = (meshes[index].Id, turn * 90f, mirror == 1, true);
                return true;
            }

            look = default;
            return false;
        }

        private static (ShapeId, float, bool, bool) AddMesh(List<(double X, double Y)> slice,
            List<AvatarSkinShape> meshes, Dictionary<string, int> keys, uint meshSeed)
        {
            var vertices = new List<Vector2Value>(slice.Count);
            foreach (var (x, y) in slice) vertices.Add(new Vector2Value((float)x, (float)y));

            var indices = new List<int>((slice.Count - 2) * 3);
            for (var i = 1; i + 1 < slice.Count; i++)
            {
                indices.Add(0);
                indices.Add(i);
                indices.Add(i + 1);
            }

            var id = MeshId(meshSeed, meshes.Count);
            keys[Key(slice)] = meshes.Count;
            meshes.Add(new AvatarSkinShape(id, vertices, indices));
            return (id, 0f, false, true);
        }

        private static ShapeId MeshId(uint seed, int index)
        {
            var state = (seed * 0x9E3779B97F4A7C15UL) ^ ((ulong)(index + 1) * 0xBF58476D1CE4E5B9UL);
            var bytes = new byte[16];
            for (var i = 0; i < bytes.Length; i++)
            {
                state = (state ^ (state >> 31)) * 0x94D049BB133111EBUL + 0x632BE59BD9B4E019UL;
                bytes[i] = (byte)(state >> 56);
            }

            return new ShapeId(new Guid(bytes));
        }

        private static string Key(List<(double X, double Y)> polygon)
        {
            var corners = new List<(long X, long Y)>(polygon.Count);
            foreach (var (x, y) in polygon)
                corners.Add(((long)Math.Round(x * KeyScale), (long)Math.Round(y * KeyScale)));
            corners.Sort();

            var builder = new StringBuilder();
            foreach (var (x, y) in corners) builder.Append(x).Append(',').Append(y).Append(';');
            return builder.ToString();
        }

        private static bool IsEdge(bool[,] filled, int side, int row, int column)
        {
            bool Empty(int r, int c) => r < 0 || r >= side || c < 0 || c >= side || !filled[r, c];
            return Empty(row - 1, column) || Empty(row + 1, column) || Empty(row, column - 1) || Empty(row, column + 1);
        }

        // Sutherland-Hodgman against the four sides of the cell; a convex subject stays one convex piece.

        private static List<(double X, double Y)> Clip(List<(double X, double Y)> subject, double x0, double y0,
            double x1, double y1)
        {
            var result = subject;
            result = ClipEdge(result, p => p.X - x0, (a, b) => Lerp(a, b, (x0 - a.X) / (b.X - a.X)));
            result = ClipEdge(result, p => x1 - p.X, (a, b) => Lerp(a, b, (x1 - a.X) / (b.X - a.X)));
            result = ClipEdge(result, p => p.Y - y0, (a, b) => Lerp(a, b, (y0 - a.Y) / (b.Y - a.Y)));
            result = ClipEdge(result, p => y1 - p.Y, (a, b) => Lerp(a, b, (y1 - a.Y) / (b.Y - a.Y)));
            return result;
        }

        private static List<(double X, double Y)> ClipEdge(List<(double X, double Y)> input,
            Func<(double X, double Y), double> inside,
            Func<(double X, double Y), (double X, double Y), (double X, double Y)> cross)
        {
            var output = new List<(double X, double Y)>(input.Count + 2);
            for (var i = 0; i < input.Count; i++)
            {
                var current = input[i];
                var previous = input[(i + input.Count - 1) % input.Count];
                var currentIn = inside(current) >= 0;
                var previousIn = inside(previous) >= 0;
                if (currentIn != previousIn) output.Add(cross(previous, current));
                if (currentIn) output.Add(current);
            }

            return output;
        }

        private static (double X, double Y) Lerp((double X, double Y) a, (double X, double Y) b, double t) =>
            (a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

        private static double Area(List<(double X, double Y)> polygon)
        {
            var twice = 0.0;
            for (var i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                twice += a.X * b.Y - b.X * a.Y;
            }

            return twice * 0.5;
        }

        // Clipping leaves repeated corners and corners on a straight edge; either makes a degenerate triangle.

        private static List<(double X, double Y)> Clean(List<(double X, double Y)> polygon)
        {
            var points = new List<(double X, double Y)>(polygon.Count);
            foreach (var (x, y) in polygon)
            {
                var point = (Math.Max(-0.5, Math.Min(0.5, x)), Math.Max(-0.5, Math.Min(0.5, y)));
                if (points.Count == 0 || Distance(points[points.Count - 1], point) > 1e-5) points.Add(point);
            }

            if (points.Count > 1 && Distance(points[0], points[points.Count - 1]) <= 1e-5) points.RemoveAt(points.Count - 1);

            for (var changed = true; changed && points.Count > 3;)
            {
                changed = false;
                for (var i = 0; i < points.Count && points.Count > 3; i++)
                {
                    var a = points[(i + points.Count - 1) % points.Count];
                    var b = points[i];
                    var c = points[(i + 1) % points.Count];
                    var turn = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
                    if (turn > 1e-5) continue;
                    points.RemoveAt(i);
                    changed = true;
                    break;
                }
            }

            return points;
        }

        private static double Distance((double X, double Y) a, (double X, double Y) b) =>
            Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

        private static void Regular(List<(double X, double Y)> points, int sides, double startDegrees)
        {
            for (var i = 0; i < sides; i++)
            {
                var angle = (startDegrees + 360.0 * i / sides) * Math.PI / 180.0;
                points.Add((Math.Cos(angle), Math.Sin(angle)));
            }
        }

        private static void Arc(List<(double X, double Y)> points, double fromDegrees, double toDegrees, int steps,
            double centreX, double centreY, double radius, bool closed)
        {
            var count = closed ? steps : steps + 1;
            for (var i = 0; i < count; i++)
            {
                var angle = (fromDegrees + (toDegrees - fromDegrees) * i / steps) * Math.PI / 180.0;
                points.Add((centreX + radius * Math.Cos(angle), centreY + radius * Math.Sin(angle)));
            }
        }

        private static List<Vector2Value> Fit(List<(double X, double Y)> points)
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var (x, y) in points)
            {
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }

            var scale = 1.0 / Math.Max(Math.Max(maxX - minX, maxY - minY), Epsilon);
            var centreX = (minX + maxX) * 0.5;
            var centreY = (minY + maxY) * 0.5;

            var result = new List<Vector2Value>(points.Count);
            foreach (var (x, y) in points)
                result.Add(new Vector2Value((float)((x - centreX) * scale), (float)((y - centreY) * scale)));
            return result;
        }
    }
}
