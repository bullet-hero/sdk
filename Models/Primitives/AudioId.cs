using System;
using System.Runtime.CompilerServices;
using BH.SDK.Models.Interfaces.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Models.Primitives
{
    /// <summary>
    /// Identity of one LevelTrack inside AudioLevel.Tracks. Positive-only: unlike every other int id
    /// here it has no game-defined negative half, since audio tracks only ever exist per level.
    /// </summary>
    [Serializable]
    public struct AudioId : IEquatable<AudioId>, IPrimitiveInt
    {
        /// <summary> The raw number, handed out by LevelSettings.GetNextAudioId. </summary>
        public int value;
        int IPrimitiveInt.Value => value;

        /// <summary> Built from its value. </summary>
        public AudioId(int value)
        {
            this.value = value;
        }
        /// <summary> Back to the values the constructor writes. </summary>
        public void Reset()
        {
            value = NullValue;
        }

        // Level-scoped like ObjectId, but with no game-defined negative range: negatives are banned and
        // 0 is Null. Docs/IDENTIFIERS.md

        /// <summary> The reserved "unset" number. Never a real id. </summary>
        public const int NullValue = 0;

        /// <summary> Lowest number that can be handed out. </summary>
        public const int MinValue = 1;

        /// <summary> True when this is an id something can actually be found under. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid() => value >= MinValue;

        /// <summary> The same test on a bare number. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(int value) => value >= MinValue;

        /// <summary> The unset id. </summary>
        public static readonly AudioId Null = new(NullValue);

        /// <summary> The lowest id that can be handed out. </summary>
        public static readonly AudioId Min = new(MinValue);

        
        /// <summary> Value equality. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(AudioId a, AudioId b) => a.value == b.value;
        
        /// <summary> Its negation. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(AudioId a, AudioId b) => a.value != b.value;

        /// <summary> Member by member. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AudioId other) => value == other.value;
        
        /// <summary> The same, boxed. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is AudioId other && Equals(other);

        /// <summary> Matches the equality above. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => value;

        /// <summary> One line, for a log. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{nameof(AudioId)}={value}";
    }
}