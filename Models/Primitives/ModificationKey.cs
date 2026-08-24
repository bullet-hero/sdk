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

        public ModificationKey(ObjectId objectId, string path)
        {
            ObjectId = objectId;
            Path = path;
        }

        public void Reset()
        {
            ObjectId = ObjectId.Null;
            Path = string.Empty;
        }

        public readonly object Clone() => Copy();
        public readonly ModificationKey Copy() => new(ObjectId, Path);

        public void Update(ModificationKey src)
        {
            ObjectId = src.ObjectId;
            Path = src.Path;
        }

        public void Pull(ModificationKey src)
        {
            ObjectId = src.ObjectId;
            Path = src.Path;
        }

        public readonly bool Equals(ModificationKey other) => ObjectId.Equals(other.ObjectId) && Path == other.Path;
        public readonly override bool Equals(object obj) => obj is ModificationKey other && Equals(other);
        public readonly override int GetHashCode() => HashCode.Combine(ObjectId, Path);

        public readonly int CompareTo(ModificationKey other)
        {
            var cmp = ObjectId.value.CompareTo(other.ObjectId.value);
            return cmp != 0 ? cmp : string.CompareOrdinal(Path, other.Path);
        }

        public readonly override string ToString() => $"{ObjectId}/{Path}";
    }
}
