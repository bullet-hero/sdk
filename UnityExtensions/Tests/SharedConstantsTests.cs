using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BH.SDK.UnityExtensions.Tests;
using NUnit.Framework;
using Unity.Mathematics;

namespace BH.SDK.Tests
{
    // Pins the STRUCTURE of the constant tables, not their values (a table cannot prove itself). Anchors are
    // the unit square's corners/edges/centre, colours are opaque, and every color has a color2 twin holding
    // it twice.

    /// <summary> The three constant tables in BH.Shared - `alignment`, `color`, `color2` - checked
    /// for the structure their entries have to have rather than for the values themselves. </summary>
    public class SharedConstantsTests
    {
        private const float Tolerance = 1e-5f;

        /// <summary> The `color` entries that are allowed a zero alpha, with the reason implied by
        /// the name. Adding to this needs one. </summary>
        private static readonly HashSet<string> Transparent = new() { nameof(color.clear) };

        #region Helpers

        private static IEnumerable<FieldInfo> StaticFields(Type type, Type valueType) => type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == valueType);

        private static string Report(List<string> failures)
        {
            var builder = new StringBuilder();
            builder.Append(failures.Count).Append(" constant(s) break the table's own shape:");
            foreach (var failure in failures) builder.Append('\n').Append("  ").Append(failure);
            return builder.ToString();
        }

        #endregion

        #region alignment

        // The nine of them are the unit square's own points, so they are spelled out rather than
        // derived: this is the one place in the project where "left" has to mean 0 and "top" has to
        // mean 1, and a table that derived them from each other could be consistently wrong.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Alignment_RectangularAnchors_AreTheUnitSquare()
        {
            AssertPoint(alignment.left_bottom, 0f, 0f, nameof(alignment.left_bottom));
            AssertPoint(alignment.left_middle, 0f, 0.5f, nameof(alignment.left_middle));
            AssertPoint(alignment.left_top, 0f, 1f, nameof(alignment.left_top));
            AssertPoint(alignment.center_bottom, 0.5f, 0f, nameof(alignment.center_bottom));
            AssertPoint(alignment.center_middle, 0.5f, 0.5f, nameof(alignment.center_middle));
            AssertPoint(alignment.center_top, 0.5f, 1f, nameof(alignment.center_top));
            AssertPoint(alignment.right_bottom, 1f, 0f, nameof(alignment.right_bottom));
            AssertPoint(alignment.right_middle, 1f, 0.5f, nameof(alignment.right_middle));
            AssertPoint(alignment.right_top, 1f, 1f, nameof(alignment.right_top));
        }

        private static void AssertPoint(float2 value, float x, float y, string name)
        {
            Assert.AreEqual(x, value.x, Tolerance, $"{name}.x");
            Assert.AreEqual(y, value.y, Tolerance, $"{name}.y");
        }

        // A regular polygon's centre of mass sits above the box centre for odd sides; the offset shrinks as
        // sides are added - the ordering a mistyped digit breaks.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Alignment_CentroidPivots_ClimbTowardTheBoxCentreAsSidesAreAdded()
        {
            var ladder = new[]
            {
                (nameof(alignment.equilateral3), alignment.equilateral3),
                (nameof(alignment.equilateral5), alignment.equilateral5),
                (nameof(alignment.equilateral7), alignment.equilateral7),
                (nameof(alignment.equilateral9), alignment.equilateral9),
                (nameof(alignment.equilateral11), alignment.equilateral11),
            };

            foreach (var (name, value) in ladder)
            {
                Assert.AreEqual(0.5f, value.x, Tolerance,
                    $"{name} is off the vertical midline, which no regular polygon's centroid is");
                Assert.Greater(value.y, 0f, $"{name} is outside its own shape");
                Assert.Less(value.y, 0.5f,
                    $"{name} is at or above the box centre, which only an even-sided polygon is");
            }

            for (var i = 1; i < ladder.Length; i++)
            {
                Assert.Greater(ladder[i].Item2.y, ladder[i - 1].Item2.y,
                    $"{ladder[i].Item1} sits lower than {ladder[i - 1].Item1} - a polygon with more " +
                    "sides is closer to a circle, so its centroid is closer to the box centre");
            }
        }

        // The right triangle is the one entry here that is not a regular polygon, and its centroid is
        // the arithmetic mean of three corners of the unit square - a third of the way in from two
        // sides. That is exact, so it is asserted exactly.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Alignment_RightTrianglePivot_IsTheMeanOfItsThreeCorners()
        {
            AssertPoint(alignment.rightTriangle, 1f / 3f, 1f / 3f, nameof(alignment.rightTriangle));
        }

        #endregion

        #region color / color2

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Color_EveryPreset_IsAnInRangeOpaqueRgba()
        {
            var failures = new List<string>();
            var checkedColors = 0;

            foreach (var field in StaticFields(typeof(color), typeof(float4)))
            {
                var value = (float4)field.GetValue(null);
                checkedColors++;

                for (var channel = 0; channel < 4; channel++)
                {
                    var component = value[channel];
                    if (float.IsNaN(component) || float.IsInfinity(component))
                        failures.Add($"color.{field.Name} channel {channel} is {component}");
                    else if (component is < 0f or > 1f)
                        failures.Add($"color.{field.Name} channel {channel} is {component}, outside [0, 1]");
                }

                // No colour is transparent except `clear`: alpha silently puts a user on the transparent
                // path.
                if (Transparent.Contains(field.Name))
                {
                    if (value.w != 0f)
                        failures.Add($"color.{field.Name} is listed as transparent but has alpha {value.w}");
                }
                else if (Math.Abs(value.w - 1f) > Tolerance)
                {
                    failures.Add($"color.{field.Name} has alpha {value.w}, expected 1");
                }
            }

            Assert.Greater(checkedColors, 100, "the reflection filter matched almost nothing");
            Assert.IsEmpty(failures, Report(failures));
        }

        // color2 is a hand-written mirror of color; it can drift by name or by either half of a pair,
        // silently.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Color2_EveryPair_MirrorsTheColorOfTheSameName()
        {
            var failures = new List<string>();

            var singles = StaticFields(typeof(color), typeof(float4))
                .ToDictionary(field => field.Name, field => (float4)field.GetValue(null));
            var pairs = StaticFields(typeof(color2), typeof((float4, float4)))
                .ToDictionary(field => field.Name, field => ((float4, float4))field.GetValue(null));

            foreach (var name in singles.Keys.Where(name => !pairs.ContainsKey(name)))
                failures.Add($"color.{name} has no color2 twin");

            foreach (var name in pairs.Keys.Where(name => !singles.ContainsKey(name)))
                failures.Add($"color2.{name} names a colour that color does not have");

            foreach (var (name, pair) in pairs)
            {
                if (!singles.TryGetValue(name, out var single)) continue;

                if (!pair.Item1.Equals(single))
                    failures.Add($"color2.{name}.Item1 is {pair.Item1}, color.{name} is {single}");
                if (!pair.Item2.Equals(single))
                    failures.Add($"color2.{name}.Item2 is {pair.Item2}, color.{name} is {single}");
            }

            Assert.Greater(singles.Count, 100, "the reflection filter matched almost nothing");
            Assert.IsEmpty(failures, Report(failures));
        }

        // Both ByteSize constants are read by whoever packs one of these into a buffer, so they are
        // the two numbers here that a reader trusts without looking.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ColorByteSizes_MatchWhatTheyDescribe()
        {
            Assert.AreEqual(sizeof(float) * 4, color.ByteSize);
            Assert.AreEqual(color.ByteSize * 2, color2.ByteSize);
        }

        #endregion
    }
}