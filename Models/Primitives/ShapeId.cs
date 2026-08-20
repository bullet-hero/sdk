using System;
using System.Runtime.CompilerServices;
using BH.SDK.Models.Interfaces.Primitives;
using BH.SDK.Utils;
// ReSharper disable InconsistentNaming

namespace BH.SDK.Models.Primitives
{
    /// <summary>
    /// Identity of a reusable shape (CompositeShape). The only Guid id here with a large set of
    /// well-known constants - the built-in shape library, which lives in the generated half of this
    /// type (ShapeId.g.cs) as one nested class per form: ShapeId.Hexagon.S4_2_T8_I and so on.
    /// </summary>
    public partial struct ShapeId : IEquatable<ShapeId>, IPrimitiveGuid
    {
        /// <summary> The raw Guid; game-defined vs. user-defined is decided by which collection it
        /// is found in, not by the value. </summary>
        public Guid value;
        Guid IPrimitiveGuid.Value => value;

        public ShapeId(Guid value)
        {
            this.value = value;
        }
        public ShapeId(string str)
        {
            value = new Guid(str);
        }
        public void Reset()
        {
            value = Guid.Empty;
        }

        // One id type serves BOTH of ShapeObject's shape fields - ShapeId (what is drawn) and
        // ColliderId (what is hit) - because a shape and a hitbox are the same thing described the
        // same way: triangles inside [-0.5, 0.5]. Nothing distinguishes "a shape you can render"
        // from "a shape you can collide against", so a second id type would only be able to
        // disagree with this one.
        //
        // Shape ids are a stable identifier for a CompositeShape/CompositeShapeScriptable entry,
        // same role ThemeId/EffectId play for Theme/Effect - a ShapeObject references a shared,
        // reusable shape by id instead of embedding its own copy. There is no game-defined/
        // user-defined range split - a Guid has no meaningful "positive/negative" ordering to split
        // on (see ThemeId/EffectId/PrefabId/LevelId for the same reasoning); "game-defined" vs
        // "user-defined" is determined by which collection an id is found in
        // (GameResources.CompositeShapes vs Level.Resources.CompositeShapes), not by the id's own
        // value. Guid.Empty is the only reserved/Null value.

        // THE BUILT-IN IDS ARE PACKED PARAMETERS, one axis per nibble, and ShapeCatalogService owns
        // the layout (see its header for the six rules that keep it extensible). What matters here
        // is that they are NOT a dense sequence: the library this replaced numbered its shapes
        // 1..78 by their position in an array, so inserting a form renumbered every shape after it.
        // A packed id is derived from what the shape IS, so a new side count, a new thickness rung
        // or a whole new axis leaves every id already written alone.
        //
        // Form code 0 is reserved and never issued, which is what makes the retired 1..78 ids
        // undecodable rather than silently resolving to some other shape.

        public static readonly Guid NullValue = Guid.Empty;

        public static readonly ShapeId Null = new(NullValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEnabled() => value != Guid.Empty;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnabled(Guid value) => value != Guid.Empty;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ShapeId NewId() => new(Guid.NewGuid());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ShapeId NewGuid() => new(Guid.NewGuid());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ShapeId a, ShapeId b) => a.value == b.value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ShapeId a, ShapeId b) => a.value != b.value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ShapeId other) => value == other.value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is ShapeId other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => value.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{nameof(ShapeId)}={value}";
    }
}
