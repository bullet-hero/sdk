using System;

namespace BH.SDK.Models.Enums
{
    // A bit PRESENT means EXPANDED (the inverse of a filter mask).
    // Lockstep: a bit's index is its ObjectType value (ToMask shifts by it); ObjectTypeMaskTests pins it.

    /// <summary> Which object kinds a surface shows unfolded. </summary>
    [Flags]
    public enum ObjectTypeMask : byte
    {
        None = 0,

        RectObject = 1 << 0,
        ShapeObject = 1 << 1,
        EffectObject = 1 << 2,
        TextObject = 1 << 3,
        PrefabObject = 1 << 4,

        /// <summary> Every kind unfolded - what "expand everything" resolves to. </summary>
        All = RectObject | ShapeObject | EffectObject | TextObject | PrefabObject,
    }

    public static class ObjectTypeMaskExtensions
    {
        /// <summary> The single-bit mask for one kind. </summary>
        public static ObjectTypeMask ToMask(this ObjectType type) => (ObjectTypeMask)(1 << (int)type);

        /// <summary> Whether a surface holding this mask shows that kind unfolded. </summary>
        public static bool Has(this ObjectTypeMask mask, ObjectType type)
            => (mask & type.ToMask()) != ObjectTypeMask.None;

        public static ObjectTypeMask With(this ObjectTypeMask mask, ObjectType type)
            => mask | type.ToMask();

        public static ObjectTypeMask Without(this ObjectTypeMask mask, ObjectType type)
            => mask & ~type.ToMask();
    }
}
