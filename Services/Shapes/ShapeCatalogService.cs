using System;
using System.Collections.Generic;
using BH.SDK.Models.Data;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Utils;

namespace BH.SDK.Services.Shapes
{
    // THE ID IS THE PARAMETERS, one axis per nibble, and the layout is a contract rather than a
    // packing detail. Six rules keep it extensible, and every one of them exists because the
    // library it replaces broke on exactly that point:
    //
    //   1. One axis per nibble. Two axes sharing a nibble means a new value in one can collide
    //      with the other.
    //   2. Zero in any field is today's behaviour. A future axis living in the reserved nibbles is
    //      zero in every id ever written, so adding it cannot change what an existing id means.
    //   3. Enumerated axes get a nibble each; booleans share the flags nibble.
    //   4. The form is the SIDE COUNT itself, offset by ShapeForm.PolygonBase - so an eleven-,
    //      thirteen- or twenty-sided shape slots in later with no renumbering and no lookup table.
    //      The four named starting shapes sit in a low band below the ladder, which is what puts
    //      them first when ids are sorted, without a hand-kept ordering list.
    //   5. Reserved nibbles are documented with their intended occupants, so the next axis does not
    //      take another one's room.
    //   6. The NAME's field order mirrors the id's, so a name is decodable and sorts the same way.
    //
    // The library this replaces numbered its shapes 1..78 by position in an array, which meant
    // inserting a form renumbered everything after it and rewrote every level that referenced them.
    // Form code 0 is reserved precisely so none of those old ids can decode into a valid new shape.

    /// <summary>
    /// The built-in shape catalogue: which shapes exist, what each one's <see cref="ShapeId"/> is,
    /// and how to build its geometry. Engine-agnostic - the consuming project bakes what this
    /// enumerates into assets and meshes.
    /// </summary>
    public static class ShapeCatalogService
    {
        #region Id layout

        public const int ThicknessShift = 0;
        public const int SliceShift = 4;
        public const int VariantShift = 8;
        public const int FormShift = 12;
        public const int FlagsShift = 20;

        public const int NibbleMask = 0xF;
        public const int FormMask = 0xFF;

        /// <summary> The one flag the flags nibble currently carries. </summary>
        public const int InvertFlag = 1;

        /// <summary> Everything above the flags nibble, held for future axes: corner roundness, a
        /// star's inner radius, rim phase. Any id with a bit set here was written by a build that
        /// knows an axis this one does not. </summary>
        public const int ReservedMask = unchecked((int)0xFF000000);

        #endregion

        #region Catalogue

        /// <summary> Side counts of the regular-polygon ladder. Three and four sides are absent
        /// because they already have names of their own - Triangle and Square. </summary>
        public static readonly int[] PolygonLadder = { 5, 6, 7, 8, 9, 10, 11, 12, 16 };

        /// <summary> Every form, in the order their ids sort - the four named shapes first. </summary>
        public static IEnumerable<ShapeForm> EnumerateForms()
        {
            yield return ShapeForm.Square;
            yield return ShapeForm.Circle;
            yield return ShapeForm.RightTriangle;
            yield return ShapeForm.Triangle;

            foreach (var sides in PolygonLadder)
                yield return ShapeForm.Polygon(sides);
        }

        // A SLICE OF THESE THREE IS ANOTHER SHAPE PLUS A SIZE, which is why they are the forms with
        // no sectors: half a square is a rectangle and a quarter is a smaller square, both of which
        // are what Size already does; half an equilateral triangle is a 30-60-90 triangle, which is
        // RightTriangle under a non-uniform Size. Circle is NOT in this list - it is the form whose
        // sectors are used most.

        /// <summary> Whether this form is only ever whole. </summary>
        public static bool HasNoSectors(ShapeForm form)
            => form == ShapeForm.Square || form == ShapeForm.RightTriangle || form == ShapeForm.Triangle;

