using System;
using System.Runtime.CompilerServices;
using BH.SDK.Models.Interfaces.Primitives;

namespace BH.SDK.Models.Primitives.Resources
{
    /// <summary>
    /// Points at a typeface - a FontResource of Level.Resources.Fonts or a game-shipped one.
    /// Referenced by TextObject.
    /// </summary>
    [Serializable]
    public struct FontResourceId : IEquatable<FontResourceId>, IPrimitiveInt
    {
        /// <summary> The raw number, sharing TypedResourceId's sign convention. </summary>
        public int value;
        int IPrimitiveInt.Value => value;

        /// <summary> Built from its value. </summary>
        public FontResourceId(int value)
        {
            this.value = value;
        }

        /// <summary> Narrows a generic resource id to this kind. </summary>
        public FontResourceId(TypedResourceId typedResourceId)
        {
            value = typedResourceId.value;
        }
        /// <summary> Back to the values the constructor writes. </summary>
        public void Reset()
        {
            value = NullValue;
        }
        
        // Same range semantics as TypedResourceId, narrowed to font resources

        /// <summary> The reserved "unset" number. Never a real id. </summary>
        public const int NullValue = 0;

        /// <summary> Lowest number the game's own shipped resources use. </summary>
        public const int MinGameDefinedValue = 1;

        /// <summary> Highest number a level's own resources use - the user range runs downwards from here. </summary>
        public const int MaxUserDefinedValue = -1;

        /// <summary> The unset id. </summary>
        public static readonly FontResourceId Null = new(NullValue);

        /// <summary> The first id a game-shipped resource takes. </summary>
        public static readonly FontResourceId MinGameDefined = new(MinGameDefinedValue);

        /// <summary> The highest id a level's own resource takes. </summary>
        public static readonly FontResourceId MaxUserDefined = new(MaxUserDefinedValue);
        
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
        
        /// <summary> The font a text object uses when it names none. </summary>
        public static readonly FontResourceId Default = new(1);
        
        
        /// <summary> Value equality. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FontResourceId a, FontResourceId b) => a.value == b.value;
        
        /// <summary> Its negation. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FontResourceId a, FontResourceId b) => a.value != b.value;
        
        /// <summary> Member by member. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FontResourceId other) => value == other.value;
        
        /// <summary> The same, boxed. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is FontResourceId other && Equals(other);

        /// <summary> Matches the equality above. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => value;

        /// <summary> One line, for a log. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{nameof(FontResourceId)}={value}";
    }
}