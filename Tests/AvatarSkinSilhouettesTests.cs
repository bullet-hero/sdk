using System;
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Services.Skins;
using BH.SDK.Utils;
using BH.SDK.Validations;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> That a sliced outline is a drawable, symmetric, solid skin that draws the outline itself.
    /// </summary>
    [TestFixture]
    public class AvatarSkinSilhouettesTests
    {
        private const int Samples = 9;
        private const double EdgeMargin = 0.01;

        private static IEnumerable<(string Outline, int Side)> Every()
        {
            foreach (var outline in AvatarSkinSilhouettes.Outlines)
                for (var side = 5; side <= AvatarSkinRules.MaxGridSide; side++)
                    yield return (outline, side);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EveryOutline_Validates_IsSymmetric_AndHasNoHole()
        {
            foreach (var (outline, side) in Every())
            {
                var skin = AvatarSkinSilhouettes.Build(outline, side, 7u);
                skin.Id = AvatarSkinGenerator.PresetId(1);
                var name = outline + " " + side;

                CollectionAssert.IsEmpty(AvatarSkinValidation.Validate(skin), name);
                Assert.LessOrEqual(skin.Meshes.Count, AvatarSkinRules.MaxMeshes, name);
                Assert.LessOrEqual(skin.Parts.Count, AvatarSkinRules.MaxParts, name);
                Assert.AreEqual(AvatarSkinElements.Classic, skin.Elements, name);

                var filled = new bool[side, side];
                for (var row = 0; row < side; row++)
                for (var column = 0; column < side; column++)
                {
                    filled[side - 1 - row, column] = AvatarSkinMask.Has(skin.Mask, column, row);
                    Assert.AreEqual(AvatarSkinMask.Has(skin.Mask, column, row),
                        AvatarSkinMask.Has(skin.Mask, side - 1 - column, row), $"{name}: row {row}");
                }

                Assert.IsFalse(HasHole(filled, side), name);
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void EveryFilledCell_DrawsTheOutline_ThroughItsTurnAndMirror()
        {
            foreach (var (outline, side) in Every())
            {
                var polygon = AvatarSkinSilhouettes.Outline(outline);
                var skin = AvatarSkinSilhouettes.Build(outline, side, 7u);
                var cell = 1.0 / side;
                var rank = 0;

                for (var row = 0; row < side; row++)
                for (var column = 0; column < side; column++)
                {
                    if (!AvatarSkinMask.Has(skin.Mask, column, row)) continue;
                    var part = skin.Parts[skin.Cells[rank++]];

                    for (var sy = 0; sy < Samples; sy++)
                    for (var sx = 0; sx < Samples; sx++)
                    {
                        var x = -0.5 + (column + (sx + 0.5) / Samples) * cell;
                        var y = -0.5 + (row + (sy + 0.5) / Samples) * cell;
                        if (DistanceToEdge(polygon, x, y) < EdgeMargin) continue;

                        Assert.AreEqual(Inside(polygon, x, y), Drawn(skin, part, (x + 0.5) / cell - column - 0.5,
                            (y + 0.5) / cell - row - 0.5), $"{outline} {side}: cell {column},{row} at {x:F3},{y:F3}");
                    }
                }
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TheSameOutline_BuildsTheSameSkin()
        {
            var a = AvatarSkinSilhouettes.Build("circle", 8, 3u);
            var b = AvatarSkinSilhouettes.Build("circle", 8, 3u);

            Assert.AreEqual(AvatarSkinGenerator.GeometryKey(a), AvatarSkinGenerator.GeometryKey(b));
            for (var i = 0; i < a.Meshes.Count; i++) Assert.AreEqual(a.Meshes[i].Id, b.Meshes[i].Id);
            Assert.IsNull(AvatarSkinSilhouettes.Outline("no such outline"));
        }

        // The drawn point is R(turn) * M * stored, so the stored one is M * R(-turn) * drawn.

        private static bool Drawn(AvatarSkin skin, AvatarSkinPart part, double x, double y)
        {
            var mesh = skin.Meshes.Find(candidate => candidate.Id == part.Shape);
            if (mesh == null) return Math.Abs(x) <= 0.5 && Math.Abs(y) <= 0.5;

            var radians = -part.Rotation * Math.PI / 180.0;
            var (sin, cos) = (Math.Sin(radians), Math.Cos(radians));
            var px = x * cos - y * sin;
            var py = x * sin + y * cos;
            if (part.Mirror) px = -px;

            for (var t = 0; t + 2 < mesh.Indices.Count; t += 3)
                if (InTriangle(mesh.Vertices[mesh.Indices[t]], mesh.Vertices[mesh.Indices[t + 1]],
                        mesh.Vertices[mesh.Indices[t + 2]], px, py))
                    return true;
            return false;
        }

        private static bool InTriangle(Vector2Value a, Vector2Value b, Vector2Value c, double x, double y)
        {
            var d1 = Cross(a.X, a.Y, b.X, b.Y, x, y);
            var d2 = Cross(b.X, b.Y, c.X, c.Y, x, y);
            var d3 = Cross(c.X, c.Y, a.X, a.Y, x, y);
            return (d1 >= -1e-9 && d2 >= -1e-9 && d3 >= -1e-9) || (d1 <= 1e-9 && d2 <= 1e-9 && d3 <= 1e-9);
        }

        private static bool Inside(List<Vector2Value> polygon, double x, double y)
        {
            for (var i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                if (Cross(a.X, a.Y, b.X, b.Y, x, y) < 0) return false;
            }

            return true;
        }

        private static double DistanceToEdge(List<Vector2Value> polygon, double x, double y)
        {
            var best = double.MaxValue;
            for (var i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                double dx = b.X - a.X, dy = b.Y - a.Y;
                var t = Math.Max(0, Math.Min(1, ((x - a.X) * dx + (y - a.Y) * dy) / (dx * dx + dy * dy)));
                best = Math.Min(best, Math.Sqrt(Math.Pow(a.X + t * dx - x, 2) + Math.Pow(a.Y + t * dy - y, 2)));
            }

            return best;
        }

        private static double Cross(double ax, double ay, double bx, double by, double x, double y) =>
            (bx - ax) * (y - ay) - (by - ay) * (x - ax);

        // Rows top first; an empty cell the border cannot reach through empty cells is a hole.
        private static bool HasHole(bool[,] filled, int side)
        {
            var reached = new bool[side, side];
            var stack = new Stack<(int Row, int Column)>();
            for (var row = 0; row < side; row++)
            for (var column = 0; column < side; column++)
                if ((row == 0 || column == 0 || row == side - 1 || column == side - 1) && !filled[row, column])
                    stack.Push((row, column));

            while (stack.Count > 0)
            {
                var (row, column) = stack.Pop();
                if (row < 0 || column < 0 || row >= side || column >= side) continue;
                if (filled[row, column] || reached[row, column]) continue;
                reached[row, column] = true;
                stack.Push((row - 1, column));
                stack.Push((row + 1, column));
                stack.Push((row, column - 1));
                stack.Push((row, column + 1));
            }

            for (var row = 0; row < side; row++)
            for (var column = 0; column < side; column++)
                if (!filled[row, column] && !reached[row, column])
                    return true;
            return false;
        }
    }
}
