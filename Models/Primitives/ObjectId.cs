using System;
using System.Runtime.CompilerServices;
using BH.SDK.Models.Interfaces.Primitives;

namespace BH.SDK.Models.Primitives
{
    /// <summary>
    /// Identity of a scene object inside one object scope (GameLevel.Objects or a Prefab's own
    /// Objects). A bare int wrapper whose sign carves the space into user objects, reserved game
    /// objects and runtime-only ids - see the range table below.
    /// </summary>
    [Serializable]
    public struct ObjectId : IEquatable<ObjectId>, IPrimitiveInt
    {
        /// <summary> The raw number. Public field, not a property, so jobs/structs can touch it
        /// directly; serialized as a bare scalar by PrimitiveIntConverter. </summary>
        public int value;
        int IPrimitiveInt.Value => value;

        /// <summary> Built from its value. </summary>
        public ObjectId(int value)
        {
            this.value = value;
        }
        /// <summary> Back to the values the constructor writes. </summary>
        public void Reset()
        {
            value = NullValue;
        }
        
        // ObjectId - stable identifier for saving.
        // Unique only inside each object scope. Can be referred (as pid) only in scope.
        // When PrefabObjects converts to regular Objects, they change all ids for each hierarchy level
        
        // What certain objectId's meaning
        // (1 - int.MaxValue) => user-space objects, valid for both ObjectId and ParentObjectId
        // 0 => undefined (for ObjectId) or null (for ParentObjectId), exists as a fallback value
        // All negative numbers (int.MinValue - -1) reserved for game-space objects
        
        // There are 3 types of objectIds: user objects, public game object and private game objects
        // user objects - objects created by users in levels, exists in this
        
        // public game objects - predefined game objects, stable and has permanent public objectId,
        // can be used by user objects (mostly as a parent), also they can use user game objects
        
        // private game objects - predefined game object, unstable and has changeable private objectId,
        // can not be used by user objects (throw error if you try), but they also can use user game objects
        
        /// <summary> The reserved "unset" number. Never a real id. </summary>
        public const int NullValue = 0;

        /// <summary> Lowest number a level's own objects use; everything above it is user space. </summary>
        public const int MinLevelValue = 1;

        /// <summary> Lowest number a PARENT reference may name - below a level object, since the reserved parents live there. </summary>
        public const int MinLevelParentValue = -3;

        /// <summary> Highest number reserved for runtime-only inframe objects. </summary>
        public const int MaxInframeValue = -4;

        /// <summary> The unset id. </summary>
        public static readonly ObjectId Null = new(NullValue);

        /// <summary> The first id a level's own objects take. </summary>
        public static readonly ObjectId MinLevel = new(MinLevelValue);

        /// <summary> The lowest id a parent reference may name. </summary>
        public static readonly ObjectId MinLevelParent = new(MinLevelParentValue);

        /// <summary> The highest id an inframe object takes. </summary>
        public static readonly ObjectId MaxInframe = new(MaxInframeValue);
        
        /// <summary> True when the id has been set at all. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNotNull() => value != NullValue;
        
        /// <summary> True when this is an id something can actually be found under. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid() => value >= MinLevelValue;
        
        /// <summary> True when this may be used as a parent, the reserved parents included. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValidParent() => value >= MinLevelParentValue;
        
        /// <summary> True when this names a runtime-only inframe object rather than authored content. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInframe() => value <= MaxInframeValue;
        
        /// <summary> The same test on a bare number. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotNull(int value) => value != NullValue;
        
        /// <summary> The same test on a bare number. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(int value) => value >= MinLevelValue;
        
        /// <summary> The same test on a bare number. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidParent(int value) => value >= MinLevelParentValue;
        
        /// <summary> The same test on a bare number. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInframe(int value) => value <= MaxInframeValue;
        
        // Public game objects (stable ids)
        
        // -1 => camera game object, exists only in player runtime (for ObjectId - error),
        // can be used as a parent with unique transform
        // Size is not regular (1f, 1f), but is "aspect >= 1f ? (10f * aspect, 10f) : (10f, 10f / aspect)"

        /// <summary> The camera, usable as a parent with a transform of its own. Runtime only. </summary>
        public static readonly ObjectId Camera = new(-1);
        
        // -2 => local player game object, can be used as a regular parent for user objects
        // In multiplayer each localPlayer works individually (no effect on other players)
        // Recommend: try not to use colliders for user objects parented from localPlayer

        /// <summary> The local player's avatar, usable as an ordinary parent. </summary>
        public static readonly ObjectId LocalPlayer = new(-2);

        // -3 => explicit "attach to this placement's own root" parent, only meaningful for an
        // object living inside a Prefab template (Level.Resources.Prefabs[x].Objects) - resolves to
        // the SAME id as an unset/Null ParentObjectId does there (see
        // BH.Core.Services.PrefabMaterializer.RemapParents), just spelled out explicitly instead of
        // relying on the "null auto-parents to the placement" fallback. Meaningless at level scope
        // (GameLevel.Objects has no "root" to attach to), and RuleParentObjectIdValid rejects it
        // there - just as it rejects Camera/LocalPlayer inside a prefab template, since those are
        // level-runtime objects a template's inner object cannot reach.

        /// <summary> "This placement's own root" - meaningful only inside a prefab template. </summary>
        public static readonly ObjectId PrefabRoot = new(-3);

        
        /// <summary> Value equality. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ObjectId a, ObjectId b) => a.value == b.value;
        
        /// <summary> Its negation. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ObjectId a, ObjectId b) => a.value != b.value;

        /// <summary> Member by member. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ObjectId other) => value == other.value;
        
        /// <summary> The same, boxed. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is ObjectId other && Equals(other);

        /// <summary> Matches the equality above. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => value;

        /// <summary> One line, for a log. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{nameof(ObjectId)}={value}";
    }
}