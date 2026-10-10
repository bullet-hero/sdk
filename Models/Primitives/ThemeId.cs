using System;
using System.Runtime.CompilerServices;
using BH.SDK.Models.Interfaces.Primitives;

namespace BH.SDK.Models.Primitives
{
    /// <summary>
    /// Identity of a ThemeData palette in Level.Resources.Themes, picked over time by ThemeKeyframe. A
    /// single color slot inside it is a plain int index (Color4ThemeRef.ThemeColorIndex), not an id.
    /// </summary>
    public struct ThemeId : IEquatable<ThemeId>, IPrimitiveGuid
    {
        /// <summary> The raw Guid. Serialized as a string under JSON and as a native UUID under
        /// a binary format by PrimitiveGuidConverter. </summary>
        public Guid value;
        Guid IPrimitiveGuid.Value => value;

        /// <summary> Built from its value. </summary>
        public ThemeId(Guid value)
        {
            this.value = value;
        }

        /// <summary> Parses the textual form. </summary>
        public ThemeId(string str)
        {
            value = new Guid(str);
        }
        /// <summary> Back to the values the constructor writes. </summary>
        public void Reset()
        {
            value = Guid.Empty;
        }

        // A stable id for a Theme entry (Level.Resources.Themes), replacing the positional ThemeIndex (an
        // id never changes on reorder or delete). Guid.Empty is the only reserved value.

        /// <summary> The reserved "unset" value. Never a real id. </summary>
        public static readonly Guid NullValue = Guid.Empty;

        /// <summary> The unset id. </summary>
        public static readonly ThemeId Null = new(NullValue);

        // default, not NullValue: reading a static field would drag this type's initializer into
        // any Burst job that asks - see ShapeId.IsEnabled for the failure that caused.

        /// <summary> True when the id names something rather than being unset. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEnabled() => value != Guid.Empty;

        /// <summary> The same test on a bare guid. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnabled(Guid value) => value != Guid.Empty;

        /// <summary> A fresh id, unique for practical purposes. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ThemeId NewId() => new(Guid.NewGuid());

        /// <summary> A fresh id. The guid spelling, kept beside NewId for callers that read better that way. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ThemeId NewGuid() => new(Guid.NewGuid());


        /// <summary> Value equality. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ThemeId a, ThemeId b) => a.value == b.value;

        /// <summary> Its negation. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ThemeId a, ThemeId b) => a.value != b.value;

        /// <summary> Member by member. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ThemeId other) => value == other.value;

        /// <summary> The same, boxed. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is ThemeId other && Equals(other);

        /// <summary> Matches the equality above. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => value.GetHashCode();

        /// <summary> One line, for a log. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{nameof(ThemeId)}={value}";
    }
}
