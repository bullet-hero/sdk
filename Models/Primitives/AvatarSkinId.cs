using System;
using System.Runtime.CompilerServices;
using BH.SDK.Models.Interfaces.Primitives;

namespace BH.SDK.Models.Primitives
{
    // Built-in skins take version-0 guids, which Guid.NewGuid (always version 4) never hands out, so a
    // built-in id and a player's skin can never collide.

    /// <summary> Identity of an avatar skin, carried inside the skin and naming its file
    /// (<c>skins/&lt;id&gt;.json</c>). </summary>
    public struct AvatarSkinId : IEquatable<AvatarSkinId>, IPrimitiveGuid
    {
        /// <summary> The raw Guid. </summary>
        public Guid value;
        Guid IPrimitiveGuid.Value => value;

        /// <summary> Built from its value. </summary>
        public AvatarSkinId(Guid value)
        {
            this.value = value;
        }

        /// <summary> Parses the textual form. </summary>
        public AvatarSkinId(string str)
        {
            value = new Guid(str);
        }

        /// <summary> Back to the values the constructor writes. </summary>
        public void Reset()
        {
            value = Guid.Empty;
        }

        /// <summary> The reserved "unset" value. Never a real id. </summary>
        public static readonly Guid NullValue = Guid.Empty;

        /// <summary> The unset id. </summary>
        public static readonly AvatarSkinId Null = new(NullValue);

        /// <summary> The classic avatar, which needs no file. </summary>
        public static readonly AvatarSkinId Classic = new(new Guid("00000000-0000-0000-0000-000000000001"));

        /// <summary> True when the id names a skin rather than being unset. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEnabled() => value != Guid.Empty;

        /// <summary> True for a skin the game carries without a file. </summary>
        public bool IsBuiltin() => value != Guid.Empty && (value.ToByteArray()[7] & 0xF0) == 0;

        /// <summary> A fresh id, unique for practical purposes. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AvatarSkinId NewId() => new(Guid.NewGuid());

        /// <summary> Value equality. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(AvatarSkinId a, AvatarSkinId b) => a.value == b.value;

        /// <summary> Its negation. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(AvatarSkinId a, AvatarSkinId b) => a.value != b.value;

        /// <summary> Member by member. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AvatarSkinId other) => value == other.value;

        /// <summary> The same, boxed. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is AvatarSkinId other && Equals(other);

        /// <summary> Matches the equality above. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => value.GetHashCode();

        /// <summary> One line, for a log. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{nameof(AvatarSkinId)}={value}";
    }
}
