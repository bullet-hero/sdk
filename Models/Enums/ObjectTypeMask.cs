using System;

namespace BH.SDK.Models.Enums
{
    // A BIT PRESENT MEANS EXPANDED, which is the inverse of how a filter mask reads and is the one
    // thing about this type a reader gets backwards. It is not a filter over what exists; it is the
    // answer to "does a row of this kind open by itself", one bit per kind, and an author who wants
    // prefabs folded away ticks every OTHER box.
    //
    // EACH BIT'S INDEX IS ITS ObjectType'S OWN VALUE, and that is a contract rather than a
    // coincidence: ToMask is a shift by (int)type, so a member added to ObjectType without one added
    // here silently maps onto whatever bit sits at its ordinal. ObjectTypeMaskTests pins the pairing
    // member by member, so the addition fails there rather than in a hierarchy that folds the wrong
    // rows.

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