        /// <summary> Every sector this form carries, as a (slice, variant) pair. </summary>
        public static IEnumerable<(ShapeSlice Slice, ShapeSliceVariant Variant)> EnumerateSectors(ShapeForm form)
        {
            yield return (ShapeSlice.Full, ShapeSliceVariant.Primary);
            if (HasNoSectors(form)) yield break;

            var sides = form.Sides;

            yield return (ShapeSlice.Half, ShapeSliceVariant.Primary);
            yield return (ShapeSlice.Quarter, ShapeSliceVariant.Primary);

            // The lower-right quarter is the upper-right one turned by -90 degrees exactly when 90
            // degrees is a whole number of this form's own rotational steps. Otherwise it is a
            // different shape and has to exist as one.
            if (sides % 4 != 0)
                yield return (ShapeSlice.Quarter, ShapeSliceVariant.Second);

            // An eighth is cut on the 45 degree diagonal, which only lands on a corner when the
            // side count divides by eight. Anywhere else it would be an arbitrary sliver, and every
            // eighth would need variants of its own.
            if (sides % 8 == 0)
                yield return (ShapeSlice.Eighth, ShapeSliceVariant.Primary);
        }

        public static readonly ShapeThickness[] Thicknesses =
        {
            ShapeThickness.Filled, ShapeThickness.T2, ShapeThickness.T4,
            ShapeThickness.T8, ShapeThickness.T16, ShapeThickness.T32,
        };

        // Not omissions - each of these is another entry in the catalogue seen from a different
        // angle, and shipping both would put two ids on one shape.
        //
        //   Square inverted   - the square fills its own box, so subtracting it leaves nothing at
        //                       all; and subtracting a square RING leaves the ring's inner square,
        //                       which is Square under a smaller Size.
        //   RightTriangle     - the box minus a filled right triangle is the other right triangle,
        //   inverted, filled    i.e. the same shape rotated 180 degrees. Its rings invert fine:
        //                       those leave two pieces, not one.

        /// <summary> Whether this combination is another catalogue entry in disguise. </summary>
        public static bool IsDegenerate(ShapeParameters parameters)
        {
            if (!parameters.Invert) return false;
            if (parameters.Form == ShapeForm.Square) return true;
            if (parameters.Form == ShapeForm.RightTriangle && !parameters.IsRing) return true;
            return false;
        }

        /// <summary> Every shape the game ships, in id order. </summary>
        public static IEnumerable<ShapeParameters> EnumerateCatalog()
        {
            foreach (var form in EnumerateForms())
            foreach (var (slice, variant) in EnumerateSectors(form))
            foreach (var thickness in Thicknesses)
            foreach (var invert in new[] { false, true })
            {
                var parameters = new ShapeParameters(form, slice, variant, thickness, invert);
                if (IsDegenerate(parameters)) continue;
                yield return parameters;
            }
        }

        #endregion

        #region Encoding

        /// <summary> The packed code behind a shape's <see cref="ShapeId"/>. </summary>
        public static int Encode(ShapeParameters parameters)
            => ((parameters.Invert ? InvertFlag : 0) << FlagsShift)
               | (parameters.Form.Code << FormShift)
               | ((int)parameters.Variant << VariantShift)
               | ((int)parameters.Slice << SliceShift)
               | ((int)parameters.Thickness << ThicknessShift);

        public static ShapeId ToShapeId(ShapeParameters parameters)
            => new(GuidHelper.FromIntAtEnd(Encode(parameters)));

        /// <summary>
        /// Reads a built-in shape's parameters back out of its id. False for a level-authored shape,
        /// for one of the retired 1..78 ids, and for an id carrying a field this build does not
        /// know - which a caller must report rather than draw something plausible for.
        /// </summary>
        public static bool TryDecode(ShapeId shapeId, out ShapeParameters parameters)
        {
            parameters = default;

            var guid = shapeId.value;
            if (guid == Guid.Empty) return false;

            // A built-in id is an int in the last four bytes and nothing else; a level-authored
            // shape is an ordinary Guid and will not survive the round trip.
            var code = GuidHelper.ToIntFromEnd(guid);
            if (code <= 0) return false;
            if (!GuidHelper.FromIntAtEnd(code).Equals(guid)) return false;

            if ((code & ReservedMask) != 0) return false;

            var flags = (code >> FlagsShift) & NibbleMask;
            if ((flags & ~InvertFlag) != 0) return false;

            var formCode = (code >> FormShift) & FormMask;
            if (formCode == 0) return false;

            var variant = (code >> VariantShift) & NibbleMask;
            var slice = (code >> SliceShift) & NibbleMask;
            var thickness = (code >> ThicknessShift) & NibbleMask;

            if (variant > (int)ShapeSliceVariant.Second) return false;
            if (slice > (int)ShapeSlice.Eighth) return false;
            if (thickness > (int)ShapeThickness.T32) return false;

            parameters = new ShapeParameters(new ShapeForm((byte)formCode), (ShapeSlice)slice,
                (ShapeSliceVariant)variant, (ShapeThickness)thickness, (flags & InvertFlag) != 0);
            return true;
        }

