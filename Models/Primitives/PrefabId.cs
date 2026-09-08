using System;
using System.Runtime.CompilerServices;
using BH.SDK.Models.Interfaces.Primitives;

namespace BH.SDK.Models.Primitives
{
    /// <summary>
    /// Identity of a Prefab template in Level.Resources.Prefabs - the link a PrefabObject placement
    /// follows to know what to materialize, and what Resync walks back along when the template changes.
    /// </summary>
    public struct PrefabId : IEquatable<PrefabId>, IPrimitiveGuid
    {
        /// <summary> The raw Guid. </summary>
        public Guid value;
        Guid IPrimitiveGuid.Value => value;

        /// <summary> Built from its value. </summary>
        public PrefabId(Guid value)
        {
            this.value = value;
        }

        /// <summary> Parses the textual form. </summary>
        public PrefabId(string str)
        {
            value = new Guid(str);
        }
        /// <summary> Back to the values the constructor writes. </summary>
        public void Reset()
        {
            value = Guid.Empty;
        }

        // Prefab ids are a stable identifier for a Prefab entry (Level.Resources.Prefabs) - what
        // PrefabObject.PrefabId references back to. Unlike the int-based ids (AudioId,
        // TypedResourceId, ...) there is no game-defined/user-defined range split - prefabs are always
        // per-level authored data, and a Guid has no meaningful "positive/negative" ordering to
        // split on anyway. Guid.Empty is the only reserved/Null value.

        /// <summary> The reserved "unset" value. Never a real id. </summary>
        public static readonly Guid NullValue = Guid.Empty;
        
        /// <summary> The unset id. </summary>
        public static readonly PrefabId Null = new(NullValue);
        
        /// <summary> True when the id names something rather than being unset. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEnabled() => value != Guid.Empty;

        /// <summary> The same test on a bare guid. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnabled(Guid value) => value != Guid.Empty;

        /// <summary> A fresh id, unique for practical purposes. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrefabId NewId() => new(Guid.NewGuid());

        /// <summary> A fresh id. The guid spelling, kept beside NewId for callers that read better that way. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrefabId NewGuid() => new(Guid.NewGuid());

        
        /// <summary> Value equality. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(PrefabId a, PrefabId b) => a.value == b.value;

        /// <summary> Its negation. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(PrefabId a, PrefabId b) => a.value != b.value;

        /// <summary> Member by member. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(PrefabId other) => value == other.value;

        /// <summary> The same, boxed. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is PrefabId other && Equals(other);

        /// <summary> Matches the equality above. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => value.GetHashCode();

        /// <summary> One line, for a log. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{nameof(PrefabId)}={value}";
    }
}
