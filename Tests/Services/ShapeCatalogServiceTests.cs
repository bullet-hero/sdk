using System;
using System.Collections.Generic;
using System.Linq;
using BH.SDK.Models.Data;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Services.Shapes;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    // WHAT IS ACTUALLY AT RISK HERE IS NOT "does a shape look right" but two invariants a person
    // cannot check by eye across five hundred entries:
    //
    //   An id means one thing forever. The library this replaced numbered its shapes by their
    //   position in an array, so inserting a form renumbered everything after it and every level
    //   referencing them drew something else. The packed layout is what fixes that, and it is only
    //   fixed while the round trip below holds and the reserved bits stay refused.
    //
    //   A shape and its inverse tile the sector they were cut from. That single equality catches
    //   the whole class of triangulation bugs this generator can have - a folded ribbon, a wedge
    //   spanning a pinch point, a rim clipped to the wrong sector - because every one of them shows
    //   up as area counted twice or not at all, while the picture still looks plausible.
    public class ShapeCatalogServiceTests
    {
        private static List<ShapeParameters> Catalog => ShapeCatalogService.EnumerateCatalog().ToList();

        private static float Area(CompositeShape shape)
        {
            var area = 0f;
            for (var i = 0; i + 2 < shape.Indices.Count; i += 3)
            {
                var a = shape.Vertices[shape.Indices[i]];
                var b = shape.Vertices[shape.Indices[i + 1]];
                var c = shape.Vertices[shape.Indices[i + 2]];
                area += Math.Abs((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y)) * 0.5f;
            }
            return area;
        }

        private static float SectorArea(ShapeSlice slice) => slice switch
        {
            ShapeSlice.Half => 0.5f,
            ShapeSlice.Quarter => 0.25f,
            ShapeSlice.Eighth => 0.125f,
            _ => 1f,
        };

        #region Identity

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Catalog_HasNoDuplicateIds()
        {
            var seen = new HashSet<Guid>();
            foreach (var parameters in Catalog)
            {
                var id = ShapeCatalogService.ToShapeId(parameters);
                Assert.IsTrue(seen.Add(id.value),
                    $"{ShapeCatalogService.GetName(parameters)} shares an id with another shape");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Catalog_EveryIdDecodesBackToItsOwnParameters()
        {
            foreach (var parameters in Catalog)
            {
                var id = ShapeCatalogService.ToShapeId(parameters);

                Assert.IsTrue(ShapeCatalogService.TryDecode(id, out var decoded),
                    $"{ShapeCatalogService.GetName(parameters)} does not decode");
                Assert.AreEqual(parameters, decoded,
                    $"{ShapeCatalogService.GetName(parameters)} decodes to something else");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Catalog_NamesAreUniqueAndDecodable()
        {
            var seen = new HashSet<string>();
            foreach (var parameters in Catalog)
            {
                var name = ShapeCatalogService.GetName(parameters);
                Assert.IsTrue(seen.Add(name), $"'{name}' names two shapes");
                Assert.IsTrue(name.StartsWith(parameters.Form.Name, StringComparison.Ordinal),
                    $"'{name}' does not lead with its form");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TryDecode_RefusesNullAndLevelAuthoredIds()
        {
            Assert.IsFalse(ShapeCatalogService.TryDecode(ShapeId.Null, out _));
            Assert.IsFalse(ShapeCatalogService.TryDecode(ShapeId.NewId(), out _),
                "a random Guid must not read as a built-in shape");
        }

        // The four starting shapes occupy 0x01..0x04 and the ladder starts at 0x10, so every id the
        // retired library issued (1..78) has form code 0 - which is reserved and never issued. That
        // is the whole reason a level written against the old library resolves to nothing visible
        // instead of to whichever new shape happened to land on its number.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TryDecode_RefusesEveryRetiredId()
        {
            for (var legacy = 1; legacy <= 78; legacy++)
            {
                var id = new ShapeId(GuidHelper.FromIntAtEnd(legacy));
                Assert.IsFalse(ShapeCatalogService.TryDecode(id, out _),
                    $"retired id {legacy} decodes to a shape");
            }
        }

        // A build that meets an id carrying an axis it does not know must say so rather than draw
        // the shape that id would mean with the unknown part ignored - which is what makes the
        // reserved nibbles safe to spend later.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TryDecode_RefusesAnIdCarryingAFutureAxis()
        {
            var known = ShapeCatalogService.Encode(new ShapeParameters(ShapeForm.Polygon(6)));
            var future = known | (1 << 24);

            Assert.IsTrue(ShapeCatalogService.TryDecode(new ShapeId(GuidHelper.FromIntAtEnd(known)), out _));
            Assert.IsFalse(ShapeCatalogService.TryDecode(new ShapeId(GuidHelper.FromIntAtEnd(future)), out _),
                "an id with a reserved bit set must not resolve");
        }

        // THE ORDER IS FORM-MAJOR, NOT ASCENDING BY ID, and the difference is deliberate. Invert is
        // a flag high in the id, so sorting numerically would put every ordinary shape first and
        // every inverted one after them - splitting each form's block in half wherever it is read as
        // a list. Grouping by form keeps a picker legible and puts the four named shapes an author
        // reaches for at the very front, which is the ordering that was asked for.
        //
        // Extensibility does not depend on this. It comes from the id being derived from what a
        // shape IS, so a form added later takes ids nobody else can hold whatever order it is
        // enumerated in.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Catalog_IsGroupedByFormWithTheFourNamedFormsFirst()
        {
            var forms = ShapeCatalogService.EnumerateForms().Take(4).ToList();
            CollectionAssert.AreEqual(
                new[] { ShapeForm.Square, ShapeForm.Circle, ShapeForm.RightTriangle, ShapeForm.Triangle },
                forms);

            var order = new List<ShapeForm>();
            foreach (var parameters in Catalog)
            {
                if (order.Count > 0 && order[order.Count - 1] == parameters.Form) continue;

                Assert.IsFalse(order.Contains(parameters.Form),
                    $"{parameters.Form.Name} appears in two separate runs");
                order.Add(parameters.Form);
            }

            CollectionAssert.AreEqual(ShapeCatalogService.EnumerateForms().ToList(), order);
        }

        #endregion

        #region Geometry

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Build_EveryShapeIsRealGeometryInsideTheBox()
        {
            foreach (var parameters in Catalog)
            {
                var name = ShapeCatalogService.GetName(parameters);
                var shape = ShapeCatalogService.Build(parameters);

                Assert.IsNotNull(shape, $"{name} built nothing");
                Assert.GreaterOrEqual(shape.TriangleCount, ValueRules.MinShapeTriangles, name);
                Assert.LessOrEqual(shape.TriangleCount, ValueRules.MaxShapeTriangles, name);
                Assert.Greater(Area(shape), 0.0005f, $"{name} has no area worth drawing");

                foreach (var vertex in shape.Vertices)
                {
                    Assert.That(vertex.X, Is.InRange(ValueRules.MinShapePoint - 0.001f,
                        ValueRules.MaxShapePoint + 0.001f), name);
                    Assert.That(vertex.Y, Is.InRange(ValueRules.MinShapePoint - 0.001f,
                        ValueRules.MaxShapePoint + 0.001f), name);
                }
            }
        }

        // The invariant the whole AABB-centring decision rests on: fitting a form by its BOUNDS
        // rather than by its circumradius means every one of them measures exactly one unit across
        // its longer axis and sits centred on the origin - so nothing reaches outside its own rect
        // and Size means the same thing whichever shape it is applied to.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void FormRim_IsCentredOnItsBoundsAndFillsTheBox()
        {
            foreach (var form in ShapeCatalogService.EnumerateForms())
            {
                var rim = ShapeCatalogService.BuildFormRim(form);
                ShapeLoopUtils.GetBounds(rim, out var min, out var max);

                Assert.AreEqual(0f, (min.X + max.X) * 0.5f, 1e-4f, $"{form.Name} x centre");
                Assert.AreEqual(0f, (min.Y + max.Y) * 0.5f, 1e-4f, $"{form.Name} y centre");
                Assert.AreEqual(1f, Math.Max(max.X - min.X, max.Y - min.Y), 2e-3f,
                    $"{form.Name} does not fill the box on its longer axis");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Build_InvertedShapeIsExactlyTheRestOfItsOwnSector()
        {
            foreach (var parameters in Catalog)
            {
                if (parameters.Invert) continue;

                var inverted = new ShapeParameters(parameters.Form, parameters.Slice,
                    parameters.Variant, parameters.Thickness, invert: true);
                if (ShapeCatalogService.IsDegenerate(inverted)) continue;

                var shape = ShapeCatalogService.Build(parameters);
                var hole = ShapeCatalogService.Build(inverted);
                Assert.IsNotNull(shape);
                Assert.IsNotNull(hole);

                Assert.AreEqual(SectorArea(parameters.Slice), Area(shape) + Area(hole), 2e-3f,
                    $"{ShapeCatalogService.GetName(parameters)} and its inverse do not tile their sector");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Build_SlicesTileTheWholeShape()
        {
            foreach (var form in ShapeCatalogService.EnumerateForms())
            {
                if (ShapeCatalogService.HasNoSectors(form)) continue;

                var whole = Area(ShapeCatalogService.Build(new ShapeParameters(form)));
                var half = Area(ShapeCatalogService.Build(
                    new ShapeParameters(form, ShapeSlice.Half)));

                Assert.AreEqual(whole, half * 2f, 2e-3f, $"{form.Name}: two halves are not the whole");

                var upper = Area(ShapeCatalogService.Build(
                    new ShapeParameters(form, ShapeSlice.Quarter)));
                var lower = form.Sides % 4 != 0
                    ? Area(ShapeCatalogService.Build(new ShapeParameters(
                        form, ShapeSlice.Quarter, ShapeSliceVariant.Second)))
                    : upper;

                Assert.AreEqual(whole, (upper + lower) * 2f, 2e-3f,
                    $"{form.Name}: four quarters are not the whole");
            }
        }

        // A slice must TRUNCATE the shape where it stands rather than move or rescale it - that is
        // what makes switching an object from Hexagon to Hexagon_S4 read as cutting it in place.
        // The family's own offset being shared is what guarantees it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Build_ASliceStaysInsideTheWholeShapesBounds()
        {
            foreach (var form in ShapeCatalogService.EnumerateForms())
            {
                if (ShapeCatalogService.HasNoSectors(form)) continue;

                var whole = ShapeCatalogService.Build(new ShapeParameters(form));
                ShapeLoopUtils.GetBounds(whole.Vertices, out var wholeMin, out var wholeMax);

                foreach (var (slice, variant) in ShapeCatalogService.EnumerateSectors(form))
                {
                    if (slice == ShapeSlice.Full) continue;

                    var part = ShapeCatalogService.Build(new ShapeParameters(form, slice, variant));
                    ShapeLoopUtils.GetBounds(part.Vertices, out var partMin, out var partMax);

                    var name = ShapeCatalogService.GetName(new ShapeParameters(form, slice, variant));
                    Assert.GreaterOrEqual(partMin.X, wholeMin.X - 1e-3f, name);
                    Assert.GreaterOrEqual(partMin.Y, wholeMin.Y - 1e-3f, name);
                    Assert.LessOrEqual(partMax.X, wholeMax.X + 1e-3f, name);
                    Assert.LessOrEqual(partMax.Y, wholeMax.Y + 1e-3f, name);
                }
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Build_ARingIsThinnerTheHigherItsRung()
        {
            foreach (var form in ShapeCatalogService.EnumerateForms())
            {
                var previous = float.MaxValue;
                foreach (var thickness in ShapeCatalogService.Thicknesses)
                {
                    if (thickness == ShapeThickness.Filled) continue;

                    var area = Area(ShapeCatalogService.Build(
                        new ShapeParameters(form, thickness: thickness)));

                    Assert.Less(area, previous,
                        $"{form.Name} {thickness} is not thinner than the rung below it");
                    previous = area;
                }
            }
        }

        #endregion

        #region Pivots

        // Exactly six forms have a centre of mass that is not their box centre, and every one of
        // them is odd-sided or the right triangle. An even-sided polygon balancing anywhere else
        // would mean the rim is not being built symmetrically.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void GetCentroidPivot_DiffersOnlyForOddFormsAndTheRightTriangle()
        {
            foreach (var form in ShapeCatalogService.EnumerateForms())
            {
                var pivot = ShapeCatalogService.GetCentroidPivot(form);
                var offCentre = Math.Abs(pivot.X - 0.5f) > 1e-4f || Math.Abs(pivot.Y - 0.5f) > 1e-4f;
                var expected = form.IsRightTriangle || form.Sides % 2 == 1;

                Assert.AreEqual(expected, offCentre,
                    $"{form.Name}: centre of mass {(offCentre ? "differs from" : "equals")} the box centre");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void GetCentroidPivot_MatchesTheKnownTriangleValues()
        {
            var right = ShapeCatalogService.GetCentroidPivot(ShapeForm.RightTriangle);
            Assert.AreEqual(1f / 3f, right.X, 1e-5f);
            Assert.AreEqual(1f / 3f, right.Y, 1e-5f);

            var equilateral = ShapeCatalogService.GetCentroidPivot(ShapeForm.Triangle);
            Assert.AreEqual(0.5f, equilateral.X, 1e-5f);
            Assert.AreEqual(0.5f - 0.1443376f, equilateral.Y, 1e-5f);
        }

        #endregion
    }
}
