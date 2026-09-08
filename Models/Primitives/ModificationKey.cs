using System;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;

namespace BH.SDK.Models.Primitives
{
    /// <summary>
    /// Address of a single overridable field inside a prefab placement: which object, which field.
    /// Used as the key of PrefabObject.Modifications, which is what makes "one override per
    /// (object, field) pair" a structural guarantee instead of a rule to enforce.
    /// </summary>
    public struct ModificationKey : IModel<ModificationKey>, IComparable<ModificationKey>
    {
        /// <summary> Object being overridden, addressed by its id inside the TEMPLATE, not the
        /// materialized outer id - so the key survives re-materialization. </summary>
        [JsonProperty(Names.ObjectId)]
        public ObjectId ObjectId { get; set; }

        /// <summary> Dotted/indexed field path ("pos[0].v"), resolved by ModificationService through
        /// each property's JsonProperty name. </summary>
        [JsonProperty(Names.PathShort)]
        public string Path { get; set; }

        /// <summary> The only way to build one - both halves are get-only afterwards. </summary>
        public ModificationKey(ObjectId objectId, string path)
        {
            ObjectId = objectId;
            Path = path;
        }

        /// <summary> Back to the values the constructor writes. </summary>
        public void Reset()
        {
            ObjectId = ObjectId.Null;
            Path = string.Empty;
        }

        /// <summary> The untyped spelling of <c>Copy</c>. </summary>
        public readonly object Clone() => Copy();
        /// <summary> A deep copy, sharing nothing mutable with this one. </summary>
        public readonly ModificationKey Copy() => new(ObjectId, Path);

        /// <summary> Becomes the source, replacing everything this instance held. </summary>
        public void Update(ModificationKey src)
        {
            ObjectId = src.ObjectId;
            Path = src.Path;
        }

        /// <summary> Takes the source's contents in place, so nothing pointing inside this instance is invalidated. </summary>
        public void Pull(ModificationKey src)
        {
            ObjectId = src.ObjectId;
            Path = src.Path;
        }

        /// <summary> Member by member. </summary>
        public readonly bool Equals(ModificationKey other) => ObjectId.Equals(other.ObjectId) && Path == other.Path;
        /// <summary> The same, boxed. </summary>
        public readonly override bool Equals(object obj) => obj is ModificationKey other && Equals(other);
        /// <summary> Matches the equality above. </summary>
        public readonly override int GetHashCode() => HashCode.Combine(ObjectId, Path);

        /// <summary> Ordered by object, then by path, so a placement's overrides list stably. </summary>
        public readonly int CompareTo(ModificationKey other)
        {
            var cmp = ObjectId.value.CompareTo(other.ObjectId.value);
            return cmp != 0 ? cmp : string.CompareOrdinal(Path, other.Path);
        }

        /// <summary> One line, for a log. </summary>
        public readonly override string ToString() => $"{ObjectId}/{Path}";
    }
}