        #endregion

        #region Names

        /// <summary> The catalogue name, whose field order mirrors the id's own. </summary>
        public static string GetName(ShapeParameters parameters)
        {
            var name = parameters.Form.Name;

            name += parameters.Slice switch
            {
                ShapeSlice.Half => "_S2",
                ShapeSlice.Quarter => "_S4",
                ShapeSlice.Eighth => "_S8",
                _ => string.Empty,
            };

            if (parameters.Variant == ShapeSliceVariant.Second) name += "_2";

            name += parameters.Thickness switch
            {
                ShapeThickness.T2 => "_T2",
                ShapeThickness.T4 => "_T4",
                ShapeThickness.T8 => "_T8",
                ShapeThickness.T16 => "_T16",
                ShapeThickness.T32 => "_T32",
                _ => string.Empty,
            };

            if (parameters.Invert) name += "_I";
            return name;
        }

        // Written out in words because the editor's search matches an entry's TITLE and nothing
        // else - so "half", "quarter", "ring" and "inverted" have to appear as text somewhere or an
        // author has no way to find the shape without knowing the code for it.

        /// <summary> The same shape said in words, for search and for inspector labels. </summary>
        public static string GetDisplayName(ShapeParameters parameters)
        {
            var parts = new List<string>(5) { parameters.Form.Name };

            switch (parameters.Slice)
            {
                case ShapeSlice.Half: parts.Add("half"); break;
                case ShapeSlice.Quarter:
                    parts.Add(parameters.Variant == ShapeSliceVariant.Second ? "quarter lower" : "quarter");
                    break;
                case ShapeSlice.Eighth: parts.Add("eighth"); break;
            }

            if (parameters.IsRing) parts.Add($"ring 1/{RungDivisor(parameters.Thickness)}");
            if (parameters.Invert) parts.Add("inverted");

            return string.Join(", ", parts);
        }

        private static int RungDivisor(ShapeThickness thickness) => thickness switch
        {
            ShapeThickness.T2 => 2,
            ShapeThickness.T4 => 4,
            ShapeThickness.T8 => 8,
            ShapeThickness.T16 => 16,
            ShapeThickness.T32 => 32,
            _ => 1,
        };

        /// <summary> The constant name this shape gets inside its form's group in ShapeId.g.cs -
        /// the catalogue name with the form stripped off its front. </summary>
        public static string GetConstantName(ShapeParameters parameters)
        {
            var name = GetName(parameters);
            var form = parameters.Form.Name;
            return name.Length > form.Length ? name.Substring(form.Length + 1) : "Fill";
        }

        #endregion

        #region Geometry

        /// <summary> Angular bounds of a shape's sector, measured clockwise from straight up. Both
        /// zero when the shape covers the whole turn. </summary>
        public static void GetSector(ShapeParameters parameters, out double from, out double to)
        {
            switch (parameters.Slice)
            {
                case ShapeSlice.Half:
                    from = 0.0;
                    to = Math.PI;
                    return;
                case ShapeSlice.Quarter:
                    from = parameters.Variant == ShapeSliceVariant.Second ? Math.PI * 0.5 : 0.0;
                    to = parameters.Variant == ShapeSliceVariant.Second ? Math.PI : Math.PI * 0.5;
                    return;
                case ShapeSlice.Eighth:
                    from = 0.0;
                    to = Math.PI * 0.25;
                    return;
                default:
                    from = 0.0;
                    to = 0.0;
                    return;
            }
        }

        // THE OFFSET IS THE FULL SHAPE'S, applied to every sector, ring and inversion of it. That is
        // what makes switching an object from Hexagon to Hexagon_S4 truncate the shape where it
        // stands instead of moving and rescaling it, and it is what keeps a ring concentric with the
        // disc it belongs to. Centring each entry on its OWN bounds would do neither.

        /// <summary> A form's outer rim, already AABB-centred - the frame every one of its
        /// catalogue entries is built in. </summary>
        public static List<Vector2Value> BuildFormRim(ShapeForm form)
        {
            var rim = form.IsRightTriangle
                ? ShapeLoopUtils.RightTriangleRim()
                : ShapeLoopUtils.RegularRim(form.Sides,
                    ShapeLoopUtils.FitRadius(form.Sides, form.UsesHalfStepPhase),
                    form.UsesHalfStepPhase);

            var centre = ShapeLoopUtils.GetBoundsCenter(rim);
            return ShapeLoopUtils.Translate(rim, new Vector2Value(-centre.X, -centre.Y));
        }

