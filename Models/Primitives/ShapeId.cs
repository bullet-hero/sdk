using System;
using System.Runtime.CompilerServices;
using BH.SDK.Models.Interfaces.Primitives;
using BH.SDK.Utils;
// ReSharper disable InconsistentNaming

namespace BH.SDK.Models.Primitives
{
    /// <summary>
    /// Identity of a reusable shape (CompositeShape). The only Guid id with well-known constants: the
    /// built-in library, in the generated half (ShapeId.g.cs), one nested class per form.
    /// </summary>
    public partial struct ShapeId : IEquatable<ShapeId>, IPrimitiveGuid
    {
        /// <summary> The raw Guid; game-defined vs. user-defined is decided by which collection it
        /// is found in, not by the value. </summary>
        public Guid value;
        Guid IPrimitiveGuid.Value => value;

        /// <summary> Built from its value. </summary>
        public ShapeId(Guid value)
        {
            this.value = value;
        }

        /// <summary> Parses the textual form. </summary>
        public ShapeId(string str)
        {
            value = new Guid(str);
        }
        /// <summary> Back to the values the constructor writes. </summary>
        public void Reset()
        {
            value = Guid.Empty;
        }

        // One id type serves BOTH ShapeObject fields (ShapeId drawn, ColliderId hit): a second type could
        // only disagree. A Guid has no range to split: game vs user is decided by WHICH collection holds it.

        // Built-in ids are packed parameters, one axis per nibble (ShapeCatalogService owns the layout), not
        // a dense sequence; form code 0 is never issued. Docs/IDENTIFIERS.md

        /// <summary> The reserved "unset" value. Never a real id. </summary>
        public static readonly Guid NullValue = Guid.Empty;

        /// <summary> The unset id - a shape slot holding it draws nothing. </summary>
        public static readonly ShapeId Null = new(NullValue);

        /// <summary> True when the id names a shape rather than being unset. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEnabled() => value != Guid.Empty;

        /// <summary> The same test on a bare guid. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnabled(Guid value) => value != Guid.Empty;

        /// <summary> A fresh id, for a shape a level authors itself. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ShapeId NewId() => new(Guid.NewGuid());

        /// <summary> A fresh id. The guid spelling, kept beside NewId for callers that read better that way. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ShapeId NewGuid() => new(Guid.NewGuid());

        /// <summary> Value equality. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ShapeId a, ShapeId b) => a.value == b.value;

        /// <summary> Its negation. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ShapeId a, ShapeId b) => a.value != b.value;

        /// <summary> Member by member. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ShapeId other) => value == other.value;

        /// <summary> The same, boxed. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is ShapeId other && Equals(other);

        /// <summary> Matches the equality above. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => value.GetHashCode();

        /// <summary> One line, for a log. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{nameof(ShapeId)}={value}";
    }
}
