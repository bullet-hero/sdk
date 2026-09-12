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
    // THREE CONSTANT TABLES, AND THE REASON THEY GET A TEST IS THAT NOTHING ELSE READS THEM AS A
    // WHOLE. `alignment`, `color` and `color2` are structs in name only - no constructor, no
    // equality, no behaviour, just a few hundred named values that every screen and every level
    // reaches into one at a time. A wrong entry is therefore never a compile error and never a
    // crash; it is one anchor placed slightly off, or one swatch the wrong shade, in one place.
    //
    // So what is pinned here is not "these numbers are correct" - a table cannot prove itself - but
    // the STRUCTURE the numbers have to have, which is checkable without restating them:
    //
    //   the nine rectangular anchors are the corners, edges and centre of the unit square;
    //   the centroid pivots all sit on the vertical midline and climb toward the centre as the
    //     polygon gains sides, which is what "centre of mass of a regular n-gon" means geometrically
    //     and is exactly what a transcription error breaks;
    //   every colour is a real, in-range, opaque RGBA;
    //   every `color` entry has a `color2` twin holding it twice - the invariant that makes color2 a
    //     view of color rather than a second table someone has to keep in step.
    //
    // The last one is the one worth having. color2 is 150 entries written by hand as
    // `(color.x, color.x)`, and a pair that names one colour and holds another is invisible in both
    // the source and the game.

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

        // A regular polygon's centre of mass is its circumcentre, which sits above the centre of its
        // bounding box for an odd side count and exactly ON it for an even one. As sides are added
        // the shape approaches a circle, so the offset shrinks monotonically toward the box centre -
        // that ordering is the invariant a mistyped digit breaks, and it needs none of the values
        // restated to check.
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

                // Every entry is a colour, not a colour-and-a-fade: a preset with a transparent alpha
                // silently puts whatever uses it on the transparent render path, which is a cost
                // nobody reaching for a named colour is expecting to pay. `clear` is the deliberate
                // exception and the only one - it IS the absence of a colour, and naming it here is
                // what keeps a second one from being added quietly.
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

        // THE ONE THAT EARNS ITS KEEP. color2 is a hand-written mirror of color - same names, each
        // holding its colour twice - so the two tables can drift in three ways, all of them silent:
        // a name present in one and not the other, and a pair whose halves disagree with the colour
        // it is named after in either slot.
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