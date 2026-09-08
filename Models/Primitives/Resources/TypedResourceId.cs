using System;
using System.Runtime.CompilerServices;
using BH.SDK.Models.Interfaces.Primitives;

namespace BH.SDK.Models.Primitives.Resources
{
    /// <summary>
    /// Category-agnostic resource id: the shared numeric convention (0 = null, positive = shipped
    /// with the game, negative = defined by this level) that every per-type id below reuses.
    /// Exists so code that doesn't care which kind of resource it holds - UGC metadata, validation -
    /// can still speak about one.
    /// </summary>
    [Serializable]
    public struct TypedResourceId : IEquatable<TypedResourceId>, IPrimitiveInt
    {
        /// <summary> The raw number; its sign alone decides game-defined vs. user-defined. </summary>
        public int value;
        int IPrimitiveInt.Value => value;

        /// <summary> Built from its value. </summary>
        public TypedResourceId(int value)
        {
            this.value = value;
        }
        /// <summary> Built from its resource id. </summary>
        public TypedResourceId(BytesResourceId bytesResourceId)
        {
            value = bytesResourceId.value;
        }
        /// <summary> Built from its resource id. </summary>
        public TypedResourceId(TextResourceId textResourceId)
        {
            value = textResourceId.value;
        }
        /// <summary> Built from its resource id. </summary>
        public TypedResourceId(TextureResourceId textureResourceId)
        {
            value = textureResourceId.value;
        }
        /// <summary> Built from its resource id. </summary>
        public TypedResourceId(AudioResourceId audioResourceId)
        {
            value = audioResourceId.value;
        }
        /// <summary> Built from its resource id. </summary>
        public TypedResourceId(FontResourceId fontResourceId)
        {
            value = fontResourceId.value;
        }
        /// <summary> Back to the values the constructor writes. </summary>
        public void Reset()
        {
            value = NullValue;
        }
        
        // ------------------------------------------------------------------------------------------------------------
        // TypedResourceId
        // ------------------------------------------------------------------------------------------------------------

        // TypedResourceId (AudioResourceId, TextureResourceId, ...) is a stable identifier for saving,
        // unique per resource type (audio, textures, ...). What each number means:
        // 0 => Null / not-set, reserved value, never a valid resource id
        // (1 - int.MaxValue) => game-space resources, defaults for whole game, stored in game, each resource has permanent id
        // (int.MinValue - -1) => user-space resources, unique for each level, stored externally: level folder, url...
        //
        // Each per-type struct below (AudioResourceId, TextureResourceId, ...) shares this exact
        // range semantics, just narrowed to its own resource type.
        
        /// <summary> The reserved "unset" number. Never a real id. </summary>
        public const int NullValue = 0;

        /// <summary> Lowest number the game's own shipped resources use. </summary>
        public const int MinGameDefinedValue = 1;

        /// <summary> Highest number a level's own resources use - the user range runs downwards from here. </summary>
        public const int MaxUserDefinedValue = -1;
        
        /// <summary> The unset id. </summary>
        public static readonly TypedResourceId Null = new(NullValue);

        /// <summary> The first id a game-shipped resource takes. </summary>
        public static readonly TypedResourceId MinGameDefined = new(MinGameDefinedValue);

        /// <summary> The highest id a level's own resource takes. </summary>
        public static readonly TypedResourceId MaxUserDefined = new(MaxUserDefinedValue);
        
        /// <summary> True when this is an id something can actually be found under. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid() => value != NullValue;
        
        /// <summary> True when this names a resource the game ships. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsGameDefined() => value >= MinGameDefinedValue;
        
        /// <summary> True when this names a resource the level carries. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUserDefined() => value <= MaxUserDefinedValue;
        
        /// <summary> The same test on a bare number. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(int value) => value != NullValue;
        
        /// <summary> The same test on a bare number. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGameDefined(int value) => value >= MinGameDefinedValue;
        
        /// <summary> The same test on a bare number. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUserDefined(int value) => value <= MaxUserDefinedValue;
        
        
        /// <summary> Value equality. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(TypedResourceId a, TypedResourceId b) => a.value == b.value;
        
        /// <summary> Its negation. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(TypedResourceId a, TypedResourceId b) => a.value != b.value;
        
        /// <summary> Member by member. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(TypedResourceId other) => value == other.value;
        
        /// <summary> The same, boxed. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is TypedResourceId other && Equals(other);

        /// <summary> Matches the equality above. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => value;

        /// <summary> One line, for a log. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{nameof(TypedResourceId)}={value}";
    }
}