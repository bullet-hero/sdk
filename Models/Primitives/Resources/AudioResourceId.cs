using System;
using System.Runtime.CompilerServices;
using BH.SDK.Models.Interfaces.Primitives;

namespace BH.SDK.Models.Primitives.Resources
{
    /// <summary>
    /// Points at a music/sfx clip - an AudioResource of Level.Resources.Audios or a game-shipped one.
    /// Referenced by LevelTrack, not by any RectObject: audio is placed on the timeline, not in space.
    /// </summary>
    [Serializable]
    public struct AudioResourceId : IEquatable<AudioResourceId>, IPrimitiveInt
    {
        /// <summary> The raw number, sharing TypedResourceId's sign convention. </summary>
        public int value;
        int IPrimitiveInt.Value => value;

        /// <summary> Built from its value. </summary>
        public AudioResourceId(int value)
        {
            this.value = value;
        }

        /// <summary> Narrows a generic resource id to this kind. </summary>
        public AudioResourceId(TypedResourceId typedResourceId)
        {
            value = typedResourceId.value;
        }
        /// <summary> Back to the values the constructor writes. </summary>
        public void Reset()
        {
            value = NullValue;
        }
        
        // Same range semantics as TypedResourceId, narrowed to audio resources

        /// <summary> The reserved "unset" number. Never a real id. </summary>
        public const int NullValue = 0;

        /// <summary> Lowest number the game's own shipped resources use. </summary>
        public const int MinGameDefinedValue = 1;

        /// <summary> Highest number a level's own resources use - the user range runs downwards from here. </summary>
        public const int MaxUserDefinedValue = -1;

        /// <summary> The unset id. </summary>
        public static readonly AudioResourceId Null = new(NullValue);

        /// <summary> The first id a game-shipped resource takes. </summary>
        public static readonly AudioResourceId MinGameDefined = new(MinGameDefinedValue);

        /// <summary> The highest id a level's own resource takes. </summary>
        public static readonly AudioResourceId MaxUserDefined = new(MaxUserDefinedValue);
        
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
        public static bool operator ==(AudioResourceId a, AudioResourceId b) => a.value == b.value;
        
        /// <summary> Its negation. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(AudioResourceId a, AudioResourceId b) => a.value != b.value;
        
        /// <summary> Member by member. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AudioResourceId other) => value == other.value;
        
        /// <summary> The same, boxed. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is AudioResourceId other && Equals(other);

        /// <summary> Matches the equality above. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => value;

        /// <summary> One line, for a log. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{nameof(AudioResourceId)}={value}";
    }
}