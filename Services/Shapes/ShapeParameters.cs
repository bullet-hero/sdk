using System;

namespace BH.SDK.Services.Shapes
{
    /// <summary> Ring width, as the fraction 1/N of the radius. Filled is not a ring at all. </summary>
    public enum ShapeThickness : byte
    {
        Filled = 0,
        T2 = 1,
        T4 = 2,
        T8 = 3,
        T16 = 4,
        T32 = 5,
    }

    // THE CUTS ARE THE BOX'S OWN AXES, not the polygon's. Half is cut by the vertical through the
    // box centre, quarter adds the horizontal, eighth adds the 45 degree diagonal - so the sector a
    // slice covers is exactly the sector of the BOX that carries the same name, which is what lets
    // Invert subtract the shape from its own sector instead of from the whole box.
    //
    // It is also what the existing library already does: Pentagon_F4 starts at y = 0.0503 and the
    // pentagon's own AABB centre is 0.0502, i.e. the horizontal cut was made through the AABB
    // centre. Once the geometry is AABB-centred that line IS the box centre, so the rule below
    // reproduces the shipped shapes rather than replacing them.

    /// <summary> Which fraction of a turn the shape covers, measured from straight up, clockwise. </summary>
    public enum ShapeSlice : byte
    {
        Full = 0,
        Half = 1,
        Quarter = 2,
        Eighth = 3,
    }

    // A quarter has two forms and only sometimes needs both. The lower-right quarter is the
    // upper-right one turned by -90 degrees whenever 90 degrees is a whole number of the form's own
    // rotational steps (360/N), i.e. whenever N % 4 == 0. For every other N the two are genuinely
    // different shapes and both have to exist - nothing here relies on mirroring through a negative
    // Size, which would also reverse the winding the renderer culls on.

    /// <summary> Which of a sector's congruence classes this is. </summary>
    public enum ShapeSliceVariant : byte
    {
        Primary = 0,
        Second = 1,
    }

    // FORM CODES ARE A LOW BAND PLUS A LADDER, and the split is what keeps the four starting shapes
    // first without a hand-kept ordering list: sorting by id does it. Square, Circle, RightTriangle
    // and Triangle are named things an author reaches for, not "the 4-gon" and "the 32-gon", so they
    // live at 0x01..0x04 with 0x05..0x0F held for whatever else earns a name of its own.
    //
    // The ladder is the SIDE COUNT itself, offset by PolygonBase - so an eleven-, thirteen- or
    // twenty-sided shape slots in later without renumbering anything and without a lookup table.
    // 0x13 and 0x14 are therefore permanently unused: the three- and four-sided shapes already have
    // names above.

