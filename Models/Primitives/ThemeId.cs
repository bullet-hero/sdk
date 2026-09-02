using System;
using System.Runtime.CompilerServices;
using BH.SDK.Models.Interfaces.Primitives;

namespace BH.SDK.Models.Primitives
{
    /// <summary>
    /// Identity of a ThemeData palette in Level.Resources.Themes, picked over time by ThemeKeyframe.
    /// Note it addresses a whole palette - a single color slot inside it is addressed by a plain
    /// int index (Color4ThemeRef.ThemeColorIndex), not by an id.
    /// </summary>
    public struct ThemeId : IEquatable<ThemeId>, IPrimitiveGuid
    {
        /// <summary> The raw Guid. Serialized as a string under JSON and as a native UUID under
        /// a binary format by PrimitiveGuidConverter. </summary>
        public Guid value;
        Guid IPrimitiveGuid.Value => value;

        public ThemeId(Guid value)
        {
            this.value = value;
        }
        public ThemeId(string str)
        {
            value = new Guid(str);
        }
        public void Reset()
        {
            value = Guid.Empty;
        }

        // Theme ids are a stable identifier for a Theme entry (Level.Resources.Themes), replacing
        // positional ThemeIndex - a theme's position in the list can change (reorder/delete), an
        // id never does. Unlike the previous int-based id, there is no game-defined/user-defined
        // range split - a Guid has no meaningful "positive/negative" ordering to split on (see
        // PrefabId/LevelId for the same reasoning). Guid.Empty is the only reserved/Null value.

        public static readonly Guid NullValue = Guid.Empty;

        public static readonly ThemeId Null = new(NullValue);

        // default, not NullValue: reading a static field would drag this type's initializer into
        // any Burst job that asks - see ShapeId.IsEnabled for the failure that caused.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEnabled() => value != Guid.Empty;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnabled(Guid value) => value != Guid.Empty;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ThemeId NewId() => new(Guid.NewGuid());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ThemeId NewGuid() => new(Guid.NewGuid());


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ThemeId a, ThemeId b) => a.value == b.value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ThemeId a, ThemeId b) => a.value != b.value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ThemeId other) => value == other.value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is ThemeId other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => value.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{nameof(ThemeId)}={value}";
    }
}