        /// <summary>
        /// The pivot that puts an object's transform on this form's centre of mass, in the same
        /// 0..1 space RectObject.Pivots uses. (0.5, 0.5) - the default - is already the centre of
        /// the AABB, so any form whose two centres coincide answers exactly that.
        /// </summary>
        public static Vector2Value GetCentroidPivot(ShapeForm form)
        {
            var rim = BuildFormRim(form);
            var centroid = ShapeLoopUtils.GetCornerCentroid(rim);
            return new Vector2Value(0.5f + centroid.X, 0.5f + centroid.Y);
        }

        /// <summary> The geometry of one catalogue entry. Null when nothing survived sanitizing,
        /// which for a catalogue entry is a bug rather than an input the caller has to tolerate. </summary>
        public static CompositeShape Build(ShapeParameters parameters)
        {
            var outer = BuildFormRim(parameters.Form);
            var full = parameters.IsFullTurn;
            GetSector(parameters, out var from, out var to);

            var vertices = new List<Vector2Value>();
            var indices = new List<int>();

            if (!parameters.Invert)
            {
                if (!parameters.IsRing)
                {
                    AddSectorFan(vertices, indices, outer, full, from, to);
                }
                else
                {
                    // A whole ring is measured from the SHAPE's centroid, not the box's, because
                    // those differ for an odd-sided form and only the first is inside both rims -
                    // the right triangle's hypotenuse runs straight through the box centre. A
                    // SLICED ring has no such choice: its two cut rays leave the box centre, so
                    // that is where its angles have to be measured from too.
                    var inner = ShapeLoopUtils.Inset(outer, parameters.ThicknessFraction);
                    var centre = full ? ShapeLoopUtils.GetCornerCentroid(outer) : Vector2Value.Zero;
                    AddSectorAnnulus(vertices, indices, outer, inner, full, from, to, centre);
                }
            }
            else
            {
                // The shape comes out of ITS OWN SECTOR of the box, not out of the whole box: the
                // same vertical, horizontal and diagonal that cut the shape cut the box too. So an
                // inverted quarter is a quarter of a wall with a quarter of a hole in it, and the
                // two cuts line up instead of leaving a sliver of wall along the cut.
                AddSectorAnnulus(vertices, indices, ShapeLoopUtils.BoxRim(), outer,
                    full, from, to, Vector2Value.Zero);

                // Inverting a RING leaves two pieces, and this is the second: what was the hole in
                // the middle is solid now.
                if (parameters.IsRing)
                {
                    var inner = ShapeLoopUtils.Inset(outer, parameters.ThicknessFraction);
                    AddSectorFan(vertices, indices, inner, full, from, to);
                }
            }

            return ShapeSynthUtils.Build(ToShapeId(parameters), GetName(parameters), vertices, indices);
        }

        // A sliced fan carries the ORIGIN as the corner where its two cut edges meet. For a half it
        // is redundant - the two cut edges are one straight chord through it - and Sanitize drops
        // the collinear triangle it makes, so there is no case to branch on here.
        private static void AddSectorFan(List<Vector2Value> vertices, List<int> indices,
            IReadOnlyList<Vector2Value> loop, bool full, double from, double to)
        {
            if (full)
            {
                ShapeLoopUtils.AddFan(vertices, indices, loop);
                return;
            }

            var sector = ShapeLoopUtils.ClipToSector(loop, from, to);
            sector.Add(Vector2Value.Zero);
            ShapeLoopUtils.AddFan(vertices, indices, sector);
        }

        private static void AddSectorAnnulus(List<Vector2Value> vertices, List<int> indices,
            IReadOnlyList<Vector2Value> outer, IReadOnlyList<Vector2Value> inner,
            bool full, double from, double to, Vector2Value centre)
        {
            if (full)
            {
                ShapeLoopUtils.AddAnnulus(vertices, indices, outer, inner, closed: true, centre);
                return;
            }

            ShapeLoopUtils.AddAnnulus(vertices, indices,
                ShapeLoopUtils.ClipToSector(outer, from, to),
                ShapeLoopUtils.ClipToSector(inner, from, to),
                closed: false, centre);
        }

        #endregion
    }
}