    /// <summary> Which silhouette a shape is built from. </summary>
    public readonly struct ShapeForm : IEquatable<ShapeForm>
    {
        /// <summary> First code of the regular-polygon ladder; a polygon's code is this plus its side count. </summary>
        public const int PolygonBase = 0x10;

        /// <summary> How many sides the shape a Circle is built from actually has. </summary>
        public const int CircleSides = 32;

        public readonly byte Code;

        public ShapeForm(byte code) => Code = code;

        public static readonly ShapeForm Square = new(0x01);
        public static readonly ShapeForm Circle = new(0x02);
        public static readonly ShapeForm RightTriangle = new(0x03);
        public static readonly ShapeForm Triangle = new(0x04);

        /// <summary> A regular polygon of <paramref name="sides"/> sides. Three and four sides are
        /// <see cref="Triangle"/> and <see cref="Square"/> and never reach the ladder. </summary>
        public static ShapeForm Polygon(int sides) => new((byte)(PolygonBase + sides));

        public bool IsLadder => Code >= PolygonBase;

        /// <summary> Side count of the polygon this form is built out of. RightTriangle answers 3
        /// because it has three corners, even though it is the one form that is not regular. </summary>
        public int Sides
        {
            get
            {
                if (IsLadder) return Code - PolygonBase;
                if (Code == Square.Code) return 4;
                if (Code == Circle.Code) return CircleSides;
                return 3; // Triangle and RightTriangle
            }
        }

        /// <summary> Whether this form is the one shape built from something other than a regular
        /// polygon's rim - so every generator has to branch on it exactly once. </summary>
        public bool IsRightTriangle => Code == RightTriangle.Code;

        // EVERY form is built with a corner pointing straight up, which costs nothing for all but
        // one of them: ShapeLoopUtils measures its angles clockwise FROM straight up, so a rim laid
        // out from angle zero already has a corner there whatever the side count. Four sides is the
        // exception and has to be turned by half a step, because a 4-gon with a corner at the top is
        // a diamond and the shape wanted here is a square.
        //
        // (ShapeSynthUtils.CornerAngle answers this differently - `sides == 4 || sides % 2 == 1` -
        // and both are right: that one measures from straight DOWN, so an odd count needs the half
        // step to move a corner from the bottom to the top. Do not copy one predicate to the other.)

        /// <summary> Whether the first corner sits half a step round from straight up. </summary>
        public bool UsesHalfStepPhase => Sides == 4;

        public string Name
        {
            get
            {
                if (Code == Square.Code) return "Square";
                if (Code == Circle.Code) return "Circle";
                if (Code == RightTriangle.Code) return "RightTriangle";
                if (Code == Triangle.Code) return "Triangle";
                return PolygonName(Sides);
            }
        }

        private static string PolygonName(int sides) => sides switch
        {
            5 => "Pentagon",
            6 => "Hexagon",
            7 => "Heptagon",
            8 => "Octagon",
            9 => "Nonagon",
            10 => "Decagon",
            11 => "Hendecagon",
            12 => "Dodecagon",
            13 => "Tridecagon",
            14 => "Tetradecagon",
            15 => "Pentadecagon",
            16 => "Hexadecagon",
            20 => "Icosagon",
            24 => "Icositetragon",
            _ => $"Polygon{sides}",
        };

        public bool Equals(ShapeForm other) => Code == other.Code;
        public override bool Equals(object obj) => obj is ShapeForm other && Equals(other);
        public override int GetHashCode() => Code;
        public override string ToString() => Name;

        public static bool operator ==(ShapeForm a, ShapeForm b) => a.Code == b.Code;
        public static bool operator !=(ShapeForm a, ShapeForm b) => a.Code != b.Code;
    }

    /// <summary>
    /// Everything that decides which built-in shape this is. The whole catalogue is the cross
    /// product of these fields minus a handful of degenerate combinations, and a ShapeId is these
    /// fields packed - see <see cref="ShapeCatalogService"/>.
    /// </summary>
    public readonly struct ShapeParameters : IEquatable<ShapeParameters>
    {
        public readonly ShapeForm Form;
        public readonly ShapeSlice Slice;
        public readonly ShapeSliceVariant Variant;
        public readonly ShapeThickness Thickness;

        /// <summary> The shape's own sector of the box, with the shape itself taken out of it. </summary>
        public readonly bool Invert;

        public ShapeParameters(ShapeForm form, ShapeSlice slice = ShapeSlice.Full,
            ShapeSliceVariant variant = ShapeSliceVariant.Primary,
            ShapeThickness thickness = ShapeThickness.Filled, bool invert = false)
        {
            Form = form;
            Slice = slice;
            Variant = variant;
            Thickness = thickness;
            Invert = invert;
        }

        public bool IsRing => Thickness != ShapeThickness.Filled;
        public bool IsFullTurn => Slice == ShapeSlice.Full;

        /// <summary> How much of a full turn the sector covers. </summary>
        public float Turns => Slice switch
        {
            ShapeSlice.Half => 0.5f,
            ShapeSlice.Quarter => 0.25f,
            ShapeSlice.Eighth => 0.125f,
            _ => 1f,
        };

        /// <summary> Ring width as a fraction of the radius; zero when the shape is filled. </summary>
        public float ThicknessFraction => Thickness switch
        {
            ShapeThickness.T2 => 1f / 2f,
            ShapeThickness.T4 => 1f / 4f,
            ShapeThickness.T8 => 1f / 8f,
            ShapeThickness.T16 => 1f / 16f,
            ShapeThickness.T32 => 1f / 32f,
            _ => 0f,
        };

        public bool Equals(ShapeParameters other)
            => Form == other.Form && Slice == other.Slice && Variant == other.Variant
               && Thickness == other.Thickness && Invert == other.Invert;

        public override bool Equals(object obj) => obj is ShapeParameters other && Equals(other);
        public override int GetHashCode()
            => HashCode.Combine(Form.Code, (int)Slice, (int)Variant, (int)Thickness, Invert);
        public override string ToString() => ShapeCatalogService.GetName(this);
    }
}
