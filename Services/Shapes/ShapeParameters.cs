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

    // The cuts are the BOX's own axes, not the polygon's (half: vertical; quarter adds horizontal;
    // eighth adds the diagonal), so a slice is a sector of the box and Invert subtracts from it.
    // Docs/SHAPES.md.

    /// <summary> Which fraction of a turn the shape covers, measured from straight up, clockwise. </summary>
    public enum ShapeSlice : byte
    {
        Full = 0,
        Half = 1,
        Quarter = 2,
        Eighth = 3,
    }

    // A quarter has two forms and only sometimes needs both: they are one shape turned by -90 degrees
    // when N % 4 == 0. No mirroring through a negative Size (it reverses the culled winding).

    /// <summary> Which of a sector's congruence classes this is. </summary>
    public enum ShapeSliceVariant : byte
    {
        Primary = 0,
        Second = 1,
    }

    // Form codes are a low band (0x01..0x04 named shapes, 0x05..0x0F held) plus a ladder: the side
    // count offset by PolygonBase. 0x13 and 0x14 are permanently unused. Docs/SHAPES.md.

    /// <summary> Which silhouette a shape is built from. </summary>
    public readonly struct ShapeForm : IEquatable<ShapeForm>
    {
        /// <summary> First code of the regular-polygon ladder; a polygon's code is this plus its side count. </summary>
        public const int PolygonBase = 0x10;

        /// <summary> How many sides the shape a Circle is built from actually has. </summary>
        public const int CircleSides = 32;

        /// <summary> The form's own number, which is what the id carries. </summary>
        public readonly byte Code;

        /// <summary> Built from its code. </summary>
        public ShapeForm(byte code) => Code = code;

        /// <summary> A square. </summary>
        public static readonly ShapeForm Square = new(0x01);
        /// <summary> A circle. </summary>
        public static readonly ShapeForm Circle = new(0x02);
        /// <summary> A right triangle. </summary>
        public static readonly ShapeForm RightTriangle = new(0x03);
        /// <summary> An equilateral triangle. </summary>
        public static readonly ShapeForm Triangle = new(0x04);

        /// <summary> A regular polygon of <paramref name="sides"/> sides. Three and four sides are
        /// <see cref="Triangle"/> and <see cref="Square"/> and never reach the ladder. </summary>
        public static ShapeForm Polygon(int sides) => new((byte)(PolygonBase + sides));

        /// <summary> True for the regular polygons, which are numbered consecutively above the named forms. </summary>
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

        // Every form has a corner straight up (angles run clockwise from up); four sides is turned half
        // a step, or the 4-gon would be a diamond.

        /// <summary> Whether the first corner sits half a step round from straight up. </summary>
        public bool UsesHalfStepPhase => Sides == 4;

        /// <summary> What this form is called, for a report. </summary>
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

        /// <summary> Member by member. </summary>
        public bool Equals(ShapeForm other) => Code == other.Code;
        /// <summary> The same, boxed. </summary>
        public override bool Equals(object obj) => obj is ShapeForm other && Equals(other);
        /// <summary> Matches the equality above. </summary>
        public override int GetHashCode() => Code;
        /// <summary> One line, for a log. </summary>
        public override string ToString() => Name;

        /// <summary> Value equality. </summary>
        public static bool operator ==(ShapeForm a, ShapeForm b) => a.Code == b.Code;
        /// <summary> Its negation. </summary>
        public static bool operator !=(ShapeForm a, ShapeForm b) => a.Code != b.Code;
    }

    /// <summary> Everything that decides which built-in shape this is: the catalogue is the cross product
    /// of these fields minus degenerate combinations, and a ShapeId is them packed (ShapeCatalogService).
    /// </summary>
    public readonly struct ShapeParameters : IEquatable<ShapeParameters>
    {
        /// <summary> Which form the shape is. </summary>
        public readonly ShapeForm Form;
        /// <summary> How much of it is kept - the whole, a half, a quarter, an eighth. </summary>
        public readonly ShapeSlice Slice;
        /// <summary> Which of two quarters, where the second is not a rotation of the first. </summary>
        public readonly ShapeSliceVariant Variant;
        /// <summary> Filled, or a ring of one of the thickness rungs. </summary>
        public readonly ShapeThickness Thickness;

        /// <summary> The shape's own sector of the box, with the shape itself taken out of it. </summary>
        public readonly bool Invert;

        /// <summary> Every member at once, in declaration order. </summary>
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

        /// <summary> True when the shape is hollow. </summary>
        public bool IsRing => Thickness != ShapeThickness.Filled;
        /// <summary> True when nothing was sliced off. </summary>
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

        /// <summary> Member by member. </summary>
        public bool Equals(ShapeParameters other)
            => Form == other.Form && Slice == other.Slice && Variant == other.Variant
               && Thickness == other.Thickness && Invert == other.Invert;

        /// <summary> The same, boxed. </summary>
        public override bool Equals(object obj) => obj is ShapeParameters other && Equals(other);
        /// <summary> Matches the equality above. </summary>
        public override int GetHashCode()
            => HashCode.Combine(Form.Code, (int)Slice, (int)Variant, (int)Thickness, Invert);
        /// <summary> One line, for a log. </summary>
        public override string ToString() => ShapeCatalogService.GetName(this);
    }
}
